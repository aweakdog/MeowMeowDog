using System.Collections.Generic;
using UnityEngine;

namespace MeowMeowDog.Level
{
    /// <summary>
    /// 关卡一「回家的路」：用代码在所有端上确定性地生成静态场景。
    /// 流程：出发 → 狗狗推箱子开门 → 猫猫二段跳过沟拉杆放桥 → 一起变鱼游过池塘 → 双人同踩机关 → 终点拱门。
    /// 只有玩家和关卡状态走网络同步，静态几何体各端本地生成，节省带宽。
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public static LevelBuilder Instance { get; private set; }

        // ---- 供其它系统读取的关卡数据 ----
        public Vector3[] Checkpoints { get; private set; }
        public Bounds WaterBounds { get; private set; }
        public Bounds BoxPlateBounds { get; private set; }
        public Bounds DualPlateA { get; private set; }
        public Bounds DualPlateB { get; private set; }
        public Bounds GoalBounds { get; private set; }
        public Vector3 LeverPos { get; private set; }
        public Vector3 BoxStartPos { get; private set; }

        public Transform Door1;       // 推箱门（向下滑开）
        public Transform Bridge;      // 拉杆桥（横向滑出）
        public Transform Gate;        // 双人门（向上滑开）
        public Transform LeverHandle; // 拉杆手柄
        public Transform Box;         // 可推的箱子
        public Transform GoalSpinner; // 终点旋转的爱心方块
        public MeshRenderer PlateBoxMr, PlateAMr, PlateBMr;

        static readonly Color Grass = new(0.48f, 0.71f, 0.38f);
        static readonly Color Dirt = new(0.55f, 0.42f, 0.30f);
        static readonly Color Stone = new(0.62f, 0.62f, 0.66f);
        static readonly Color Wood = new(0.72f, 0.52f, 0.30f);
        static readonly Color WaterC = new(0.25f, 0.55f, 0.95f, 0.45f);
        public static readonly Color PlateRed = new(0.9f, 0.35f, 0.3f);
        public static readonly Color PlateBlue = new(0.3f, 0.5f, 0.95f);
        static readonly Color Gold = new(1f, 0.8f, 0.25f);
        static readonly Color Pink = new(1f, 0.55f, 0.7f);

        Transform _root;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            _root = new GameObject("Level1").transform;

            Checkpoints = new[]
            {
                new Vector3(1.5f, 1.2f, 0),   // 出生点
                new Vector3(25.2f, 1.2f, 0),  // 过了推箱门
                new Vector3(33f, 1.2f, 0),    // 过了沟
                new Vector3(59.5f, 1.2f, 0),  // 过了池塘
            };

            // ===== 第一段：出发平台 + 推箱子谜题 (x -2..26) =====
            Cube("GroundA", new Vector3(12, -0.5f, 0), new Vector3(28, 1, 12), Grass);
            BoxStartPos = new Vector3(16, 0.65f, 0);
            Box = Cube("PushBox", BoxStartPos, new Vector3(1.2f, 1.2f, 1.2f), Wood).transform;

            PlateBoxMr = Cube("PlateBox", new Vector3(20, 0.08f, 2.5f), new Vector3(1.7f, 0.16f, 1.7f), PlateRed).GetComponent<MeshRenderer>();
            BoxPlateBounds = new Bounds(new Vector3(20, 0.7f, 2.5f), new Vector3(1.7f, 1.6f, 1.7f));

            Cube("Wall1L", new Vector3(24, 1.5f, -3.6f), new Vector3(1, 3, 4.8f), Stone);
            Cube("Wall1R", new Vector3(24, 1.5f, 3.6f), new Vector3(1, 3, 4.8f), Stone);
            Door1 = Cube("Door1", new Vector3(24, 1.5f, 0), new Vector3(1, 3, 2.4f), Dirt).transform;

            // ===== 第二段：沟 + 猫猫拉杆桥 (x 26..44) =====
            Cube("GroundB", new Vector3(38, -0.5f, 0), new Vector3(12, 1, 12), Grass);
            // 桥收起时藏在 B 平台下方，放出时滑到沟上
            Bridge = Cube("Bridge", new Vector3(38, -1.3f, 0), new Vector3(6.4f, 0.4f, 3f), Wood).transform;

