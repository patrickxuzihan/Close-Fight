using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CloseFight.Core.Campaign;
using CloseFight.Core.Units;
using NUnit.Framework;

namespace CloseFight.Tests
{
    /// <summary>The strategic layer's rules (M16, D55–D62), on a five-district test map.</summary>
    public class CampaignTests
    {
        //  gw (German supply point) — st — sq — ld (Soviet crossing)
        //    \_______ nh ________/
        static CampaignDefinition Definition()
        {
            DistrictDefinition D(string id, Side owner, params string[] neighbours) =>
                new DistrictDefinition { id = id, owner = owner, neighbours = neighbours.ToList() };

            var gw = D("gw", Side.German, "st", "nh");
            gw.depotOf = Side.German;
            var ld = D("ld", Side.Soviet, "sq");
            ld.depotOf = Side.Soviet;
            ld.crossing = true;
            ld.key = true;
            return new CampaignDefinition
            {
                id = "test",
                seed = 5,
                turns = 10,
                districts = new List<DistrictDefinition> { gw, D("st", Side.Soviet, "sq"), D("sq", Side.Soviet, "nh"), D("nh", Side.Soviet), ld },
                regiments = new List<RegimentDefinition>
                {
                    Regiment("ir194", Side.German, "gw", "de_rifle", "de_rifle", "de_mg"),
                    Regiment("ir191", Side.German, "gw", "de_rifle"),
                    Regiment("nkvd272", Side.Soviet, "st", "su_rifle", "su_rifle", "su_mg"),
                    Regiment("gv42", Side.Soviet, "sq", "su_rifle"),
                },
                purchases = new List<PurchaseDefinition> { new PurchaseDefinition { side = Side.German, template = "de_stug", name = "StuG-Batterie", cost = 50 } },
            };
        }

        static RegimentDefinition Regiment(string id, Side side, string district, params string[] companies) => new RegimentDefinition
        {
            id = id, side = side, name = id, district = district,
            companies = companies.Select((t, i) => new CompanyDefinition { template = t, name = $"{i + 1}." }).ToList(),
        };

        /// <summary>Gives a battle the winner the test wants, killing every fifth man and wounding every seventh on both sides.</summary>
        sealed class FixedResolver : IBattleResolver
        {
            public Side? Winner;
            public bool Wounds = true;
            public int Battles;

            public BattleOutcome Resolve(CampaignState state, PendingBattle battle)
            {
                Battles++;
                foreach (var id in battle.attackers.Concat(battle.defenders))
                {
                    int i = 0;
                    foreach (var soldier in state.Regiment(id).Soldiers.Where(s => s.IsPresent).ToList())
                    {
                        i++;
                        if (i % 5 == 0) soldier.status = SoldierStatus.Dead;
                        else if (Wounds && i % 7 == 0) soldier.status = SoldierStatus.Wounded;
                    }
                }

                return new BattleOutcome { winner = Winner };
            }
        }

        static SideOrders Orders(Side side) => new SideOrders { side = side };

        static List<PendingBattle> Move(CampaignState state, params (string regiment, string district)[] moves)
        {
            var de = Orders(Side.German);
            var su = Orders(Side.Soviet);
            foreach (var (regiment, district) in moves)
            {
                (state.Regiment(regiment).side == Side.German ? de : su).Move(regiment, district);
            }

            state.SubmitOrders(de);
            state.SubmitOrders(su);
            return state.ResolveMovement();
        }

        // ---- Rolls ----

        [Test]
        public void CompaniesFollowTheTablesAndEveryManHasAName()
        {
            var state = CampaignState.Create(Definition());
            var rifle = state.Regiment("ir194").companies[0];
            Assert.AreEqual(3 + 2 + 3 * 3 * 10, rifle.PresentCount, "Kompanietrupp, sniper pair and three Züge of three Gruppen");
            Assert.AreEqual(4, rifle.platoons.Count);
            Assert.AreEqual("2. Zug", rifle.platoons[2].name);
            var soviet = state.Regiment("gv42").companies[0];
            Assert.AreEqual(3 + 2 + 3 * 3 * 11, soviet.PresentCount);
            var everyone = state.regiments.SelectMany(r => r.Soldiers).ToList();
            Assert.IsTrue(everyone.All(s => s.first.Length > 0 && s.last.Length > 0 && s.rank.StartsWith("rank.")));
            Assert.AreEqual(everyone.Count, everyone.Select(s => s.id).Distinct().Count(), "ids are unique");
        }

