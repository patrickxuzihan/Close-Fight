using CloseFight.Core.Battle;
using CloseFight.Core.Map;
using CloseFight.Game.CameraControl;
using CloseFight.Game.Controls;
using CloseFight.Game.UI;
using CloseFight.Game.View;
using UnityEngine;

namespace CloseFight.Game.Battle
{
    /// <summary>
    /// Entry point of the battle scene: loads the map and scenario JSON, builds
    /// the battle state and its views, frames the camera, and drives the
    /// simulation clock in play mode.
    /// In edit mode it builds a preview (map and starting positions, not saved
    /// into the scene), so the battlefield is visible without pressing Play.
    /// </summary>
    [ExecuteAlways]
    public sealed class BattleBootstrap : MonoBehaviour
    {
        [SerializeField] TextAsset mapJson;
        [SerializeField] TextAsset scenarioJson;
        [SerializeField] TextAsset localizationJson;
        [SerializeField] MapView mapView;
        [SerializeField] UnitsView unitsView;
        [SerializeField] BattleInput battleInput;
        [SerializeField] TopDownCameraController cameraController;

        public TacticalMap Map { get; private set; }
        public BattleState Battle { get; private set; }
        public ScenarioDefinition Scenario { get; private set; }
        public UnitsView UnitsView => unitsView;

        void OnEnable()
        {
            if (!Application.isPlaying)
            {
                Build();
            }
        }

        void OnDisable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (unitsView != null)
            {
                unitsView.ClearView();
            }

            if (mapView != null)
            {
                mapView.ClearView();
            }
        }

        void Start()
        {
            if (Application.isPlaying)
            {
                Build();
            }
        }

        void Update()
        {
            if (!Application.isPlaying || Battle == null)
            {
                return;
            }

            Battle.Update(Time.unscaledDeltaTime);
            unitsView.Sync(Battle.Clock.Alpha);
        }

        /// <summary>Builds everything. Also callable in edit mode (used by the screenshot tool).</summary>
        public BattleState Build()
        {
            if (mapJson == null || mapView == null || cameraController == null)
            {
                // While the scene is being set up in the editor, references arrive later.
                if (Application.isPlaying)
                {
                    Debug.LogError("BattleBootstrap: map, map view or camera controller is not assigned.", this);
                }

                return null;
            }

            Loc.Load(localizationJson);

            var definition = MapSerializer.FromJson(mapJson.text);
            Map = MapBuilder.Build(definition);
            mapView.Build(Map);
            cameraController.Configure(new Rect(0f, 0f, definition.width, definition.height));

            Battle = null;
            if (scenarioJson != null && unitsView != null)
            {
                Scenario = ScenarioLoader.FromJson(scenarioJson.text);
                if (!string.IsNullOrEmpty(Scenario.mapId) && Scenario.mapId != definition.id)
                {
                    Debug.LogWarning($"Scenario '{Scenario.id}' is meant for map '{Scenario.mapId}', but '{definition.id}' is loaded.", this);
                }

                Battle = ScenarioLoader.CreateBattle(Scenario, Map);
                unitsView.Build(Battle);

                if (Application.isPlaying)
                {
                    unitsView.SetViewSide(Scenario.playerSide);
                    if (battleInput != null)
                    {
                        battleInput.Bind(Battle, Scenario.playerSide);
                    }
                }
            }

            Debug.Log($"Battle map '{definition.id}' loaded: {Map.Grid.Width}x{Map.Grid.Height} cells, {Map.Buildings.Count} buildings, " +
                      $"{(Battle == null ? 0 : Battle.Teams.Count)} teams.");
            return Battle;
        }
    }
}
