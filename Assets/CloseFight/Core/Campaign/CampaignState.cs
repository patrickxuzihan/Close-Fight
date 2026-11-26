using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CloseFight.Core.Sim;
using CloseFight.Core.Units;
using Newtonsoft.Json;

namespace CloseFight.Core.Campaign
{
    public enum TurnPhase : byte
    {
        /// <summary>Both sides are giving orders (D57: at the same time).</summary>
        Orders = 0,
        /// <summary>Moves are done; the battles they caused are being fought or auto-resolved.</summary>
        Battles = 1,
    }

    public enum CampaignEventKind : byte
    {
        Battle = 0,
        /// <summary>A district changed hands without a fight.</summary>
        Captured = 1,
        /// <summary>An undecided battle left both sides in the district (D33).</summary>
        Contested = 2,
        Retreated = 3,
        /// <summary>A beaten regiment had nowhere to fall back to.</summary>
        Surrendered = 4,
        Destroyed = 5,
        Arrived = 6,
        Replacements = 7,
        Resupplied = 8,
        CutOff = 9,
        Purchased = 10,
        SupportBought = 11,
        /// <summary>A side's supply points came in (amount = points).</summary>
        Income = 12,
    }

    /// <summary>One line of the turn summary.</summary>
    [Serializable]
    public sealed class CampaignEvent
    {
        public int turn;
        public CampaignEventKind kind;
        public Side side;
        public string district = "";
        public string regiment = "";
        public int amount;
    }

    /// <summary>
    /// A campaign in progress (M16): who holds which district, the regiments and every man
    /// on their rolls, the supply points, and this turn's battles. A turn goes:
    /// <see cref="SubmitOrders"/> for both sides (D57), <see cref="ResolveMovement"/>,
    /// <see cref="ApplyBattle"/> for each battle it returns (fought or auto-resolved, D59),
    /// then <see cref="FinishTurn"/> for supply, recovery and arrivals (D61, D62).
    /// Saved and loaded whole with <see cref="CampaignLoader.SaveToJson"/>.
    /// </summary>
    [Serializable]
    public sealed class CampaignState
    {
        public CampaignDefinition definition;
        /// <summary>1-based; odd turns are days, even turns nights (D15).</summary>
        public int turn = 1;
        public TurnPhase phase = TurnPhase.Orders;
        public bool finished;
        /// <summary>The side that took the whole map (D60), or null.</summary>
        public Side? winner;
        public List<DistrictState> districts = new List<DistrictState>();
        public List<RegimentState> regiments = new List<RegimentState>();
        /// <summary>Supply points banked (D62).</summary>
        public int germanPoints;
        public int sovietPoints;
        /// <summary>Off-map support points bought for the next battles (D36).</summary>
        public int germanSupport;
        public int sovietSupport;
        public int nextSoldierId = 1;
        public List<PendingBattle> battles = new List<PendingBattle>();
        public List<CampaignEvent> events = new List<CampaignEvent>();

        readonly Dictionary<Side, SideOrders> _orders = new Dictionary<Side, SideOrders>();
        Dictionary<string, DistrictDefinition> _definitions;
        Dictionary<string, DistrictState> _states;

        public static CampaignState Create(CampaignDefinition definition)
        {
            CampaignLoader.Validate(definition);
            var state = new CampaignState { definition = definition };
            foreach (var district in definition.districts)
            {
                state.districts.Add(new DistrictState { id = district.id, owner = district.owner });
            }

            state.AfterLoad();
            var random = new SimRandom(definition.seed, 7UL);
            foreach (var regiment in definition.regiments)
            {
                state.regiments.Add(state.BuildRegiment(regiment, random));
            }

            foreach (var arrival in definition.arrivals)
            {
                if (arrival.turn <= 1)
                {
                    state.Arrive(arrival.regiment, random);
                }
            }

            return state;
        }

        /// <summary>Rebuilds the lookups after loading a save.</summary>
        public void AfterLoad()
        {
            _definitions = definition.districts.ToDictionary(d => d.id);
            _states = districts.ToDictionary(d => d.id);
        }

        // ---- Queries --------------------------------------------------------------------------

        [JsonIgnore] public bool IsNight => turn % 2 == 0;