        [Test]
        public void StrengthLeavesSeatsEmptyButKeepsLeadersAndCrews()
        {
            var definition = Definition();
            definition.regiments[0].strength = 0.5f;
            var state = CampaignState.Create(definition);
            var regiment = state.Regiment("ir194");
            var squads = regiment.Teams.Where(t => t.type == TeamType.Rifle).ToList();
            Assert.IsTrue(squads.All(t => t.soldiers.Any(s => s.seat == 0)), "every squad keeps its leader");
            int men = squads.Sum(t => t.soldiers.Count);
            Assert.That(men, Is.InRange(squads.Count * 3, squads.Count * 7), "about half the table");
            Assert.IsTrue(regiment.Teams.Where(t => t.type == TeamType.Mortar).All(t => t.soldiers.Count == t.TableSize), "gun crews are full");
        }

        [Test]
        public void LoadingChecksTheMap()
        {
            var noDepot = Definition();
            noDepot.districts.ForEach(d => d.depotOf = d.depotOf == Side.Soviet ? (Side?)null : d.depotOf);
            Assert.Throws<FormatException>(() => CampaignLoader.Validate(noDepot), "each side needs a supply point (D61)");

            var wrongSide = Definition();
            wrongSide.regiments[0].companies.Add(new CompanyDefinition { template = "su_rifle" });
            Assert.Throws<FormatException>(() => CampaignLoader.Validate(wrongSide));

            var oneWay = Definition();
            CampaignLoader.Validate(oneWay);
            Assert.Contains("gw", oneWay.districts.First(d => d.id == "st").neighbours, "neighbours are made symmetric");
        }

        // ---- Movement and battles ----

        [Test]
        public void WalkingIntoAnEmptyEnemyDistrictTakesItWithoutAFight()
        {
            var state = CampaignState.Create(Definition());
            var battles = Move(state, ("ir191", "nh"));
            Assert.IsEmpty(battles);
            Assert.AreEqual(Side.German, state.District("nh").owner);
            Assert.AreEqual("nh", state.Regiment("ir191").district);
        }

        [Test]
        public void OnlyNeighbouringDistrictsCanBeReached()
        {
            var state = CampaignState.Create(Definition());
            Move(state, ("ir191", "ld"));
            Assert.AreEqual("gw", state.Regiment("ir191").district);
        }

        [Test]
        public void TheGermansCannotCrossTheVolga()
        {
            var definition = Definition();
            definition.districts.First(d => d.id == "ld").closedTo = Side.German;
            var state = CampaignState.Create(definition);
            state.Regiment("ir194").district = "sq";
            state.District("sq").owner = Side.German;
            state.regiments.Remove(state.Regiment("gv42"));
            Move(state, ("ir194", "ld"));
            Assert.AreEqual("sq", state.Regiment("ir194").district, "the east bank is closed to them");
            Assert.AreEqual(Side.Soviet, state.District("ld").owner);
        }

        [Test]
        public void AnAttackOnAHeldDistrictIsABattle()
        {
            var state = CampaignState.Create(Definition());
            var battles = Move(state, ("ir194", "st"));
            Assert.AreEqual(1, battles.Count);
            Assert.AreEqual("st", battles[0].district);
            Assert.AreEqual(Side.German, battles[0].attacker);
            CollectionAssert.AreEqual(new[] { "ir194" }, battles[0].attackers);
            CollectionAssert.AreEqual(new[] { "nkvd272" }, battles[0].defenders);
            Assert.IsFalse(battles[0].night, "turn 1 is a day");
        }

