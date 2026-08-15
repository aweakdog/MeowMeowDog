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
            ConfigurePlayerSettings();

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
            // EnableSceneManagement 必须开：场景内置的 NetworkObject（LevelState）
            // 依赖 NGO 场景同步才会在客户端 spawn（冒烟测试验证过关闭时不同步）
            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = utp,
                PlayerPrefab = playerPrefab,
                EnableSceneManagement = true,
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

        /// <summary>窗口化启动 + 可调大小，游戏里也有 Esc 暂停菜单可退出。</summary>
        static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "MeowMeowDog";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
        }

        /// <summary>
        /// 把运行时用 Shader.Find 引用的 Standard 加入 Always Included，保证打包后不丢材质。
        /// 注意：不能加 "GUI/Text Shader" 之类 HideFlags.DontSave 的编辑器内部着色器，
        /// 会导致打包时写 unity_builtin_extra 失败（它们本来就随播放器内置，无需显式包含）。
        /// </summary>
        static void AddAlwaysIncludedShaders()
        {
            var gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) return;

            // 清除会破坏打包的 DontSaveInBuild 着色器条目（修复历史写入）
            for (int i = arr.arraySize - 1; i >= 0; i--)
            {
                var obj = arr.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null && (obj.hideFlags & HideFlags.DontSaveInBuild) != 0)
                {
                    Debug.Log($"[MMDog] 从 Always Included Shaders 移除不可打包的 {obj.name}");
                    arr.DeleteArrayElementAtIndex(i);
                }
            }

            var standard = Shader.Find("Standard");
            if (standard != null && !Enumerable.Range(0, arr.arraySize)
                    .Any(i => arr.GetArrayElementAtIndex(i).objectReferenceValue == standard))
            {
                arr.InsertArrayElementAtIndex(arr.arraySize);
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = standard;
            }
            so.ApplyModifiedProperties();
        }
    }
}
