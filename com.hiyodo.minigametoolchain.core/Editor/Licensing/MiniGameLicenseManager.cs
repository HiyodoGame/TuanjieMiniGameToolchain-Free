#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 统一许可证管理器。当前基于 EditorPrefs 存储，便于本地评估和测试。
    /// 商业版本可替换为授权服务器、本地加密文件或 AssetStore 收据校验。
    /// </summary>
    public static class MiniGameLicenseManager
    {
        private const string EditorPrefsKey = "MiniGameToolchain_LicenseTier";

        private static readonly Dictionary<string, MiniGameLicenseTier> FeatureRequirements =
            new Dictionary<string, MiniGameLicenseTier>
            {
                // Build Optimizer
                { MiniGameLicenseFeature.BuildOptimizerOneClickOptimize, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuildOptimizerFullDiagnostics, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuildOptimizerBundleStrategy, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuildOptimizerSettingsSnapshots, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuildOptimizerSmartBundle, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.BuildOptimizerFontSubset, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.BuildOptimizerShaderVariant, MiniGameLicenseTier.Professional },

                // Performance Suite
                { MiniGameLicenseFeature.PerformanceSuiteMemoryHistory, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.PerformanceSuiteLeakDetection, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.PerformanceSuiteSnapshotDiff, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.PerformanceSuiteAlerts, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.PerformanceSuiteStartupReport, MiniGameLicenseTier.Personal },

                // Starter Kit
                { MiniGameLicenseFeature.StarterKitProjectTemplate, MiniGameLicenseTier.Free },
                { MiniGameLicenseFeature.StarterKitAdvancedTemplates, MiniGameLicenseTier.Personal },

                // Builder Pro
                { MiniGameLicenseFeature.BuilderProLocalPipeline, MiniGameLicenseTier.Free },
                { MiniGameLicenseFeature.BuilderProTeamDashboard, MiniGameLicenseTier.Team },
                { MiniGameLicenseFeature.BuilderProMultiPlatformSync, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.BuilderProCustomRules, MiniGameLicenseTier.Professional },
                { MiniGameLicenseFeature.BuilderProAddressableConfig, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuilderProShaderWarmUp, MiniGameLicenseTier.Personal },
                { MiniGameLicenseFeature.BuilderProCICDTemplates, MiniGameLicenseTier.Team }
            };

        /// <summary>
        /// 当前许可证等级。
        /// </summary>
        public static MiniGameLicenseTier CurrentTier
        {
            get
            {
                var value = EditorPrefs.GetString(EditorPrefsKey, ((int)MiniGameLicenseTier.Free).ToString());
                if (int.TryParse(value, out int tier) && tier >= (int)MiniGameLicenseTier.Free && tier <= (int)MiniGameLicenseTier.Enterprise)
                {
                    return (MiniGameLicenseTier)tier;
                }
                return MiniGameLicenseTier.Free;
            }
            set
            {
                EditorPrefs.SetString(EditorPrefsKey, ((int)value).ToString());
                Debug.Log($"[MiniGameLicense] 当前许可证已切换为: {value}");
            }
        }

        /// <summary>
        /// 当前许可证是否达到指定等级。
        /// </summary>
        public static bool IsAtLeast(MiniGameLicenseTier tier)
        {
            return CurrentTier >= tier;
        }

        /// <summary>
        /// 指定功能是否在当前许可证下可用。
        /// </summary>
        public static bool IsFeatureEnabled(string featureId)
        {
            if (!FeatureRequirements.TryGetValue(featureId, out var required))
            {
                Debug.LogWarning($"[MiniGameLicense] 未注册的功能标识: {featureId}，默认禁止。请在 FeatureRequirements 中注册该功能。");
                return false;
            }

            return CurrentTier >= required;
        }

        /// <summary>
        /// 获取指定功能所需的最低许可证等级。
        /// </summary>
        public static MiniGameLicenseTier GetRequiredTier(string featureId)
        {
            return FeatureRequirements.TryGetValue(featureId, out var required)
                ? required
                : MiniGameLicenseTier.Enterprise;
        }

        /// <summary>
        /// 返回功能的授权提示文本。
        /// </summary>
        public static string GetUpgradeHint(string featureId)
        {
            var required = GetRequiredTier(featureId);
            return $"需要 {required} 许可证（当前为 {CurrentTier}），请升级以使用此功能。";
        }
    }
}
#endif