        [Test]
        public void ABeatenAttackerFallsBackToWhereItCameFrom()
        {
            var state = CampaignState.Create(Definition());
            var battle = Move(state, ("ir194", "st"))[0];
            int before = state.Regiment("ir194").PresentCount;
            state.ApplyBattle(battle, new FixedResolver { Winner = Side.Soviet }.Resolve(state, battle));
            var regiment = state.Regiment("ir194");
            Assert.AreEqual("gw", regiment.district);
            Assert.AreEqual(Side.Soviet, state.District("st").owner);
            Assert.Less(regiment.PresentCount, before);
            Assert.Greater(regiment.killed, 0, "the dead are struck off and counted");
            Assert.Less(regiment.ammo, 1f);
            Assert.Less(regiment.morale, 70f);
        }

        [Test]
        public void ABeatenDefenderFallsBackToItsOwnNeighbour()
        {
            var state = CampaignState.Create(Definition());
            var battle = Move(state, ("ir194", "st"))[0];
            state.ApplyBattle(battle, new FixedResolver { Winner = Side.German }.Resolve(state, battle));
            Assert.AreEqual(Side.German, state.District("st").owner);
            Assert.AreEqual("sq", state.Regiment("nkvd272").district, "it falls back to its own neighbour");
        }

        [Test]
        public void ARegimentWithNowhereToFallBackSurrenders()
        {
            var state = CampaignState.Create(Definition());
            // Every Soviet neighbour of st is gone: sq is German, so nkvd272 is trapped.
            state.District("sq").owner = Side.German;
            state.regiments.Remove(state.Regiment("gv42"));
            var battle = Move(state, ("ir194", "st"))[0];
            int men = state.Regiment("nkvd272").PresentCount;
            state.ApplyBattle(battle, new BattleOutcome { winner = Side.German });
            Assert.IsNull(state.Regiment("nkvd272"));
            Assert.IsTrue(state.events.Any(e => e.kind == CampaignEventKind.Surrendered && e.regiment == "nkvd272" && e.amount == men));
        }

        [Test]
        public void AnUndecidedBattleLeavesTheDistrictContestedAndTheFightGoesOn()
        {
            var state = CampaignState.Create(Definition());
            var battle = Move(state, ("ir194", "st"))[0];
            state.ApplyBattle(battle, new BattleOutcome { winner = null });
            Assert.IsTrue(state.District("st").contested);
            Assert.AreEqual("st", state.Regiment("ir194").district, "both stay (D33)");
            Assert.AreEqual("st", state.Regiment("nkvd272").district);
            state.FinishTurn();

            var next = Move(state);
            Assert.AreEqual(1, next.Count, "next turn the fight continues without new orders");
            Assert.IsTrue(next[0].continued);
            Assert.AreEqual(Side.German, next[0].attacker, "the attacker is still the attacker");
            Assert.IsTrue(next[0].night);
        }

        [Test]
        public void AHeadOnSwapIsAnAttackByTheCampaignsAttacker()
        {
            var state = CampaignState.Create(Definition());
            var battles = Move(state, ("ir194", "st"), ("nkvd272", "gw"));
            Assert.AreEqual(1, battles.Count);
            Assert.AreEqual("st", battles[0].district, "the Germans go in, the Soviets stay to meet them");
            Assert.AreEqual("st", state.Regiment("nkvd272").district);
        }

        [Test]
        public void AtMostTwoRegimentsOfASideShareADistrict()
        {
            var definition = Definition();
            definition.regiments.Add(Regiment("ir211", Side.German, "nh", "de_rifle"));
            definition.districts.First(d => d.id == "nh").owner = Side.German;
            var state = CampaignState.Create(definition);
            Move(state, ("ir211", "gw"));
            Assert.AreEqual("nh", state.Regiment("ir211").district, "gw already holds two");
        }

        [Test]
        public void ARegimentCanFollowOneThatLeaves()
        {
            var definition = Definition();
            definition.regiments.Add(Regiment("ir211", Side.German, "nh", "de_rifle"));
            definition.districts.First(d => d.id == "nh").owner = Side.German;
            var state = CampaignState.Create(definition);
            Move(state, ("ir211", "gw"), ("ir191", "st"));
            Assert.AreEqual("gw", state.Regiment("ir211").district);
        }

