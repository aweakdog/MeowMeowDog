using UnityEngine;

namespace MeowMeowDog.Player
{
    /// <summary>
    /// 用基础几何体拼出狗狗/猫猫的萌系造型（初版占位美术，后续换正式模型）。
    /// 下水后切换成"狗狗鱼/猫猫鱼"造型。
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        static readonly Color DogColor = new(0.85f, 0.55f, 0.25f);
        static readonly Color CatColor = new(0.62f, 0.62f, 0.70f);
        static readonly Color EarInner = new(0.95f, 0.75f, 0.78f);

        Transform _root;
        GameObject _landParts;
        GameObject _fishParts;
        bool _isDog;

        public void Build(bool isDog)
        {
            _isDog = isDog;
            if (_root != null) Destroy(_root.gameObject);
            _root = new GameObject("Visuals").transform;
            _root.SetParent(transform, false);

            Color body = isDog ? DogColor : CatColor;

            // ---- 陆地造型 ----
            _landParts = new GameObject("Land");
            _landParts.transform.SetParent(_root, false);
            var lp = _landParts.transform;

            Part(PrimitiveType.Capsule, lp, new Vector3(0, 0.55f, 0), new Vector3(0.62f, 0.42f, 0.62f), body);      // 身体
            Part(PrimitiveType.Sphere, lp, new Vector3(0, 1.05f, 0.08f), Vector3.one * 0.55f, body);                 // 头
            Part(PrimitiveType.Sphere, lp, new Vector3(-0.12f, 1.12f, 0.32f), Vector3.one * 0.09f, Color.black);     // 眼睛
            Part(PrimitiveType.Sphere, lp, new Vector3(0.12f, 1.12f, 0.32f), Vector3.one * 0.09f, Color.black);
            Part(PrimitiveType.Sphere, lp, new Vector3(0, 1.0f, 0.34f), Vector3.one * 0.13f, isDog ? new Color(0.3f, 0.2f, 0.15f) : new Color(0.9f, 0.6f, 0.65f)); // 鼻子

            if (isDog)
            {
                // 垂耳 + 上翘尾巴
                Part(PrimitiveType.Cube, lp, new Vector3(-0.26f, 1.22f, 0.05f), new Vector3(0.12f, 0.3f, 0.16f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, 25));
                Part(PrimitiveType.Cube, lp, new Vector3(0.26f, 1.22f, 0.05f), new Vector3(0.12f, 0.3f, 0.16f), new Color(0.6f, 0.38f, 0.18f), new Vector3(0, 0, -25));
                Part(PrimitiveType.Cube, lp, new Vector3(0, 0.75f, -0.38f), new Vector3(0.1f, 0.1f, 0.42f), body, new Vector3(-40, 0, 0));
            }
            else
            {
                // 尖耳 + 细长尾巴
                Part(PrimitiveType.Cube, lp, new Vector3(-0.18f, 1.38f, 0.03f), new Vector3(0.16f, 0.22f, 0.06f), body, new Vector3(0, 0, 18));
                Part(PrimitiveType.Cube, lp, new Vector3(0.18f, 1.38f, 0.03f), new Vector3(0.16f, 0.22f, 0.06f), body, new Vector3(0, 0, -18));
                Part(PrimitiveType.Cube, lp, new Vector3(-0.18f, 1.36f, 0.05f), new Vector3(0.08f, 0.12f, 0.04f), EarInner, new Vector3(0, 0, 18));
                Part(PrimitiveType.Cube, lp, new Vector3(0.18f, 1.36f, 0.05f), new Vector3(0.08f, 0.12f, 0.04f), EarInner, new Vector3(0, 0, -18));
                Part(PrimitiveType.Cube, lp, new Vector3(0, 0.7f, -0.42f), new Vector3(0.07f, 0.07f, 0.55f), body, new Vector3(-55, 0, 0));
            }

            // 名牌
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

            // ---- 鱼鱼造型 ----
            _fishParts = new GameObject("Fish");
            _fishParts.transform.SetParent(_root, false);
            var fp = _fishParts.transform;
            Color fishBody = Color.Lerp(body, new Color(0.4f, 0.7f, 1f), 0.35f);

            Part(PrimitiveType.Sphere, fp, new Vector3(0, 0.6f, 0.1f), new Vector3(0.55f, 0.6f, 1.05f), fishBody);   // 鱼身
            Part(PrimitiveType.Sphere, fp, new Vector3(-0.14f, 0.75f, 0.5f), Vector3.one * 0.1f, Color.black);       // 眼睛
            Part(PrimitiveType.Sphere, fp, new Vector3(0.14f, 0.75f, 0.5f), Vector3.one * 0.1f, Color.black);
            Part(PrimitiveType.Cube, fp, new Vector3(0, 0.6f, -0.55f), new Vector3(0.06f, 0.42f, 0.3f), fishBody, new Vector3(0, 0, 0)); // 尾鳍
            Part(PrimitiveType.Cube, fp, new Vector3(0, 0.95f, 0f), new Vector3(0.06f, 0.25f, 0.35f), fishBody);     // 背鳍
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

            _fishParts.SetActive(false);
        }

        public void SetSwimming(bool swim)
        {
            if (_landParts != null) _landParts.SetActive(!swim);
            if (_fishParts != null) _fishParts.SetActive(swim);
        }

        public void FaceMoveDirection(Vector3 move)
        {
            move.y = 0;
            if (move.sqrMagnitude < 0.01f || _root == null) return;
            var target = Quaternion.LookRotation(move.normalized);
            _root.rotation = Quaternion.Slerp(_root.rotation, target, 12f * Time.deltaTime);
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
