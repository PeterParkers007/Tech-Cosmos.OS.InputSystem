using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TechCosmos.InputSystem.Runtime
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [SerializeField] private InputConfig config;
        public InputConfig Config => config;

        private Dictionary<string, InputBinding> bindings = new Dictionary<string, InputBinding>();

        private string savePath;
        private const string SAVE_FILE_NAME = "keybindings.json";

        public event Action<string, InputBinding> OnKeyRebinded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

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
                RegisterKey(keyConfig.name, keyConfig.GetEffectiveBinding());
            }

            LoadBindings();
        }

        public void RegisterKey(string name, InputBinding defaultBinding)
        {
            if (bindings.ContainsKey(name))
            {
                Debug.LogWarning($"按键 {name} 已注册，将覆盖为新的默认值。");
            }
            bindings[name] = defaultBinding;
        }

        public void RegisterKey(string name, KeyCode defaultKey)
        {
            RegisterKey(name, InputBinding.FromKeyCode(defaultKey));
        }

        public void RebindKey(string name, InputBinding newBinding, bool checkConflicts = true)
        {
            if (!bindings.ContainsKey(name))
            {
                Debug.LogWarning($"未找到按键 {name}，请先注册。");
                return;
            }

            if (checkConflicts && IsBindingAlreadyBound(newBinding, name))
            {
                Debug.LogWarning($"组合键 {newBinding.GetDisplayName()} 已被其他动作使用，请先解除绑定。");
                return;
            }

            bindings[name] = newBinding;
            Debug.Log($"按键 {name} 已绑定到 {newBinding.GetDisplayName()}");

            OnKeyRebinded?.Invoke(name, newBinding);
        }

        public void RebindKey(string name, KeyCode newKey, bool checkConflicts = true)
        {
            RebindKey(name, InputBinding.FromKeyCode(newKey), checkConflicts);
        }

        public bool IsBindingAlreadyBound(InputBinding binding, string excludeAction = null)
        {
            foreach (var kvp in bindings)
            {
                if (kvp.Value.Equals(binding))
                {
                    if (excludeAction == null || kvp.Key != excludeAction)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsKeyAlreadyBound(KeyCode key, string excludeAction = null)
        {
            return IsBindingAlreadyBound(InputBinding.FromKeyCode(key), excludeAction);
        }

        public string GetActionNameByBinding(InputBinding binding)
        {
            foreach (var kvp in bindings)
            {
                if (kvp.Value.Equals(binding))
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public string GetActionNameByKey(KeyCode key)
        {
            return GetActionNameByBinding(InputBinding.FromKeyCode(key));
        }

        public InputBinding GetBinding(string name)
        {
            if (bindings.TryGetValue(name, out InputBinding binding))
            {
                return binding;
            }
            Debug.LogWarning($"未找到按键 {name}");
            return InputBinding.FromKeyCode(KeyCode.None);
        }

        public KeyCode GetKeyCode(string name)
        {
            return GetBinding(name).key;
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
                InputBinding defaultBinding = keyConfig.GetEffectiveBinding();
                bindings[keyConfig.name] = defaultBinding;
                OnKeyRebinded?.Invoke(keyConfig.name, defaultBinding);
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
                InputBinding defaultBinding = keyConfig.GetEffectiveBinding();
                bindings[name] = defaultBinding;
                OnKeyRebinded?.Invoke(name, defaultBinding);
                SaveBindings();
                Debug.Log($"按键 {name} 已重置为默认值 {defaultBinding.GetDisplayName()}");
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

                foreach (var kvp in bindings)
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
                    if (bindings.ContainsKey(entry.name))
                    {
                        InputBinding savedBinding = entry.GetBinding();

                        if (IsBindingAlreadyBound(savedBinding, entry.name))
                        {
                            Debug.LogWarning($"加载按键绑定 {entry.name} 时发现冲突：{savedBinding.GetDisplayName()} 已被使用，使用默认值。");
                            continue;
                        }

                        bindings[entry.name] = savedBinding;
                        OnKeyRebinded?.Invoke(entry.name, savedBinding);
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
            if (!bindings.TryGetValue(name, out InputBinding binding))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return binding.IsPressed();
        }

        public bool GetKeyDown(string name)
        {
            if (!bindings.TryGetValue(name, out InputBinding binding))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return binding.WasPressedThisFrame();
        }

        public bool GetKeyUp(string name)
        {
            if (!bindings.TryGetValue(name, out InputBinding binding))
            {
                Debug.LogWarning($"未注册按键：{name}");
                return false;
            }
            return binding.WasReleasedThisFrame();
        }

        public List<string> GetAllActionNames()
        {
            return new List<string>(bindings.Keys);
        }

        public Dictionary<string, InputBinding> GetAllBindings()
        {
            return new Dictionary<string, InputBinding>(bindings);
        }

        public Dictionary<string, KeyCode> GetAllKeyCodes()
        {
            var result = new Dictionary<string, KeyCode>();
            foreach (var kvp in bindings)
            {
                result[kvp.Key] = kvp.Value.key;
            }
            return result;
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
            public int modifiers;
            public int comboKey;

            public KeyBindingEntry(string name, InputBinding binding)
            {
                this.name = name;
                keyCode = (int)binding.key;
                modifiers = (int)binding.modifiers;
                comboKey = (int)binding.comboKey;
            }

            public InputBinding GetBinding()
            {
                if (modifiers == 0 && comboKey == 0)
                {
                    return InputBinding.FromKeyCode((KeyCode)keyCode);
                }

                return new InputBinding
                {
                    key = (KeyCode)keyCode,
                    modifiers = (ModifierKey)modifiers,
                    comboKey = (KeyCode)comboKey
                };
            }
        }
    }
}