        // ---- Supply ----

        [Test]
        public void ACutOffRegimentGetsNothingAndLosesHeart()
        {
            var state = CampaignState.Create(Definition());
            // The supply point gw falls; ir191 holds out in nh behind the Soviet lines.
            state.regiments.Remove(state.Regiment("ir194"));
            state.District("gw").owner = Side.Soviet;
            state.District("nh").owner = Side.German;
            state.Regiment("ir191").district = "nh";
            state.Regiment("ir191").ammo = 0.5f;
            Move(state);
            state.FinishTurn();
            var regiment = state.Regiment("ir191");
            Assert.IsFalse(regiment.supplied);
            Assert.AreEqual(0.5f, regiment.ammo, 1e-4f, "no ammunition reaches it");
            Assert.Less(regiment.morale, 70f);
        }

        [Test]
        public void RestRestoresMoreThanStayingInTheLineButLessThanABattleCosts()
        {
            var state = CampaignState.Create(Definition());
            state.Regiment("ir194").ammo = 0.4f;
            state.Regiment("ir191").ammo = 0.4f;
            var de = Orders(Side.German);
            de.Rest("ir194");
            state.SubmitOrders(de);
            state.ResolveMovement();
            state.FinishTurn();
            float rested = state.Regiment("ir194").ammo, inLine = state.Regiment("ir191").ammo;
            Assert.Greater(rested, inLine);
            Assert.Greater(inLine, 0.4f, "a supplied regiment in the line still gets a little");
            Assert.Less(CampaignRules.AmmoRecoveryInLine, CampaignRules.DefaultAmmoUsed, "D61: refilling is slower than fighting");
            Assert.Less(CampaignRules.MoraleRecoveryInLine, CampaignRules.DefaultMoraleLoss);
        }

        [Test]
        public void SupplyComesAcrossTheVolgaOnlyAtNight()
        {
            var state = CampaignState.Create(Definition());
            Move(state);
            state.FinishTurn();
            Assert.AreEqual(0, state.sovietPoints, "turn 1 is a day: nothing crosses (D40)");
            Assert.AreEqual(CampaignRules.DepotIncome, state.germanPoints);
            Move(state);
            state.FinishTurn();
            Assert.AreEqual(CampaignRules.CrossingIncome, state.sovietPoints);
        }

        [Test]
        public void ReplacementsGoOnlyToRestingSuppliedRegimentsAndAreGreen()
        {
            var state = CampaignState.Create(Definition());
            state.germanPoints = 500;
            var battle = Move(state, ("ir194", "st"))[0];
            state.ApplyBattle(battle, new FixedResolver { Winner = Side.Soviet, Wounds = false }.Resolve(state, battle));
            state.FinishTurn();
            int depleted = state.Regiment("ir194").PresentCount;

            var holdAndAsk = Orders(Side.German);
            holdAndAsk.supply.Add(new SupplyRequest { regiment = "ir194", replacements = 40 });
            state.SubmitOrders(holdAndAsk);
            state.ResolveMovement();
            state.FinishTurn();
            Assert.AreEqual(depleted, state.Regiment("ir194").PresentCount, "not resting: no replacements");

            var restAndAsk = Orders(Side.German);
            restAndAsk.Rest("ir194");
            restAndAsk.supply.Add(new SupplyRequest { regiment = "ir194", replacements = 40 });
            int points = state.germanPoints;
            state.SubmitOrders(restAndAsk);
            state.ResolveMovement();
            state.FinishTurn();
            var regiment = state.Regiment("ir194");
            Assert.AreEqual(depleted + 40, regiment.PresentCount);
            Assert.AreEqual(points + CampaignRules.DepotIncome - 40 * CampaignRules.ReplacementCost, state.germanPoints);
            var green = regiment.Soldiers.Where(s => s.id >= state.nextSoldierId - 40).ToList();
            Assert.AreEqual(40, green.Count);
            Assert.IsTrue(green.All(s => s.attributes.Experience <= CampaignRules.ReplacementExperience + 22), "replacements are green");
        }

