using System;
using UnityEditor;
using UnityEngine;

namespace MiniGame.BuildOptimizer.Editor.Analyzers
{
    /// <summary>
    /// 纹理内存/体积估算器。
    /// </summary>
    public static class TextureMemoryEstimator
    {
        /// <summary>
        /// 估算纹理在 GPU/包体中占用的字节数。
        /// </summary>
        public static long EstimateMemorySize(Texture2D texture, TextureImporter importer)
        {
            if (texture == null) return 0;

            var bpp = GetBitsPerPixel(texture.format);
            var platform = importer != null ? importer.GetDefaultPlatformTextureSettings() : null;
            if (platform != null && platform.overridden)
            {
                bpp = GetBitsPerPixel(platform.format, bpp);
            }

            long baseSize = (long)texture.width * texture.height * bpp / 8;

            // Mipmap 链约为 1 + 1/4 + 1/16 + ... ≈ 1.333
            if (importer != null && importer.mipmapEnabled)
            {
                baseSize = (long)(baseSize * 1.333f);
            }

            return baseSize;
        }

        /// <summary>
        /// 判断当前平台设置是否为压缩格式。
        /// </summary>
        public static bool IsCompressed(TextureImporter importer)
        {
            if (importer == null) return true;

            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                return false;

            var platform = importer.GetDefaultPlatformTextureSettings();
            if (platform.overridden)
            {
                return IsPlatformFormatCompressed(platform.format);
            }

            return IsPlatformFormatCompressed(platform.format);
        }

        /// <summary>
        /// 判断纹理尺寸是否为 2 的幂。
        /// </summary>
        public static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// 估算把纹理缩放到指定最大边后的字节数。
        /// </summary>
        public static long EstimateResizedMemory(Texture2D texture, TextureImporter importer, int targetMaxSize)
        {
            if (texture == null) return 0;

            var maxSide = Mathf.Max(texture.width, texture.height);
            if (maxSide <= targetMaxSize) return EstimateMemorySize(texture, importer);

            var scale = (float)targetMaxSize / maxSide;
            var newWidth = Mathf.Max(1, Mathf.RoundToInt(texture.width * scale));
            var newHeight = Mathf.Max(1, Mathf.RoundToInt(texture.height * scale));

            var bpp = GetBitsPerPixel(texture.format);
            var platform = importer != null ? importer.GetDefaultPlatformTextureSettings() : null;
            if (platform != null && platform.overridden)
            {
                bpp = GetBitsPerPixel(platform.format, bpp);
            }

            long size = (long)newWidth * newHeight * bpp / 8;
            if (importer != null && importer.mipmapEnabled)
            {
                size = (long)(size * 1.333f);
            }
            return size;
        }

        private static int GetBitsPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                case TextureFormat.R8:
                    return 8;
                case TextureFormat.RGB565:
                case TextureFormat.RGBA4444:
                case TextureFormat.R16:
                case TextureFormat.RG16:
                case TextureFormat.RHalf:
                    return 16;
                case TextureFormat.RGB24:
                    return 24;
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                case TextureFormat.RGHalf:
                case TextureFormat.RFloat:
                    return 32;
                case TextureFormat.RGBAHalf:
                case TextureFormat.RGFloat:
                    return 64;
                case TextureFormat.RGBAFloat:
                    return 128;
                // Compressed
                case TextureFormat.DXT1:
                case TextureFormat.DXT1Crunched:
                case TextureFormat.BC4:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC_RGB4Crunched:
                case TextureFormat.EAC_R:
                    return 4;
                case TextureFormat.DXT5:
                case TextureFormat.DXT5Crunched:
                case TextureFormat.BC5:
                case TextureFormat.BC6H:
                case TextureFormat.BC7:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ETC2_RGBA8Crunched:
                case TextureFormat.EAC_RG:
                case TextureFormat.ASTC_4x4:
                    return 8;
                case TextureFormat.ASTC_5x5:
                    return 5;
                case TextureFormat.ASTC_6x6:
                    return 4;
                case TextureFormat.ASTC_8x8:
                    return 3;
                case TextureFormat.ASTC_10x10:
                    return 2;
                case TextureFormat.ASTC_12x12:
                    return 2;
                default:
                    return 32;
            }
        }

        private static int GetBitsPerPixel(TextureImporterFormat format, int fallbackBpp)
        {
            if (format == TextureImporterFormat.Automatic)
                return fallbackBpp;

            switch (format)
            {
                // Uncompressed
                case TextureImporterFormat.Alpha8:
                case TextureImporterFormat.R8:
                    return 8;
                case TextureImporterFormat.R16:
                case TextureImporterFormat.RG16:
                case TextureImporterFormat.RHalf:
                    return 16;
                case TextureImporterFormat.RGB24:
                    return 24;
                case TextureImporterFormat.RGBA32:
                case TextureImporterFormat.ARGB32:
                case TextureImporterFormat.RGHalf:
                case TextureImporterFormat.RFloat:
                    return 32;
                case TextureImporterFormat.RGBAHalf:
                case TextureImporterFormat.RGFloat:
                    return 64;
                case TextureImporterFormat.RGBAFloat:
                    return 128;
                // Compressed
                case TextureImporterFormat.DXT1:
                case TextureImporterFormat.DXT1Crunched:
                case TextureImporterFormat.BC4:
                case TextureImporterFormat.PVRTC_RGB2:
                case TextureImporterFormat.PVRTC_RGBA2:
                case TextureImporterFormat.ETC_RGB4:
                case TextureImporterFormat.ETC_RGB4Crunched:
                    return 4;
                case TextureImporterFormat.DXT5:
                case TextureImporterFormat.DXT5Crunched:
                case TextureImporterFormat.BC5:
                case TextureImporterFormat.BC6H:
                case TextureImporterFormat.BC7:
                case TextureImporterFormat.PVRTC_RGB4:
                case TextureImporterFormat.PVRTC_RGBA4:
                case TextureImporterFormat.ETC2_RGBA8:
                case TextureImporterFormat.ETC2_RGBA8Crunched:
                case TextureImporterFormat.ASTC_4x4:
                    return 8;
                case TextureImporterFormat.ASTC_5x5:
                    return 5;
                case TextureImporterFormat.ASTC_6x6:
                    return 4;
                case TextureImporterFormat.ASTC_8x8:
                    return 3;
                case TextureImporterFormat.ASTC_10x10:
                    return 2;
                case TextureImporterFormat.ASTC_12x12:
                    return 2;
                default:
                    return fallbackBpp;
            }
        }

        private static bool IsPlatformFormatCompressed(TextureImporterFormat format)
        {
            if (format == TextureImporterFormat.Automatic)
            {
                return true;
            }

            switch (format)
            {
                case TextureImporterFormat.Alpha8:
                case TextureImporterFormat.RGB24:
                case TextureImporterFormat.RGBA32:
                case TextureImporterFormat.ARGB32:
                case TextureImporterFormat.R8:
                case TextureImporterFormat.R16:
                case TextureImporterFormat.RG16:
                case TextureImporterFormat.RHalf:
                case TextureImporterFormat.RGHalf:
                case TextureImporterFormat.RGBAHalf:
                case TextureImporterFormat.RFloat:
                case TextureImporterFormat.RGFloat:
                case TextureImporterFormat.RGBAFloat:
                    return false;
                default:
                    return true;
            }
        }
    }
}
