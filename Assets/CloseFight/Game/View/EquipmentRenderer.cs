using System.Collections.Generic;
using CloseFight.Core.Battle;
using CloseFight.Core.Units;
using Unity.Mathematics;
using UnityEngine;

namespace CloseFight.Game.View
{
    /// <summary>
    /// Draws heavy equipment: tanks as hull + turret, guns and mortars as one
    /// image, plus shell bursts. Uses the Codex top-down art (<see cref="EquipmentArt"/>)
    /// with a soft drop shadow cast the same way as the buildings' shadows; an item
    /// without art yet falls back to placeholder boxes.
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
        static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.42f);
        /// <summary>Tint over painted art: wrecks burnt dark, abandoned vehicles a little grey.</summary>
        static readonly Color WreckTint = new Color(0.32f, 0.29f, 0.27f);
        static readonly Color AbandonedTint = new Color(0.78f, 0.78f, 0.78f);

        /// <summary>Drop shadow length per meter of height (sun at about 50° elevation).</summary>
        const float VehicleShadow = 0.9f;
        const float TurretShadow = 0.5f;
        const float GunShadow = 0.3f;

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
            public SpriteRenderer HullShadow;
            public SpriteRenderer TurretShadow;
            public bool Painted;
        }

        sealed class GunSprites
        {
            public HeavyWeapon Gun;
            public Transform Mount;
            public SpriteRenderer Carriage;
            public SpriteRenderer Barrel;
            public SpriteRenderer Shadow;
            public bool Painted;
        }

        readonly List<VehicleSprites> _vehicles = new List<VehicleSprites>();
        readonly List<GunSprites> _guns = new List<GunSprites>();
        readonly List<SpriteRenderer> _flashPool = new List<SpriteRenderer>();
        readonly HashSet<int> _seenVehicles = new HashSet<int>();
        Transform _root;
        BattleState _battle;
        /// <summary>Ground direction shadows fall in (x, z), from the scene's sun.</summary>
        Vector3 _shadowDirection = new Vector3(-0.57f, 0f, 0.82f);

        public void Build(Transform root, BattleState battle)
        {
            Clear();
            _root = root;
            _battle = battle;
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                var d = sun.transform.forward;
                var flat = new Vector3(d.x, 0f, d.z);
                if (flat.sqrMagnitude > 1e-4f)
                {
                    _shadowDirection = flat.normalized;
                }
            }

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

        /// <summary>A sprite of Codex art, on its own material (see <see cref="EquipmentArt.MaterialFor"/>).</summary>
        SpriteRenderer NewArtSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var sr = NewSprite(name, parent, sprite, order);
            sr.sharedMaterial = EquipmentArt.MaterialFor(sprite, sr.sharedMaterial);
            return sr;
        }

        VehicleSprites CreateVehicle(Vehicle vehicle)
        {
            var definition = vehicle.Definition;
            var hull = NewChild("Vehicle " + vehicle.Team.Key, _root).transform;
            var hullArt = EquipmentArt.Hull(definition.Id);
            var turretArt = definition.HasTurret ? EquipmentArt.Turret(definition.Id) : null;
            var sprites = hullArt != null && (!definition.HasTurret || turretArt != null)
                ? CreatePaintedVehicle(hull, definition, hullArt, turretArt)
                : CreatePlaceholderVehicle(hull, vehicle);

            var ring = NewSprite("Selection", hull, MarkerSprites.Ring, 8);
            ring.transform.localScale = Vector3.one * (definition.Length * 1.25f);
            ring.color = SelectedRing;
            ring.enabled = false;

            sprites.Vehicle = vehicle;
            sprites.Hull = hull;
            sprites.Ring = ring;
            return sprites;
        }

        VehicleSprites CreatePaintedVehicle(Transform hull, VehicleDefinition definition, Sprite hullArt, Sprite turretArt)
        {
            var sprites = new VehicleSprites { Painted = true };
            sprites.HullShadow = NewArtSprite("HullShadow", hull, hullArt, 3);
            sprites.HullShadow.color = ShadowColor;
            sprites.HullSprite = NewArtSprite("Hull", hull, hullArt, 4);

            // A casemate gun is part of the hull image; the turret turns around its ring.
            var turret = NewChild("Turret", hull).transform;
            var ringOffset = EquipmentArt.TurretRing(definition.Id) ?? Vector2.zero;
            turret.localPosition = new Vector3(ringOffset.x, ringOffset.y, 0f);
            sprites.Turret = turret;
            if (turretArt != null)
            {
                sprites.TurretShadow = NewArtSprite("TurretShadow", turret, turretArt, 5);
                sprites.TurretShadow.color = ShadowColor;
                sprites.TurretSprite = NewArtSprite("TurretBody", turret, turretArt, 6);
            }

            return sprites;
        }

        VehicleSprites CreatePlaceholderVehicle(Transform hull, Vehicle vehicle)
        {
            var definition = vehicle.Definition;
            var paint = vehicle.Team.Side == Side.German ? GermanPaint : SovietPaint;
            var hullSprite = NewSprite("Hull", hull, MarkerSprites.Box, 5);
            hullSprite.transform.localScale = new Vector3(definition.Width, definition.Length, 1f);
            hullSprite.color = paint;

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

            return new VehicleSprites { HullSprite = hullSprite, Turret = turret, TurretSprite = turretSprite, BarrelSprite = barrel };
        }

        GunSprites CreateGun(HeavyWeapon gun)
        {
            var mount = NewChild("Gun " + gun.Team.Key, _root).transform;
            var art = EquipmentArt.Gun(gun.Definition.Id);
            if (art != null)
            {
                var shadow = NewArtSprite("Shadow", mount, art, 4);
                shadow.color = ShadowColor;
                var body = NewArtSprite("Gun", mount, art, 5);
                return new GunSprites { Gun = gun, Mount = mount, Carriage = body, Shadow = shadow, Painted = true };
            }

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

            sprites.Ring.enabled = selected.Contains(vehicle.Team) && vehicle.IsOperational;
            if (sprites.Painted)
            {
                var tint = vehicle.Destroyed ? WreckTint : vehicle.Abandoned ? AbandonedTint : Color.white;
                sprites.HullSprite.color = tint;
                sprites.HullShadow.transform.position = sprites.Hull.position + _shadowDirection * VehicleShadow - Vector3.up * 0.05f;
                if (sprites.TurretSprite != null)
                {
                    sprites.TurretSprite.color = tint;
                    sprites.TurretShadow.transform.position = sprites.Turret.position + _shadowDirection * TurretShadow - Vector3.up * 0.03f;
                }

                return;
            }

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
            if (sprites.Painted)
            {
                sprites.Carriage.color = gun.Destroyed ? WreckTint : !crewAlive ? AbandonedTint : Color.white;
                sprites.Shadow.transform.position = sprites.Mount.position + _shadowDirection * GunShadow - Vector3.up * 0.03f;
                return;
            }

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
