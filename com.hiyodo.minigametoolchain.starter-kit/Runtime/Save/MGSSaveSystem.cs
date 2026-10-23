using System;
using System.IO;
using UnityEngine;

namespace MiniGame.StarterKit.Runtime.Save
{
    /// <summary>
    /// 本地存档系统。基于 JSON + PlayerPrefs 索引，支持微信小游戏与本地平台。
    /// </summary>
    public static class MGSSaveSystem
    {
        private static string SaveDirectory => Application.persistentDataPath;

        /// <summary>
        /// 保存对象到指定槽位。
        /// </summary>
        public static bool Save<T>(string slotName, T data) where T : class
        {
            try
            {
                string json = JsonUtility.ToJson(data);
                string path = GetSlotPath(slotName);
                File.WriteAllText(path, json);
                PlayerPrefs.SetString($"mgs_save_{slotName}", DateTime.Now.ToString("O"));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MGSSaveSystem] Save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从指定槽位加载对象。
        /// </summary>
        public static T Load<T>(string slotName) where T : class
        {
            string path = GetSlotPath(slotName);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MGSSaveSystem] Load failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 删除指定槽位。
        /// </summary>
        public static bool Delete(string slotName)
        {
            try
            {
                string path = GetSlotPath(slotName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                PlayerPrefs.DeleteKey($"mgs_save_{slotName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MGSSaveSystem] Delete failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查槽位是否存在。
        /// </summary>
        public static bool Exists(string slotName)
        {
            return File.Exists(GetSlotPath(slotName));
        }

        /// <summary>
        /// 获取存档最后保存时间。
        /// </summary>
        public static DateTime? GetLastSaveTime(string slotName)
        {
            string value = PlayerPrefs.GetString($"mgs_save_{slotName}", string.Empty);
            if (string.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, out var time)) return time;
            return null;
        }

        private static string GetSlotPath(string slotName)
        {
            string safeName = Path.GetFileNameWithoutExtension(slotName);
            return Path.Combine(SaveDirectory, $"{safeName}.json");
        }
    }
}
