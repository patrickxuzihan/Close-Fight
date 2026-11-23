using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.AI
{
    /// <summary>An enemy group the side has seen or heard, as it was last observed.</summary>
    public sealed class Contact
    {
        public float2 Position;
        public float SeenAt;
        /// <summary>Most soldiers seen together at once.</summary>
        public int Count;
        public bool MachineGun;
        public bool Vehicle;
        public bool AntiTankGun;
        /// <summary>Only heard firing, never seen.</summary>
        public bool Heard;
        /// <summary>Grouping key for what is seen together. Never read for knowledge (D17).</summary>
        internal object Key;

        /// <summary>How much fire it can put down: an MG counts double, a tank or gun more.</summary>
        public float Weight => Vehicle ? 6f : AntiTankGun ? 3f : math.max(1, Count) * (MachineGun ? 2f : 1f) * (Heard ? 0.6f : 1f);
    }

    /// <summary>
    /// What a commander remembers of the enemy (D17: only what its soldiers saw
    /// or heard). Seen groups are kept for a few minutes at their last position;
    /// gunfire heard but not seen becomes a vaguer contact for a minute. A
    /// contact is dropped once our own men stand on the spot and find nobody.
    /// </summary>
    public sealed class ContactMemory
    {
        public const float SeenMemory = 180f;
        public const float HeardMemory = 60f;

        readonly AiView _view;
        readonly List<Contact> _contacts = new List<Contact>();

        public ContactMemory(AiView view)
        {
            _view = view;
        }

        public IReadOnlyList<Contact> All => _contacts;

        public void Update()
        {
            float now = _view.Time;

            // Seen soldiers, grouped by the group they move with.
            var groups = new Dictionary<object, (float2 sum, int count, bool mg, bool atGun)>();
            foreach (var enemy in _view.VisibleEnemies)
            {
                groups.TryGetValue(enemy.Team, out var g);
                bool mg = g.mg || (enemy.Weapon != null && enemy.Weapon.Class == WeaponClass.MachineGun);
                // A towed anti-tank gun is plain to see next to its crew.
                bool atGun = g.atGun || (enemy.Team.Gun != null && enemy.Team.Vehicle == null && !enemy.Team.Gun.Definition.Indirect);
                groups[enemy.Team] = (g.sum + enemy.Position, g.count + 1, mg, atGun);
            }

            foreach (var pair in groups)
            {
                var (sum, count, mg, atGun) = pair.Value;
                var contact = Find(pair.Key) ?? Add(pair.Key);
                contact.Position = sum / count;
                contact.SeenAt = now;
                contact.Count = math.max(contact.Count, count);
                contact.MachineGun |= mg;
                contact.AntiTankGun |= atGun;
                contact.Heard = false;
            }

            foreach (var vehicle in _view.VisibleEnemyVehicles)
            {
                var contact = Find(vehicle.Team) ?? Add(vehicle.Team);
                contact.Position = vehicle.Position;
                contact.SeenAt = now;
                contact.Vehicle = true;
                contact.Heard = false;
                contact.Count = math.max(contact.Count, 1);
            }

            // Gunfire heard but not seen: a vague contact, merged with any nearby.
            foreach (var marker in _view.HeardEnemies)
            {
                if (now - marker.CreatedAt > HeardMemory || NearSeen(marker.Position, 30f, now))
                {
                    continue;
                }

                Contact near = null;
                foreach (var c in _contacts)
                {
                    if (c.Heard && math.distance(c.Position, marker.Position) < 25f)
                    {
                        near = c;
                        break;
                    }
                }

                if (near == null)
                {
                    near = Add(null);
                    near.Heard = true;
                    near.Count = 1;
                    near.Position = marker.Position;
                }

                near.SeenAt = math.max(near.SeenAt, marker.CreatedAt);
            }

            // Forget old contacts, and those where our men now stand and see nobody.
            for (int i = _contacts.Count - 1; i >= 0; i--)
            {
                var c = _contacts[i];
                float age = now - c.SeenAt;
                bool expired = age > (c.Heard ? HeardMemory : SeenMemory);
                if (expired || (age > 10f && OwnSoldierWithin(c.Position, 12f)))
                {
                    _contacts.RemoveAt(i);
                }
            }
        }

        /// <summary>Remembered threats for a <see cref="DangerMap"/>, fading with age.</summary>
        public IEnumerable<(float2, float)> Threats()
        {
            float now = _view.Time;
            foreach (var c in _contacts)
            {
                float memory = c.Heard ? HeardMemory : SeenMemory;
                float fade = 1f - 0.5f * math.saturate((now - c.SeenAt) / memory);
                yield return (c.Position, c.Weight * fade);
            }
        }

        /// <summary>Total remembered strength within <paramref name="radius"/> of a point.</summary>
        public float StrengthNear(float2 point, float radius)
        {
            float total = 0f;
            foreach (var c in _contacts)
            {
                if (math.distance(c.Position, point) <= radius)
                {
                    total += c.Weight;
                }
            }

            return total;
        }

        Contact Find(object key)
        {
            foreach (var c in _contacts)
            {
                if (c.Key != null && ReferenceEquals(c.Key, key))
                {
                    return c;
                }
            }

            return null;
        }

        Contact Add(object key)
        {
            var c = new Contact { Key = key };
            _contacts.Add(c);
            return c;
        }

        bool NearSeen(float2 point, float radius, float now)
        {
            foreach (var c in _contacts)
            {
                if (!c.Heard && now - c.SeenAt < 30f && math.distance(c.Position, point) < radius)
                {
                    return true;
                }
            }

            return false;
        }

        bool OwnSoldierWithin(float2 point, float radius)
        {
            foreach (var team in _view.OwnTeams)
            {
                foreach (var member in team.Members)
                {
                    if (member.IsActive && math.distance(member.Position, point) < radius)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
