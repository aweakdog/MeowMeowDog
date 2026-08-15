using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MeowMeowDog.Net
{
    /// <summary>
    /// 开局菜单：创建房间(Host) / 输入 IP 加入(Join)。
    /// 初版走直连（同一 WiFi 下互联，或公网需端口转发），下一步可加 Unity Relay 实现真正的公网联机。
    /// </summary>
    public class ConnectionUI : MonoBehaviour
    {
        const ushort Port = 7777;

        string _joinIp = "127.0.0.1";
        string _status = "";
        string _lanIp = "127.0.0.1";
        Font _cjkFont;

        void Start()
        {
            _cjkFont = Font.CreateDynamicFontFromOSFont("PingFang SC", 16);
            _lanIp = GetLanIp();
            var nm = NetworkManager.Singleton;
            nm.OnClientDisconnectCallback += OnClientDisconnect;
            nm.OnClientConnectedCallback += id => Debug.Log($"[MMDog] client connected: {id}, total={(nm.IsServer ? nm.ConnectedClientsIds.Count : -1)}");

            // 命令行自动联机（自动化测试/快速开发用）：-autohost 或 -autojoin [ip]
            // 注意：NGO 场景同步会在客户端连接时重载场景，Start 会再次执行，所以要防重入
            if (nm.IsClient || nm.IsServer) return;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-autohost") { Debug.Log("[MMDog] autohost"); StartHost(); }
                else if (args[i] == "-autojoin")
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-")) _joinIp = args[i + 1];
                    Debug.Log($"[MMDog] autojoin {_joinIp}");
                    StartClient();
                }
            }
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        void OnClientDisconnect(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (!nm.IsServer && clientId == nm.LocalClientId)
            {
                _status = "连接断开了";
                nm.Shutdown();
            }
        }

        static string GetLanIp()
        {
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        return ip.ToString();
            }
            catch { }
            return "127.0.0.1";
        }

        void OnGUI()
        {
            if (_cjkFont != null) GUI.skin.font = _cjkFont;

            var nm = NetworkManager.Singleton;
            if (nm == null) return;

            if (nm.IsClient || nm.IsServer)
            {
                DrawInGamePanel(nm);
                return;
            }

            // ---- 主菜单 ----
            float w = 340, h = 320;
            var rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            GUI.Box(rect, "");
            GUILayout.BeginArea(rect);
            GUILayout.Space(14);

            var title = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (_cjkFont != null) title.font = _cjkFont;
            GUILayout.Label("喵喵狗  MeowMeowDog", title);
            var sub = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            if (_cjkFont != null) sub.font = _cjkFont;
            GUILayout.Label("汪汪与喵喵的双人冒险", sub);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.BeginVertical();

            if (GUILayout.Button("创建房间（我当狗狗）", GUILayout.Height(44)))
                StartHost();

            GUILayout.Space(14);
            GUILayout.Label("对方的 IP 地址：");
            _joinIp = GUILayout.TextField(_joinIp, GUILayout.Height(28));
            if (GUILayout.Button("加入房间（我当猫猫）", GUILayout.Height(44)))
                StartClient();

            GUILayout.Space(10);
            GUILayout.Label($"本机局域网 IP：{_lanIp}（告诉对方用这个加入）");
            if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status);

            GUILayout.EndVertical();
            GUILayout.Space(30);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void DrawInGamePanel(NetworkManager nm)
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 90));
            string role = nm.IsHost ? "汪汪（狗狗）" : "喵喵（猫猫）";
            int count = nm.IsServer ? nm.ConnectedClientsIds.Count : -1;
            GUILayout.Label($"你是：{role}");
            if (nm.IsServer)
                GUILayout.Label(count >= 2 ? "小伙伴已加入！" : $"等待小伙伴加入…（IP: {_lanIp}）");
            if (GUILayout.Button("离开房间", GUILayout.Width(90)))
            {
                nm.Shutdown();
                _status = "";
            }
            GUILayout.EndArea();
        }

        void StartHost()
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            NetworkManager.Singleton.StartHost();
            _status = "";
        }

        void StartClient()
        {
            var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(_joinIp.Trim(), Port);
            if (NetworkManager.Singleton.StartClient())
                _status = "连接中…";
            else
                _status = "连接失败，检查 IP 是否正确";
        }
    }
}
