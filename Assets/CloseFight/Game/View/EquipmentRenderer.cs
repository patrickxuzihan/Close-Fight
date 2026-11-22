using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Units;
using Unity.Mathematics;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Placeholder drawing for heavy equipment (M6): tanks as hull + turret +
    /// barrel boxes, anti-tank guns as a small carriage with a barrel, mortars
    /// as a tube disc, plus shell bursts. Replaced by Codex top-down art later
    /// (art-asset-list.md section 3: hull and turret as separate images).
    /// Owned by <see cref="UnitsView"/>.
    /// </summary>
    sealed class EquipmentRenderer
    {
        static readonly Color GermanPaint = new Color(0.36f, 0.38f, 0.38f);
        static readonly Color SovietPaint = new Color(0.33f, 0.40f, 0.24f);
        static readonly Color WreckPaint = new Color(0.14f, 0.12f, 0.11f);
        static readonly Color FlashColor = new Color(1f, 0.6f, 0.2f);
        static readonly Color PuffColor = new Color(0.85f, 0.85f, 0.82f);
        static readonly Color SelectedRing = new Color(1f, 0.85f, 0.2f);

        const float VehicleHeight = 0.35f;
        const float FlashSeconds = 0.8f;

        sealed class VehicleSprites
        {
            public Vehicle Vehicle;
            public Transform Hull;
            public SpriteRenderer HullSprite;
            public Transform Turret;
            public SpriteRenderer TurretSprite;
            public SpriteRenderer BarrelSprite;
            public SpriteRenderer Ring;
        }

        sealed class GunSprites
        {
            public HeavyWeapon Gun;
            public Transform Mount;
            public SpriteRenderer Carriage;
            public SpriteRenderer Barrel;
        }

        readonly List<VehicleSprites> _vehicles = new List<VehicleSprites>();
        readonly List<GunSprites> _guns = new List<GunSprites>();
        readonly List<SpriteRenderer> _flashPool = new List<SpriteRenderer>();
        readonly HashSet<int> _seenVehicles = new HashSet<int>();
        Transform _root;
        BattleState _battle;

        public void Build(Transform root, BattleState battle)
        {
            Clear();
            _root = root;
            _battle = battle;

            foreach (var vehicle in battle.Vehicles)
            {
                _vehicles.Add(CreateVehicle(vehicle));
            }

            foreach (var team in battle.Teams)
            {
                if (team.Gun != null && team.Gun.Vehicle == null)
                {
                    _guns.Add(CreateGun(team.Gun));
                }
            }
        }

        public void Clear()
        {
            _vehicles.Clear();
            _guns.Clear();
            _flashPool.Clear();
            _seenVehicles.Clear();
            _root = null;
            _battle = null;
        }

        GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (!Application.isPlaying)
            {
                go.hideFlags = HideFlags.DontSave;
            }

            return go;
        }

        SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var sr = NewChild(name, parent).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        VehicleSprites CreateVehicle(Vehicle vehicle)
        {
            var definition = vehicle.Definition;
            var paint = vehicle.Team.Side == Side.German ? GermanPaint : SovietPaint;

            var hull = NewChild("Vehicle " + vehicle.Team.Key, _root).transform;
            var hullSprite = NewSprite("Hull", hull, MarkerSprites.Box, 5);
            hullSprite.transform.localScale = new Vector3(definition.Width, definition.Length, 1f);
            hullSprite.color = paint;

            var ring = NewSprite("Selection", hull, MarkerSprites.Ring, 8);
            ring.transform.localScale = Vector3.one * (definition.Length * 1.25f);
            ring.color = SelectedRing;
            ring.enabled = false;

            // Turret sits a little behind the hull center; a casemate gun sits in the hull front.
            var turret = NewChild("Turret", hull).transform;
            turret.localPosition = definition.HasTurret ? new Vector3(0f, -definition.Length * 0.05f, 0f) : new Vector3(0f, definition.Length * 0.2f, 0f);
            SpriteRenderer turretSprite = null;
            if (definition.HasTurret)
            {
                turretSprite = NewSprite("TurretBody", turret, MarkerSprites.Box, 6);
                float size = definition.Width * 0.62f;
                turretSprite.transform.localScale = new Vector3(size, size * 1.1f, 1f);
                turretSprite.color = paint * 0.92f;
            }

            float barrelLength = definition.Length * (definition.HasTurret ? 0.55f : 0.35f);
            var barrel = NewSprite("Barrel", turret, MarkerSprites.Box, 7);
            barrel.transform.localScale = new Vector3(0.22f, barrelLength, 1f);
            barrel.transform.localPosition = new Vector3(0f, barrelLength * 0.5f, 0f);
            barrel.color = paint * 0.8f;

            return new VehicleSprites
            {
                Vehicle = vehicle, Hull = hull, HullSprite = hullSprite, Turret = turret,
                TurretSprite = turretSprite, BarrelSprite = barrel, Ring = ring,
            };
        }

        GunSprites CreateGun(HeavyWeapon gun)
        {
            var mount = NewChild("Gun " + gun.Team.Key, _root).transform;
            var paint = gun.Team.Side == Side.German ? GermanPaint : SovietPaint;
            bool mortar = gun.Definition.Indirect;

            var carriage = NewSprite("Carriage", mount, mortar ? MarkerSprites.Body : MarkerSprites.Box, 5);
            carriage.transform.localScale = mortar ? new Vector3(0.9f, 0.9f, 1f) : new Vector3(1.6f, 1.2f, 1f);
            carriage.color = paint;

            var barrel = NewSprite("Barrel", mount, MarkerSprites.Box, 6);
            float length = mortar ? 0.7f : 2.6f;
            barrel.transform.localScale = new Vector3(mortar ? 0.35f : 0.16f, length, 1f);
            barrel.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
            barrel.color = paint * 0.8f;

            return new GunSprites { Gun = gun, Mount = mount, Carriage = carriage, Barrel = barrel };
        }

        public void Sync(float alpha, bool fog, Side viewSide, HashSet<Team> selected)
        {
            if (_battle == null)
            {
                return;
            }

            foreach (var v in _vehicles)
            {
                SyncVehicle(v, alpha, fog, viewSide, selected);
            }

            foreach (var g in _guns)
            {
                SyncGun(g, alpha, fog, viewSide);
            }

            SyncFlashes();
        }

        void SyncVehicle(VehicleSprites sprites, float alpha, bool fog, Side viewSide, HashSet<Team> selected)
        {
            var vehicle = sprites.Vehicle;
            bool visible = !fog || _battle.Visibility.CanSee(viewSide, vehicle);
            if (visible)
            {
                _seenVehicles.Add(vehicle.Id);
            }
            else if (!vehicle.IsOperational && _seenVehicles.Contains(vehicle.Id))
            {
                visible = true; // a wreck seen once stays on the map
            }

            sprites.Hull.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            float2 p = math.lerp(vehicle.PreviousPosition, vehicle.Position, alpha);
            float hull = Mathf.LerpAngle(vehicle.PreviousHullHeading, vehicle.HullHeading, alpha);
            sprites.Hull.position = new Vector3(p.x, VehicleHeight, p.y);
            sprites.Hull.rotation = Quaternion.Euler(90f, hull, 0f);

            var gun = vehicle.Gun;
            float gunHeading = gun != null ? Mathf.LerpAngle(gun.PreviousHeading, gun.Heading, alpha) : hull;
            sprites.Turret.rotation = Quaternion.Euler(90f, gunHeading, 0f);

            var paint = vehicle.Team.Side == Side.German ? GermanPaint : SovietPaint;
            if (vehicle.Destroyed)
            {
                paint = WreckPaint;
            }
            else if (vehicle.Abandoned)
            {
                paint = Color.Lerp(paint, new Color(0.5f, 0.5f, 0.5f), 0.5f);
            }

            sprites.HullSprite.color = paint;
            if (sprites.TurretSprite != null)
            {
                sprites.TurretSprite.color = paint * 0.92f;
            }

            sprites.BarrelSprite.color = paint * 0.8f;
            sprites.Ring.enabled = selected.Contains(vehicle.Team) && vehicle.IsOperational;
        }

        void SyncGun(GunSprites sprites, float alpha, bool fog, Side viewSide)
        {
            var gun = sprites.Gun;
            bool crewAlive = gun.Team.ActiveCount > 0;
            bool visible = !fog || gun.Team.Side == viewSide;
            if (!visible)
            {
                // Seen when any of its crew is seen.
                foreach (var member in gun.Team.Members)
                {
                    if (member.IsActive && _battle.Visibility.CanSee(viewSide, member))
                    {
                        visible = true;
                        break;
                    }
                }
            }

            sprites.Mount.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            float2 p = gun.Position;
            sprites.Mount.position = new Vector3(p.x, 0.12f, p.y);
            float heading = Mathf.LerpAngle(gun.PreviousHeading, gun.Heading, alpha);
            sprites.Mount.rotation = Quaternion.Euler(90f, heading, 0f);
            var paint = gun.Team.Side == Side.German ? GermanPaint : SovietPaint;
            if (!crewAlive || gun.Destroyed)
            {
                paint = Color.Lerp(paint, WreckPaint, 0.6f);
            }

            sprites.Carriage.color = paint;
            sprites.Barrel.color = paint * 0.8f;
        }

        void SyncFlashes()
        {
            float now = _battle.Clock.ElapsedSeconds;
            int used = 0;
            foreach (var burst in _battle.Gunnery.RecentExplosions)
            {
                float age = now - burst.Time;
                if (age < 0f || age > FlashSeconds)
                {
                    continue;
                }

                if (used == _flashPool.Count)
                {
                    var sr = NewSprite("Burst", _root, MarkerSprites.SoftDisc, 9);
                    sr.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    _flashPool.Add(sr);
                }

                var flash = _flashPool[used++];
                float t = age / FlashSeconds;
                float size = math.max(2.5f, burst.Radius * 2.5f) * math.lerp(0.6f, 1.4f, t);
                flash.enabled = true;
                flash.transform.position = new Vector3(burst.Position.x, 20f, burst.Position.y);
                flash.transform.localScale = new Vector3(size, size, 1f);
                var color = burst.Smoke ? PuffColor : Color.Lerp(FlashColor, new Color(0.35f, 0.3f, 0.25f), t);
                color.a = 1f - t;
                flash.color = color;
            }

            for (int i = used; i < _flashPool.Count; i++)
            {
                _flashPool[i].enabled = false;
            }
        }
    }
}
