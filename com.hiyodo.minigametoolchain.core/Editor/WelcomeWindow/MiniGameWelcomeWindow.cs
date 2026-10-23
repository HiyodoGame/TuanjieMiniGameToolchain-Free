#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Welcome
{
    /// <summary>
    /// MiniGame Toolchain 欢迎页。
    /// 在首次导入或打开项目时展示当前版本、功能列表和文档入口。
    /// </summary>
    [InitializeOnLoad]
    public class MiniGameWelcomeWindow : EditorWindow
    {
        private const string PrefsKey = "MiniGameToolchain.WelcomeShownAtVersion";
        private const string LogoResourceName = "MiniGameToolchain/Logo";
        private const string LogoTextResourceName = "MiniGameToolchain/LogoText";

        private static readonly Vector2 WindowSize = new Vector2(560, 720);

        static MiniGameWelcomeWindow()
        {
            EditorApplication.delayCall += ShowOnStartup;
        }

        [MenuItem("Window/MiniGame/Welcome", false, 0)]
        public static void Open()
        {
            var window = GetWindow<MiniGameWelcomeWindow>(true, "Welcome to MiniGame Toolchain", true);
            MiniGameBranding.SetTitleIcon(window, "Welcome");
            window.minSize = WindowSize;
            window.maxSize = new Vector2(WindowSize.x, 1200);
            window.Show();
        }

        private static void ShowOnStartup()
        {
            string version = GetPackageVersion();
            string lastVersion = EditorPrefs.GetString(PrefsKey, "");
            if (lastVersion == version)
            {
                return;
            }

            // 用户手动关闭了“自动显示”则不再弹出
            if (EditorPrefs.GetBool(PrefsKey + "_Disabled", false))
            {
                return;
            }

            EditorPrefs.SetString(PrefsKey, version);
            Open();
        }

        private static string GetPackageVersion()
        {
            // 优先使用代码中的版本常量（覆盖 .unitypackage 导入场景，此时 package.json 不会随包导入）
            string codeVersion = MiniGameToolchainVersion.CurrentVersion;

            try
            {
                var package = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                    .FirstOrDefault(p => p.name == "com.hiyodo.minigametoolchain.core");
                if (package != null && !string.IsNullOrEmpty(package.version))
                {
                    // 如果 UPM 版本与代码常量不一致，以 UPM 为准（通常是开发期本地包覆盖）
                    return package.version;
                }
            }
            catch
            {
                // 忽略 UPM 读取异常，降级到代码常量
            }

            return string.IsNullOrEmpty(codeVersion) ? "0.1.2" : codeVersion;
        }

        private bool _showOnStartup = true;
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _showOnStartup = !EditorPrefs.GetBool(PrefsKey + "_Disabled", false);
        }

        private void OnGUI()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Space(24);
            MiniGameBranding.DrawHeader(
                "MiniGame Toolchain",
                "专为团结引擎微信小游戏打造的一站式开发工具链\nA one-stop toolchain for WeChat MiniGame development on Tuanjie Engine",
                showLogoText: true,
                showIcon: false,
                iconSize: 96);
            GUILayout.Space(16);
            DrawVersionBadge();
            GUILayout.Space(24);
            DrawFeatureSection();
            GUILayout.Space(24);
            DrawDocSection();
            GUILayout.Space(24);
            DrawFooter();

            GUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Texture2D logo = LoadTexture(LogoResourceName);
            if (logo != null)
            {
                GUILayout.Label(logo, GUILayout.Width(96), GUILayout.Height(96));
            }
            else
            {
                GUILayout.Label(
                    "🎮",
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 64,
                        alignment = TextAnchor.MiddleCenter
                    },
                    GUILayout.Width(96),
                    GUILayout.Height(96)
                );
            }

            Texture2D logoText = LoadTexture(LogoTextResourceName);
            if (logoText != null)
            {
                GUILayout.Label(logoText, GUILayout.Width(260), GUILayout.Height(96));
            }
            else
            {
                GUILayout.Label(
                    "MiniGame\nToolchain",
                    new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 28,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        normal = new GUIStyleState { textColor = Color.white }
                    },
                    GUILayout.Width(260),
                    GUILayout.Height(96)
                );
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            var subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };
            GUILayout.Label("专为团结引擎微信小游戏打造的一站式开发工具链", subtitleStyle);
            GUILayout.Label("A one-stop toolchain for WeChat MiniGame development on Tuanjie Engine", subtitleStyle);
        }

        private void DrawVersionBadge()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string tierLabel = GetTierLabel();
            Color tierColor = GetTierColor();

            var badgeStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 6, 6),
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = new GUIStyleState
                {
                    textColor = tierColor,
                    background = MakeTexture(2, 2, new Color(tierColor.r, tierColor.g, tierColor.b, 0.15f))
                }
            };
            GUILayout.Label($"当前版本：{tierLabel}  <color=#4DDA80>v{GetPackageVersion()}</color>", badgeStyle);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private string GetTierLabel()
        {
            return Licensing.MiniGameLicenseManager.IsAssetStoreEdition
                ? "Builder Pro"
                : "Free Edition";
        }

        private Color GetTierColor()
        {
            return Licensing.MiniGameLicenseManager.IsAssetStoreEdition
                ? new Color(1f, 0.45f, 0.45f)
                : new Color(0.3f, 0.85f, 0.5f);
        }

        private void DrawFeatureSection()
        {
            DrawSectionTitle("当前已具备的功能");

            var contentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true,
                normal = new GUIStyleState { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            GUILayout.Label(
                "<b>核心框架 Core</b>\n" +
                "• 诊断引擎：统一扫描器编排\n" +
                "• 规则引擎：反射发现、热插拔规则\n" +
                "• 微信桥接：安全的 JS API 封装\n" +
                "• 编辑器 UI 框架：一致的暗色窗口基座",
                contentStyle
            );

            GUILayout.Space(12);

            GUILayout.Label(
                "<b>构建优化器 Build Optimizer</b>\n" +
                "• 纹理扫描：未压缩、超大、重复纹理检测\n" +
                "• Shader 扫描：变体数量与预热开销分析\n" +
                "• 字体扫描：字体文件体积与 TMP Atlas 检查\n" +
                "• 设置扫描：PlayerSettings 微信小游戏合规校验",
                contentStyle
            );

            GUILayout.Space(12);

            GUILayout.Label(
                "<b>性能套件 Performance Suite</b>\n" +
                "• 真机性能数据采集（FPS / 内存 / 启动耗时）\n" +
                "• 编辑器可视化分析窗口\n" +
                "• 多次测试数据对比",
                contentStyle
            );

            GUILayout.Space(12);

            GUILayout.Label(
                "<b>启动套件 Starter Kit</b>\n" +
                "• 项目目录结构与命名规范模板\n" +
                "• 核心系统框架（场景/音频/UI）\n" +
                "• 微信 SDK 封装",
                contentStyle
            );

            GUILayout.Space(12);

            GUILayout.Label(
                "<b>构建套件 Builder Pro</b>\n" +
                "• 可视化构建流水线：一键多平台同步构建\n" +
                "• 团队看板：构建历史、版本归档、CI/CD 模板\n" +
                "• 自定义规则：首包大小、必需场景、资源体积检查\n" +
                "• CDN 上传与 Webhook 通知",
                contentStyle
            );

            GUILayout.Space(12);

            var tipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            if (!Licensing.MiniGameLicenseManager.IsAssetStoreEdition)
            {
                GUILayout.Label(
                    "提示：一键自动优化、自动 AssetBundle 分组、实时性能 HUD、内存快照等高级功能仅在付费版提供。",
                    tipStyle
                );
            }
        }

        private void DrawDocSection()
        {
            DrawSectionTitle("文档与支持");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("GitHub 免费仓库", GUILayout.Width(140), GUILayout.Height(32)))
            {
                Application.OpenURL("https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free");
            }
            if (GUILayout.Button("Gitee 镜像", GUILayout.Width(140), GUILayout.Height(32)))
            {
                Application.OpenURL("https://gitee.com/HiyodoGame/TuanjieMiniGameToolchain-Free");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("查看 README", GUILayout.Width(180), GUILayout.Height(32)))
            {
                Application.OpenURL("https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free/blob/upm/README.md");
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginChangeCheck();
            _showOnStartup = GUILayout.Toggle(_showOnStartup, "每次版本更新后自动显示欢迎页");
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(PrefsKey + "_Disabled", !_showOnStartup);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            var footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = new Color(0.5f, 0.5f, 0.5f) }
        };
            GUILayout.Label("Made with ❤️ by Hiyodo Studio", footerStyle);
        }

        private void DrawSectionTitle(string title)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState { textColor = Color.white }
            };
            GUILayout.Label(title, style);
            GUILayout.Space(8);
        }

        private Texture2D LoadTexture(string resourceName)
        {
            // 优先从 Editor/Resources 加载，支持 UPM 和 .unitypackage 两种导入方式
            Texture2D texture = Resources.Load<Texture2D>(resourceName);
            if (texture == null)
            {
                texture = EditorGUIUtility.Load(resourceName) as Texture2D;
            }

            if (texture == null)
            {
                Debug.LogWarning($"[MiniGameWelcome] 找不到 Logo 资源: {resourceName}\n" +
                    "请确认已将 Logo 图片放入 Packages/com.hiyodo.minigametoolchain.core/Editor/Resources/MiniGameToolchain/，\n" +
                    "并命名为 Logo.png 和 LogoText.png。");
            }

            return texture;
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
#endif
