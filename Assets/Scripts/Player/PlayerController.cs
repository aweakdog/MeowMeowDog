using MeowMeowDog.Core;
using MeowMeowDog.Level;
using Unity.Netcode;
using UnityEngine;

namespace MeowMeowDog.Player
{
    /// <summary>
    /// 玩家控制器（拥有者权威）。Host = 狗狗，Client = 猫猫。
    /// 陆地：WASD 移动 + 空格跳（猫猫二段跳）；水里：变成鱼，空格上浮 / Shift 下潜。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        const float Gravity = -25f;

        CharacterController _cc;
        PlayerAvatar _avatar;
        float _vy;
        int _jumpsUsed;
        int _checkpoint;
        bool _wasInWater;

        public NetworkVariable<bool> NetSwimming = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool IsDog => OwnerClientId == NetworkManager.ServerClientId;

        float MoveSpeed => IsDog ? 6.5f : 6.0f;
        float JumpVelocity => IsDog ? 9.5f : 9.0f;
        int MaxJumps => IsDog ? 1 : 2; // 猫猫会二段跳

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _avatar = GetComponent<PlayerAvatar>();
        }

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[MMDog] Player spawned owner={OwnerClientId} isDog={IsDog} isLocal={IsOwner} pos={transform.position}");
            _avatar.Build(IsDog);
            NetSwimming.OnValueChanged += (_, swim) => _avatar.SetSwimming(swim);
            _avatar.SetSwimming(NetSwimming.Value);

            if (!IsOwner) return;

            Teleport(SpawnPoint());
            var rig = Core.CameraRig.Instance;
            if (rig != null) rig.Target = transform;
        }

        Vector3 SpawnPoint()
        {
            var lb = LevelBuilder.Instance;
            Vector3 basePos = lb != null && lb.Checkpoints.Length > 0 ? lb.Checkpoints[0] : new Vector3(1.5f, 1f, 0f);
            return basePos + new Vector3(0, 0, IsDog ? -1f : 1f);
        }

        void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _vy = 0;
        }

        void Update()
        {
            if (!IsOwner || LevelBuilder.Instance == null) return;

            bool inWater = LevelBuilder.Instance.WaterBounds.Contains(transform.position + Vector3.up * 0.5f);
            if (NetSwimming.Value != inWater) NetSwimming.Value = inWater;
            if (inWater != _wasInWater)
            {
                // 按住空格冲出水面时，像鱼一样跃起，方便跳上岸边台阶
                if (!inWater && Input.GetKey(KeyCode.Space)) _vy = 8f;
                _wasInWater = inWater;
            }

            if (inWater) UpdateSwim();
            else UpdateWalk();

            UpdateCheckpointAndRespawn();

            if (Input.GetKeyDown(KeyCode.E) && LevelState.Instance != null)
                LevelState.Instance.TryInteract(transform.position);
        }

        void UpdateWalk()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            var move = Vector3.ClampMagnitude(input, 1f) * MoveSpeed;

            if (_cc.isGrounded)
            {
                _jumpsUsed = 0;
                _vy = -2f;
            }

            if (Input.GetKeyDown(KeyCode.Space) && _jumpsUsed < MaxJumps)
            {
                _vy = JumpVelocity * (_jumpsUsed == 0 ? 1f : 0.9f);
                _jumpsUsed++;
            }

            _vy += Gravity * Time.deltaTime;
            _cc.Move((move + Vector3.up * _vy) * Time.deltaTime);
            _avatar.FaceMoveDirection(move);
        }

        void UpdateSwim()
        {
            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")) * 5f;
            float up = 0;
            if (Input.GetKey(KeyCode.Space)) up = 4f;
            else if (Input.GetKey(KeyCode.LeftShift)) up = -4f;
            else up = 0.3f; // 轻微浮力

            _vy = 0;
            _jumpsUsed = 0;
            _cc.Move((move + Vector3.up * up) * Time.deltaTime);
            _avatar.FaceMoveDirection(move);
        }

        void UpdateCheckpointAndRespawn()
        {
            var cps = LevelBuilder.Instance.Checkpoints;
            for (int i = _checkpoint + 1; i < cps.Length; i++)
                if (transform.position.x >= cps[i].x) _checkpoint = i;

            if (transform.position.y < -9f)
                Teleport(cps[_checkpoint]);
        }
    }
}
