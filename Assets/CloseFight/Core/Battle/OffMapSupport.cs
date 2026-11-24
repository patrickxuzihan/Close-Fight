using System;
using System.Collections.Generic;
using CloseFight.Core.Map;
using CloseFight.Core.Units;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    public enum SupportRefusal : byte
    {
        None,
        UnknownAsset,
        /// <summary>Stukas fly by day only; night bombers by night only.</summary>
        WrongTimeOfDay,
        NotEnoughPoints,
        /// <summary>No officer of ours can see the target (D36).</summary>
        NoObserver,
        OffMap,
    }

    /// <summary>A called mission: when it arrives, where it aims, how many rounds are still to come.</summary>
    public sealed class SupportMission
    {
        public SupportAsset Asset { get; internal set; }
        public Side Side { get; internal set; }
        /// <summary>Where the officer asked for it.</summary>
        public float2 Target { get; internal set; }
        /// <summary>Where it will actually fall (the first-round error is already in it).</summary>
        public float2 AimPoint { get; internal set; }
        public float CalledAt { get; internal set; }
        public float ArrivesAt { get; internal set; }
        public int RoundsLeft { get; internal set; }
        public Soldier Observer { get; internal set; }
        internal float NextRoundAt;
        public bool Firing => RoundsLeft < Asset.Rounds;
    }

    /// <summary>
    /// Off-map support (M13, D36). Each side has support points and a list of
    /// assets for the scenario. Calling one needs an officer (a headquarters
    /// team member) who can see the target. The rounds come 1–3 minutes later,
    /// the first aim is off by more the further the officer is from the target,
    /// and every round scatters: shells do not care whose men they land on, so
    /// a strike close to one's own troops hits them too (why Chuikov's men
    /// fought "hugging" the Germans).
    /// </summary>
    public sealed class OffMapSupport
    {
        /// <summary>An officer can direct fire on what he sees within this distance.</summary>
        public const float ObserverRange = 600f;

        readonly BattleState _battle;
        readonly int[] _points = new int[2];
        readonly List<SupportAsset>[] _assets = { new List<SupportAsset>(), new List<SupportAsset>() };
        readonly List<SupportMission> _missions = new List<SupportMission>();

        public OffMapSupport(BattleState battle)
        {
            _battle = battle;
        }

        public int Points(Side side) => _points[(int)side];
        public IReadOnlyList<SupportAsset> Assets(Side side) => _assets[(int)side];
        public IReadOnlyList<SupportMission> Missions => _missions;

        /// <summary>Gives a side its points and the assets it may call.</summary>
        public void Configure(Side side, int points, IEnumerable<string> assetIds)
        {
            _points[(int)side] = math.max(0, points);
            _assets[(int)side].Clear();
            if (assetIds == null)
            {
                return;
            }

            foreach (var id in assetIds)
            {
                var asset = SupportAssets.Get(id);
                if (asset != null && asset.Side == side && !_assets[(int)side].Contains(asset))
                {
                    _assets[(int)side].Add(asset);
                }
            }
        }

        public bool Available(SupportAsset asset)
        {
            switch (asset.Time)
            {
                case SupportTime.DayOnly: return !_battle.IsNight;
                case SupportTime.NightOnly: return _battle.IsNight;
                default: return true;
            }
        }

        /// <summary>Whether <paramref name="side"/> could call <paramref name="asset"/> on <paramref name="target"/> now, and who would direct it.</summary>
        public SupportRefusal Check(Side side, SupportAsset asset, float2 target, out Soldier observer)
        {
            observer = null;
            if (asset == null || !_assets[(int)side].Contains(asset))
            {
                return SupportRefusal.UnknownAsset;
            }

            if (!Available(asset))
            {
                return SupportRefusal.WrongTimeOfDay;
            }

            if (_points[(int)side] < asset.Cost)
            {
                return SupportRefusal.NotEnoughPoints;
            }

            var grid = _battle.Map.Grid;
            if (!grid.InBounds(grid.WorldToCell(target)))
            {
                return SupportRefusal.OffMap;
            }

            observer = ObserverFor(side, target);
            return observer == null ? SupportRefusal.NoObserver : SupportRefusal.None;
        }

        /// <summary>The nearest officer (headquarters) or forward observer of the side who can see the target, or null.</summary>
        public Soldier ObserverFor(Side side, float2 target)
        {
            Soldier best = null;
            float bestDistance = ObserverRange;
            foreach (var team in _battle.Teams)
            {
                if (team.Side != side || (team.Type != TeamType.Headquarters && team.Type != TeamType.Observer))
                {
                    continue;
                }

                foreach (var member in team.Members)
                {
                    if (!member.IsActive || member.IsOutOfControl || member.IsMounted)
                    {
                        continue;
                    }

                    float d = math.distance(member.Position, target);
                    if (d >= bestDistance)
                    {
                        continue;
                    }

                    float eye = LineOfSight.EyeHeightAt(_battle.Map, member.Position, Posture.Kneeling);
                    if (LineOfSight.Check(_battle.Map, member.Position, eye, target, LineOfSight.EyeKneeling).Visible)
                    {
                        best = member;
                        bestDistance = d;
                    }
                }
            }

            return best;
        }

        /// <summary>Calls an asset on a target. Null (with the reason) if it cannot be called.</summary>
        public SupportMission Request(Side side, string assetId, float2 target, out SupportRefusal refusal)
        {
            var asset = SupportAssets.Get(assetId);
            refusal = Check(side, asset, target, out var observer);
            if (refusal != SupportRefusal.None)
            {
                return null;
            }

            float now = _battle.Clock.ElapsedSeconds;
            var random = _battle.Random;
            float error = math.distance(observer.Position, target) * asset.ErrorPerMeter * random.Range(0.3f, 1f);
            float angle = random.Range(0f, math.PI * 2f);
            var mission = new SupportMission
            {
                Asset = asset,
                Side = side,
                Target = target,
                AimPoint = target + new float2(math.cos(angle), math.sin(angle)) * error,
                CalledAt = now,
                ArrivesAt = now + random.Range(asset.DelayMin, asset.DelayMax),
                RoundsLeft = asset.Rounds,
                Observer = observer,
            };
            mission.NextRoundAt = mission.ArrivesAt;
            _points[(int)side] -= asset.Cost;
            _missions.Add(mission);
            return mission;
        }

        public void Step(float now)
        {
            for (int i = _missions.Count - 1; i >= 0; i--)
            {
                var mission = _missions[i];
                while (mission.RoundsLeft > 0 && now >= mission.NextRoundAt)
                {
                    float2 impact = mission.AimPoint + RandomInDisk(mission.Asset.Spread);
                    _battle.Gunnery.Blast(impact, mission.Asset.Round, now, mission.Observer);
                    mission.RoundsLeft--;
                    mission.NextRoundAt += mission.Asset.Interval;
                }

                if (mission.RoundsLeft == 0)
                {
                    _missions.RemoveAt(i);
                }
            }
        }

        float2 RandomInDisk(float radius)
        {
            float angle = _battle.Random.Range(0f, math.PI * 2f);
            float distance = radius * math.sqrt(_battle.Random.NextFloat());
            return new float2(math.cos(angle), math.sin(angle)) * distance;
        }
    }
}
