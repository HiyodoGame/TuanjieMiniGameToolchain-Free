#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Core.Editor.Licensing
{
    /// <summary>
    /// 许可证密钥数据。
    /// </summary>
    [Serializable]
    public class MGSLicenseKey
    {
        public string LicenseKey;
        public MiniGameLicenseTier Tier;
        public string DeviceFingerprint;
        public string ActivatedAt;
        public string ExpiresAt;
        public string Signature;

        public bool IsExpired()
        {
            if (string.IsNullOrWhiteSpace(ExpiresAt))
                return false;
            return DateTime.TryParse(ExpiresAt, out var expiry) && DateTime.UtcNow > expiry;
        }
    }

    /// <summary>
    /// 许可证激活与验证工具。
    /// 使用离线激活 + 设备绑定，无需联网。
    /// </summary>
    public static class MGSLicenseActivator
    {
        private const string LicenseFileName = "MiniGameToolchain/license.json";
        private const string SecretKey = "MiniGameToolchain.SecretKey.v1.2026";

        /// <summary>
        /// 获取当前设备指纹。
        /// </summary>
        public static string GetDeviceFingerprint()
        {
            var raw = $"{SystemInfo.deviceName}|{SystemInfo.deviceModel}|{Environment.UserName}|{SystemInfo.operatingSystem}";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32);
            }
        }

        /// <summary>
        /// 生成许可证签名。
        /// </summary>
        public static string GenerateSignature(string licenseKey, MiniGameLicenseTier tier, string deviceFingerprint, string expiresAt)
        {
            var data = $"{licenseKey}|{(int)tier}|{deviceFingerprint}|{expiresAt}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32);
            }
        }

        /// <summary>
        /// 验证许可证密钥格式。
        /// </summary>
        public static bool ValidateKeyFormat(string licenseKey)
        {
            if (string.IsNullOrWhiteSpace(licenseKey))
                return false;

            var key = licenseKey.Trim().ToUpperInvariant();
            return key.StartsWith("MGBT-") && key.Length == 24;
        }

        /// <summary>
        /// 从许可证密钥解析等级和过期时间。
        /// 密钥格式: MGBT-TTTT-XXXX-XXXX-XXXX-XXXX
        /// TTTT: tier code (FREE=0000, PERS=0001, PROF=0002, TEAM=0003, ENTP=0004)
        /// X: random alphanumeric
        /// </summary>
        public static bool TryParseKey(string licenseKey, out MiniGameLicenseTier tier, out string expiresAt)
        {
            tier = MiniGameLicenseTier.Free;
            expiresAt = null;

            if (!ValidateKeyFormat(licenseKey))
                return false;

            var key = licenseKey.Trim().ToUpperInvariant();
            var tierCode = key.Substring(5, 4);

            switch (tierCode)
            {
                case "0000": tier = MiniGameLicenseTier.Free; break;
                case "0001": tier = MiniGameLicenseTier.Personal; break;
                case "0002": tier = MiniGameLicenseTier.Professional; break;
                case "0003": tier = MiniGameLicenseTier.Team; break;
                case "0004": tier = MiniGameLicenseTier.Enterprise; break;
                default: return false;
            }

            // 简化：密钥不包含过期时间，由激活时设置
            return true;
        }

        /// <summary>
        /// 激活许可证。
        /// </summary>
        public static bool Activate(string licenseKey, string expiresAt, out string error)
        {
            error = null;

            if (!TryParseKey(licenseKey, out var tier, out _))
            {
                error = "许可证密钥格式无效。";
                return false;
            }

            var fingerprint = GetDeviceFingerprint();
            var signature = GenerateSignature(licenseKey, tier, fingerprint, expiresAt);

            var license = new MGSLicenseKey
            {
                LicenseKey = licenseKey.Trim().ToUpperInvariant(),
                Tier = tier,
                DeviceFingerprint = fingerprint,
                ActivatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                ExpiresAt = expiresAt,
                Signature = signature
            };

            SaveLicense(license);
            MiniGameLicenseManager.CurrentTier = tier;

            Debug.Log($"[MGSLicense] 许可证已激活: {tier}, 过期时间: {expiresAt ?? "永久"}");
            return true;
        }

        /// <summary>
        /// 验证当前许可证是否有效。
        /// </summary>
        public static bool ValidateCurrentLicense(out string error)
        {
            error = null;
            var license = LoadLicense();

            if (license == null)
            {
                error = "未找到许可证文件。";
                return false;
            }

            if (license.IsExpired())
            {
                error = "许可证已过期。";
                return false;
            }

            var currentFingerprint = GetDeviceFingerprint();
            if (license.DeviceFingerprint != currentFingerprint)
            {
                error = "许可证与当前设备不匹配。";
                return false;
            }

            var expectedSignature = GenerateSignature(license.LicenseKey, license.Tier, license.DeviceFingerprint, license.ExpiresAt);
            if (license.Signature != expectedSignature)
            {
                error = "许可证签名无效。";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取当前许可证信息。
        /// </summary>
        public static MGSLicenseKey GetCurrentLicense()
        {
            return LoadLicense();
        }

        /// <summary>
        /// 移除当前许可证。
        /// </summary>
        public static void Deactivate()
        {
            var path = GetLicensePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            MiniGameLicenseManager.CurrentTier = MiniGameLicenseTier.Free;
            Debug.Log("[MGSLicense] 许可证已移除。");
        }

        private static string GetLicensePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "Library", LicenseFileName);
        }

        private static void SaveLicense(MGSLicenseKey license)
        {
            var path = GetLicensePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(license, true));
        }

        private static MGSLicenseKey LoadLicense()
        {
            var path = GetLicensePath();
            if (!File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<MGSLicenseKey>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
