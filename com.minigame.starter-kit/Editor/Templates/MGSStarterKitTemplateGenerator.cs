#if UNITY_EDITOR
using System.IO;
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
    /// 生成 Starter Kit 免费版项目模板：
    /// 空项目启动场景 + 微信登录示例 + UGUI 安全区适配。
    /// </summary>
    public static class MGSStarterKitTemplateGenerator
    {
        /// <summary>
        /// 生成模板到指定根目录。
        /// </summary>
        public static void Generate(string rootFolder)
        {
            if (string.IsNullOrEmpty(rootFolder))
            {
                rootFolder = "Assets/MiniGameStarterKit";
            }

            if (!rootFolder.StartsWith("Assets/") && !rootFolder.StartsWith("Assets\\"))
            {
                EditorUtility.DisplayDialog("路径错误", "项目模板必须生成在 Assets 目录下。", "确定");
                return;
            }

            Directory.CreateDirectory(rootFolder);
            Directory.CreateDirectory(Path.Combine(rootFolder, "Scenes"));
            Directory.CreateDirectory(Path.Combine(rootFolder, "Prefabs"));
            Directory.CreateDirectory(Path.Combine(rootFolder, "Scripts"));

            AssetDatabase.Refresh();

            var prefabPath = Path.Combine(rootFolder, "Prefabs/BootUI.prefab").Replace('\\', '/');
            var scenePath = Path.Combine(rootFolder, "Scenes/Boot.scene").Replace('\\', '/');
            var readmePath = Path.Combine(rootFolder, "README.md").Replace('\\', '/');

            // 创建 BootUI 预制体
            var bootUiRoot = CreateBootUIRoot();
            PrefabUtility.SaveAsPrefabAsset(bootUiRoot, prefabPath);
            Object.DestroyImmediate(bootUiRoot);

            // 创建启动场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
            }

            EditorSceneManager.SaveScene(scene, scenePath);

            // 将场景加入 Build Settings
            AddSceneToBuildSettings(scenePath);

            // 生成说明文档
            File.WriteAllText(readmePath, GetReadmeContent());

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Starter Kit 模板生成完成", $"已生成到: {rootFolder}", "确定");
        }

        private static GameObject CreateBootUIRoot()
        {
            var root = new GameObject("BootUI", typeof(RectTransform));
            root.SetActive(false);

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MGSUGUIFit));
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

            // LoginPanel
            var panelGo = new GameObject("LoginPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);

            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.3f);
            panelRect.anchorMax = new Vector2(0.9f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Title
            var titleGo = CreateText("Title", "微信小游戏登录示例", 48, TextAnchor.MiddleCenter);
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // ResultText
            var resultGo = CreateText("ResultText", "点击登录按钮开始", 32, TextAnchor.MiddleCenter);
            resultGo.transform.SetParent(panelGo.transform, false);
            var resultRect = resultGo.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0f, 0.45f);
            resultRect.anchorMax = new Vector2(1f, 0.7f);
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;

            // LoginButton
            var buttonGo = new GameObject("LoginButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panelGo.transform, false);

            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.25f, 0.15f);
            buttonRect.anchorMax = new Vector2(0.75f, 0.4f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonLabel = CreateText("Text", "微信登录", 36, TextAnchor.MiddleCenter);
            buttonLabel.transform.SetParent(buttonGo.transform, false);
            var labelRect = buttonLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // LoginManager
            var loginManager = panelGo.AddComponent<MGSLoginManager>();
            loginManager.LoginButton = buttonGo.GetComponent<Button>();
            loginManager.ResultText = resultGo.GetComponent<Text>();

            // EventSystem
            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemGo.transform.SetParent(root.transform, false);

            root.SetActive(true);
            return root;
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
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static string GetReadmeContent()
        {
            return "# MiniGame Starter Kit - 免费版项目模板\n\n" +
                   "本目录由 MiniGame Starter Kit 自动生成，包含一个最简可运行的微信小游戏启动模板。\n\n" +
                   "## 目录结构\n\n" +
                   "- Scenes/Boot.scene：启动场景，包含登录示例 UI。\n" +
                   "- Prefabs/BootUI.prefab：可复用的启动 UI 预制体（Canvas + 登录面板 + EventSystem）。\n" +
                   "- Scripts/：可放置项目业务脚本（当前为空，由开发者自行扩展）。\n\n" +
                   "## 关键组件\n\n" +
                   "- MGSUGUIFit：挂载在 Canvas 上，根据 Screen.safeArea 自动适配刘海屏/圆角屏。\n" +
                   "- MGSLoginManager：绑定登录按钮，调用 MGSWeChatAdapter.Login()。\n" +
                   "- MGSWeChatAdapter：提供微信登录/分享/云存储的 Editor Mock，真机环境下接入 JSBridge。\n\n" +
                   "## 使用说明\n\n" +
                   "1. 打开 Scenes/Boot.scene。\n" +
                   "2. 点击 Play，在 Game 视图中点击 [微信登录] 按钮。\n" +
                   "3. 在 Editor 环境下会模拟登录失败（非微信环境）；构建到微信小游戏后即可调用真实登录。\n" +
                   "4. 根据业务需求扩展 Scripts/ 目录下的脚本。\n";
        }
    }
}
#endif
