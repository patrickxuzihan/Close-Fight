using System.Collections.Generic;
using Unity.Mathematics;

namespace CloseFight.Core.Battle
{
    /// <summary>One smoke cloud: grows after it lands, hangs, then thins out.</summary>
    public sealed class SmokeCloud
    {
        public const float GrowSeconds = 4f;
        public const float HoldSeconds = 50f;
        public const float FadeSeconds = 10f;
        public const float FullRadius = 8f;

        public float2 Center { get; }
        public float CreatedAt { get; }

        public SmokeCloud(float2 center, float createdAt)
        {
            Center = center;
            CreatedAt = createdAt;
        }

        public float Age(float now) => now - CreatedAt;
        public bool Expired(float now) => Age(now) >= GrowSeconds + HoldSeconds + FadeSeconds;

        public float Radius(float now)
        {
            float age = Age(now);
            return age < GrowSeconds ? math.lerp(2f, FullRadius, age / GrowSeconds) : FullRadius;
        }

        /// <summary>0..1; sight is blocked only while the cloud is dense enough.</summary>
        public float Density(float now)
        {
            float age = Age(now);
            if (age < GrowSeconds + HoldSeconds)
            {
                return 1f;
            }

            return math.saturate(1f - (age - GrowSeconds - HoldSeconds) / FadeSeconds);
        }
    }

    /// <summary>Smoke screens (D31 Smoke order). Dense smoke blocks line of sight through it.</summary>
    public sealed class SmokeField
    {
        const float BlockingDensity = 0.35f;
        /// <summary>Someone standing inside a cloud still sees this far.</summary>
        const float SeeThroughInside = 3f;

        readonly List<SmokeCloud> _clouds = new List<SmokeCloud>();

        public IReadOnlyList<SmokeCloud> Clouds => _clouds;

        public void Add(float2 center, float now) => _clouds.Add(new SmokeCloud(center, now));

        public void Expire(float now) => _clouds.RemoveAll(c => c.Expired(now));

        /// <summary>True when dense smoke lies between the two points.</summary>
        public bool Blocks(float2 from, float2 to, float now)
        {
            foreach (var cloud in _clouds)
            {
                if (cloud.Density(now) < BlockingDensity)
                {
                    continue;
                }

                float radius = cloud.Radius(now) * 0.9f;
                float2 segment = to - from;
                float lengthSq = math.lengthsq(segment);
                float t = lengthSq > 1e-6f ? math.saturate(math.dot(cloud.Center - from, segment) / lengthSq) : 0f;
                float2 closest = from + segment * t;
                if (math.distance(closest, cloud.Center) > radius)
                {
                    continue;
                }

                // Through the cloud: blocked, unless both ends are close together inside it.
                if (math.length(segment) <= SeeThroughInside)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
