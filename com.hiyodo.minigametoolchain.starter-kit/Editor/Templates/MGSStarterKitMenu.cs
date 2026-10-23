#if UNITY_EDITOR
using MiniGame.StarterKit.Editor.UI;
using UnityEditor;

namespace MiniGame.StarterKit.Editor.Templates
{
    /// <summary>
    /// Starter Kit 编辑器菜单快捷入口。
    /// </summary>
    public static class MGSStarterKitMenu
    {
        [MenuItem("Window/MiniGame/Starter Kit/Create Project")]
        private static void OpenStarterKitWindow()
        {
            MGSStarterKitWindow.ShowWindow();
        }
    }
}
#endif
