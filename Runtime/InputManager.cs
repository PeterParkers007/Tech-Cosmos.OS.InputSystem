using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace TechCosmos.InputSystem.Runtime
{
    public class InputManager : MonoBehaviour
    {
        // 单例
        public static InputManager Instance { get; private set; }

        // 配置资产引用
        [SerializeField] private InputConfig config;
        public InputConfig Config => config;

        // 存储按键名称和对应按键的映射
        private Dictionary<string, KeyCode> keys = new Dictionary<string, KeyCode>();

        // 保存路径
        private string savePath;
        private const string SAVE_FILE_NAME = "keybindings.json";

        // 事件：按键绑定改变时触发
        public event Action<string, KeyCode> OnKeyRebinded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 设置保存路径
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

            InitializeBindings();
        }

        private void InitializeBindings()
        {
            if (config == null)
            {
                Debug.LogError("InputConfig 未设置！请在 Inspector 中为 InputManager 拖拽赋值 Config 字段。");
                return;
            }

            foreach (var keyConfig in config.keyConfigs)
            {
                RegisterKey(keyConfig.name, keyConfig.keyCode);
            }

            LoadBindings();
        }

        public void RegisterKey(string name, KeyCode defaultKey)
        {
            if (keys.ContainsKey(name))
            {
                Debug.LogWarning($"按键 {name} 已注册，将覆盖为新的默认值。");
            }
            keys[name] = defaultKey;
        }

        public void RebindKey(string name, KeyCode newKey, bool checkConflicts = true)
        {
            if (!keys.ContainsKey(name))
            {
                Debug.LogWarning($"未找到按键 {name}，请先注册。");
                return;
            }

            if (checkConflicts && IsKeyAlreadyBound(newKey, name))
            {
                Debug.LogWarning($"按键 {newKey} 已被其他动作使用，请先解除绑定。");
                return;
            }

            keys[name] = newKey;
            Debug.Log($"按键 {name} 已绑定到 {newKey}");

            OnKeyRebinded?.Invoke(name, newKey);
        }

        public bool IsKeyAlreadyBound(KeyCode key, string excludeAction = null)
        {
            foreach (var kvp in keys)
            {
                if (kvp.Value == key)
                {
                    if (excludeAction == null || kvp.Key != excludeAction)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public string GetActionNameByKey(KeyCode key)
        {
            foreach (var kvp in keys)
            {
                if (kvp.Value == key)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public KeyCode GetKeyCode(string name)
        {
            if (keys.TryGetValue(name, out KeyCode key))
            {
                return key;
            }
            Debug.LogWarning($"未找到按键 {name}");
            return KeyCode.None;
        }

        public void ResetToDefault()
        {
            if (config == null)
            {
                Debug.LogError("InputConfig 未设置，无法重置。");
                return;
            }

            foreach (var keyConfig in config.keyConfigs)
            {
                keys[keyConfig.name] = keyConfig.keyCode;
                OnKeyRebinded?.Invoke(keyConfig.name, keyConfig.keyCode);
            }

            ClearSavedBindings();
            Debug.Log("所有按键已重置为默认值。");
        }

        public void ResetKeyToDefault(string name)
        {
            if (config == null)
            {
                Debug.LogError("InputConfig 未设置，无法重置。");
                return;
            }

            var keyConfig = config.keyConfigs.Find(k => k.name == name);
            if (keyConfig.name == name)
            {
                keys[name] = keyConfig.keyCode;
                OnKeyRebinded?.Invoke(name, keyConfig.keyCode);
                SaveBindings();
                Debug.Log($"按键 {name} 已重置为默认值 {keyConfig.keyCode}");
            }
            else
            {
                Debug.LogWarning($"未在默认配置中找到按键 {name}");
            }
        }

        public void SaveBindings()
        {
            try
            {
                KeyBindingData data = new KeyBindingData();

                foreach (var kvp in keys)
                {
                    data.bindings.Add(new KeyBindingEntry(kvp.Key, kvp.Value));
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json);

                Debug.Log($"按键绑定已保存到: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存按键绑定失败: {e.Message}");
            }
        }

        public void LoadBindings()
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("未找到保存的按键绑定文件，使用默认配置。");
                return;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                KeyBindingData data = JsonUtility.FromJson<KeyBindingData>(json);

                if (data?.bindings == null || data.bindings.Count == 0)
                {
                    Debug.LogWarning("按键绑定文件为空或格式错误。");
                    return;
                }

                List<string> loadedKeys = new List<string>();

                foreach (var entry in data.bindings)
                {
                    if (keys.ContainsKey(entry.name))
                    {
                        KeyCode savedKey = entry.GetKeyCode();

                        if (IsKeyAlreadyBound(savedKey, entry.name))
                        {
                            Debug.LogWarning($"加载按键绑定 {entry.name} 时发现冲突：{savedKey} 已被使用，使用默认值。");
                            continue;
                        }

                        keys[entry.name] = savedKey;
                        OnKeyRebinded?.Invoke(entry.name, savedKey);
                        loadedKeys.Add(entry.name);
                    }
                }

                if (loadedKeys.Count > 0)
                {
                    Debug.Log($"已加载 {loadedKeys.Count} 个按键绑定：{string.Join(", ", loadedKeys)}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载按键绑定失败: {e.Message}");
            }
        }

        public void ClearSavedBindings()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log("已删除保存的按键绑定文件。");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"删除按键绑定文件失败: {e.Message}");
            }
        }

        public bool GetKey(string name)
        {
            if (!keys.TryGetValue(name, out KeyCode key))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return Input.GetKey(key);
        }

        public bool GetKeyDown(string name)
        {
            if (!keys.TryGetValue(name, out KeyCode key))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return Input.GetKeyDown(key);
        }

        public bool GetKeyUp(string name)
        {
            if (!keys.TryGetValue(name, out KeyCode key))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return Input.GetKeyUp(key);
        }

        public List<string> GetAllActionNames()
        {
            return new List<string>(keys.Keys);
        }

        public Dictionary<string, KeyCode> GetAllBindings()
        {
            return new Dictionary<string, KeyCode>(keys);
        }

        [Serializable]
        private class KeyBindingData
        {
            public List<KeyBindingEntry> bindings = new List<KeyBindingEntry>();
        }

        [Serializable]
        private class KeyBindingEntry
        {
            public string name;
            public int keyCode;

            public KeyBindingEntry(string name, KeyCode key)
            {
                this.name = name;
                this.keyCode = (int)key;
            }

            public KeyCode GetKeyCode()
            {
                return (KeyCode)keyCode;
            }
        }
    }
}
