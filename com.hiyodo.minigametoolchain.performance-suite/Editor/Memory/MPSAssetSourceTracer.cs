#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MiniGame.PerformanceSuite.Editor.Memory
{
    /// <summary>
    /// 资源来源追踪工具。识别内存中的资源来自 AssetBundle、Resources、场景还是内置/动态生成。
    /// </summary>
    public static class MPSAssetSourceTracer
    {
        /// <summary>
        /// 获取资源来源描述。
        /// </summary>
        public static string GetSource(UnityEngine.Object obj)
        {
            if (obj == null) return "Unknown";

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return "动态生成/内置";
            }

            string bundleName = null;
            var importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                bundleName = importer.assetBundleName;
            }

            if (!string.IsNullOrEmpty(bundleName))
            {
                return $"AssetBundle: {bundleName}";
            }

            if (path.Replace("\\", "/").Contains("/Resources/"))
            {
                return "Resources";
            }

            if (path.EndsWith(".unity"))
            {
                return "场景";
            }

            return "项目资源";
        }

        /// <summary>
        /// 获取资源明细描述（纹理尺寸格式 / Mesh 顶点数据等）。
        /// </summary>
        public static string GetDetail(UnityEngine.Object obj)
        {
            switch (obj)
            {
                case Texture2D texture:
                    return $"{texture.width}x{texture.height} {texture.format}";

                case Mesh mesh:
                    long vertexBytes = EstimateVertexBytes(mesh);
                    long indexBytes = (long)mesh.triangles.Length * 2;
                    return $"{mesh.vertexCount} verts / {mesh.subMeshCount} submesh，顶点数据约 {vertexBytes / 1024f:F0}KB，索引约 {indexBytes / 1024f:F0}KB";

                case AudioClip clip:
                    return $"{clip.channels}ch {clip.frequency}Hz {clip.length:F1}s";

                default:
                    return "";
            }
        }

        /// <summary>
        /// 估算 Mesh 顶点缓冲大小（字节）。按各通道格式累加，不含索引缓冲。
        /// </summary>
        public static long EstimateVertexBytes(Mesh mesh)
        {
            if (mesh == null) return 0;

            long perVertex = 0;
            for (int channel = 0; channel < 8; channel++)
            {
                if (!mesh.HasVertexAttribute((UnityEngine.Rendering.VertexAttribute)channel)) continue;

                var format = mesh.GetVertexAttributeFormat((UnityEngine.Rendering.VertexAttribute)channel);
                int dimension = mesh.GetVertexAttributeDimension((UnityEngine.Rendering.VertexAttribute)channel);
                perVertex += GetFormatSize(format) * dimension;
            }

            return perVertex * mesh.vertexCount;
        }

        private static int GetFormatSize(UnityEngine.Rendering.VertexAttributeFormat format)
        {
            switch (format)
            {
                case UnityEngine.Rendering.VertexAttributeFormat.Float32: return 4;
                case UnityEngine.Rendering.VertexAttributeFormat.Float16: return 2;
                case UnityEngine.Rendering.VertexAttributeFormat.UNorm8: return 1;
                case UnityEngine.Rendering.VertexAttributeFormat.SNorm8: return 1;
                case UnityEngine.Rendering.VertexAttributeFormat.UNorm16: return 2;
                case UnityEngine.Rendering.VertexAttributeFormat.SNorm16: return 2;
                case UnityEngine.Rendering.VertexAttributeFormat.UInt8: return 1;
                case UnityEngine.Rendering.VertexAttributeFormat.SInt8: return 1;
                case UnityEngine.Rendering.VertexAttributeFormat.UInt16: return 2;
                case UnityEngine.Rendering.VertexAttributeFormat.SInt16: return 2;
                case UnityEngine.Rendering.VertexAttributeFormat.UInt32: return 4;
                case UnityEngine.Rendering.VertexAttributeFormat.SInt32: return 4;
                default: return 4;
            }
        }
    }
}
#endif