            LeverPos = new Vector3(34.5f, 0.5f, 3.5f);
            Cube("LeverBase", LeverPos + new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), Stone);
            LeverHandle = Cube("LeverHandle", LeverPos + new Vector3(0, 0.9f, 0), new Vector3(0.12f, 0.9f, 0.12f), PlateRed).transform;
            LeverHandle.localEulerAngles = new Vector3(0, 0, 35);

            // ===== 第三段：池塘（变鱼游泳）(x 44..58) =====
            Cube("BasinFloor", new Vector3(51, -4.5f, 0), new Vector3(14, 1, 12), Dirt);
            Cube("BasinL", new Vector3(51, -1.5f, -5.5f), new Vector3(14, 5, 1), Dirt);
            Cube("BasinR", new Vector3(51, -1.5f, 5.5f), new Vector3(14, 5, 1), Dirt);
            Cube("BasinEntry", new Vector3(44.2f, -2f, 0), new Vector3(1.6f, 4, 12), Dirt);
            // 出口台阶
            Cube("Step1", new Vector3(56.5f, -3.2f, 0), new Vector3(3, 1.6f, 4), Stone);
            Cube("Step2", new Vector3(57.5f, -2.2f, 0), new Vector3(2, 1.6f, 4), Stone);
            Cube("Step3", new Vector3(58.2f, -1.1f, 0), new Vector3(1.6f, 1.8f, 4), Stone);
            // 封住出口平台下方的缝隙，防止掉进虚空
            Cube("ExitWallL", new Vector3(58.5f, -2f, -4f), new Vector3(1, 4, 4), Dirt);
            Cube("ExitWallR", new Vector3(58.5f, -2f, 4f), new Vector3(1, 4, 4), Dirt);
            Cube("ExitPlug", new Vector3(58.5f, -3.3f, 0), new Vector3(1, 1.4f, 4), Stone);

            WaterBounds = new Bounds(new Vector3(51, -2.1f, 0), new Vector3(14, 3.8f, 11));
            var water = Cube("Water", WaterBounds.center, WaterBounds.size, WaterC, false);
            MakeTransparent(water.GetComponent<MeshRenderer>().material);

            // 水下小拱门（游过去很好玩）
            Cube("RingT", new Vector3(51, -1.2f, 0), new Vector3(0.4f, 0.4f, 3.2f), Gold);
            Cube("RingB", new Vector3(51, -3.6f, 0), new Vector3(0.4f, 0.4f, 3.2f), Gold);
            Cube("RingL", new Vector3(51, -2.4f, -1.6f), new Vector3(0.4f, 2.8f, 0.4f), Gold);
            Cube("RingR", new Vector3(51, -2.4f, 1.6f), new Vector3(0.4f, 2.8f, 0.4f), Gold);

            // ===== 第四段：双人机关 + 终点 (x 58..74) =====
            Cube("GroundC", new Vector3(66, -0.5f, 0), new Vector3(16, 1, 12), Grass);
            PlateAMr = Cube("PlateA", new Vector3(61, 0.08f, -2.2f), new Vector3(1.7f, 0.16f, 1.7f), PlateBlue).GetComponent<MeshRenderer>();
            PlateBMr = Cube("PlateB", new Vector3(61, 0.08f, 2.2f), new Vector3(1.7f, 0.16f, 1.7f), PlateBlue).GetComponent<MeshRenderer>();
            DualPlateA = new Bounds(new Vector3(61, 0.7f, -2.2f), new Vector3(1.7f, 1.6f, 1.7f));
            DualPlateB = new Bounds(new Vector3(61, 0.7f, 2.2f), new Vector3(1.7f, 1.6f, 1.7f));

            Cube("Wall2L", new Vector3(64, 1.5f, -3.6f), new Vector3(1, 3, 4.8f), Stone);
            Cube("Wall2R", new Vector3(64, 1.5f, 3.6f), new Vector3(1, 3, 4.8f), Stone);
            Gate = Cube("Gate", new Vector3(64, 1.5f, 0), new Vector3(1, 3, 2.4f), Stone).transform;

            // 终点拱门 + 旋转爱心
            Cube("GoalL", new Vector3(69, 1.5f, -1.6f), new Vector3(0.6f, 3, 0.6f), Gold);
            Cube("GoalR", new Vector3(69, 1.5f, 1.6f), new Vector3(0.6f, 3, 0.6f), Gold);
            Cube("GoalTop", new Vector3(69, 3.2f, 0), new Vector3(0.6f, 0.6f, 3.8f), Gold);
            GoalSpinner = Cube("GoalHeart", new Vector3(69, 1.8f, 0), new Vector3(0.6f, 0.6f, 0.6f), Pink, false).transform;
            GoalBounds = new Bounds(new Vector3(69, 1.5f, 0), new Vector3(4.5f, 3.5f, 5f));

            BuildDecorations();
        }

        void BuildDecorations()
        {
            // 小树
            foreach (var (x, z) in new[] { (4f, 4.5f), (9f, -4.6f), (21f, -4.4f), (36f, -4.5f), (61f, 4.6f), (72f, -4.3f), (72f, 4.3f) })
            {
                Cylinder($"Trunk{x}{z}", new Vector3(x, 0.8f, z), new Vector3(0.3f, 0.8f, 0.3f), new Color(0.5f, 0.35f, 0.2f));
                Sphere($"Leaf{x}{z}", new Vector3(x, 2.1f, z), Vector3.one * 1.6f, new Color(0.3f, 0.6f, 0.3f));
            }
            // 云朵（无碰撞）
            foreach (var (x, y, z) in new[] { (8f, 9f, 6f), (26f, 10f, 8f), (48f, 9.5f, 7f), (66f, 10f, 6f) })
            {
                var c = Sphere($"Cloud{x}", new Vector3(x, y, z), new Vector3(3.2f, 1.1f, 1.6f), Color.white, false);
                c.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, 0.95f);
            }
        }

        // ---- 几何体小工厂 ----
        GameObject Cube(string name, Vector3 pos, Vector3 size, Color color, bool collider = true)
            => Prim(PrimitiveType.Cube, name, pos, size, color, collider);

        GameObject Sphere(string name, Vector3 pos, Vector3 size, Color color, bool collider = true)
            => Prim(PrimitiveType.Sphere, name, pos, size, color, collider);

        GameObject Cylinder(string name, Vector3 pos, Vector3 size, Color color, bool collider = true)
            => Prim(PrimitiveType.Cylinder, name, pos, size, color, collider);

        GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 size, Color color, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().material.color = color;
            return go;
        }

        static void MakeTransparent(Material m)
        {
            m.SetFloat("_Mode", 3);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
        }
    }
}
