using System;
using System.Collections.Generic;
using CloseFight.Core.Units;
using Newtonsoft.Json;

namespace CloseFight.Core.Campaign
{
    /// <summary>
    /// One man on a regiment's roll (D1, D30): he keeps his name, rank, experience and
    /// wounds from battle to battle. A battle is fought with these men (M16 step 3).
    /// </summary>
    [Serializable]
    public sealed class SoldierRecord
    {
        public int id;
        public string first = "";
        public string last = "";
        /// <summary>Localization key of the rank, e.g. "rank.de.squad_leader".</summary>
        public string rank = "";
        /// <summary>His place in the team's table (0 = leader); it decides the weapon he is issued.</summary>
        public int seat;
        public SoldierAttributes attributes;
        /// <summary>Healthy, or Wounded while he recovers (D39: a light wound heals in a few turns).</summary>
        public SoldierStatus status = SoldierStatus.Healthy;
        /// <summary>Turn on which a wounded man rejoins; 0 when he is with the team.</summary>
        public int returnsOnTurn;
        public int battles;
        public int kills;

        [JsonIgnore] public bool IsPresent => status == SoldierStatus.Healthy;
        [JsonIgnore] public string FullName => first + " " + last;
    }

    [Serializable]
    public sealed class TeamRecord
    {
        public string key = "";
        public TeamType type;
        public string name = "";
        /// <summary>Vehicle or gun id for vehicle and gun teams.</summary>
        public string equipment = "";
        /// <summary>The vehicle or gun was destroyed; the team cannot fight until it is replaced.</summary>
        public bool equipmentLost;
        public List<SoldierRecord> soldiers = new List<SoldierRecord>();

        /// <summary>Men present and fit to fight.</summary>
        [JsonIgnore]
        public int PresentCount
        {
            get
            {
                int count = 0;
                foreach (var soldier in soldiers)
                {
                    if (soldier.IsPresent)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>Men on the table of organisation (for replacements).</summary>
        [JsonIgnore] public int TableSize => CompanyTemplates.TableSize(this);

        /// <summary>Able to take part in a battle: someone present and, for heavy teams, its weapon intact.</summary>
        [JsonIgnore] public bool CanFight => PresentCount > 0 && !equipmentLost;
    }

    [Serializable]
    public sealed class PlatoonRecord
    {
        public string key = "";
        public PlatoonType type;
        /// <summary>Designation, e.g. "1. Zug".</summary>
        public string name = "";
        public List<TeamRecord> teams = new List<TeamRecord>();
    }

    [Serializable]
    public sealed class CompanyRecord
    {
        public string id = "";
        /// <summary>Designation, e.g. "9./IR 194".</summary>
        public string name = "";
        public string template = "";
        public List<PlatoonRecord> platoons = new List<PlatoonRecord>();

        [JsonIgnore]
        public IEnumerable<TeamRecord> Teams
        {
            get
            {
                foreach (var platoon in platoons)
                {
                    foreach (var team in platoon.teams)
                    {
                        yield return team;
                    }
                }
            }
        }

        [JsonIgnore]
        public int PresentCount
        {
            get
            {
                int count = 0;
                foreach (var team in Teams)
                {
                    count += team.PresentCount;
                }

                return count;
            }
        }
    }

    /// <summary>
    /// A regiment on the strategic map: one counter (D56). Its companies are the pool a
    /// battle draws from (D21: at most two thirds of them in one battle).
    /// </summary>
    [Serializable]
    public sealed class RegimentState
    {
        public string id = "";
        public Side side;
        public string name = "";
        public string division = "";
        /// <summary>District it stands in.</summary>
        public string district = "";
        /// <summary>District it stood in before this turn's move: where it falls back to after a failed attack.</summary>
        public string origin = "";
        public int experience = 50;
        /// <summary>0..100. Battles wear it down; rest restores it (D61).</summary>
        public float morale = 70f;
        /// <summary>Share of a full basic load of ammunition, 0..1 (D40, D61).</summary>
        public float ammo = 1f;
        /// <summary>Resting this turn (D61): it recovers faster and can take replacements.</summary>
        public bool resting;
        /// <summary>Set each supply phase: connected to one of its side's supply points (D61).</summary>
        public bool supplied = true;
        /// <summary>Battles fought since it last rested.</summary>
        public int battlesWithoutRest;
        public List<CompanyRecord> companies = new List<CompanyRecord>();

        // Losses so far, for the after-action summaries.
        public int killed;
        public int evacuated;
        public int captured;

        [JsonIgnore]
        public IEnumerable<TeamRecord> Teams
        {
            get
            {
                foreach (var company in companies)
                {
                    foreach (var team in company.Teams)
                    {
                        yield return team;
                    }
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<SoldierRecord> Soldiers
        {
            get
            {
                foreach (var team in Teams)
                {
                    foreach (var soldier in team.soldiers)
                    {
                        yield return soldier;
                    }
                }
            }
        }

        /// <summary>Men present and fit to fight.</summary>
        [JsonIgnore]
        public int PresentCount
        {
            get
            {
                int count = 0;
                foreach (var company in companies)
                {
                    count += company.PresentCount;
                }

                return count;
            }
        }

        /// <summary>No one left who could fight: the regiment is gone from the map.</summary>
        [JsonIgnore] public bool IsDestroyed => PresentCount == 0 && !HasWoundedToReturn;

        [JsonIgnore]
        bool HasWoundedToReturn
        {
            get
            {
                foreach (var soldier in Soldiers)
                {
                    if (soldier.status == SoldierStatus.Wounded)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    [Serializable]
    public sealed class DistrictState
    {
        public string id = "";
        public Side owner;
        /// <summary>Both sides still hold part of it after an undecided battle (D33); fighting goes on next turn.</summary>
        public bool contested;
        /// <summary>Side that attacked in the undecided battle; it is still the attacker next turn.</summary>
        public Side contestedAttacker;
    }
}
