#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.StarterKit.Editor.Templates
{
    /// <summary>
    /// Starter Kit 编辑器菜单。
    /// </summary>
    public static class MGSStarterKitMenu
    {
        [MenuItem("Window/MiniGame/Starter Kit/Create Free Project Template")]
        private static void CreateFreeProjectTemplate()
        {
            var folder = EditorUtility.OpenFolderPanel("选择项目模板生成位置", Application.dataPath, "MiniGameStarterKit");
            if (string.IsNullOrEmpty(folder)) return;

            // 转换为 Assets 相对路径
            var dataPath = Application.dataPath.Replace('\\', '/');
            var selected = folder.Replace('\\', '/');
            if (!selected.StartsWith(dataPath))
            {
                EditorUtility.DisplayDialog("路径错误", "请选择项目 Assets 目录下的文件夹。", "确定");
                return;
            }

            var relativePath = "Assets" + selected.Substring(dataPath.Length);
            MGSStarterKitTemplateGenerator.Generate(relativePath);
        }
    }
}
#endif
