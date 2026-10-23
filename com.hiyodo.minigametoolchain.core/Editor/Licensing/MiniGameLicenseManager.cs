#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 统一许可证管理器。采用二态门控模型，不再支持秘钥激活：
    /// - 完整版（源码仓库 / AssetStore 付费版）：检测到 <see cref="MGSAssetStoreEdition"/> 标记，全部功能自动解锁。
    /// - 免费裁剪版：通过 <c>build_free_package.py</c> 物理裁剪 Pro 代码，仅 Free 级功能可用。
    /// </summary>
    public static class MiniGameLicenseManager
    {
        private const string AssetStoreEditionTypeName = "MiniGame.BuilderPro.Editor.Licensing.MGSAssetStoreEdition";
        private const string AssetStoreEditionAssemblyName = "MiniGame.BuilderPro.Editor";

        /// <summary>
        /// 功能到最低所需等级的映射。
        /// 二态模型下仅区分 Free（免费版可用）与 Enterprise（完整版可用）。
        /// 所有非 Free 功能统一映射到 Enterprise，避免遗留的中间 tier 造成误解。
        /// </summary>
        private static readonly Dictionary<string, MiniGameLicenseTier> FeatureRequirements =
            new Dictionary<string, MiniGameLicenseTier>
            {
                // Build Optimizer: Free
                { MiniGameLicenseFeature.StarterKitProjectTemplate, MiniGameLicenseTier.Free },
                { MiniGameLicenseFeature.BuilderProLocalPipeline, MiniGameLicenseTier.Free },

                // Build Optimizer: Pro (Enterprise only in full edition)
                { MiniGameLicenseFeature.BuildOptimizerOneClickOptimize, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerFullDiagnostics, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerBundleStrategy, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerSettingsSnapshots, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerSmartBundle, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerFontSubset, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuildOptimizerShaderVariant, MiniGameLicenseTier.Enterprise },

                // Performance Suite: Pro (Enterprise only in full edition)
                { MiniGameLicenseFeature.PerformanceSuiteMemoryHistory, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteLeakDetection, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteSnapshotDiff, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteAlerts, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteStartupReport, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteDeviceTier, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteOptimizationAdvisor, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteAutomatedTesting, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteResourceWaterfall, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteReportSharing, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteHotspots, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.PerformanceSuiteMemoryDeepAnalysis, MiniGameLicenseTier.Enterprise },

                // Starter Kit: Pro (Enterprise only in full edition)
                { MiniGameLicenseFeature.StarterKitAdvancedTemplates, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.StarterKitLocalization, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.StarterKitCdnLoader, MiniGameLicenseTier.Enterprise },

                // Builder Pro: Pro (Enterprise only in full edition)
                { MiniGameLicenseFeature.BuilderProTeamDashboard, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProMultiPlatformSync, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProCustomRules, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProAddressableConfig, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProShaderWarmUp, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProCICDTemplates, MiniGameLicenseTier.Enterprise },
                { MiniGameLicenseFeature.BuilderProWebhookNotifications, MiniGameLicenseTier.Enterprise }
            };

        private static bool? s_assetStoreEdition;

        /// <summary>
        /// 是否为完整版（源码仓库与 AssetStore 付费版均包含标记，免费裁剪版不包含）。
        /// 当为 true 时全部功能自动解锁，无需秘钥激活。
        /// </summary>
        public static bool IsAssetStoreEdition
        {
            get
            {
                if (!s_assetStoreEdition.HasValue)
                {
                    s_assetStoreEdition = DetectAssetStoreEdition();
                }
                return s_assetStoreEdition.Value;
            }
        }

        /// <summary>
        /// 当前许可证等级。完整版返回 <see cref="MiniGameLicenseTier.Enterprise"/>，免费裁剪版返回 <see cref="MiniGameLicenseTier.Free"/>。
        /// </summary>
        public static MiniGameLicenseTier CurrentTier =>
            IsAssetStoreEdition ? MiniGameLicenseTier.Enterprise : MiniGameLicenseTier.Free;

        /// <summary>
        /// 指定功能在当前版本下是否可用。
        /// 完整版全部可用；免费裁剪版仅 Free 级功能可用（Pro 代码已被物理裁剪）。
        /// </summary>
        public static bool IsFeatureEnabled(string featureId)
        {
            if (IsAssetStoreEdition)
            {
                return true;
            }

            if (!FeatureRequirements.TryGetValue(featureId, out var required))
            {
                Debug.LogWarning($"[MiniGameLicense] 未注册的功能标识: {featureId}，默认禁止。请在 FeatureRequirements 中注册该功能。");
                return false;
            }

            return required == MiniGameLicenseTier.Free;
        }

        /// <summary>
        /// 通过反射检测 Builder Pro 程序集中是否存在完整版标记类型。
        /// </summary>
        private static bool DetectAssetStoreEdition()
        {
            try
            {
                var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == AssetStoreEditionAssemblyName);
                if (assembly == null)
                    return false;

                return assembly.GetType(AssetStoreEditionTypeName, throwOnError: false) != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MiniGameLicense] 检测完整版标记时出错: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 返回功能的授权提示文本，引导用户前往团结引擎 AssetStore 获取完整版。
        /// </summary>
        public static string GetUpgradeHint(string featureId)
        {
            return "此功能在完整版（Builder Pro）中提供，请前往团结引擎 AssetStore 获取。";
        }
    }
}
#endif
