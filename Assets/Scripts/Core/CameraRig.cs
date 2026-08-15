using UnityEngine;

namespace MeowMeowDog.Core
{
    /// <summary>
    /// 2.5D 第三人称跟随相机：固定俯视角度，平滑跟随自己的角色。
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        public Transform Target;

        static readonly Vector3 Offset = new(0f, 8.5f, -9.5f);
        Vector3 _velocity;

        void Awake()
        {
            Instance = this;
            transform.rotation = Quaternion.Euler(38f, 0f, 0f);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            var desired = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 0.18f);
        }
    }
}