        [Test]
        public void AmmunitionAndSupportCanBeBought()
        {
            var state = CampaignState.Create(Definition());
            state.germanPoints = 1000;
            state.Regiment("ir194").ammo = 0.2f;
            var orders = Orders(Side.German);
            orders.supply.Add(new SupplyRequest { regiment = "ir194", ammo = true });
            orders.support = 30;
            orders.purchases.Add(new PurchaseRequest { template = "de_stug", regiment = "ir191" });
            state.SubmitOrders(orders);
            state.ResolveMovement();
            int companies = state.Regiment("ir191").companies.Count;
            state.FinishTurn();
            Assert.AreEqual(1f, state.Regiment("ir194").ammo, 1e-4f);
            Assert.AreEqual(30, state.germanSupport);
            Assert.AreEqual(companies + 1, state.Regiment("ir191").companies.Count, "the bought StuG battery joins");
            Assert.AreEqual(30, state.TakeSupport(Side.German, 50), "a battle draws on the support bought");
        }

        // ---- Rolls across battles ----

        [Test]
        public void TheWoundedComeBackAfterAFewTurns()
        {
            var state = CampaignState.Create(Definition());
            var battle = Move(state, ("ir194", "st"))[0];
            state.ApplyBattle(battle, new FixedResolver { Winner = Side.Soviet }.Resolve(state, battle));
            var wounded = state.Regiment("ir194").Soldiers.Where(s => s.status == SoldierStatus.Wounded).ToList();
            Assert.IsNotEmpty(wounded);
            Assert.IsTrue(wounded.All(s => s.returnsOnTurn >= 1 + CampaignRules.WoundedReturnMinTurns && s.returnsOnTurn <= 1 + CampaignRules.WoundedReturnMaxTurns));
            state.FinishTurn();
            for (int i = 0; i < CampaignRules.WoundedReturnMaxTurns; i++)
            {
                Move(state);
                state.FinishTurn();
            }

            Assert.IsTrue(wounded.All(s => s.IsPresent), "D39: the lightly wounded rejoin");
        }

        [Test]
        public void ASavedCampaignLoadsBackTheSame()
        {
            var state = CampaignState.Create(Definition());
            var battle = Move(state, ("ir194", "st"))[0];
            state.ApplyBattle(battle, new FixedResolver { Winner = Side.Soviet }.Resolve(state, battle));
            state.FinishTurn();
            string json = CampaignLoader.SaveToJson(state);
            var loaded = CampaignLoader.LoadFromJson(json);
            Assert.AreEqual(json, CampaignLoader.SaveToJson(loaded));
            Assert.AreEqual(state.Regiment("ir194").PresentCount, loaded.Regiment("ir194").PresentCount);
            Assert.AreEqual(Side.Soviet, loaded.District("st").owner);
            Move(loaded, ("ir194", "st"));
            Assert.AreEqual("st", loaded.Regiment("ir194").district, "a loaded campaign plays on");
        }

        [Test]
        public void ReinforcementsArriveOnTheirTurn()
        {
            var definition = Definition();
            definition.arrivals.Add(new ArrivalDefinition { turn = 3, regiment = Regiment("gv39", Side.Soviet, "ld", "su_rifle") });
            var state = CampaignState.Create(definition);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsNull(state.Regiment("gv39"));
                Move(state);
                state.FinishTurn();
            }

            Assert.AreEqual(3, state.turn);
            Assert.AreEqual("ld", state.Regiment("gv39").district, "13th Guards style: across the river on its date");
        }

        [Test]
        public void TheCampaignEndsAfterItsTurnsOrWhenOneSideHoldsEverything()
        {
            var state = CampaignState.Create(Definition());
            var resolver = new FixedResolver();
            for (int i = 0; i < 10; i++)
            {
                state.AdvanceTurn(resolver);
            }

            Assert.IsTrue(state.finished);
            Assert.IsNull(state.winner, "D60: no winner, the front is where it is");

            var sweep = CampaignState.Create(Definition());
            sweep.districts.ForEach(d => d.owner = Side.German);
            sweep.regiments.RemoveAll(r => r.side == Side.Soviet);
            sweep.AdvanceTurn(resolver);
            Assert.IsTrue(sweep.finished);
            Assert.AreEqual(Side.German, sweep.winner, "taking the whole map is a victory");
        }

