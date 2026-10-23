using System;
using System.Collections.Generic;

namespace MiniGame.BuildOptimizer.Editor.AssetBundle
{
    /// <summary>
    /// AssetBundle 分组类型。
    /// </summary>
    public enum BundleGroupType
    {
        FirstPackage,
        RemoteCDN,
        Preload
    }

    /// <summary>
    /// 单个 AssetBundle 分组。
    /// </summary>
    [Serializable]
    public class AssetBundleGroup
    {
        public string GroupName;
        public BundleGroupType Type;
        public List<string> AssetGuids = new List<string>();
        public long EstimatedSizeBytes;
        public int DownloadPriority;
    }

    /// <summary>
    /// 自动分包策略结果。
    /// </summary>
    [Serializable]
    public class BundleStrategy
    {
        public List<AssetBundleGroup> Groups = new List<AssetBundleGroup>();
        public long EstimatedFirstPackageSizeBytes;
        public long EstimatedRemotePackageSizeBytes;
        public long EstimatedPreloadSizeBytes;
        public List<string> Warnings = new List<string>();
    }
}
