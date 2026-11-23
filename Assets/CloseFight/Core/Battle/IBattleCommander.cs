using CloseFight.Core.Units;

namespace CloseFight.Core.Battle
{
    /// <summary>A computer commander for one side, stepped by the battle every couple of seconds.</summary>
    public interface IBattleCommander
    {
        Side Side { get; }
        void Think();

        /// <summary>Called once when the deployment phase opens: place teams before the shooting starts.</summary>
        void Deploy() { }
    }
}