        /// <summary>Attacks the weakest neighbouring enemy district with every regiment that has one (a stand-in until the real strategic AI).</summary>
        sealed class Aggressor : ICampaignCommander
        {
            public Side Side { get; set; }

            public SideOrders Plan(CampaignState state)
            {
                var orders = new SideOrders { side = Side };
                foreach (var regiment in state.RegimentsOf(Side))
                {
                    var target = state.Neighbours(regiment.district).FirstOrDefault(n => state.District(n).owner != Side);
                    if (target != null) orders.Move(regiment.id, target);
                    else orders.Rest(regiment.id);
                    orders.supply.Add(new SupplyRequest { regiment = regiment.id, ammo = true, replacements = 60 });
                }

                return orders;
            }
        }

        sealed class AlternatingResolver : IBattleResolver
        {
            int _count;

            public BattleOutcome Resolve(CampaignState state, PendingBattle battle)
            {
                var inner = new FixedResolver { Winner = (_count++ % 3) switch { 0 => battle.attacker, 1 => battle.Defender, _ => (Side?)null } };
                return inner.Resolve(state, battle);
            }
        }

        [Test]
        public void TheStalingradCampaignLoadsAndPlaysToItsEnd()
        {
            var definition = CampaignLoader.FromJson(File.ReadAllText("Assets/CloseFight/Data/Campaigns/stalingrad_center.json"));
            Assert.GreaterOrEqual(definition.districts.Count, 40, "D55: a ring around the centre, about twice the first draft");
            Assert.IsTrue(definition.districts.Where(d => d.closedTo == Side.German).All(d => d.owner == Side.Soviet), "the east bank and the islands are Soviet");
            Assert.AreEqual(28, definition.turns);
            var state = CampaignState.Create(definition);
            Assert.AreEqual(new DateTime(1942, 9, 13), state.Date);
            Assert.AreEqual(Side.German, state.District("aviagorodok").owner);
            Assert.AreEqual("stalingrad_station", state.DistrictDefinition("station").mapId, "the station is fought on the hand-made map");

            var resolver = new AlternatingResolver();
            while (!state.finished)
            {
                state.AdvanceTurn(resolver, new Aggressor { Side = Side.German }, new Aggressor { Side = Side.Soviet });
                Assert.IsTrue(state.regiments.All(r => state.District(r.district) != null));
                Assert.IsTrue(state.regiments.All(r => r.ammo >= 0f && r.ammo <= 1f && r.morale >= 0f && r.morale <= 100f));
                foreach (var district in state.districts)
                {
                    Assert.LessOrEqual(state.RegimentsIn(district.id, Side.German).Count, CampaignRules.MaxRegimentsPerSide);
                    Assert.LessOrEqual(state.RegimentsIn(district.id, Side.Soviet).Count, CampaignRules.MaxRegimentsPerSide);
                }
            }

            Assert.IsTrue(state.turn > 28 || state.winner.HasValue);
            Assert.IsTrue(state.events.Any(e => e.kind == CampaignEventKind.Arrived && e.regiment == "su_42gvsp" && e.turn == 4), "13th Guards cross on the night of 14/15 September");
        }

        [Test]
        public void TheSameOrdersGiveTheSameCampaign()
        {
            string Play()
            {
                var state = CampaignState.Create(Definition());
                var battle = Move(state, ("ir194", "st"))[0];
                state.ApplyBattle(battle, new FixedResolver { Winner = Side.Soviet }.Resolve(state, battle));
                state.FinishTurn();
                var rest = Orders(Side.German);
                rest.Rest("ir194");
                rest.supply.Add(new SupplyRequest { regiment = "ir194", replacements = 30 });
                state.SubmitOrders(rest);
                state.ResolveMovement();
                state.FinishTurn();
                return CampaignLoader.SaveToJson(state);
            }

            Assert.AreEqual(Play(), Play());
        }
    }
}
