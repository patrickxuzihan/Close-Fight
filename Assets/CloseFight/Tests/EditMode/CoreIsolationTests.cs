using System.Linq;
using CloseFight.Core.Map;
using NUnit.Framework;

namespace CloseFight.Tests
{
    /// <summary>
    /// The simulation core must stay independent of the engine, so it can be tested
    /// headless and moved into Jobs/Burst. Since Unity 6.6, Unity.Mathematics lives
    /// in the engine module UnityEngine.MathematicsModule, so Core's asmdef has to allow
    /// engine references. This test enforces that Mathematics is the only engine
    /// assembly Core actually uses.
    /// </summary>
    public class CoreIsolationTests
    {
        static readonly string[] AllowedEngineAssemblies =
        {
            "UnityEngine.MathematicsModule",
        };

        [Test]
        public void CoreUsesNoEngineAssemblyExceptMathematics()
        {
            var core = typeof(TacticalMap).Assembly;
            var engineReferences = core.GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(name => name.StartsWith("UnityEngine") || name.StartsWith("UnityEditor"))
                .Where(name => !AllowedEngineAssemblies.Contains(name))
                .ToArray();

            Assert.IsEmpty(engineReferences,
                "CloseFight.Core must not use the engine. Offending references: " + string.Join(", ", engineReferences));
        }
    }
}
