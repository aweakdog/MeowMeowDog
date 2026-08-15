# 喵喵狗 MeowMeowDog

汪汪（狗狗）和喵喵（猫猫）的双人合作冒险游戏。为情侣设计，两个人一起玩才能通关！

## 环境要求

- macOS（Apple Silicon）
- Unity **6000.3.22f1**（Unity 6.3 LTS，用 Unity Hub 安装）
- 首次打开 Unity Hub 需要登录 Unity 账号（免费 Personal 许可证）

> **校园网注意**：在港科大校园网（及类似地域）下，Unity 编辑器下载 CDN 会被重定向到中国镜像并 404。
> **装 Editor 时需要先开 VPN**；装好后日常开发、局域网联机都不需要 VPN。
> 包管理（packages.unity.com）不受影响。

## 如何运行

1. 用 Unity Hub 打开本项目文件夹（Add project from disk）
2. 首次打开会自动导入包并生成场景（`Assets/Scenes/Main.unity`）和玩家 Prefab
   - 如果没有自动生成，点菜单 `MeowMeowDog → Setup（重新生成场景和 Prefab）`
3. 打开 `Assets/Scenes/Main.unity`，点 Play

## 双人联机（初版为局域网直连）

两台 Mac 连同一个 WiFi：

1. 一方点 **创建房间（我当狗狗）**，屏幕上会显示本机 IP
2. 另一方输入这个 IP，点 **加入房间（我当猫猫）**

单机自测：开一个编辑器 Host，再打一个 Mac 包（File → Build And Run）作为 Client 连 `127.0.0.1`。

> 公网联机（Unity Relay + 房间码）在路线图里，之后加上就不用同一 WiFi 了。

## 操作

| 按键 | 功能 |
| --- | --- |
| WASD | 移动 |
| 空格 | 跳跃（喵喵可以二段跳！） |
| E | 互动（拉杆等） |
| 水里：空格 / Shift | 上浮 / 下潜 |

## 第一关「回家的路」流程

1. **汪汪**力气大：把木箱推到红色踏板上，石门打开
2. **喵喵**身手好：二段跳跃过壕沟，按 E 拉下拉杆，为汪汪放下木桥
3. 一起跳进池塘——变成**狗狗鱼**和**猫猫鱼**游过去（可以钻金色圆环）
4. 两人**同时**踩住两块蓝色踏板，闸门打开
5. 一起穿过金色拱门通关！

## 项目结构

```
Assets/
  Editor/ProjectSetup.cs        # 一键生成场景/Prefab 的编辑器脚本
  Scripts/
    Core/    CameraRig, GameHud  # 相机跟随、任务提示 HUD
    Net/     ConnectionUI, ClientNetworkTransform  # 联机菜单、位置同步
    Player/  PlayerController, PlayerAvatar        # 角色控制、狗猫/鱼鱼造型
    Level/   LevelBuilder, LevelState              # 关卡生成、机关联机状态
prompts/                        # 设计想法记录
```

架构要点：静态场景由 `LevelBuilder` 在各端本地确定性生成；只有玩家位置和机关状态走网络（Host 权威，NGO 的 NetworkVariable 同步）。

**协作注意**：首次打开 Unity 后会生成 `Assets/**/*.meta`、`Packages/packages-lock.json` 和 `ProjectSettings/` 下的配置文件——这些都要一起提交（`.gitignore` 已配置好，`git add -A` 即可）。两个人要用同一个 Unity 版本。

## 开发者命令（可选）

```bash
U="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"

# 重新生成场景/Prefab（改了 ProjectSetup 后用）
"$U" -projectPath . -batchmode -quit -executeMethod MeowMeowDog.EditorTools.ProjectSetup.Run -logFile /tmp/setup.log

# 命令行打包（输出 Builds/MeowMeowDog.app，注意不能加 -nographics）
"$U" -projectPath . -batchmode -executeMethod MeowMeowDog.EditorTools.BuildScript.BuildMac -buildPath Builds/MeowMeowDog.app -logFile /tmp/build.log

# 联机冒烟测试：起两个无头实例互连，看日志里的 [MMDog] 标记
BIN=Builds/MeowMeowDog.app/Contents/MacOS/MeowMeowDog
"$BIN" -batchmode -nographics -autohost -logFile /tmp/host.log &
"$BIN" -batchmode -nographics -autojoin 127.0.0.1 -logFile /tmp/client.log &
```

单机双人自测最方便的方式：编辑器点 Play 当 Host，再开 `Builds/MeowMeowDog.app` 输入 `127.0.0.1` 加入。

## 路线图（对应 prompts/1.idea）

- [x] 第一关：推箱 / 二段跳 / 变鱼游泳 / 双人机关（局域网联机）
- [ ] 公网联机（Unity Relay + 房间码，不用同一 WiFi）
- [ ] 夜晚场景：蝙蝠侠狗狗 & 罗宾猫咪造型和夜间能力
- [ ] 第二关起的连续剧情 + 每关不同主题能力
- [ ] 正式美术模型替换几何体占位
- [ ] 音效和背景音乐
