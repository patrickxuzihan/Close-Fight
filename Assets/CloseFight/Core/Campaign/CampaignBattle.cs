using System;
using System.Collections.Generic;
using System.Linq;
using CloseFight.Core.Battle;
using CloseFight.Core.Geometry;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// The ground a district's battle is fought on (M16 step 3): its map, where the attacker and
    /// the defender deploy, which way each falls back, and the objectives. Hand-made battlefields
    /// come from a scenario file (the station); the others are generated (M16 step 4).
    /// </summary>
    public sealed class Battlefield
    {
        public string mapId = "";
        public List<MapPoint> attackerZone = new List<MapPoint>();
        public List<MapPoint> defenderZone = new List<MapPoint>();
        public MapEdge attackerRetreat = MapEdge.West;
        public MapEdge defenderRetreat = MapEdge.East;
        /// <summary>Their owner is set to the defender when the battle is built.</summary>
        public List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();
        /// <summary>Companies a side can bring onto this map: a small map holds less than a regiment's two thirds.</summary>
        public int capacity = 3;

        /// <summary>The battlefield of a hand-made scenario: its attacker's zone and retreat edge become the attacker's.</summary>
        public static Battlefield FromScenario(ScenarioDefinition scenario, int capacity)
        {
            var attacker = scenario.sides.Find(s => s.side == scenario.attacker);
            var defender = scenario.sides.Find(s => s.side != scenario.attacker);
            return new Battlefield
            {
                mapId = scenario.mapId,
                attackerZone = new List<MapPoint>(attacker.deployZone),
                defenderZone = new List<MapPoint>(defender.deployZone),
                attackerRetreat = attacker.retreatEdge,
                defenderRetreat = defender.retreatEdge,
                objectives = scenario.objectives.Select(o => new ObjectiveDefinition
                {
                    id = o.id, nameKey = o.nameKey, position = o.position, radius = o.radius, value = o.value, owner = o.owner,
                }).ToList(),
                capacity = capacity,
            };
        }
    }

    /// <summary>The companies each regiment sends into a battle (D21), by regiment id.</summary>
    public sealed class BattleForces
    {
        public Dictionary<string, List<string>> companies = new Dictionary<string, List<string>>();

        public IEnumerable<(RegimentState regiment, CompanyRecord company)> Resolve(CampaignState state)
        {
            foreach (var pair in companies)
            {
                var regiment = state.Regiment(pair.Key);
                if (regiment == null)
                {
                    continue;
                }

                foreach (var id in pair.Value)
                {
                    var company = regiment.companies.Find(c => c.id == id);
                    if (company != null)
                    {
                        yield return (regiment, company);
                    }
                }
            }
        }

        /// <summary>Most companies of one regiment allowed into a battle (D21: two thirds, rounded down, at least one).</summary>
        public static int MaxFrom(RegimentState regiment) =>
            Math.Max(1, (int)Math.Floor(regiment.companies.Count(c => c.Teams.Any(t => t.CanFight)) * CampaignRules.MaxCompanyShareInBattle));

        /// <summary>
        /// A sensible pick for a side: up to <paramref name="capacity"/> companies over its regiments,
        /// no more than two thirds of any one; a staff and a rifle company first, then the heavy
        /// weapons the role needs, the strongest first. The AI uses it, and it is the player's default.
        /// </summary>
        public static BattleForces Default(CampaignState state, PendingBattle battle, Side side, int capacity)
        {
            var forces = new BattleForces();
            bool attacking = side == battle.attacker;
            var regiments = (attacking ? battle.attackers : battle.defenders).Select(state.Regiment).Where(r => r != null).ToList();
            var taken = regiments.ToDictionary(r => r.id, r => 0);
            var candidates = regiments
                .SelectMany(r => r.companies.Where(c => c.Teams.Any(t => t.CanFight)).Select(c => (regiment: r, company: c)))
                .OrderBy(x => Priority(x.company.template, attacking))
                .ThenByDescending(x => x.company.PresentCount)
                .ToList();

            int count = 0;
            var used = new HashSet<string>();
            // Two passes: first one company of each kind in priority order, then the rest by priority.
            for (int pass = 0; pass < 2 && count < capacity; pass++)
            {
                var kinds = new HashSet<string>();
                foreach (var (regiment, company) in candidates)
                {
                    if (count >= capacity)
                    {
                        break;
                    }

                    if (used.Contains(company.id) || taken[regiment.id] >= MaxFrom(regiment) || (pass == 0 && !kinds.Add(Kind(company.template))))
                    {
                        continue;
                    }

                    if (!forces.companies.TryGetValue(regiment.id, out var list))
                    {
                        list = new List<string>();
                        forces.companies[regiment.id] = list;
                    }

                    list.Add(company.id);
                    used.Add(company.id);
                    taken[regiment.id]++;
                    count++;
                }
            }

            return forces;
        }

        static string Kind(string template) => template.Substring(3);

        static int Priority(string template, bool attacking)
        {
            string kind = Kind(template);
            string[] order = attacking
                ? new[] { "rifle", "staff", "pioneer", "sapper", "stug", "panzer", "tank", "light_tank", "mg", "mortar", "antitank" }
                : new[] { "rifle", "staff", "mg", "antitank", "mortar", "sapper", "pioneer", "tank", "light_tank", "stug", "panzer" };
            int index = Array.IndexOf(order, kind);
            return index < 0 ? order.Length : index;
        }
    }

    /// <summary>
    /// A battle of the campaign built from its rolls (M16 step 3): the companies chosen go in with
    /// their own named men, their regiment's ammunition and morale; when it is over,
    /// <see cref="Finish"/> writes every man's fate, kills and experience back into the rolls and
    /// returns the outcome for <see cref="CampaignState.ApplyBattle"/>.
    /// </summary>
    public sealed class CampaignBattle
    {
        /// <summary>Most off-map support points a side draws from its stock for one battle (D36).</summary>
        public const int MaxSupportPerBattle = 20;
        /// <summary>Parachute flares each side carries into a night battle (D35).</summary>
        public const int NightFlares = 4;

        static readonly string[] GermanSupport = { "lefh18", "sfh18", "stuka" };
        static readonly string[] SovietSupport = { "ml20", "katyusha", "po2" };

        sealed class Link
        {
            public Soldier Soldier;
            public SoldierRecord Record;
            public RegimentState Regiment;
            public int StartAmmo;
            public int FullAmmo;
        }

        readonly CampaignState _state;
        readonly List<Link> _links = new List<Link>();
        readonly List<(Team team, TeamRecord record)> _teams = new List<(Team, TeamRecord)>();
        readonly Dictionary<RegimentState, int> _menInBattle = new Dictionary<RegimentState, int>();
        readonly Dictionary<RegimentState, int> _strengthBefore = new Dictionary<RegimentState, int>();

        public PendingBattle Pending { get; }
        public BattleState Battle { get; }
        public bool Finished { get; private set; }

        CampaignBattle(CampaignState state, PendingBattle pending, BattleState battle)
        {
            _state = state;
            Pending = pending;
            Battle = battle;
        }

        /// <summary>
        /// Builds the battle on <paramref name="map"/>. With <paramref name="playerSide"/> null both
        /// sides are under the AI (auto-resolve, D59); otherwise the other side is. With
        /// <paramref name="deployment"/>, it opens in the deployment phase (the AI places its teams at once).
        /// </summary>
        public static CampaignBattle Create(CampaignState state, PendingBattle pending, Battlefield field, TacticalMap map,
            BattleForces german, BattleForces soviet, Side? playerSide, bool deployment = true)
        {
            var battle = new BattleState(map, state.definition.seed + (ulong)state.turn * 7919UL + CampaignHash.Of(pending.district))
            {
                IsNight = pending.night,
            };
            var result = new CampaignBattle(state, pending, battle);
            DistrictDamage.Apply(map, state.District(pending.district));
            var attacker = pending.attacker;
            var defender = pending.Defender;

            battle.SetRetreatEdge(attacker, field.attackerRetreat);
            battle.SetRetreatEdge(defender, field.defenderRetreat);
            battle.AddDeployZone(attacker, field.attackerZone.ConvertAll(p => (float2)p));
            battle.AddDeployZone(defender, field.defenderZone.ConvertAll(p => (float2)p));
            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                battle.Support.Configure(side, state.TakeSupport(side, MaxSupportPerBattle), side == Side.German ? GermanSupport : SovietSupport);
                battle.Illumination.SetFlares(side, pending.night ? NightFlares : 0);
            }

            result.AddSide(Side.German, german, attacker == Side.German ? field.attackerZone : field.defenderZone,
                attacker == Side.German ? field.defenderZone : field.attackerZone, map);
            result.AddSide(Side.Soviet, soviet, attacker == Side.Soviet ? field.attackerZone : field.defenderZone,
                attacker == Side.Soviet ? field.defenderZone : field.attackerZone, map);

            foreach (var objective in field.objectives)
            {
                battle.AddObjective(new ObjectiveDefinition
                {
                    id = objective.id, nameKey = objective.nameKey, position = objective.position, radius = objective.radius,
                    value = objective.value, owner = defender,
                });
            }

            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                if (side != playerSide && battle.Teams.Any(t => t.Side == side))
                {
                    battle.AddCommander(side == attacker ? new AI.AttackCommander(battle, side) : (IBattleCommander)new AI.DefenseCommander(battle, side));
                }
            }

            if (deployment)
            {
                battle.BeginDeployment();
            }

            battle.Visibility.Recompute();
            return result;
        }

        void AddSide(Side side, BattleForces forces, List<MapPoint> zone, List<MapPoint> enemyZone, TacticalMap map)
        {
            var zonePoints = zone.ConvertAll(p => (float2)p);
            float2 enemy = Centre(enemyZone);
            float facing = Soldier.HeadingOf(enemy - Centre(zone));
            var picked = forces.Resolve(_state).Where(x => x.regiment.side == side).ToList();
            var teams = picked.SelectMany(x => x.company.platoons.SelectMany(p => p.teams.Where(t => t.CanFight))).ToList();
            var spots = Spots(zonePoints, map, teams.Count, enemy);
            int spot = 0;
            foreach (var (regiment, company) in picked)
            {
                if (!_strengthBefore.ContainsKey(regiment))
                {
                    _strengthBefore[regiment] = regiment.PresentCount;
                }

                foreach (var platoon in company.platoons)
                {
                    var fighting = platoon.teams.Where(t => t.CanFight).ToList();
                    if (fighting.Count == 0)
                    {
                        continue;
                    }

                    var battlePlatoon = Battle.AddPlatoon(platoon.key, side, platoon.type, platoon.name, company.name);
                    int number = 0;
                    foreach (var record in fighting)
                    {
                        number++;
                        var heavy = record.type == TeamType.Vehicle || record.type == TeamType.AntiTankGun || record.type == TeamType.Mortar;
                        var position = heavy ? spots.Clear[spot % spots.Clear.Count] : spots.All[spot % spots.All.Count];
                        spot++;
                        var present = record.soldiers.Where(s => s.IsPresent).OrderBy(s => s.seat).ToList();
                        var men = present.Select(s => new SoldierSpec
                        {
                            Seat = s.seat, FirstName = s.first, LastName = s.last, RankKey = s.rank, Attributes = s.attributes,
                            Morale = math.clamp(s.attributes.Morale * (0.5f + regiment.morale / 140f), 5f, 100f),
                            AmmoShare = regiment.ammo,
                        }).ToList();
                        var team = Battle.AddTeam(record.key, side, record.type, TeamName(side, record, number), position, facing, men, record.equipment);
                        Battle.AssignToPlatoon(team, battlePlatoon);
                        _teams.Add((team, record));
                        for (int i = 0; i < present.Count; i++)
                        {
                            var soldier = team.Members[i];
                            int full = soldier.Weapon != null ? soldier.Weapon.StartingAmmo : 0;
                            _links.Add(new Link { Soldier = soldier, Record = present[i], Regiment = regiment, StartAmmo = soldier.Ammo, FullAmmo = full });
                        }

                        _menInBattle[regiment] = (_menInBattle.TryGetValue(regiment, out var n) ? n : 0) + present.Count;
                    }
                }
            }
        }

        /// <summary>Places in the zone on a grid, nearest the enemy first: all of them, and those clear of roofs (for vehicles and guns).</summary>
        static (List<float2> All, List<float2> Clear) Spots(List<float2> zone, TacticalMap map, int wanted, float2 enemy)
        {
            float minX = zone.Min(p => p.x), maxX = zone.Max(p => p.x), minY = zone.Min(p => p.y), maxY = zone.Max(p => p.y);
            float area = math.max(1f, (maxX - minX) * (maxY - minY));
            float step = math.clamp(math.sqrt(area / math.max(1, wanted * 2)), 6f, 30f);
            var any = new List<float2>();
            var clear = new List<float2>();
            for (float y = minY + step / 2; y < maxY; y += step)
            {
                for (float x = minX + step / 2; x < maxX; x += step)
                {
                    var p = new float2(x, y);
                    if (!PolygonMath.Contains(zone, p) || Blocked(map, p))
                    {
                        continue;
                    }

                    any.Add(p);
                    if (ClearOfRoofs(map, p, 4f))
                    {
                        clear.Add(p);
                    }
                }
            }

            if (any.Count == 0)
            {
                any.Add(Centre(zone));
            }

            if (clear.Count == 0)
            {
                clear.AddRange(any);
            }

            // Spread them out rather than packing the front edge: alternate near and far rows.
            any = any.OrderBy(p => math.distance(p, enemy)).ToList();
            clear = clear.OrderBy(p => math.distance(p, enemy)).ToList();
            return (Spread(any, wanted), Spread(clear, wanted));
        }

        static List<float2> Spread(List<float2> spots, int wanted)
        {
            if (spots.Count <= wanted || wanted <= 0)
            {
                return spots;
            }

            var spread = new List<float2>(wanted);
            float stride = spots.Count / (float)wanted;
            for (int i = 0; i < wanted; i++)
            {
                spread.Add(spots[(int)(i * stride)]);
            }

            return spread;
        }

        /// <summary>Water, walls and props: nobody can stand there.</summary>
        static bool Blocked(TacticalMap map, float2 p)
        {
            var cell = map.Grid.WorldToCell(p);
            return !map.Grid.InBounds(cell) || map.Grid.HasFlag(map.Grid.IndexOf(cell), CellFlags.BlocksMovement) && map.BuildingAt(map.Grid.IndexOf(cell)) == null;
        }

        static bool ClearOfRoofs(TacticalMap map, float2 p, float radius)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = map.Grid.WorldToCell(p + new float2(dx, dy) * radius);
                    if (!map.Grid.InBounds(cell) || map.BuildingAt(map.Grid.IndexOf(cell)) != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static float2 Centre(List<MapPoint> polygon)
        {
            float2 sum = float2.zero;
            foreach (var p in polygon)
            {
                sum += (float2)p;
            }

            return polygon.Count > 0 ? sum / polygon.Count : sum;
        }

        static float2 Centre(List<float2> polygon)
        {
            float2 sum = float2.zero;
            foreach (var p in polygon)
            {
                sum += p;
            }

            return polygon.Count > 0 ? sum / polygon.Count : sum;
        }

        /// <summary>A team's name in its platoon, in the style of the scenarios ("2. Gruppe", "3-ye otdeleniye").</summary>
        static string TeamName(Side side, TeamRecord team, int number)
        {
            if (!string.IsNullOrEmpty(team.name))
            {
                return team.name;
            }

            bool de = side == Side.German;
            switch (team.type)
            {
                case TeamType.Rifle: return de ? $"{number}. Gruppe" : $"{number}-ye otdeleniye";
                case TeamType.MachineGun: return de ? $"{number}. sMG-Gruppe" : $"Maksim {number}";
                case TeamType.Headquarters: return de ? "Trupp" : "Upravleniye";
                case TeamType.Sniper: return de ? "Scharfschützen" : "Snaypery";
                case TeamType.Observer: return de ? $"Artilleriebeobachter {number}" : $"Artnablyudateli {number}";
                case TeamType.Engineer: return de ? $"{number}. Pioniergruppe" : $"Sapyory {number}";
                case TeamType.Mortar: return de ? $"Granatwerfer {number}" : $"Minomyot {number}";
                case TeamType.AntiTankGun: return de ? $"PaK {number}" : $"Sorokapyatka {number}";
                case TeamType.Vehicle:
                    switch (team.equipment)
                    {
                        case "stug3f": return $"StuG {number}";
                        case "pz3j": return $"Panzer {number}";
                        case "t34_76": return $"T-34 {number}";
                        case "t60": return $"T-60 {number}";
                        default: return $"{team.equipment} {number}";
                    }
                default: return $"{number}";
            }
        }

        /// <summary>
        /// Writes the battle back into the rolls (D1, D30, D39): each man's fate (killed, wounded,
        /// evacuated, or back from a rout), his kills and the experience he gained; lost vehicles
        /// and guns; and for each regiment the ammunition fired and the morale lost. Returns the
        /// outcome for <see cref="CampaignState.ApplyBattle"/>.
        /// </summary>
        public BattleOutcome Finish()
        {
            if (Finished)
            {
                throw new InvalidOperationException("This battle has already been written back.");
            }

            Finished = true;
            DistrictDamage.Capture(Battle.Map, _state.District(Pending.district));
            var outcome = new BattleOutcome
            {
                winner = Battle.Victory.Result == CloseFight.Core.Battle.BattleResult.GermanVictory ? Side.German
                    : Battle.Victory.Result == CloseFight.Core.Battle.BattleResult.SovietVictory ? Side.Soviet
                    : (Side?)null,
            };

            var fired = new Dictionary<RegimentState, float>();
            var lost = new Dictionary<RegimentState, int>();
            foreach (var link in _links)
            {
                var soldier = link.Soldier;
                var record = link.Record;
                record.status = soldier.Status;
                record.battles++;
                record.kills += soldier.Kills;
                if (soldier.IsActive)
                {
                    int gain = 3 + math.min(5, soldier.Kills);
                    record.attributes.Experience = (byte)math.min(100, record.attributes.Experience + gain);
                }
                else
                {
                    lost[link.Regiment] = (lost.TryGetValue(link.Regiment, out var n) ? n : 0) + 1;
                }

                if (link.FullAmmo > 0)
                {
                    float share = math.saturate((link.StartAmmo - soldier.Ammo) / (float)link.FullAmmo);
                    fired[link.Regiment] = (fired.TryGetValue(link.Regiment, out var f) ? f : 0f) + share;
                }
            }

            foreach (var (team, record) in _teams)
            {
                if ((team.Vehicle != null && team.Vehicle.Destroyed) || (team.Vehicle == null && team.Gun != null && team.Gun.Destroyed))
                {
                    record.equipmentLost = true;
                }
            }

            foreach (var pair in _menInBattle)
            {
                var regiment = pair.Key;
                int total = math.max(1, _strengthBefore[regiment]);
                float present = pair.Value / (float)total;
                float meanFired = fired.TryGetValue(regiment, out var f) ? f / math.max(1, pair.Value) : 0f;
                outcome.ammoUsed[regiment.id] = meanFired * present;
                float casualties = (lost.TryGetValue(regiment, out var l) ? l : 0) / (float)math.max(1, pair.Value);
                bool won = outcome.winner == regiment.side;
                bool beaten = outcome.winner.HasValue && !won;
                outcome.moraleLost[regiment.id] = math.max(0f, (6f + 40f * casualties * present + (beaten ? 8f : 0f) - (won ? 4f : 0f)));
            }

            return outcome;
        }
    }
}
