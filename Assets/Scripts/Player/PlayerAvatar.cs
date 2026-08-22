using UnityEngine;

namespace MeowMeowDog.Player
{
    /// <summary>
    /// 角色外观：Quaternius CC0 低模动物（狗/猫，下水变成"狗狗鱼/猫猫鱼"用食人鱼模型），
    /// 带 Idle/Walking/Swimming 骨骼动画，动画状态由位移自驱动（远端玩家也生效）。
    /// 模型资源缺失时退回几何体拼装占位造型。
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        static readonly Color DogColor = new(0.85f, 0.55f, 0.25f);
        static readonly Color CatColor = new(0.62f, 0.62f, 0.70f);
        static readonly Color EarInner = new(0.95f, 0.75f, 0.78f);
        static readonly Color DogTint = new(0.76f, 0.54f, 0.33f);      // 狗狗模型染成暖棕色
        static readonly Color DogFishTint = new(0.82f, 0.60f, 0.36f);  // 狗狗鱼
        static readonly Color CatFishTint = new(0.55f, 0.60f, 0.72f);  // 猫猫鱼

        Transform _root;
        GameObject _landParts;
        GameObject _fishParts;
        Transform _landPivot;   // 俯仰枢轴：跳跃/游泳时倾斜模型
        Transform _fishPivot;
        Animator _landAnim;
        Animator _fishAnim;
        string _landState;
        string _fishState;
        bool _isDog;
        bool _swimming;
        Vector3 _lastPos;
        bool _hasLastPos;
        float _planarSpeed;
        float _vy;
        float _lastExternalFace = -99f;

        public void Build(bool isDog)
        {
            _isDog = isDog;
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Visuals").transform;
            _root.SetParent(transform, false);
            _landState = _fishState = null;
            _hasLastPos = false;

            // ---- 陆地造型（狗/猫模型头朝 +X，偏航 -90° 转到 +Z 前方）----
            _landParts = new GameObject("Land");
            _landParts.transform.SetParent(_root, false);
            _landPivot = new GameObject("Pivot").transform;
            _landPivot.SetParent(_landParts.transform, false);
            _landAnim = SpawnModel(isDog ? "Dog" : "Cat", _landPivot, isDog ? 1.1f : 1.0f, -90f, alignFeet: true, 0f);
            if (_landAnim != null)
            {
                if (isDog) TintMaterials(_landAnim.gameObject, "Dog", DogTint);
            }
            else BuildLandFallback(_landPivot, isDog);

            // ---- 鱼鱼造型（食人鱼模型头朝 +Z）----
            _fishParts = new GameObject("Fish");
            _fishParts.transform.SetParent(_root, false);
            _fishPivot = new GameObject("Pivot").transform;
            _fishPivot.SetParent(_fishParts.transform, false);
            _fishAnim = SpawnModel("Piranha", _fishPivot, 0.75f, 0f, alignFeet: false, 0.6f);
            if (_fishAnim != null) TintMaterials(_fishAnim.gameObject, "Brown", isDog ? DogFishTint : CatFishTint);
            else BuildFishFallback(_fishPivot, isDog);

            _fishParts.SetActive(false);

            // ---- 名牌 ----
            var tagGo = new GameObject("NameTag");
            tagGo.transform.SetParent(_root, false);
            tagGo.transform.localPosition = new Vector3(0, 1.75f, 0);
            var tm = tagGo.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tagGo.GetComponent<MeshRenderer>().material = font.material;
            tm.text = isDog ? "汪汪" : "喵喵";
            tm.fontSize = 48;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = isDog ? new Color(1f, 0.85f, 0.4f) : new Color(0.8f, 0.9f, 1f);
            tagGo.AddComponent<Billboard>();
        }

        /// <summary>实例化模型并归一化：按包围盒缩放到目标身高，水平居中，脚底/身体中心对齐到指定高度。</summary>
        Animator SpawnModel(string modelName, Transform parent, float targetHeight, float yaw, bool alignFeet, float y)
        {
            var prefab = Resources.Load<GameObject>($"Models/{modelName}");
            var ctrl = Resources.Load<RuntimeAnimatorController>($"Anim/{modelName}");
            if (prefab == null || ctrl == null)
            {
                Debug.LogWarning($"[MMDog] 缺少模型或动画控制器 {modelName}，退回几何体占位造型");
                return null;
            }

            var go = Instantiate(prefab, parent, false);
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);

