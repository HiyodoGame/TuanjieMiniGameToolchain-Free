#if UNITY_EDITOR
namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 跨产品的功能标识。
    /// </summary>
    public static class MiniGameLicenseFeature
    {
        // Build Optimizer
        public const string BuildOptimizerOneClickOptimize = "BuildOptimizer.OneClickOptimize";
        public const string BuildOptimizerFullDiagnostics = "BuildOptimizer.FullDiagnostics";
        public const string BuildOptimizerBundleStrategy = "BuildOptimizer.BundleStrategy";
        public const string BuildOptimizerSettingsSnapshots = "BuildOptimizer.SettingsSnapshots";
        public const string BuildOptimizerSmartBundle = "BuildOptimizer.SmartBundle";
        public const string BuildOptimizerFontSubset = "BuildOptimizer.FontSubset";
        public const string BuildOptimizerShaderVariant = "BuildOptimizer.ShaderVariant";

        // Performance Suite
        public const string PerformanceSuiteMemoryHistory = "PerformanceSuite.MemoryHistory";
        public const string PerformanceSuiteLeakDetection = "PerformanceSuite.LeakDetection";
        public const string PerformanceSuiteSnapshotDiff = "PerformanceSuite.SnapshotDiff";
        public const string PerformanceSuiteAlerts = "PerformanceSuite.Alerts";
        public const string PerformanceSuiteStartupReport = "PerformanceSuite.StartupReport";
        public const string PerformanceSuiteDeviceTier = "PerformanceSuite.DeviceTier";
        public const string PerformanceSuiteOptimizationAdvisor = "PerformanceSuite.OptimizationAdvisor";
        public const string PerformanceSuiteAutomatedTesting = "PerformanceSuite.AutomatedTesting";

        // Starter Kit
        public const string StarterKitProjectTemplate = "StarterKit.ProjectTemplate";
        public const string StarterKitAdvancedTemplates = "StarterKit.AdvancedTemplates";

        // Builder Pro
        public const string BuilderProLocalPipeline = "BuilderPro.LocalPipeline";
        public const string BuilderProTeamDashboard = "BuilderPro.TeamDashboard";
        public const string BuilderProMultiPlatformSync = "BuilderPro.MultiPlatformSync";
        public const string BuilderProCustomRules = "BuilderPro.CustomRules";
        public const string BuilderProAddressableConfig = "BuilderPro.AddressableConfig";
        public const string BuilderProShaderWarmUp = "BuilderPro.ShaderWarmUp";
        public const string BuilderProCICDTemplates = "BuilderPro.CICDTemplates";
    }
}
#endif
