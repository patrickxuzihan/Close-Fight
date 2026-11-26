using System;
using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// The companies a regiment is made of (M16), after the 1942 tables of organisation
    /// as the battle uses them (M18): a German Schützenkompanie of a Kompanietrupp and
    /// three Züge of three Gruppen, a Soviet rifle company of three vzvods of three
    /// sections, and the heavy weapons companies around them. Platoons are kept at three
    /// squads, as the battle-worn companies at Stalingrad were; <c>strength</c> thins
    /// them further (D13).
    /// </summary>
    public static class CompanyTemplates
    {
        sealed class TeamSlot
        {
            public TeamType Type;
            public string Equipment;
            public int Count;
        }

        sealed class PlatoonSlot
        {
            public PlatoonType Type;
            /// <summary>Designation; "{n}" is replaced by the platoon's number for numbered platoons.</summary>
            public string Name;
            public int Count = 1;
            public TeamSlot[] Teams;
        }

        static TeamSlot T(TeamType type, int count = 1, string equipment = "") => new TeamSlot { Type = type, Count = count, Equipment = equipment };

        static readonly Dictionary<string, PlatoonSlot[]> Templates = new Dictionary<string, PlatoonSlot[]>(StringComparer.Ordinal)
        {
            // German
            ["de_staff"] = new[] { new PlatoonSlot { Type = PlatoonType.Headquarters, Name = "Btl.-Stab", Teams = new[] { T(TeamType.Headquarters), T(TeamType.Observer, 2) } } },
            ["de_rifle"] = new[]
            {
                new PlatoonSlot { Type = PlatoonType.Headquarters, Name = "Kompanietrupp", Teams = new[] { T(TeamType.Headquarters), T(TeamType.Sniper) } },
                new PlatoonSlot { Type = PlatoonType.Rifle, Name = "{n}. Zug", Count = 3, Teams = new[] { T(TeamType.Rifle, 3) } },
            },
            ["de_mg"] = new[]
            {
                new PlatoonSlot { Type = PlatoonType.MachineGun, Name = "sMG-Zug", Teams = new[] { T(TeamType.MachineGun, 4) } },
                new PlatoonSlot { Type = PlatoonType.Mortar, Name = "Granatwerfer-Zug", Teams = new[] { T(TeamType.Mortar, 3, "grw34") } },
            },
            ["de_antitank"] = new[] { new PlatoonSlot { Type = PlatoonType.AntiTank, Name = "Panzerjäger-Zug", Teams = new[] { T(TeamType.AntiTankGun, 3, "pak38") } } },
            ["de_stug"] = new[] { new PlatoonSlot { Type = PlatoonType.Armor, Name = "Sturmgeschütz-Zug", Teams = new[] { T(TeamType.Vehicle, 3, "stug3f") } } },
            ["de_panzer"] = new[] { new PlatoonSlot { Type = PlatoonType.Armor, Name = "Panzer-Zug", Teams = new[] { T(TeamType.Vehicle, 3, "pz3j") } } },
            ["de_pioneer"] = new[] { new PlatoonSlot { Type = PlatoonType.Pioneer, Name = "{n}. Pionier-Zug", Count = 2, Teams = new[] { T(TeamType.Engineer, 3) } } },

            // Soviet
            ["su_staff"] = new[] { new PlatoonSlot { Type = PlatoonType.Headquarters, Name = "Shtab bataliona", Teams = new[] { T(TeamType.Headquarters), T(TeamType.Observer, 2) } } },
            ["su_rifle"] = new[]
            {
                new PlatoonSlot { Type = PlatoonType.Headquarters, Name = "Upravleniye roty", Teams = new[] { T(TeamType.Headquarters), T(TeamType.Sniper) } },
                new PlatoonSlot { Type = PlatoonType.Rifle, Name = "{n}-y vzvod", Count = 3, Teams = new[] { T(TeamType.Rifle, 3) } },
            },
            ["su_mg"] = new[] { new PlatoonSlot { Type = PlatoonType.MachineGun, Name = "Pulemyotny vzvod", Teams = new[] { T(TeamType.MachineGun, 4) } } },
            ["su_mortar"] = new[] { new PlatoonSlot { Type = PlatoonType.Mortar, Name = "Minomyotny vzvod", Teams = new[] { T(TeamType.Mortar, 3, "bm37") } } },
            ["su_antitank"] = new[] { new PlatoonSlot { Type = PlatoonType.AntiTank, Name = "Vzvod PTO", Teams = new[] { T(TeamType.AntiTankGun, 2, "m1937_45") } } },
            ["su_tank"] = new[] { new PlatoonSlot { Type = PlatoonType.Armor, Name = "Tankovy vzvod", Teams = new[] { T(TeamType.Vehicle, 3, "t34_76") } } },
            ["su_light_tank"] = new[] { new PlatoonSlot { Type = PlatoonType.Armor, Name = "Vzvod lyogkikh tankov", Teams = new[] { T(TeamType.Vehicle, 3, "t60") } } },
            ["su_sapper"] = new[] { new PlatoonSlot { Type = PlatoonType.Pioneer, Name = "{n}-y sapyorny vzvod", Count = 2, Teams = new[] { T(TeamType.Engineer, 3) } } },
        };

        public static bool Exists(string template) => !string.IsNullOrEmpty(template) && Templates.ContainsKey(template);

        public static IEnumerable<string> All => Templates.Keys;

        /// <summary>The side a template belongs to, from its prefix.</summary>
        public static Side SideOf(string template) => template.StartsWith("su_", StringComparison.Ordinal) ? Side.Soviet : Side.German;

        /// <summary>Men in a full team of this kind: the battle's table size, or the crew of its vehicle or gun.</summary>
        public static int TableSize(TeamType type, Side side, string equipment)
        {
            if (type == TeamType.Vehicle)
            {
                return Equipment.Vehicle(equipment).Crew.Length;
            }

            if (type == TeamType.AntiTankGun || type == TeamType.Mortar)
            {
                return Equipment.GunTeam(equipment).CrewSize;
            }

            return BattleState.DefaultSize(type, side);
        }

        public static int TableSize(TeamRecord team) => TableSize(team.type, SideOfTeam(team), team.equipment);

        static Side SideOfTeam(TeamRecord team) => team.key.StartsWith("su", StringComparison.Ordinal) ? Side.Soviet : Side.German;

        /// <summary>
        /// Builds a company from its template with named men (D30, D46). <paramref name="strength"/>
        /// (0..1) leaves seats empty at random, as in the worn-down units of September 1942 (D13);
        /// team leaders and vehicle and gun crews are always present.
        /// </summary>
        public static CompanyRecord Build(string template, string id, string name, int experience, float strength, SimRandom random,
            ref int nextSoldierId)
        {
            if (!Templates.TryGetValue(template, out var platoons))
            {
                throw new ArgumentException($"Unknown company template '{template}'.");
            }

            var side = SideOf(template);
            string prefix = side == Side.German ? "de" : "su";
            var company = new CompanyRecord { id = id, name = name, template = template };
            int platoonIndex = 0;
            foreach (var slot in platoons)
            {
                for (int n = 1; n <= slot.Count; n++)
                {
                    var platoon = new PlatoonRecord
                    {
                        key = $"{prefix}_{id}_p{platoonIndex++}",
                        type = slot.Type,
                        name = slot.Name.Replace("{n}", n.ToString()),
                    };
                    int teamIndex = 0;
                    foreach (var teamSlot in slot.Teams)
                    {
                        for (int t = 0; t < teamSlot.Count; t++)
                        {
                            var team = new TeamRecord
                            {
                                key = $"{platoon.key}_t{teamIndex++}",
                                type = teamSlot.Type,
                                equipment = teamSlot.Equipment ?? "",
                            };
                            int size = TableSize(team.type, side, team.equipment);
                            bool heavy = team.type == TeamType.Vehicle || team.type == TeamType.AntiTankGun || team.type == TeamType.Mortar;
                            for (int seat = 0; seat < size; seat++)
                            {
                                if (seat > 0 && !heavy && random.NextFloat() >= strength)
                                {
                                    continue;
                                }

                                team.soldiers.Add(NewSoldier(side, team.type, seat, experience, random, nextSoldierId++));
                            }

                            platoon.teams.Add(team);
                        }
                    }

                    company.platoons.Add(platoon);
                }
            }

            return company;
        }

        /// <summary>A new man for a seat: named, ranked and rolled like the battle's own men (D30).</summary>
        public static SoldierRecord NewSoldier(Side side, TeamType type, int seat, int experience, SimRandom random, int id)
        {
            var (first, last) = NameGenerator.Next(side, random);
            byte Roll(int center, int spread) => (byte)math.clamp(center + random.Range(-spread, spread + 1), 5, 100);
            bool leader = seat == 0;
            return new SoldierRecord
            {
                id = id,
                first = first,
                last = last,
                seat = seat,
                rank = BattleState.RankKey(side, type, seat),
                attributes = new SoldierAttributes
                {
                    Experience = Roll(experience + (leader ? 10 : 0), 12),
                    Morale = Roll(60, 15),
                    Stamina = Roll(70, 15),
                    Marksmanship = Roll(experience, 15),
                    Leadership = leader ? Roll(experience, 15) : (byte)0,
                },
            };
        }
    }
}
