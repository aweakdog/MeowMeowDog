using Unity.Netcode;
using UnityEngine;

namespace MeowMeowDog.Level
{
    /// <summary>
    /// 关卡的联机状态（场景内置 NetworkObject，服务器权威）：
    /// 机关开关、箱子位置、通关判定都由 Host 计算，通过 NetworkVariable 同步；
    /// 门/桥/闸的动画各端根据同步状态本地播放。
    /// </summary>
    public class LevelState : NetworkBehaviour
    {
        public static LevelState Instance { get; private set; }

        public NetworkVariable<bool> Door1Open = new();
        public NetworkVariable<bool> BridgeOn = new();
        public NetworkVariable<bool> GateOpen = new();
        public NetworkVariable<bool> LevelComplete = new();
        public NetworkVariable<Vector3> BoxPos = new();
        public NetworkVariable<byte> PlateMask = new(); // bit0=箱板 bit1=蓝板A bit2=蓝板B

        Rigidbody _boxRb;

        void Awake() => Instance = this;

        public override void OnNetworkSpawn()
        {
            Debug.Log($"[MMDog] LevelState spawned, isServer={IsServer}");
            var lb = LevelBuilder.Instance;
            if (IsServer)
            {
                BoxPos.Value = lb.BoxStartPos;
                // 箱子物理只在服务器上模拟
                _boxRb = lb.Box.gameObject.AddComponent<Rigidbody>();
                _boxRb.mass = 3f;
                _boxRb.linearDamping = 2f;
                _boxRb.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else
            {
                lb.Box.position = BoxPos.Value;
            }
        }

        void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            var lb = LevelBuilder.Instance;

            ServerUpdateBoxPush(lb);
            ServerUpdatePlates(lb);
            ServerUpdateGoal(lb);
        }

        void ServerUpdateBoxPush(LevelBuilder lb)
        {
            // 狗狗（Host 玩家）贴近箱子就能推动；猫猫力气小推不动
            var dogObj = NetworkManager.ConnectedClients.TryGetValue(NetworkManager.ServerClientId, out var dog)
                ? dog.PlayerObject : null;
            if (dogObj != null)
            {
                Vector3 toBox = lb.Box.position - dogObj.transform.position;
                float dy = Mathf.Abs(toBox.y);
                toBox.y = 0;
                if (toBox.magnitude < 1.45f && dy < 1.3f)
                {
                    // 沿主导轴推，好对准
                    Vector3 dir = Mathf.Abs(toBox.x) > Mathf.Abs(toBox.z)
                        ? new Vector3(Mathf.Sign(toBox.x), 0, 0)
                        : new Vector3(0, 0, Mathf.Sign(toBox.z));
                    var v = _boxRb.linearVelocity;
                    var push = dir * 2.4f;
                    _boxRb.linearVelocity = new Vector3(push.x, v.y, push.z);
                }
            }
            BoxPos.Value = lb.Box.position;
        }

        void ServerUpdatePlates(LevelBuilder lb)
        {
            bool boxOn = lb.BoxPlateBounds.Contains(lb.Box.position);
            bool aOn = AnyPlayerIn(lb.DualPlateA);
            bool bOn = AnyPlayerIn(lb.DualPlateB);

            Door1Open.Value = boxOn;
            if (aOn && bOn) GateOpen.Value = true; // 双人同踩后闸门保持打开
            PlateMask.Value = (byte)((boxOn ? 1 : 0) | (aOn ? 2 : 0) | (bOn ? 4 : 0));
        }

        void ServerUpdateGoal(LevelBuilder lb)
        {
            if (LevelComplete.Value) return;
            int inside = 0, total = 0;
            foreach (var client in NetworkManager.ConnectedClientsList)
            {
                if (client.PlayerObject == null) continue;
                total++;
                if (lb.GoalBounds.Contains(client.PlayerObject.transform.position)) inside++;
            }
            if (total >= 2 && inside == total) LevelComplete.Value = true;
        }

        bool AnyPlayerIn(Bounds b)
        {
            foreach (var client in NetworkManager.ConnectedClientsList)
                if (client.PlayerObject != null && b.Contains(client.PlayerObject.transform.position))
                    return true;
            return false;
        }

        // ---- 拉杆交互 ----
        public void TryInteract(Vector3 playerPos)
        {
            if (Vector3.Distance(playerPos, LevelBuilder.Instance.LeverPos) < 3f)
                RequestLeverToggleRpc();
        }

        [Rpc(SendTo.Server)]
        void RequestLeverToggleRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var c) || c.PlayerObject == null) return;
            if (Vector3.Distance(c.PlayerObject.transform.position, LevelBuilder.Instance.LeverPos) < 3.5f)
                BridgeOn.Value = !BridgeOn.Value;
        }

        // ---- 各端本地动画 ----
        void Update()
        {
            if (!IsSpawned) return;
            var lb = LevelBuilder.Instance;
            float dt = Time.deltaTime * 3f;

            lb.Door1.position = Vector3.Lerp(lb.Door1.position,
                new Vector3(24, Door1Open.Value ? -1.6f : 1.5f, 0), dt);
            lb.Bridge.position = Vector3.Lerp(lb.Bridge.position,
                BridgeOn.Value ? new Vector3(29, -0.2f, 0) : new Vector3(38, -1.3f, 0), dt);
            lb.Gate.position = Vector3.Lerp(lb.Gate.position,
                new Vector3(64, GateOpen.Value ? 4.8f : 1.5f, 0), dt);

            var handleTilt = BridgeOn.Value ? -35f : 35f;
            lb.LeverHandle.localRotation = Quaternion.Slerp(lb.LeverHandle.localRotation,
                Quaternion.Euler(0, 0, handleTilt), dt);

            lb.GoalSpinner.Rotate(0, 60f * Time.deltaTime, 0);
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.12f;
            lb.GoalSpinner.localScale = Vector3.one * 0.6f * (LevelComplete.Value ? pulse * 1.6f : pulse);

            // 踏板按下反馈（变暗+下沉）
            SetPlate(lb.PlateBoxMr, LevelBuilder.PlateRed, (PlateMask.Value & 1) != 0);
            SetPlate(lb.PlateAMr, LevelBuilder.PlateBlue, (PlateMask.Value & 2) != 0);
            SetPlate(lb.PlateBMr, LevelBuilder.PlateBlue, (PlateMask.Value & 4) != 0);

            // 客户端插值箱子位置
            if (!IsServer)
                lb.Box.position = Vector3.Lerp(lb.Box.position, BoxPos.Value, Time.deltaTime * 12f);
        }

        static void SetPlate(MeshRenderer mr, Color baseColor, bool pressed)
        {
            var pos = mr.transform.position;
            pos.y = pressed ? 0.03f : 0.08f;
            mr.transform.position = pos;
            var c = pressed ? baseColor * 0.45f : baseColor;
            c.a = 1f;
            mr.material.color = c;
        }
    }
}
