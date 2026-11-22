using System;
using System.IO;
using CloseFight.Game.Battle;
using CloseFight.Game.CameraControl;
using CloseFight.Game.Controls;
using CloseFight.Game.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CloseFight.EditorTools
{
    /// <summary>
    /// Generates the battle scene from code, so the scene setup is reviewable and
    /// reproducible. Menu: Close Fight > Setup > Rebuild Battle Scene.
    /// Batch mode: -executeMethod CloseFight.EditorTools.BattleSceneSetup.CreateBattleScene
    /// </summary>
    public static class BattleSceneSetup
    {
        public const string ScenePath = "Assets/CloseFight/Scenes/Battle.unity";
        const string MaterialFolder = "Assets/CloseFight/Art/Materials";
        const string TestMapPath = "Assets/CloseFight/Data/Maps/test_block.json";
        const string TestScenarioPath = "Assets/CloseFight/Data/Scenarios/test_block_skirmish.json";
        const string LocalizationPath = "Assets/CloseFight/Data/Localization/strings.json";

        static TextAsset LoadText(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Data file not found: " + path);
            }

            return asset;
        }

        /// <summary>Unlit, vertex-colored material for route lines.</summary>
        static Material GetOrCreateLineMaterial()
        {
            string path = $"{MaterialFolder}/Line.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        [MenuItem("Close Fight/Setup/Rebuild Battle Scene")]
        public static void CreateBattleScene()
        {
            EnsureFolder("Assets/CloseFight/Scenes");
            EnsureFolder(MaterialFolder);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                throw new InvalidOperationException("URP Lit shader not found.");
            }

            var groundMaterial = GetOrCreateMaterial("Ground", lit, 0.05f);
            var buildingMaterial = GetOrCreateMaterial("Building", lit, 0.1f);
            var wallMaterial = GetOrCreateMaterial("Wall", lit, 0.1f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera: straight down, orthographic (D23).
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(80f, 300f, 60f), Quaternion.Euler(90f, 0f, 0f));
            var controller = cameraObject.AddComponent<TopDownCameraController>();

            // Sun: low enough that buildings cast readable shadows from above.
            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sunObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.48f);
            RenderSettings.sun = sun;

            var mapView = new GameObject("Map").AddComponent<MapView>();
            Assign(mapView, "groundMaterial", groundMaterial);
            Assign(mapView, "buildingMaterial", buildingMaterial);
            Assign(mapView, "wallMaterial", wallMaterial);

            var unitsView = new GameObject("Units").AddComponent<UnitsView>();
            Assign(unitsView, "lineMaterial", GetOrCreateLineMaterial());

            var battleObject = new GameObject("Battle");
            var bootstrap = battleObject.AddComponent<BattleBootstrap>();
            var input = battleObject.AddComponent<BattleInput>();
            Assign(input, "worldCamera", camera);
            Assign(input, "unitsView", unitsView);
            Assign(input, "cameraController", controller);

            Assign(bootstrap, "mapJson", LoadText(TestMapPath));
            Assign(bootstrap, "scenarioJson", LoadText(TestScenarioPath));
            Assign(bootstrap, "localizationJson", LoadText(LocalizationPath));
            Assign(bootstrap, "mapView", mapView);
            Assign(bootstrap, "unitsView", unitsView);
            Assign(bootstrap, "battleInput", input);
            Assign(bootstrap, "cameraController", controller);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            ConfigureShadows();
            AssetDatabase.SaveAssets();
            Debug.Log("Close Fight: battle scene rebuilt at " + ScenePath);
        }

        /// <summary>
        /// The camera sits 300 m above the ground, so the default shadow distance
        /// (about 50 m) would cut off every shadow. One cascade is enough for a
        /// top-down orthographic view.
        /// </summary>
        static void ConfigureShadows()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null)
                {
                    continue;
                }

                asset.shadowDistance = 450f;
                asset.shadowCascadeCount = 1;

                var serialized = new SerializedObject(asset);
                var resolution = serialized.FindProperty("m_MainLightShadowmapResolution");
                if (resolution != null)
                {
                    resolution.intValue = 4096;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(asset);
            }
        }

        static Material GetOrCreateMaterial(string name, Shader shader, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Assign(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name}.{field} not found.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