            var bounds = WorldBounds(go);
            if (bounds.size.y > 0.001f)
                go.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);
            bounds = WorldBounds(go);
            var pivotPos = parent.position;
            var offset = new Vector3(pivotPos.x - bounds.center.x, 0, pivotPos.z - bounds.center.z);
            offset.y = alignFeet ? pivotPos.y + y - bounds.min.y : pivotPos.y + y - bounds.center.y;
            go.transform.position += offset;

            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return anim;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static void TintMaterials(GameObject go, string materialPrefix, Color color)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials)
                    if (m.name.StartsWith(materialPrefix)) m.color = color;
        }

        void Update()
        {
            if (_root == null) return;

            var pos = transform.position;
            if (!_hasLastPos) { _lastPos = pos; _hasLastPos = true; return; }
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            var vel = (pos - _lastPos) / dt;
            _lastPos = pos;
            if (vel.magnitude > 30f) return; // 传送/重生的瞬移帧跳过

            var planar = new Vector3(vel.x, 0, vel.z);
            _planarSpeed = Mathf.Lerp(_planarSpeed, planar.magnitude, Mathf.Clamp01(25f * dt));
            _vy = Mathf.Lerp(_vy, vel.y, Mathf.Clamp01(25f * dt));

            // 远端玩家没有本地输入，按位移方向转身
            if (Time.time - _lastExternalFace > 0.25f && planar.sqrMagnitude > 0.5f)
                FaceInternal(planar);

            if (_swimming) UpdateFishAnim();
            else UpdateLandAnim();
        }

        void UpdateLandAnim()
        {
            // 模型包没有跳跃剪辑：空中用俯仰倾斜 + 慢速走路动画表现
            bool airborne = Mathf.Abs(_vy) > 2.2f;
            float targetPitch = airborne ? Mathf.Clamp(-_vy * 2.5f, -22f, 16f) : 0f;
            if (_landPivot != null)
                _landPivot.localRotation = Quaternion.Slerp(_landPivot.localRotation,
                    Quaternion.Euler(targetPitch, 0, 0), 10f * Time.deltaTime);

            if (_landAnim == null) return;
            if (_planarSpeed > 0.5f || airborne)
            {
                _landAnim.speed = airborne ? 0.55f : Mathf.Clamp(_planarSpeed * 0.3f, 0.9f, 2.2f);
                PlayState(_landAnim, ref _landState, "Walking");
            }
            else
            {
                _landAnim.speed = 1f;
                PlayState(_landAnim, ref _landState, "Idle");
            }
        }

        void UpdateFishAnim()
        {
            // 上浮/下潜时鱼头跟着仰俯
            float targetPitch = Mathf.Clamp(-_vy * 9f, -32f, 32f);
            if (_fishPivot != null)
                _fishPivot.localRotation = Quaternion.Slerp(_fishPivot.localRotation,
                    Quaternion.Euler(targetPitch, 0, 0), 8f * Time.deltaTime);

            if (_fishAnim == null) return;
            _fishAnim.speed = 0.9f + _planarSpeed * 0.15f;
            PlayState(_fishAnim, ref _fishState, "Swimming");
        }

        static void PlayState(Animator anim, ref string current, string state)
        {
            if (current == state) return;
            anim.CrossFadeInFixedTime(state, 0.18f, 0);
            current = state;
        }

        public void SetSwimming(bool swim)
        {
            _swimming = swim;
            if (_landParts != null) _landParts.SetActive(!swim);
            if (_fishParts != null) _fishParts.SetActive(swim);
        }

        /// <summary>本地输入方向优先转身（远端玩家由 Update 里按位移方向转）。</summary>
        public void FaceMoveDirection(Vector3 move)
        {
            move.y = 0;
            if (move.sqrMagnitude < 0.01f) return;
            _lastExternalFace = Time.time;
            FaceInternal(move);
        }

        void FaceInternal(Vector3 dir)
        {
            dir.y = 0;
            if (dir.sqrMagnitude < 0.01f || _root == null) return;
            var target = Quaternion.LookRotation(dir.normalized);
            _root.rotation = Quaternion.Slerp(_root.rotation, target, 12f * Time.deltaTime);
        }

        // ==== 以下为模型缺失时的几何体占位造型（兜底） ====

        void BuildLandFallback(Transform lp, bool isDog)
        {
            Color body = isDog ? DogColor : CatColor;
            Part(PrimitiveType.Capsule, lp, new Vector3(0, 0.55f, 0), new Vector3(0.62f, 0.42f, 0.62f), body);
            Part(PrimitiveType.Sphere, lp, new Vector3(0, 1.05f, 0.08f), Vector3.one * 0.55f, body);
            Part(PrimitiveType.Sphere, lp, new Vector3(-0.12f, 1.12f, 0.32f), Vector3.one * 0.09f, Color.black);
            Part(PrimitiveType.Sphere, lp, new Vector3(0.12f, 1.12f, 0.32f), Vector3.one * 0.09f, Color.black);
            Part(PrimitiveType.Sphere, lp, new Vector3(0, 1.0f, 0.34f), Vector3.one * 0.13f, isDog ? new Color(0.3f, 0.2f, 0.15f) : new Color(0.9f, 0.6f, 0.65f));

            if (isDog)
            {
                Part(PrimitiveType.Cube, lp, new Vector3(-0.26f, 1.22f, 0.05f), new Vector3(0.12f, 0.3f, 0.16f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, 25));
                Part(PrimitiveType.Cube, lp, new Vector3(0.26f, 1.22f, 0.05f), new Vector3(0.12f, 0.3f, 0.16f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, -25));
                Part(PrimitiveType.Cube, lp, new Vector3(0, 0.75f, -0.38f), new Vector3(0.1f, 0.1f, 0.42f), body, new Vector3(-40, 0, 0));
            }
            else
            {
                Part(PrimitiveType.Cube, lp, new Vector3(-0.18f, 1.38f, 0.03f), new Vector3(0.16f, 0.22f, 0.06f), body, new Vector3(0, 0, 18));
                Part(PrimitiveType.Cube, lp, new Vector3(0.18f, 1.38f, 0.03f), new Vector3(0.16f, 0.22f, 0.06f), body, new Vector3(0, 0, -18));
                Part(PrimitiveType.Cube, lp, new Vector3(-0.18f, 1.36f, 0.05f), new Vector3(0.08f, 0.12f, 0.04f), EarInner, new Vector3(0, 0, 18));
                Part(PrimitiveType.Cube, lp, new Vector3(0.18f, 1.36f, 0.05f), new Vector3(0.08f, 0.12f, 0.04f), EarInner, new Vector3(0, 0, -18));
                Part(PrimitiveType.Cube, lp, new Vector3(0, 0.7f, -0.42f), new Vector3(0.07f, 0.07f, 0.55f), body, new Vector3(-55, 0, 0));
            }
        }

        void BuildFishFallback(Transform fp, bool isDog)
        {
            Color body = isDog ? DogColor : CatColor;
            Color fishBody = Color.Lerp(body, new Color(0.4f, 0.7f, 1f), 0.35f);
            Part(PrimitiveType.Sphere, fp, new Vector3(0, 0.6f, 0.1f), new Vector3(0.55f, 0.6f, 1.05f), fishBody);
            Part(PrimitiveType.Sphere, fp, new Vector3(-0.14f, 0.75f, 0.5f), Vector3.one * 0.1f, Color.black);
            Part(PrimitiveType.Sphere, fp, new Vector3(0.14f, 0.75f, 0.5f), Vector3.one * 0.1f, Color.black);
            Part(PrimitiveType.Cube, fp, new Vector3(0, 0.6f, -0.55f), new Vector3(0.06f, 0.42f, 0.3f), fishBody, new Vector3(0, 0, 0));
            Part(PrimitiveType.Cube, fp, new Vector3(0, 0.95f, 0f), new Vector3(0.06f, 0.25f, 0.35f), fishBody);
            if (isDog)
            {
                Part(PrimitiveType.Cube, fp, new Vector3(-0.2f, 0.95f, 0.35f), new Vector3(0.1f, 0.22f, 0.12f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, 30));
                Part(PrimitiveType.Cube, fp, new Vector3(0.2f, 0.95f, 0.35f), new Vector3(0.1f, 0.22f, 0.12f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, -30));
            }
            else
            {
                Part(PrimitiveType.Cube, fp, new Vector3(-0.15f, 1.0f, 0.3f), new Vector3(0.13f, 0.18f, 0.05f), fishBody, new Vector3(0, 0, 18));
                Part(PrimitiveType.Cube, fp, new Vector3(0.15f, 1.0f, 0.3f), new Vector3(0.13f, 0.18f, 0.05f), fishBody, new Vector3(0, 0, -18));
            }
        }

        static void Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>()); // 视觉部件不要碰撞体，避免干扰 CharacterController
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            var mr = go.GetComponent<MeshRenderer>();
            mr.material.color = color;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    /// <summary>名牌始终面向相机。</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
