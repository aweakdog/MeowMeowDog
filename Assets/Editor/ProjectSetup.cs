using System.IO;
using System.Linq;
using MeowMeowDog.Core;
using MeowMeowDog.Level;
using MeowMeowDog.Net;
using MeowMeowDog.Player;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeowMeowDog.EditorTools
{
    /// <summary>
    /// 项目自动化初始化：首次打开工程时自动生成主场景和玩家 Prefab。
    /// 也可以从菜单 MeowMeowDog → Setup 手动重新生成。
    /// </summary>
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Run();
            };
        }

        [MenuItem("MeowMeowDog/Setup（重新生成场景和 Prefab）")]
        public static void Run()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Prefabs");

            var playerPrefab = CreatePlayerPrefab();
            CreateMainScene(playerPrefab);
            AddAlwaysIncludedShaders();

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MeowMeowDog] 初始化完成！直接点 Play 开始游戏。");
        }

        static GameObject CreatePlayerPrefab()
        {
            var go = new GameObject("Player");
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.7f, 0);
            cc.radius = 0.38f;
            cc.height = 1.4f;
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.4f;

            go.AddComponent<NetworkObject>();
            go.AddComponent<ClientNetworkTransform>();
            go.AddComponent<PlayerAvatar>();
            go.AddComponent<PlayerController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void CreateMainScene(GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- 相机 ----
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.60f, 0.82f, 0.96f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraRig>();
            camGo.transform.position = new Vector3(1.5f, 9.7f, -9.5f);

            // ---- 阳光 ----
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // ---- 环境光和远景雾（2.5D 景深感）----
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.32f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.95f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 90f;

            // ---- NetworkManager ----
            var nmGo = new GameObject("NetworkManager");
            var nm = nmGo.AddComponent<NetworkManager>();
            var utp = nmGo.AddComponent<UnityTransport>();
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = utp,
                PlayerPrefab = playerPrefab,
                EnableSceneManagement = false,
            };

            // ---- 游戏逻辑 ----
            var rootGo = new GameObject("GameRoot");
            rootGo.AddComponent<LevelBuilder>();
            rootGo.AddComponent<ConnectionUI>();
            rootGo.AddComponent<GameHud>();

            // ---- 关卡联机状态（场景内置 NetworkObject）----
            var lsGo = new GameObject("LevelState");
            lsGo.AddComponent<NetworkObject>();
            lsGo.AddComponent<LevelState>();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>把运行时用 Shader.Find 引用的着色器加入 Always Included，保证打包后正常。</summary>
        static void AddAlwaysIncludedShaders()
        {
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;

            foreach (var name in new[] { "Standard", "GUI/Text Shader" })
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                bool exists = Enumerable.Range(0, arr.arraySize)
                    .Any(i => arr.GetArrayElementAtIndex(i).objectReferenceValue == shader);
                if (!exists)
                {
                    arr.InsertArrayElementAtIndex(arr.arraySize);
                    arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
                }
            }
            so.ApplyModifiedProperties();
        }
    }
}
