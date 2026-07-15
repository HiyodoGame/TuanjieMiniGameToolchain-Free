#if UNITY_EDITOR
using System.IO;
using MiniGame.StarterKit.Editor.Templates;
using UnityEditor;
using UnityEngine;

namespace MiniGame.StarterKit.Editor.UI
{
    /// <summary>
    /// Starter Kit 主窗口：一键生成微信小游戏项目脚手架。
    /// </summary>
    public class MGSStarterKitWindow : EditorWindow
    {
        private string _projectName = "MiniGameProject";
        private string _targetFolder = "Assets/MiniGameProject";
        private bool _applyRecommendedSettings = true;
        private bool _switchBuildTarget = true;
        private bool _createLoginSample = true;
        private bool _openBootScene = true;

        [MenuItem("Window/MiniGame/Starter Kit/Starter Kit Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<MGSStarterKitWindow>("Starter Kit");
            window.minSize = new Vector2(480, 360);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSettings();
            DrawActions();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("MiniGame Starter Kit", EditorStyles.largeLabel);
            EditorGUILayout.LabelField("一键生成微信小游戏项目脚手架", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(10);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("项目配置", EditorStyles.boldLabel);

            _projectName = EditorGUILayout.TextField("项目名称", _projectName);
            _targetFolder = EditorGUILayout.TextField("生成路径", _targetFolder);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("生成选项", EditorStyles.boldLabel);

            _applyRecommendedSettings = EditorGUILayout.ToggleLeft("应用微信小游戏推荐 PlayerSettings", _applyRecommendedSettings);
            _switchBuildTarget = EditorGUILayout.ToggleLeft("切换 Build Target 到 WebGL", _switchBuildTarget);
            _createLoginSample = EditorGUILayout.ToggleLeft("包含登录示例", _createLoginSample);
            _openBootScene = EditorGUILayout.ToggleLeft("生成后打开 Boot 场景", _openBootScene);

            EditorGUILayout.Space(10);

            if (_applyRecommendedSettings)
            {
                EditorGUILayout.HelpBox(
                    "将自动设置 IL2CPP / High Stripping / Brotli / Exception=None / Memory=256 / Data Caching / WASM Code Splitting。",
                    MessageType.Info);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(10);

            if (GUILayout.Button("选择生成目录", GUILayout.Height(28)))
            {
                var selected = EditorUtility.OpenFolderPanel("选择项目生成位置", Application.dataPath, _projectName);
                if (!string.IsNullOrEmpty(selected))
                {
                    var dataPath = Application.dataPath.Replace('\\', '/');
                    var normalized = selected.Replace('\\', '/');
                    if (normalized.StartsWith(dataPath))
                    {
                        _targetFolder = "Assets" + normalized.Substring(dataPath.Length);
                        if (string.IsNullOrWhiteSpace(_projectName) || _projectName == "MiniGameProject")
                        {
                            _projectName = Path.GetFileName(selected);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("路径错误", "请选择项目 Assets 目录下的文件夹。", "确定");
                    }
                }
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("生成项目", GUILayout.Height(40)))
            {
                GenerateProject();
            }
            GUI.backgroundColor = Color.white;
        }

        private void GenerateProject()
        {
            if (string.IsNullOrWhiteSpace(_projectName))
            {
                EditorUtility.DisplayDialog("项目名称不能为空", "请填写项目名称。", "确定");
                return;
            }

            var folder = _targetFolder.Trim().Replace('\\', '/');
            if (!folder.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("路径错误", "项目必须生成在 Assets 目录下。", "确定");
                return;
            }

            var options = new MGSStarterKitTemplateGenerator.GenerateOptions
            {
                ProjectName = _projectName,
                ApplyRecommendedSettings = _applyRecommendedSettings,
                SwitchBuildTargetToWebGL = _switchBuildTarget,
                CreateLoginSample = _createLoginSample,
                OpenBootSceneAfterGenerate = _openBootScene
            };

            MGSStarterKitTemplateGenerator.Generate(folder, options);
        }
    }
}
#endif
