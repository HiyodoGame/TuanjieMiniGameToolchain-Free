#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MiniGame.StarterKit.Runtime.Audio;
using MiniGame.StarterKit.Runtime.Template;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniGame.StarterKit.Editor.Templates
{
    /// <summary>
    /// Starter Kit 项目脚手架引擎。
    /// 一键生成包含微信小游戏完整配置的空白项目结构、启动场景与推荐设置。
    /// </summary>
    public static class MGSStarterKitTemplateGenerator
    {
        /// <summary>
        /// 生成选项。
        /// </summary>
        public class GenerateOptions
        {
            public string ProjectName = "MiniGameProject";
            public bool ApplyRecommendedSettings = true;
            public bool SwitchBuildTargetToWebGL = true;
            public bool CreateLoginSample = true;
            public bool OpenBootSceneAfterGenerate = true;
        }

        /// <summary>
        /// 生成 Starter Kit 项目模板到指定根目录。
        /// </summary>
        public static void Generate(string rootFolder, GenerateOptions options = null)
        {
            options ??= new GenerateOptions();

            if (string.IsNullOrEmpty(rootFolder))
            {
                rootFolder = "Assets/MiniGameStarterKit";
            }

            if (!rootFolder.StartsWith("Assets/") && !rootFolder.StartsWith("Assets\\"))
            {
                EditorUtility.DisplayDialog("路径错误", "项目模板必须生成在 Assets 目录下。", "确定");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("生成 Starter Kit 项目", "创建目录结构...", 0.1f);
                CreateDirectoryStructure(rootFolder);

                if (options.ApplyRecommendedSettings)
                {
                    EditorUtility.DisplayProgressBar("生成 Starter Kit 项目", "应用微信小游戏推荐设置...", 0.3f);
                    ApplyWeChatMiniGameSettings(options.SwitchBuildTargetToWebGL);
                }

                EditorUtility.DisplayProgressBar("生成 Starter Kit 项目", "创建启动场景与预制体...", 0.5f);
                var prefabPath = Path.Combine(rootFolder, "Prefabs/BootUI.prefab").Replace('\\', '/');
                var scenePath = Path.Combine(rootFolder, "Scenes/Boot.scene").Replace('\\', '/');

                var bootUiRoot = CreateBootUIRoot(options.CreateLoginSample);
                PrefabUtility.SaveAsPrefabAsset(bootUiRoot, prefabPath);
                UnityEngine.Object.DestroyImmediate(bootUiRoot);

                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                }

                var audioConfigPath = CreateAudioConfigAsset(rootFolder);
                CreateAudioManagerInScene(scene, audioConfigPath);

                EditorSceneManager.SaveScene(scene, scenePath);
                AddSceneToBuildSettings(scenePath);

                EditorUtility.DisplayProgressBar("生成 Starter Kit 项目", "生成文档与入口脚本...", 0.8f);
                GenerateEntryScript(rootFolder);
                File.WriteAllText(
                    Path.Combine(rootFolder, "README.md").Replace('\\', '/'),
                    GetReadmeContent(options.ProjectName));
                File.WriteAllText(
                    Path.Combine(rootFolder, "QUICKSTART.md").Replace('\\', '/'),
                    GetQuickStartContent());
                File.WriteAllText(
                    Path.Combine(rootFolder, "Documentation/API_REFERENCE.md").Replace('\\', '/'),
                    GetApiReferenceContent());

                AssetDatabase.Refresh();

                if (options.OpenBootSceneAfterGenerate)
                {
                    EditorSceneManager.OpenScene(scenePath);
                }

                EditorUtility.DisplayDialog(
                    "Starter Kit 项目生成完成",
                    $"已生成到: {rootFolder}\n\n包含：启动场景、UI 预制体、音频管理器、推荐 PlayerSettings、Build Settings 入口。",
                    "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CreateDirectoryStructure(string rootFolder)
        {
            var dirs = new[]
            {
                "Scenes",
                "Prefabs",
                "Audio",
                "Scripts/WeChat",
                "Scripts/UI",
                "Scripts/Audio",
                "Scripts/Utils",
                "Resources",
                "Documentation"
            };

            foreach (var dir in dirs)
            {
                Directory.CreateDirectory(Path.Combine(rootFolder, dir));
            }

            AssetDatabase.Refresh();
        }

        private static void ApplyWeChatMiniGameSettings(bool switchBuildTarget)
        {
            var targetGroup = BuildTargetGroup.WebGL;

            if (switchBuildTarget && EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, BuildTarget.WebGL);
            }

            PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(targetGroup, ManagedStrippingLevel.High);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.dataCaching = true;

            ApplyWasmCodeSplit(true);

            AssetDatabase.SaveAssets();
            Debug.Log("[StarterKit] 已应用微信小游戏推荐 PlayerSettings。");
        }

        private static void ApplyWasmCodeSplit(bool enable)
        {
            try
            {
                var webglSettingsType = typeof(PlayerSettings.WebGL);
                var prop = webglSettingsType.GetProperty("wasmCodeSplit",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (prop != null)
                {
                    prop.SetValue(null, enable);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[StarterKit] 无法设置 wasmCodeSplit: {ex.Message}");
            }
        }

        private static void GenerateEntryScript(string rootFolder)
        {
            var path = Path.Combine(rootFolder, "Scripts/Utils/MiniGameApp.cs").Replace('\\', '/');
            if (File.Exists(path)) return;

            var content =
                "using UnityEngine;\n\n" +
                "namespace MiniGame.StarterKit\n" +
                "{\n" +
                "    /// <summary>\n" +
                "    /// 微信小游戏项目入口。可在 Boot 场景中挂载，用于初始化全局系统。\n" +
                "    /// </summary>\n" +
                "    public class MiniGameApp : MonoBehaviour\n" +
                "    {\n" +
                "        private void Awake()\n" +
                "        {\n" +
                "            DontDestroyOnLoad(gameObject);\n" +
                "            Debug.Log(\"[MiniGameApp] 项目入口已初始化\");\n" +
                "        }\n" +
                "    }\n" +
                "}\n";

            File.WriteAllText(path, content);
        }

        private static string CreateAudioConfigAsset(string rootFolder)
        {
            var path = Path.Combine(rootFolder, "Audio/MGSAudioConfig.asset").Replace('\\', '/');
            if (File.Exists(path)) return path;

            var config = ScriptableObject.CreateInstance<MGSAudioConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            return path;
        }

        private static void CreateAudioManagerInScene(Scene scene, string configPath)
        {
            var go = new GameObject("AudioManager", typeof(MGSAudioManager));
            SceneManager.MoveGameObjectToScene(go, scene);

            var manager = go.GetComponent<MGSAudioManager>();
            var config = AssetDatabase.LoadAssetAtPath<MGSAudioConfig>(configPath);
            manager.Config = config;
        }

        private static GameObject CreateBootUIRoot(bool createLoginSample)
        {
            var root = new GameObject("BootUI", typeof(RectTransform));
            root.SetActive(false);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(root.transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;

            var safeAreaGo = new GameObject("SafeArea", typeof(RectTransform), typeof(MGSUGUIFit));
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            var safeAreaRect = safeAreaGo.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            if (createLoginSample)
            {
                CreateLoginPanel(safeAreaGo.transform);
                CreateAdIAPPanel(safeAreaGo.transform);
            }
            else
            {
                var placeholder = CreateText("Placeholder", "微信小游戏项目已就绪", 48, TextAnchor.MiddleCenter);
                placeholder.transform.SetParent(safeAreaGo.transform, false);
                var rect = placeholder.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemGo.transform.SetParent(root.transform, false);

            root.SetActive(true);
            return root;
        }

        private static void CreateLoginPanel(Transform canvas)
        {
            var panelGo = new GameObject("LoginPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas, false);

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.5f);
            panelRect.anchorMax = new Vector2(0.9f, 0.82f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var titleGo = CreateText("Title", "微信小游戏登录与分享示例", 48, TextAnchor.MiddleCenter);
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var resultGo = CreateText("ResultText", "点击登录按钮开始", 32, TextAnchor.MiddleCenter);
            resultGo.transform.SetParent(panelGo.transform, false);
            var resultRect = resultGo.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0f, 0.45f);
            resultRect.anchorMax = new Vector2(1f, 0.7f);
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;

            var loginButtonGo = new GameObject("LoginButton", typeof(RectTransform), typeof(Image), typeof(Button));
            loginButtonGo.transform.SetParent(panelGo.transform, false);

            var loginButtonImage = loginButtonGo.GetComponent<Image>();
            loginButtonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

            var loginButtonRect = loginButtonGo.GetComponent<RectTransform>();
            loginButtonRect.anchorMin = new Vector2(0.1f, 0.15f);
            loginButtonRect.anchorMax = new Vector2(0.45f, 0.4f);
            loginButtonRect.offsetMin = Vector2.zero;
            loginButtonRect.offsetMax = Vector2.zero;

            var loginButtonLabel = CreateText("Text", "微信登录", 36, TextAnchor.MiddleCenter);
            loginButtonLabel.transform.SetParent(loginButtonGo.transform, false);
            var loginLabelRect = loginButtonLabel.GetComponent<RectTransform>();
            loginLabelRect.anchorMin = Vector2.zero;
            loginLabelRect.anchorMax = Vector2.one;
            loginLabelRect.offsetMin = Vector2.zero;
            loginLabelRect.offsetMax = Vector2.zero;

            var shareButtonGo = new GameObject("ShareButton", typeof(RectTransform), typeof(Image), typeof(Button));
            shareButtonGo.transform.SetParent(panelGo.transform, false);

            var shareButtonImage = shareButtonGo.GetComponent<Image>();
            shareButtonImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);

            var shareButtonRect = shareButtonGo.GetComponent<RectTransform>();
            shareButtonRect.anchorMin = new Vector2(0.55f, 0.15f);
            shareButtonRect.anchorMax = new Vector2(0.9f, 0.4f);
            shareButtonRect.offsetMin = Vector2.zero;
            shareButtonRect.offsetMax = Vector2.zero;

            var shareButtonLabel = CreateText("Text", "分享", 36, TextAnchor.MiddleCenter);
            shareButtonLabel.transform.SetParent(shareButtonGo.transform, false);
            var shareLabelRect = shareButtonLabel.GetComponent<RectTransform>();
            shareLabelRect.anchorMin = Vector2.zero;
            shareLabelRect.anchorMax = Vector2.one;
            shareLabelRect.offsetMin = Vector2.zero;
            shareLabelRect.offsetMax = Vector2.zero;

            var loginManager = panelGo.AddComponent<MGSLoginManager>();
            loginManager.LoginButton = loginButtonGo.GetComponent<Button>();
            loginManager.ShareButton = shareButtonGo.GetComponent<Button>();
            loginManager.ResultText = resultGo.GetComponent<Text>();
        }

        private static void CreateAdIAPPanel(Transform canvas)
        {
            var panelGo = new GameObject("AdIAPPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas, false);

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.42f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var titleGo = CreateText("Title", "广告与支付示例", 48, TextAnchor.MiddleCenter);
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var resultGo = CreateText("ResultText", "点击按钮测试", 32, TextAnchor.MiddleCenter);
            resultGo.transform.SetParent(panelGo.transform, false);
            var resultRect = resultGo.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0f, 0.45f);
            resultRect.anchorMax = new Vector2(1f, 0.7f);
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;

            var adButtonGo = new GameObject("WatchAdButton", typeof(RectTransform), typeof(Image), typeof(Button));
            adButtonGo.transform.SetParent(panelGo.transform, false);

            var adButtonImage = adButtonGo.GetComponent<Image>();
            adButtonImage.color = new Color(0.9f, 0.5f, 0.1f, 1f);

            var adButtonRect = adButtonGo.GetComponent<RectTransform>();
            adButtonRect.anchorMin = new Vector2(0.1f, 0.15f);
            adButtonRect.anchorMax = new Vector2(0.45f, 0.4f);
            adButtonRect.offsetMin = Vector2.zero;
            adButtonRect.offsetMax = Vector2.zero;

            var adButtonLabel = CreateText("Text", "看广告", 36, TextAnchor.MiddleCenter);
            adButtonLabel.transform.SetParent(adButtonGo.transform, false);
            var adLabelRect = adButtonLabel.GetComponent<RectTransform>();
            adLabelRect.anchorMin = Vector2.zero;
            adLabelRect.anchorMax = Vector2.one;
            adLabelRect.offsetMin = Vector2.zero;
            adLabelRect.offsetMax = Vector2.zero;

            var payButtonGo = new GameObject("PayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            payButtonGo.transform.SetParent(panelGo.transform, false);

            var payButtonImage = payButtonGo.GetComponent<Image>();
            payButtonImage.color = new Color(0.9f, 0.2f, 0.5f, 1f);

            var payButtonRect = payButtonGo.GetComponent<RectTransform>();
            payButtonRect.anchorMin = new Vector2(0.55f, 0.15f);
            payButtonRect.anchorMax = new Vector2(0.9f, 0.4f);
            payButtonRect.offsetMin = Vector2.zero;
            payButtonRect.offsetMax = Vector2.zero;

            var payButtonLabel = CreateText("Text", "支付", 36, TextAnchor.MiddleCenter);
            payButtonLabel.transform.SetParent(payButtonGo.transform, false);
            var payLabelRect = payButtonLabel.GetComponent<RectTransform>();
            payLabelRect.anchorMin = Vector2.zero;
            payLabelRect.anchorMax = Vector2.one;
            payLabelRect.offsetMin = Vector2.zero;
            payLabelRect.offsetMax = Vector2.zero;

            var manager = panelGo.AddComponent<MGSAdIAPManager>();
            manager.WatchAdButton = adButtonGo.GetComponent<Button>();
            manager.PayButton = payButtonGo.GetComponent<Button>();
            manager.ResultText = resultGo.GetComponent<Text>();
        }

        private static GameObject CreateText(string name, string content, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == scenePath)) return;

            var list = scenes.ToList();
            list.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static string GetReadmeContent(string projectName)
        {
            return $"# {projectName}\n\n" +
                   "由 **MiniGame Starter Kit** 自动生成的微信小游戏项目模板。\n" +
                   "本项目已包含启动场景、UI 预制体、微信 API 封装、安全区适配与推荐 PlayerSettings，可快速接入微信小游戏生态。\n\n" +
                   "## 目录结构\n\n" +
                   "```\n" +
                   "Assets/YourProject/\n" +
                   "├── Scenes/\n" +
                   "│   └── Boot.scene              # 启动场景，已加入 Build Settings\n" +
                   "├── Prefabs/\n" +
                   "│   └── BootUI.prefab           # 可复用的启动 UI 预制体（含 SafeArea 适配）\n" +
                   "├── Audio/\n" +
                   "│   └── MGSAudioConfig.asset    # 音频配置表（BGM/音效 ID 管理）\n" +
                   "├── Scripts/\n" +
                   "│   ├── WeChat/                 # 微信 API 相关脚本\n" +
                   "│   ├── UI/                     # UI 相关脚本\n" +
                   "│   ├── Audio/                  # 音频相关脚本\n" +
                   "│   └── Utils/                  # 通用工具与项目入口\n" +
                   "├── Resources/                  # 运行时资源目录\n" +
                   "└── Documentation/              # 项目文档\n" +
                   "    ├── QUICKSTART.md           # 5 分钟快速上手\n" +
                   "    └── API_REFERENCE.md        # API 参考手册\n" +
                   "```\n\n" +
                   "## 已集成能力\n\n" +
                   "- **登录**：`MGSWeChatAdapter.Login()`\n" +
                   "- **分享**：`MGSWeChatAdapter.ShareAppMessage()`\n" +
                   "- **云存储**：`MGSWeChatAdapter.GetCloudStorage()` / `SetCloudStorage()`\n" +
                   "- **激励视频/Banner 广告**：`MGSWeChatAdAdapter`\n" +
                   "- **米大师支付**：`MGSWeChatIAPAdapter.RequestMidasPayment()`\n" +
                   "- **音频管理**：`MGSAudioManager` + `MGSAudioConfig`\n" +
                   "- **安全区适配**：`MGSUGUIFit` + `MGSUIHelper`\n\n" +
                   "## 推荐设置\n\n" +
                   "生成时已自动应用微信小游戏推荐 PlayerSettings：\n\n" +
                   "| 配置项 | 推荐值 |\n" +
                   "|---|---|\n" +
                   "| Scripting Backend | IL2CPP |\n" +
                   "| Managed Stripping Level | High |\n" +
                   "| Compression Format | Brotli |\n" +
                   "| Exception Support | None |\n" +
                   "| Memory Size | 256 MB |\n" +
                   "| Data Caching | 启用 |\n" +
                   "| WASM Code Splitting | 启用 |\n\n" +
                   "## 文档索引\n\n" +
                   "- [快速入门](./QUICKSTART.md)：从生成项目到运行在微信开发者工具。\n" +
                   "- [API 参考](./Documentation/API_REFERENCE.md)：登录、分享、广告、支付、UI 适配 API 详细说明。\n\n" +
                   "## 下一步\n\n" +
                   "1. 打开 `Scenes/Boot.scene`。\n" +
                   "2. 阅读 `QUICKSTART.md` 配置微信小游戏 AppID。\n" +
                   "3. 参考 `API_REFERENCE.md` 替换示例中的广告位 ID、支付 offerId。\n" +
                   "4. 使用团结引擎的「微信小游戏」构建流程导出项目。\n";
        }

        private static string GetQuickStartContent()
        {
            return "# 快速入门\n\n" +
                   "本指南帮助你在 5 分钟内把自动生成的 Starter Kit 项目运行到微信开发者工具。\n\n" +
                   "## 1. 环境要求\n\n" +
                   "- 团结引擎（Tuanjie）2022.3 LTS\n" +
                   "- 已安装 `com.qq.weixin.minigame` 微信小游戏 SDK\n" +
                   "- 微信开发者工具（用于真机/模拟器预览）\n\n" +
                   "## 2. 生成项目\n\n" +
                   "1. 在编辑器菜单选择 `Window > MiniGame > Starter Kit > Create Project`。\n" +
                   "2. 填写项目名称与输出路径（必须在 `Assets/` 下）。\n" +
                   "3. 勾选「应用推荐设置」与「切换 WebGL Build Target」。\n" +
                   "4. 点击生成，等待 `Boot.scene` 自动打开。\n\n" +
                   "## 3. 配置微信小游戏 AppID\n\n" +
                   "1. 打开 `Edit > Project Settings > Player`。\n" +
                   "2. 选择 WebGL 平台。\n" +
                   "3. 在 `WeChat Mini Game` 区域填入你的小游戏 AppID。\n" +
                   "4. 确认 `Mini Game Program Description` 已填写。\n\n" +
                   "## 4. 运行示例\n\n" +
                   "打开 `Scenes/Boot.scene`，场景中包含两个示例面板：\n\n" +
                   "- **登录与分享示例**：点击「微信登录」「分享」按钮，观察结果文本。\n" +
                   "- **广告与支付示例**：点击「看广告」「支付」按钮，观察结果文本。\n\n" +
                   "> **注意**：Editor 中非微信环境会返回模拟失败（`not_wechat`）。\n" +
                   "> 如需在 Editor 中测试广告成功流程，可在初始化脚本中设置 `MGSWeChatAdAdapter.SimulateSuccessInEditor = true`。\n\n" +
                   "## 5. 构建到微信小游戏\n\n" +
                   "1. 选择 `File > Build Settings`。\n" +
                   "2. 确认 Build Target 为 **WebGL**。\n" +
                   "3. 点击 `Build And Run` 或 `Build`，选择输出目录。\n" +
                   "4. 使用微信开发者工具打开生成的 `minigame` 目录，即可预览与调试。\n\n" +
                   "## 6. 扩展项目\n\n" +
                   "- 业务脚本放在 `Scripts/` 对应子目录。\n" +
                   "- UI 预制体放在 `Prefabs/`。\n" +
                   "- 音频资源在 `Audio/MGSAudioConfig.asset` 中通过 ID 配置。\n" +
                   "- 在 `API_REFERENCE.md` 中查看各 API 的调用方式与替换点。\n\n" +
                   "## 7. 常见问题\n\n" +
                   "### Q1：Editor 中点击登录/广告/支付为什么失败？\n\n" +
                   "A：适配器在非微信环境下会返回模拟失败，这是预期行为。构建到微信小游戏后，会通过 JSBridge 调用真实微信 API。\n\n" +
                   "### Q2：刘海屏/圆角屏 UI 被遮挡怎么办？\n\n" +
                   "A：`BootUI` 预制体已包含 `SafeArea` 根节点并挂载 `MGSUGUIFit`。所有业务 UI 应放在 `SafeArea` 下，即可自动适配。\n\n" +
                   "### Q3：如何替换示例中的广告位 ID 与支付 offerId？\n\n" +
                   "A：在 `Boot.scene` 中选中 `AdIAPPanel`，在 Inspector 中修改 `MGSAdIAPManager` 的 `RewardedAdUnitId` 与 `OfferId`。\n";
        }

        private static string GetApiReferenceContent()
        {
            return "# API 参考\n\n" +
                   "本文档列出 Starter Kit 提供的微信小游戏运行时 API 与 UI 适配工具。\n\n" +
                   "---\n\n" +
                   "## 1. MGSWeChatAdapter — 登录 / 分享 / 云存储\n\n" +
                   "命名空间：`MiniGame.StarterKit.Runtime.WeChat`\n\n" +
                   "### 1.1 属性\n\n" +
                   "```csharp\n" +
                   "public static bool IsWeChat { get; }\n" +
                   "```\n\n" +
                   "当前是否运行在微信小游戏 WebGL 环境。Editor 与原生平台均返回 `false`。\n\n" +
                   "### 1.2 事件\n\n" +
                   "```csharp\n" +
                   "public static event Action<bool, WXUserInfo, string> OnLoginResult;\n" +
                   "public static event Action<bool, string> OnShareResult;\n" +
                   "public static event Action<bool, string, string> OnCloudStorageRead;\n" +
                   "public static event Action<bool, string> OnCloudStorageWrite;\n" +
                   "```\n\n" +
                   "### 1.3 Login\n\n" +
                   "```csharp\n" +
                   "public static void Login(WXCallback<WXUserInfo> callback = null)\n" +
                   "```\n\n" +
                   "触发微信登录。\n\n" +
                   "**示例：**\n\n" +
                   "```csharp\n" +
                   "MGSWeChatAdapter.Login((success, user, err) =>\n" +
                   "{\n" +
                   "    if (success)\n" +
                   "        Debug.Log($\"登录成功：{user.OpenId}\");\n" +
                   "    else\n" +
                   "        Debug.Log($\"登录失败：{err}\");\n" +
                   "});\n" +
                   "```\n\n" +
                   "### 1.4 ShareAppMessage\n\n" +
                   "```csharp\n" +
                   "public static void ShareAppMessage(WXShareInfo info, WXCallback<bool> callback = null)\n" +
                   "```\n\n" +
                   "**示例：**\n\n" +
                   "```csharp\n" +
                   "var info = new WXShareInfo(\"快来体验我的微信小游戏！\", imageUrl: \"https://...\", query: \"invite=123\");\n" +
                   "MGSWeChatAdapter.ShareAppMessage(info, (success, data, err) =>\n" +
                   "{\n" +
                   "    Debug.Log(success ? \"分享成功\" : $\"分享失败：{err}\");\n" +
                   "});\n" +
                   "```\n\n" +
                   "### 1.5 云存储\n\n" +
                   "```csharp\n" +
                   "public static void GetCloudStorage(string key)\n" +
                   "public static void SetCloudStorage(string key, string value)\n" +
                   "```\n\n" +
                   "通过 `OnCloudStorageRead` / `OnCloudStorageWrite` 接收结果。\n\n" +
                   "---\n\n" +
                   "## 2. MGSWeChatAdAdapter — 广告\n\n" +
                   "命名空间：`MiniGame.StarterKit.Runtime.WeChat`\n\n" +
                   "### 2.1 属性\n\n" +
                   "```csharp\n" +
                   "public static bool SimulateSuccessInEditor { get; set; }\n" +
                   "```\n\n" +
                   "在 Editor 中模拟广告成功回调，用于本地 UI 流程测试。\n\n" +
                   "### 2.2 事件\n\n" +
                   "```csharp\n" +
                   "public static event Action<bool, MGSAdResult, string> OnRewardedVideoClosed;\n" +
                   "public static event Action<bool, string> OnBannerLoaded;\n" +
                   "```\n\n" +
                   "### 2.3 ShowRewardedVideoAd\n\n" +
                   "```csharp\n" +
                   "public static void ShowRewardedVideoAd(string adUnitId, WXCallback<MGSAdResult> callback = null)\n" +
                   "```\n\n" +
                   "自动完成加载 → 展示 → 关闭回调。通过 `MGSAdResult.IsEnded` 判断是否完整播放。\n\n" +
                   "**示例：**\n\n" +
                   "```csharp\n" +
                   "MGSWeChatAdAdapter.ShowRewardedVideoAd(\"adunit-xxxxxx\", (success, result, err) =>\n" +
                   "{\n" +
                   "    if (success && result.IsEnded)\n" +
                   "        Debug.Log(\"发放奖励\");\n" +
                   "    else\n" +
                   "        Debug.Log($\"广告失败或提前关闭：{err}\");\n" +
                   "});\n" +
                   "```\n\n" +
                   "### 2.4 Banner 广告\n\n" +
                   "```csharp\n" +
                   "public static void LoadBannerAd(string adUnitId, int width, int height, int adIntervals = 30, WXCallback<bool> callback = null)\n" +
                   "public static void HideBannerAd()\n" +
                   "public static void DestroyBannerAd()\n" +
                   "```\n\n" +
                   "`LoadBannerAd` 会自动把 Banner 放在屏幕底部。\n\n" +
                   "---\n\n" +
                   "## 3. MGSWeChatIAPAdapter — 虚拟支付\n\n" +
                   "命名空间：`MiniGame.StarterKit.Runtime.WeChat`\n\n" +
                   "### 3.1 事件\n\n" +
                   "```csharp\n" +
                   "public static event Action<bool, string> OnMidasPaymentResult;\n" +
                   "```\n\n" +
                   "### 3.2 RequestMidasPayment\n\n" +
                   "```csharp\n" +
                   "public static void RequestMidasPayment(MGSWeChatPaymentInfo info, WXCallback<bool> callback = null)\n" +
                   "```\n\n" +
                   "**示例：**\n\n" +
                   "```csharp\n" +
                   "var info = new MGSWeChatPaymentInfo\n" +
                   "{\n" +
                   "    OfferId = \"123456\",\n" +
                   "    BuyQuantity = 10,\n" +
                   "    OutTradeNo = DateTime.UtcNow.Ticks.ToString(),\n" +
                   "    Env = 0   // 0 正式，1 沙箱\n" +
                   "};\n" +
                   "MGSWeChatIAPAdapter.RequestMidasPayment(info, (success, data, err) =>\n" +
                   "{\n" +
                   "    Debug.Log(success ? \"支付成功\" : $\"支付失败：{err}\");\n" +
                   "});\n" +
                   "```\n\n" +
                   "### 3.3 CheckIsSupportMidasPayment\n\n" +
                   "```csharp\n" +
                   "public static void CheckIsSupportMidasPayment(WXCallback<bool> callback = null)\n" +
                   "```\n\n" +
                   "建议在实际支付前调用，确认当前环境/账号支持米大师支付。\n\n" +
                   "---\n\n" +
                   "## 4. UI 适配\n\n" +
                   "命名空间：`MiniGame.StarterKit.Runtime.UI` / `MiniGame.StarterKit.Runtime.Template`\n\n" +
                   "### 4.1 MGSUIHelper\n\n" +
                   "```csharp\n" +
                   "public static Rect GetSafeArea()\n" +
                   "public static void ApplySafeArea(RectTransform rect, Rect safeArea, Vector4 padding, bool resetAnchors = true)\n" +
                   "public static void SetFullStretch(RectTransform rect)\n" +
                   "```\n\n" +
                   "`GetSafeArea` 在微信环境下读取 `WX.GetWindowInfo().safeArea`，其他环境使用 `Screen.safeArea`。\n\n" +
                   "### 4.2 MGSUGUIFit\n\n" +
                   "挂载在 Canvas 下的全屏根面板上，自动根据安全区调整 `offsetMin/offsetMax`。\n\n" +
                   "关键参数：\n\n" +
                   "- `ApplyOnAwake`：启动时自动适配\n" +
                   "- `ApplyOnResolutionChange`：屏幕旋转/尺寸变化时自动适配\n" +
                   "- `Padding`：额外边距\n" +
                   "- `ResetAnchors`：是否重置为全拉伸锚点\n\n" +
                   "---\n\n" +
                   "## 5. 音频管理\n\n" +
                   "命名空间：`MiniGame.StarterKit.Runtime.Audio`\n\n" +
                   "### 5.1 MGSAudioConfig\n\n" +
                   "音频配置表 ScriptableObject，支持通过 ID 管理 BGM 与音效。\n\n" +
                   "创建方式：`Assets > Create > MiniGame > Audio Config`。\n\n" +
                   "每个条目包含：`Id`、`Clip`、`IsMusic`、`Volume`、`Pitch`、`Loop`。\n\n" +
                   "### 5.2 MGSAudioManager\n\n" +
                   "```csharp\n" +
                   "public void PlayMusic(AudioClip clip, bool loop = true, float volume = 1f)\n" +
                   "public void PlayMusic(string id)\n" +
                   "public void StopMusic()\n" +
                   "public void PauseMusic()\n" +
                   "public void ResumeMusic()\n" +
                   "public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)\n" +
                   "public void PlaySfx(string id)\n" +
                   "public void PlayOneShot(AudioClip clip, float volumeScale = 1f)\n" +
                   "public void SetMusicVolume(float volume)\n" +
                   "public void SetSfxVolume(float volume)\n" +
                   "public void SetMuted(bool muted)\n" +
                   "```\n\n" +
                   "**示例：**\n\n" +
                   "```csharp\n" +
                   "// 通过配置表 ID 播放\n" +
                   "MGSAudioManager.PlayMusicById(\"BGM_Main\");\n" +
                   "MGSAudioManager.PlaySfxById(\"SFX_Click\");\n" +
                   "\n" +
                   "// 直接播放 AudioClip\n" +
                   "MGSAudioManager.Instance.PlaySfx(clickClip, pitch: 1.1f);\n" +
                   "```\n\n" +
                   "### 5.3 音量持久化\n\n" +
                   "`SetMusicVolume`、`SetSfxVolume`、`SetMuted` 会自动保存到 `PlayerPrefs`，下次启动自动恢复。\n\n" +
                   "---\n\n" +
                   "## 6. 示例组件\n\n" +
                   "| 组件 | 路径 | 说明 |\n" +
                   "|---|---|---|\n" +
                   "| `MGSLoginManager` | `Runtime/Template` | 登录/分享按钮示例 |\n" +
                   "| `MGSAdIAPManager` | `Runtime/Template` | 广告/支付按钮示例 |\n" +
                   "| `MGSUGUIFit` | `Runtime/Template` | 安全区适配 |\n" +
                   "| `MiniGameApp` | 生成后 `Scripts/Utils` | 项目入口 |\n";
        }
    }
}
#endif
