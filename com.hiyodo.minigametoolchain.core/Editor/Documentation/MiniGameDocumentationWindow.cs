#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Documentation
{
    /// <summary>
    /// MiniGame Toolchain 完整使用文档窗口。
    /// </summary>
    public class MiniGameDocumentationWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private static readonly Vector2 WindowSize = new Vector2(640, 720);

        [MenuItem("Window/MiniGame/Documentation", false, 100)]
        public static void Open()
        {
            var window = GetWindow<MiniGameDocumentationWindow>(true, "MiniGame Toolchain Documentation", true);
            window.minSize = WindowSize;
            window.maxSize = new Vector2(WindowSize.x, 1200);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(16);
            DrawHeader();
            GUILayout.Space(16);
            DrawSection("一、简介", Introduction);
            DrawSection("二、安装与导入", Installation);
            DrawSection("三、产品矩阵", Products);
            DrawSection("四、快速开始", QuickStart);
            DrawSection("五、各功能使用指南", Usage);
            DrawSection("六、免费版与付费版", Licensing);
            DrawSection("七、常见问题", FAQ);
            DrawSection("八、支持与反馈", Support);
            GUILayout.Space(24);

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUILayout.Label("MiniGame Toolchain 使用文档", titleStyle);

            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = new GUIStyleState { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUILayout.Label(
                $"版本 v{MiniGameToolchainVersion.CurrentVersion} | 专为团结引擎微信小游戏打造",
                subStyle);
        }

        private void DrawSection(string title, string content)
        {
            var sectionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = new GUIStyleState { textColor = new Color(0.45f, 0.85f, 0.65f) }
            };
            GUILayout.Label(title, sectionTitleStyle);
            GUILayout.Space(8);

            var contentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true,
                normal = new GUIStyleState { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            GUILayout.Label(content, contentStyle);
            GUILayout.Space(20);
        }

        private const string Introduction =
            "MiniGame Toolchain 是一套面向 <b>团结引擎（Tuanjie Engine）</b> 微信小游戏开发的编辑器工具链。\n\n" +
            "它由 5 个本地 UPM 包组成，覆盖项目构建、性能监控、项目脚手架和一站式构建流水线：\n" +
            "• <b>Core</b>：共享核心框架，提供诊断引擎、规则引擎、微信桥接和统一 UI 基座。\n" +
            "• <b>Build Optimizer</b>：构建优化工具，扫描纹理、Shader、字体和项目设置。\n" +
            "• <b>Performance Suite</b>：运行时性能监控与编辑器可视化分析。\n" +
            "• <b>Starter Kit</b>：项目脚手架，提供场景、音频、UI、存档管理器和微信 SDK 适配。\n" +
            "• <b>Builder Pro</b>：付费一站式构建流水线，支持多平台同步、团队看板、CI/CD 模板、CDN 上传和 Webhook。\n\n" +
            "所有功能均通过微信 SDK 反射桥接实现，不对编译期产生硬依赖。";

        private const string Installation =
            "<b>方式一：通过 AssetStore 导入 .unitypackage（推荐购买用户）</b>\n" +
            "1. 在 AssetStore 购买或下载对应包。\n" +
            "2. 双击下载的 .unitypackage 文件，或在 Unity 中选择 <b>Assets → Import Package → Custom Package...</b>。\n" +
            "3. 选择所有内容并点击 <b>Import</b>。导入后内容会出现在 <b>Assets/</b> 目录下。\n\n" +
            "<b>方式二：通过 UPM 安装（推荐免费版用户）</b>\n" +
            "1. 打开 <b>Window → Package Manager</b>。\n" +
            "2. 点击 <b>+ → Add package from tarball...</b>，选择对应包的 .tgz 或 .package 文件。\n" +
            "3. 按依赖顺序安装：先安装 <b>Core</b>，再安装其他包。\n\n" +
            "<b>注意</b>：Builder Pro 为付费包，必须导入合集包（含 core + build-optimizer 完整版），不可与免费裁剪版 build-optimizer 混装。";

        private const string Products =
            "<b>Core</b>\n" +
            "所有工具共享的基础：诊断扫描器、反射规则引擎、设置快照回滚、微信 SDK 桥接、统一编辑器 UI 基座。\n\n" +
            "<b>Build Optimizer</b>\n" +
            "免费版：纹理/Shader/字体/项目设置扫描与报告。\n" +
            "付费版：一键自动优化、智能 AssetBundle 分组、字体子集生成、Shader 变体分析。\n\n" +
            "<b>Performance Suite</b>\n" +
            "免费版：运行时 FPS、内存、启动耗时采集；编辑器可视化窗口。\n" +
            "付费版：优化建议引擎、设备分级模拟、自动化测试、热点函数 Top N、内存深度分析。\n\n" +
            "<b>Starter Kit</b>\n" +
            "免费版：项目目录模板、核心系统框架（场景/音频/UI/存档）、微信 SDK 反射桥接。\n" +
            "付费版：本地化系统、CDN 资源加载器。\n\n" +
            "<b>Builder Pro</b>（付费）\n" +
            "可视化构建流水线、多平台同步构建、团队看板、版本归档、CI/CD 模板、自定义构建规则、CDN 上传、Webhook 通知。";

        private const string QuickStart =
            "<b>1. 首次打开项目</b>\n" +
            "导入 Core 后，会自动弹出 <b>Welcome</b> 窗口，显示当前版本和已具备功能。可通过 <b>Window → MiniGame → Welcome</b> 重新打开。\n\n" +
            "<b>2. 打开各产品窗口</b>\n" +
            "所有窗口统一位于 <b>Window / MiniGame /</b> 菜单下：\n" +
            "• Build Optimizer：Window → MiniGame → Build Optimizer → Build Optimizer Window\n" +
            "• Performance Suite：Window → MiniGame → Performance Suite → ...\n" +
            "• Starter Kit：Window → MiniGame → Starter Kit → ...\n" +
            "• Builder Pro：Window → MiniGame → Builder Pro → Builder Window\n\n" +
            "<b>3. 创建默认 HUD（Performance Suite）</b>\n" +
            "选择 <b>Assets → Create → MiniGame → Performance Suite → Create HUD Assets</b> 生成默认 HUD Config 和 Prefab。\n\n" +
            "<b>4. 配置微信小游戏</b>\n" +
            "编辑 <b>Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset</b>，填写 AppID 和 CDN 地址。";

        private const string Usage =
            "<b>Build Optimizer 使用</b>\n" +
            "1. 打开 Build Optimizer Window。\n" +
            "2. 选择需要扫描的类别（Texture / Shader / Font / Settings）。\n" +
            "3. 点击 <b>Scan</b>，查看问题列表和优化建议。\n" +
            "4. 付费版可点击 <b>One-Click Optimize</b> 自动修复部分问题。\n\n" +
            "<b>Performance Suite 使用</b>\n" +
            "1. 在目标设备或编辑器 Play 模式下运行项目。\n" +
            "2. 通过 HUD 或 Performance Suite 窗口查看 FPS、内存、启动耗时。\n" +
            "3. 在编辑器中使用 Optimization Advisor 生成优化建议报告。\n\n" +
            "<b>Builder Pro 使用</b>\n" +
            "1. 打开 Builder Pro 窗口，点击 <b>New Config</b> 创建流水线配置。\n" +
            "2. 添加构建任务（WeChat MiniGame / WebGL / Douyin / Kuaishou / TapTap）。\n" +
            "3. 配置自定义规则（资源大小、必需场景、首包大小）。\n" +
            "4. 点击 <b>Build All</b> 或 <b>Build Selected</b> 执行构建。\n" +
            "5. 构建完成后可查看 Team Dashboard 和归档目录。";

        private const string Licensing =
            "本工具链采用 <b>二态门控模型</b>：\n\n" +
            "<b>免费版</b>\n" +
            "• 从公开 UPM 仓库或 AssetStore 免费包获取。\n" +
            "• 仅包含 Free 级功能，Pro 代码已被物理裁剪。\n" +
            "• 无需 License Key，直接导入使用。\n\n" +
            "<b>付费版（AssetStore 购买）</b>\n" +
            "• 通过 .unitypackage 合集包或付费 UPM 包交付。\n" +
            "• 源码仓库中常驻 <b>MGSAssetStoreEdition</b> 标记类，检测到该标记后自动解锁全部功能。\n" +
            "• 购买后导入即用，无需 License Key、无需激活、无需联网。\n\n" +
            "<b>注意</b>：请勿将付费版 Builder Pro 与免费裁剪版 Build Optimizer 混装，否则会因 Pro 代码缺失而编译报错。";

        private const string FAQ =
            "<b>Q：导入后顶部状态栏显示 Free Edition，但我是付费用户？</b>\n" +
            "A：请确认导入的是合集包（含 Builder Pro），并且 <b>builder-pro/Editor/Licensing/MGSAssetStoreEdition.cs</b> 文件存在。独立 builder-pro 包不会显示完整版。\n\n" +
            "<b>Q：为什么构建时提示必需场景不存在？</b>\n" +
            "A：Builder Pro 默认启用 <b>必需场景规则</b>，请检查配置中的 <b>Required Scenes</b> 路径是否匹配你的项目。\n\n" +
            "<b>Q：Performance Suite 在真机上不显示 FPS？</b>\n" +
            "A：FPS 在 WebGL/真机可通过时间采样获得，但 DrawCall / SetPassCall / 三角面数在 WebGL 无公开 API，真机上可能显示为 0。\n\n" +
            "<b>Q：免费版和付费版能否同时安装？</b>\n" +
            "A：不能。Builder Pro 硬编译依赖完整版 Build Optimizer，与免费裁剪版混装会编译报错。请使用合集包。";

        private const string Support =
            "• 官方文档：见 GitHub / Gitee 免费仓库 README\n" +
            "• 支持邮箱：dev@hiyodo.studio\n" +
            "• 问题反馈：AssetStore 商品页面或 GitHub Free 仓库 Issues\n" +
            "• 商业授权：购买 Builder Pro 或 Full Bundle 后自动获得完整版功能\n\n" +
            "Made with ❤️ by Hiyodo Studio";
    }
}
#endif
