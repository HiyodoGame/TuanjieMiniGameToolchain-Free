using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGame.StarterKit.Runtime.Scene
{
    /// <summary>
    /// 场景管理器。提供同步/异步加载、进度回调、加载画面支持。
    /// </summary>
    public class MGSSceneManager : MonoBehaviour
    {
        private static MGSSceneManager _instance;

        public static MGSSceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MGSSceneManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<MGSSceneManager>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 当前是否正在加载场景。
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// 同步加载场景。
        /// </summary>
        public static void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
        }

        /// <summary>
        /// 异步加载场景，带进度回调。
        /// </summary>
        public void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneCoroutine(sceneName, onProgress, onComplete, mode));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, Action<float> onProgress, Action onComplete, LoadSceneMode mode)
        {
            IsLoading = true;
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            if (operation == null)
            {
                IsLoading = false;
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                onProgress?.Invoke(operation.progress);
                yield return null;
            }

            onProgress?.Invoke(1f);
            operation.allowSceneActivation = true;

            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 获取当前活跃场景名称。
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