        /// <summary>The calendar day of this turn (two turns a day, D15).</summary>
        [JsonIgnore]
        public DateTime Date => DateTime.ParseExact(definition.startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays((turn - 1) / 2);

        public DistrictState District(string id) => _states.TryGetValue(id, out var state) ? state : null;

        public DistrictDefinition DistrictDefinition(string id) => _definitions.TryGetValue(id, out var definition) ? definition : null;

        public IReadOnlyList<string> Neighbours(string district) => DistrictDefinition(district).neighbours;

        /// <summary>Whether a side's regiments may enter the district (the east bank and the islands are closed to the Germans).</summary>
        public bool CanEnter(Side side, string district) => DistrictDefinition(district)?.closedTo != side;

        public RegimentState Regiment(string id) => regiments.Find(r => r.id == id);

        public IEnumerable<RegimentState> RegimentsOf(Side side) => regiments.Where(r => r.side == side);

        public List<RegimentState> RegimentsIn(string district, Side side) => regiments.FindAll(r => r.district == district && r.side == side);

        public int Points(Side side) => side == Side.German ? germanPoints : sovietPoints;

        public int Support(Side side) => side == Side.German ? germanSupport : sovietSupport;

        /// <summary>Takes up to <paramref name="amount"/> support points for a battle; returns how many it got.</summary>
        public int TakeSupport(Side side, int amount)
        {
            int taken = Math.Max(0, Math.Min(amount, Support(side)));
            if (side == Side.German) germanSupport -= taken;
            else sovietSupport -= taken;
            return taken;
        }

        void AddPoints(Side side, int amount)
        {
            if (side == Side.German) germanPoints += amount;
            else sovietPoints += amount;
        }

        void AddSupport(Side side, int amount)
        {
            if (side == Side.German) germanSupport += amount;
            else sovietSupport += amount;
        }

        /// <summary>
        /// Districts reached from the side's supply points through districts it holds outright
        /// (D61). A contested district is the front: supplies do not pass through it.
        /// </summary>
        public HashSet<string> SuppliedDistricts(Side side)
        {
            var reached = new HashSet<string>();
            var queue = new Queue<string>();
            foreach (var district in definition.districts)
            {
                var state = District(district.id);
                if (district.depotOf == side && state.owner == side && !state.contested)
                {
                    reached.Add(district.id);
                    queue.Enqueue(district.id);
                }
            }

            while (queue.Count > 0)
            {
                foreach (var next in Neighbours(queue.Dequeue()))
                {
                    var state = District(next);
                    if (state.owner == side && !state.contested && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return reached;
        }

        /// <summary>A regiment is supplied in a connected district, or fighting in a contested one next to it.</summary>
        public bool IsSupplied(RegimentState regiment, HashSet<string> supplied)
        {
            if (supplied.Contains(regiment.district))
            {
                return true;
            }

            return District(regiment.district).contested && Neighbours(regiment.district).Any(supplied.Contains);
        }

        public SideOrders OrdersOf(Side side) => _orders.TryGetValue(side, out var orders) ? orders : null;

        // ---- Turn: orders and movement ---------------------------------------------------------

        /// <summary>A side's orders for this turn (D57: both sides decide before anything moves).</summary>
        public void SubmitOrders(SideOrders orders)
        {
            if (phase != TurnPhase.Orders || finished)
            {
                throw new InvalidOperationException("Orders can only be given at the start of a turn.");
            }

            _orders[orders.side] = orders;
        }

        /// <summary>
        /// Carries out both sides' moves at once and returns the battles they cause: wherever
        /// both sides end up in one district. A district one side walks into unopposed changes
        /// hands at once. When two regiments swap districts head-on, the campaign's attacker
        /// attacks and the defender stays.
        /// </summary>
        public List<PendingBattle> ResolveMovement()
        {
            if (phase != TurnPhase.Orders || finished)
            {
                throw new InvalidOperationException("Movement has already been resolved this turn.");
            }

            var moves = new Dictionary<RegimentState, string>();
            foreach (var regiment in regiments)
            {
                regiment.origin = regiment.district;
                var order = OrdersOf(regiment.side)?.For(regiment.id);
                var action = order?.action ?? RegimentAction.Hold;
                regiment.resting = action == RegimentAction.Rest && !District(regiment.district).contested;
                if (action == RegimentAction.Move && order.district != regiment.district && Neighbours(regiment.district).Contains(order.district) &&
                    CanEnter(regiment.side, order.district))
                {
                    moves[regiment] = order.district;
                }
            }

            // Head-on swaps: the attacker goes in, the defender stays to meet it.
            foreach (var mover in moves.Keys.ToList())
            {
                if (!moves.TryGetValue(mover, out var destination))
                {
                    continue;
                }

                var swap = moves.Keys.FirstOrDefault(other => other.side != mover.side && other.district == destination && moves[other] == mover.district);
                if (swap != null)
                {
                    moves.Remove(mover.side == definition.attacker ? swap : mover);
                }
            }

            // At most MaxRegimentsPerSide of a side in a district; moves are taken in order,
            // repeatedly, so a regiment can follow one that leaves.
            var count = new Dictionary<(string, Side), int>();
            int Count(string district, Side side) => count.TryGetValue((district, side), out var n) ? n : 0;
            foreach (var regiment in regiments)
            {
                count[(regiment.district, regiment.side)] = Count(regiment.district, regiment.side) + 1;
            }

            var accepted = new HashSet<RegimentState>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var pair in moves)
                {
                    var regiment = pair.Key;
                    if (accepted.Contains(regiment) || Count(pair.Value, regiment.side) >= CampaignRules.MaxRegimentsPerSide)
                    {
                        continue;
                    }

                    accepted.Add(regiment);
                    count[(pair.Value, regiment.side)] = Count(pair.Value, regiment.side) + 1;
                    count[(regiment.district, regiment.side)] = Count(regiment.district, regiment.side) - 1;
                    changed = true;
                }
            }

            foreach (var regiment in accepted)
            {
                regiment.district = moves[regiment];
                regiment.resting = false;
            }

            battles.Clear();
            foreach (var district in definition.districts)
            {
                var state = District(district.id);
                var germans = RegimentsIn(district.id, Side.German);
                var soviets = RegimentsIn(district.id, Side.Soviet);
                if (germans.Count > 0 && soviets.Count > 0)
                {
                    var attacker = state.contested ? state.contestedAttacker : Opponent(state.owner);
                    battles.Add(new PendingBattle
                    {
                        id = $"t{turn}_{district.id}",
                        district = district.id,
                        attacker = attacker,
                        attackers = (attacker == Side.German ? germans : soviets).Select(r => r.id).ToList(),
                        defenders = (attacker == Side.German ? soviets : germans).Select(r => r.id).ToList(),
                        night = IsNight,
                        continued = state.contested,
                    });
                    Log(CampaignEventKind.Battle, attacker, district.id, "", 0);
                    continue;
                }

                var present = germans.Count > 0 ? Side.German : soviets.Count > 0 ? Side.Soviet : (Side?)null;
                if (present.HasValue && present.Value != state.owner)
                {
                    state.owner = present.Value;
                    Log(CampaignEventKind.Captured, present.Value, district.id, "", 0);
                }

                state.contested = false;
            }

            phase = TurnPhase.Battles;
            return battles;
        }

        // ---- Turn: battles ----------------------------------------------------------------------

        /// <summary>
        /// Settles a fought or auto-resolved battle. The men's fates must already be on the rolls.
        /// The loser falls back to a neighbouring district of its own (a failed attacker to where
        /// it came from), or surrenders if it has none; an undecided battle leaves the district
        /// contested (D33).
        /// </summary>
        public void ApplyBattle(PendingBattle battle, BattleOutcome result)
        {
            if (phase != TurnPhase.Battles || battle.resolved)
            {
                throw new InvalidOperationException("This battle cannot be settled now.");
            }

            battle.resolved = true;
            var random = RandomFor(1 + battles.IndexOf(battle));
            foreach (var id in battle.attackers.Concat(battle.defenders))
            {
                var regiment = Regiment(id);
                if (regiment == null)
                {
                    continue;
                }

                SettleRoll(regiment, random);
                float used = result.ammoUsed.TryGetValue(id, out var a) ? a : CampaignRules.DefaultAmmoUsed;
                float lost = result.moraleLost.TryGetValue(id, out var m) ? m : CampaignRules.DefaultMoraleLoss;
                regiment.ammo = Math.Max(0f, regiment.ammo - used);
                regiment.morale = Math.Max(0f, regiment.morale - lost);
                regiment.battlesWithoutRest++;
                regiment.resting = false;
            }

            var district = District(battle.district);
            if (result.winner.HasValue)
            {
                var winner = result.winner.Value;
                district.owner = winner;
                district.contested = false;
                var losers = winner == battle.attacker ? battle.defenders : battle.attackers;
                foreach (var id in losers)
                {
                    var regiment = Regiment(id);
                    if (regiment != null)
                    {
                        Retreat(regiment, winner == battle.attacker ? null : regiment.origin);
                    }
                }
            }
            else
            {
                district.contested = true;
                district.contestedAttacker = battle.attacker;
                Log(CampaignEventKind.Contested, battle.attacker, battle.district, "", 0);
            }

            RemoveDestroyed();
        }

        /// <summary>Clears a battle's dead and evacuated from the rolls and starts the wounded's recovery.</summary>
        void SettleRoll(RegimentState regiment, SimRandom random)
        {
            foreach (var team in regiment.Teams)
            {
                for (int i = team.soldiers.Count - 1; i >= 0; i--)
                {
                    var soldier = team.soldiers[i];
                    switch (soldier.status)
                    {
                        case SoldierStatus.Dead:
                            regiment.killed++;
                            team.soldiers.RemoveAt(i);
                            break;
                        case SoldierStatus.Incapacitated:
                            regiment.evacuated++;
                            team.soldiers.RemoveAt(i);
                            break;
                        case SoldierStatus.Wounded when soldier.returnsOnTurn == 0:
                            soldier.returnsOnTurn = turn + random.Range(CampaignRules.WoundedReturnMinTurns, CampaignRules.WoundedReturnMaxTurns + 1);
                            break;
                        case SoldierStatus.Fled:
                            // Men who ran from the field find their unit again.
                            soldier.status = SoldierStatus.Healthy;
                            break;
                    }
                }
            }
        }

        void Retreat(RegimentState regiment, string preferred)
        {
            var supplied = SuppliedDistricts(regiment.side);
            var candidates = Neighbours(regiment.district)
                .Where(n =>
                {
                    var state = District(n);
                    return state.owner == regiment.side && !state.contested && CanEnter(regiment.side, n) && RegimentsIn(n, Opponent(regiment.side)).Count == 0 &&
                           RegimentsIn(n, regiment.side).Count < CampaignRules.MaxRegimentsPerSide;
                })
                .OrderBy(n => n == preferred ? 0 : supplied.Contains(n) ? 1 : 2)
                .ToList();

            if (candidates.Count > 0)
            {
                regiment.district = candidates[0];
                Log(CampaignEventKind.Retreated, regiment.side, regiment.district, regiment.id, 0);
                return;
            }

            int men = regiment.Soldiers.Count(s => s.status == SoldierStatus.Healthy || s.status == SoldierStatus.Wounded);
            regiment.captured += men;
            regiments.Remove(regiment);
            Log(CampaignEventKind.Surrendered, regiment.side, regiment.district, regiment.id, men);
        }

        void RemoveDestroyed()
        {
            foreach (var regiment in regiments.Where(r => r.IsDestroyed).ToList())
            {
                regiments.Remove(regiment);
                Log(CampaignEventKind.Destroyed, regiment.side, regiment.district, regiment.id, 0);
            }
        }

        // ---- Turn: supply, recovery, arrivals ---------------------------------------------------

        /// <summary>
        /// Ends the turn once every battle is settled: supply points come in at the supply points
        /// (crossings only at night, D40), are spent as ordered (D62), connected regiments recover
        /// a little (more at rest) and cut-off ones lose heart (D61), the healed wounded return,
        /// and the next turn's arrivals come in.
        /// </summary>
        public void FinishTurn()
        {
            if (phase != TurnPhase.Battles || battles.Any(b => !b.resolved))
            {
                throw new InvalidOperationException("Every battle must be settled before the turn ends.");
            }

            var random = RandomFor(100);
            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                var supplied = SuppliedDistricts(side);
                foreach (var regiment in RegimentsOf(side))
                {
                    bool was = regiment.supplied;
                    regiment.supplied = IsSupplied(regiment, supplied);
                    if (was && !regiment.supplied)
                    {
                        Log(CampaignEventKind.CutOff, side, regiment.district, regiment.id, 0);
                    }
                }

                int income = 0;
                foreach (var district in definition.districts)
                {
                    var state = District(district.id);
                    if (district.depotOf == side && state.owner == side && !state.contested)
                    {
                        income += district.crossing ? (IsNight ? CampaignRules.CrossingIncome : 0) : CampaignRules.DepotIncome;
                    }
                }

                if (income > 0)
                {
                    AddPoints(side, income);
                    Log(CampaignEventKind.Income, side, "", "", income);
                }

                var orders = OrdersOf(side);
                if (orders != null)
                {
                    Spend(orders, random);
                }
            }

            foreach (var regiment in regiments)
            {
                if (regiment.supplied)
                {
                    regiment.ammo = Math.Min(1f, regiment.ammo + (regiment.resting ? CampaignRules.AmmoRecoveryAtRest : CampaignRules.AmmoRecoveryInLine));
                    regiment.morale = Math.Min(CampaignRules.MaxMorale,
                        regiment.morale + (regiment.resting ? CampaignRules.MoraleRecoveryAtRest : CampaignRules.MoraleRecoveryInLine));
                    if (regiment.resting)
                    {
                        regiment.battlesWithoutRest = 0;
                    }
                }
                else
                {
                    regiment.morale = Math.Max(0f, regiment.morale - CampaignRules.MoraleLossCutOff);
                }

                foreach (var soldier in regiment.Soldiers)
                {
                    if (soldier.status == SoldierStatus.Wounded && soldier.returnsOnTurn > 0 && soldier.returnsOnTurn <= turn + 1)
                    {
                        soldier.status = SoldierStatus.Healthy;
                        soldier.returnsOnTurn = 0;
                    }
                }
            }

            _orders.Clear();
            phase = TurnPhase.Orders;
            turn++;

            var arrivalRandom = RandomFor(200);
            foreach (var arrival in definition.arrivals)
            {
                if (arrival.turn == turn)
                {
                    Arrive(arrival.regiment, arrivalRandom);
                }
            }

            foreach (var side in new[] { Side.German, Side.Soviet })
            {
                if (districts.All(d => d.owner == side && !d.contested))
                {
                    finished = true;
                    winner = side;
                }
            }

            if (turn > definition.turns)
            {
                finished = true;
            }
        }

        void Spend(SideOrders orders, SimRandom random)
        {
            var side = orders.side;

            if (orders.support > 0)
            {
                int bought = Math.Min(orders.support, Points(side) / CampaignRules.SupportCost);
                if (bought > 0)
                {
                    AddPoints(side, -bought * CampaignRules.SupportCost);
                    AddSupport(side, bought);
                    Log(CampaignEventKind.SupportBought, side, "", "", bought);
                }
            }

            foreach (var request in orders.supply)
            {
                var regiment = Regiment(request.regiment);
                if (regiment == null || regiment.side != side || !regiment.supplied)
                {
                    continue;
                }

                if (request.ammo && regiment.ammo < 1f)
                {
                    int cost = (int)Math.Ceiling(regiment.PresentCount * (1f - regiment.ammo) * CampaignRules.AmmoCostPerMan);
                    if (cost <= Points(side))
                    {
                        AddPoints(side, -cost);
                        regiment.ammo = 1f;
                    }
                    else if (cost > 0)
                    {
                        // Partly: what the points stretch to.
                        regiment.ammo += (1f - regiment.ammo) * Points(side) / cost;
                        AddPoints(side, -Points(side));
                    }

                    Log(CampaignEventKind.Resupplied, side, regiment.district, regiment.id, cost);
                }

                if (request.replacements > 0 && regiment.resting)
                {
                    int wanted = Math.Min(request.replacements, CampaignRules.MaxReplacementsPerTurn);
                    wanted = Math.Min(wanted, Points(side) / CampaignRules.ReplacementCost);
                    int added = Replace(regiment, wanted, random);
                    if (added > 0)
                    {
                        AddPoints(side, -added * CampaignRules.ReplacementCost);
                        Log(CampaignEventKind.Replacements, side, regiment.district, regiment.id, added);
                    }
                }
            }

            foreach (var request in orders.purchases)
            {
                var offer = definition.purchases.Find(p => p.side == side && p.template == request.template);
                var regiment = Regiment(request.regiment);
                if (offer == null || regiment == null || regiment.side != side || !regiment.supplied || Points(side) < offer.cost)
                {
                    continue;
                }

                AddPoints(side, -offer.cost);
                string id = $"{regiment.id}_b{turn}_{regiment.companies.Count}";
                regiment.companies.Add(CompanyTemplates.Build(offer.template, id, offer.name, regiment.experience, 1f, random, ref nextSoldierId));
                Log(CampaignEventKind.Purchased, side, regiment.district, regiment.id, offer.cost);
            }
        }

        /// <summary>Fills empty seats with green replacements, leaders' seats first; returns how many came.</summary>
        int Replace(RegimentState regiment, int wanted, SimRandom random)
        {
            int added = 0;
            for (int pass = 0; pass < 2 && added < wanted; pass++)
            {
                foreach (var team in regiment.Teams)
                {
                    if (added >= wanted)
                    {
                        break;
                    }

                    if (team.equipmentLost)
                    {
                        continue;
                    }

                    int size = team.TableSize;
                    for (int seat = 0; seat < size && added < wanted; seat++)
                    {
                        // First pass: only leaders, so every team gets someone in charge.
                        if (pass == 0 && seat > 0)
                        {
                            break;
                        }

                        if (team.soldiers.Any(s => s.seat == seat))
                        {
                            continue;
                        }

                        team.soldiers.Add(CompanyTemplates.NewSoldier(regiment.side, team.type, seat, CampaignRules.ReplacementExperience, random, nextSoldierId++));
                        added++;
                    }
                }
            }

            foreach (var team in regiment.Teams)
            {
                team.soldiers.Sort((a, b) => a.seat.CompareTo(b.seat));
            }

            return added;
        }

        void Arrive(RegimentDefinition definitionOfRegiment, SimRandom random)
        {
            var regiment = BuildRegiment(definitionOfRegiment, random);
            var target = District(regiment.district);
            bool free = target != null && target.owner == regiment.side && !target.contested &&
                        RegimentsIn(regiment.district, regiment.side).Count < CampaignRules.MaxRegimentsPerSide;
            if (!free)
            {
                // Its sector is lost or full: it comes in at a supply point of its side instead.
                var depot = definition.districts.FirstOrDefault(d => d.depotOf == regiment.side && District(d.id).owner == regiment.side &&
                                                                    !District(d.id).contested &&
                                                                    RegimentsIn(d.id, regiment.side).Count < CampaignRules.MaxRegimentsPerSide);
                if (depot == null)
                {
                    return;
                }

                regiment.district = depot.id;
            }

            regiment.origin = regiment.district;
            regiments.Add(regiment);
            Log(CampaignEventKind.Arrived, regiment.side, regiment.district, regiment.id, regiment.PresentCount);
        }

        RegimentState BuildRegiment(RegimentDefinition source, SimRandom random)
        {
            var regiment = new RegimentState
            {
                id = source.id,
                side = source.side,
                name = source.name,
                division = source.division,
                district = source.district,
                origin = source.district,
                experience = source.experience,
                morale = source.morale,
            };
            for (int i = 0; i < source.companies.Count; i++)
            {
                var company = source.companies[i];
                regiment.companies.Add(CompanyTemplates.Build(company.template, $"{source.id}_c{i}", company.name, source.experience, source.strength, random,
                    ref nextSoldierId));
            }

            return regiment;
        }

        // ---- Whole turns ------------------------------------------------------------------------

        /// <summary>
        /// Plays a whole turn: commanders give orders for the sides that have none yet, every
        /// battle goes to the resolver, and the turn ends. For the AI-only campaign tests and
        /// for auto-playing the enemy side.
        /// </summary>
        public void AdvanceTurn(IBattleResolver resolver, params ICampaignCommander[] commanders)
        {
            foreach (var commander in commanders)
            {
                if (OrdersOf(commander.Side) == null)
                {
                    SubmitOrders(commander.Plan(this));
                }
            }

            foreach (var battle in ResolveMovement().ToList())
            {
                ApplyBattle(battle, resolver.Resolve(this, battle));
            }

            FinishTurn();
        }

        public static Side Opponent(Side side) => side == Side.German ? Side.Soviet : Side.German;

        SimRandom RandomFor(int phaseSeed) => new SimRandom(definition.seed + (ulong)turn * 1000UL + (ulong)phaseSeed, 11UL);

        void Log(CampaignEventKind kind, Side side, string district, string regiment, int amount)
        {
            events.Add(new CampaignEvent { turn = turn, kind = kind, side = side, district = district ?? "", regiment = regiment ?? "", amount = amount });
        }
    }
}
