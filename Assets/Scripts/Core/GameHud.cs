using MeowMeowDog.Level;
using Unity.Netcode;
using UnityEngine;

namespace MeowMeowDog.Core
{
    /// <summary>
    /// 游戏内提示 HUD：当前任务引导 + 操作说明 + 通关横幅。
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        Font _cjkFont;

        void Start()
        {
            _cjkFont = Font.CreateDynamicFontFromOSFont("PingFang SC", 16);
        }

        void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || (!nm.IsClient && !nm.IsServer)) return;
            var ls = LevelState.Instance;
            if (ls == null || !ls.IsSpawned) return;

            if (_cjkFont != null) GUI.skin.font = _cjkFont;

            // 操作说明（右上角）
            var ctrl = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 13 };
            if (_cjkFont != null) ctrl.font = _cjkFont;
            GUI.Label(new Rect(Screen.width - 330, 10, 320, 80),
                "WASD 移动｜空格 跳跃（喵喵可二段跳）\nE 互动｜水里：空格上浮 / Shift 下潜\nEsc 暂停菜单", ctrl);

            // 任务引导（下方居中）
            var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            if (_cjkFont != null) hint.font = _cjkFont;
            GUI.Label(new Rect(0, Screen.height - 64, Screen.width, 40), CurrentHint(ls), hint);

            // 通关横幅
            if (ls.LevelComplete.Value)
            {
                var banner = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 42, fontStyle = FontStyle.Bold };
                if (_cjkFont != null) banner.font = _cjkFont;
                banner.normal.textColor = new Color(1f, 0.75f, 0.85f);
                GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 80), "通关啦！你们真是最棒的搭档！", banner);
            }
        }

        static string CurrentHint(LevelState ls)
        {
            if (ls.LevelComplete.Value) return "第一关完成～ 更多冒险敬请期待！";
            if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsIds.Count < 2)
                return "等小伙伴加入后一起出发吧";
            if (!ls.Door1Open.Value) return "汪汪：把木箱子推到红色踏板上！";
            if (!ls.BridgeOn.Value) return "喵喵：二段跳跃过壕沟，按 E 拉下拉杆放桥！";
            if (!ls.GateOpen.Value) return "跳进池塘变成鱼鱼游过去，然后两人同时踩住蓝色踏板！";
            return "穿过金色拱门，一起到达终点！";
        }
    }
}
