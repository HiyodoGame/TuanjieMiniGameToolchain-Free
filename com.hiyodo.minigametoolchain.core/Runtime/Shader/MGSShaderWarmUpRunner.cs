using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGame.Core.Runtime.ShaderWarmUp
{
    /// <summary>
    /// Shader 分帧预热执行器。挂载到首场景即可在启动时自动预热。
    /// </summary>
    public class MGSShaderWarmUpRunner : MonoBehaviour
    {
        [Tooltip("Shader 预热配置")]
        public MGSShaderWarmUpConfig Config;

        [Tooltip("预热完成事件")]
        public UnityEvent OnWarmUpComplete;

        private void Start()
        {
            if (Config != null && Config.WarmUpOnStart)
            {
                StartCoroutine(WarmUpCoroutine());
            }
        }

        private IEnumerator WarmUpCoroutine()
        {
            yield return Config.WarmUpAsync(new Progress<float>(p =>
            {
                Debug.Log($"[MGSShaderWarmUp] 预热进度: {p:P0}");
            }));

            OnWarmUpComplete?.Invoke();
        }
    }
}
