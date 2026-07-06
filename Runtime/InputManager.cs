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

        private Dictionary<string, InputBinding> staticBindings = new Dictionary<string, InputBinding>();
        private Dictionary<string, InputBinding> dynamicBindings = new Dictionary<string, InputBinding>();
        private Dictionary<string, KeyCode> dynamicDefaults = new Dictionary<string, KeyCode>();

        private string savePath;
        private const string SAVE_FILE_NAME = "keybindings.json";

        public event Action<string, InputBinding> OnKeyRebinded;
        public event Action<string> OnActionAdded;
        public event Action<string> OnActionRemoved;

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

            InitializeStaticBindings();
            LoadBindings();
        }

        private void InitializeStaticBindings()
        {
            if (config == null)
            {
                Debug.LogError("InputConfig is not assigned! Please assign a Config in the Inspector.");
                return;
            }

            foreach (var keyConfig in config.keyConfigs)
            {
                RegisterStaticKey(keyConfig.name, keyConfig.binding);
            }
        }

        private void RegisterStaticKey(string name, InputBinding defaultBinding)
        {
            if (staticBindings.ContainsKey(name))
            {
                Debug.LogWarning($"Static action {name} is already registered, overwriting.");
            }
            staticBindings[name] = defaultBinding;
        }

        private void RegisterDynamicKey(string name, InputBinding defaultBinding)
        {
            if (dynamicBindings.ContainsKey(name))
            {
                Debug.LogWarning($"Dynamic action {name} is already registered, overwriting.");
            }
            dynamicBindings[name] = defaultBinding;
        }

        private void RemoveDynamicKey(string name)
        {
            dynamicBindings.Remove(name);
            dynamicDefaults.Remove(name);
        }

        // Public API: dynamic actions
        public string AddDynamicAction(string category, string actionName, KeyCode defaultKey)
        {
            string fullName = $"{category}_{actionName}";

            if (dynamicBindings.ContainsKey(fullName) || staticBindings.ContainsKey(fullName))
            {
                Debug.LogWarning($"Action {fullName} already exists.");
                return fullName;
            }

            RegisterDynamicKey(fullName, InputBinding.FromKeyCode(defaultKey));
            dynamicDefaults[fullName] = defaultKey;
            OnActionAdded?.Invoke(fullName);
            SaveBindings();
            Debug.Log($"Dynamic action added: {fullName} (Default: {defaultKey})");

            return fullName;
        }

        public void RemoveDynamicAction(string actionName)
        {
            if (dynamicBindings.ContainsKey(actionName))
            {
                RemoveDynamicKey(actionName);
                OnActionRemoved?.Invoke(actionName);
                SaveBindings();
                Debug.Log($"Dynamic action removed: {actionName}");
            }
            else
            {
                Debug.LogWarning($"Dynamic action {actionName} not found.");
            }
        }

        public bool IsDynamicAction(string actionName)
        {
            return dynamicBindings.ContainsKey(actionName);
        }

        public bool IsStaticAction(string actionName)
        {
            return staticBindings.ContainsKey(actionName);
        }

        public void RebindKey(string name, InputBinding newBinding, bool checkConflicts = true)
        {
            if (!staticBindings.ContainsKey(name) && !dynamicBindings.ContainsKey(name))
            {
                Debug.LogWarning($"Action {name} is not registered.");
                return;
            }

            if (checkConflicts && IsBindingAlreadyBound(newBinding, name))
            {
                Debug.LogWarning($"Binding {newBinding.GetDisplayName()} is already assigned to another action.");
                return;
            }

            if (staticBindings.ContainsKey(name))
                staticBindings[name] = newBinding;
            else
                dynamicBindings[name] = newBinding;

            Debug.Log($"Action {name} rebound to {newBinding.GetDisplayName()}");
            OnKeyRebinded?.Invoke(name, newBinding);
        }

        public void RebindKey(string name, KeyCode newKey, bool checkConflicts = true)
        {
            RebindKey(name, InputBinding.FromKeyCode(newKey), checkConflicts);
        }

        public void UnbindKey(string name)
        {
            if (!staticBindings.ContainsKey(name) && !dynamicBindings.ContainsKey(name))
            {
                Debug.LogWarning($"Action {name} is not registered.");
                return;
            }

            InputBinding emptyBinding = InputBinding.FromKeyCode(KeyCode.None);

            if (staticBindings.ContainsKey(name))
                staticBindings[name] = emptyBinding;
            else
                dynamicBindings[name] = emptyBinding;

            OnKeyRebinded?.Invoke(name, emptyBinding);
            Debug.Log($"Action {name} unbound.");
        }

        public bool IsBindingAlreadyBound(InputBinding binding, string excludeAction = null)
        {
            foreach (var kvp in staticBindings)
            {
                if (kvp.Value.Equals(binding))
                {
                    if (excludeAction == null || kvp.Key != excludeAction)
                        return true;
                }
            }

            foreach (var kvp in dynamicBindings)
            {
                if (kvp.Value.Equals(binding))
                {
                    if (excludeAction == null || kvp.Key != excludeAction)
                        return true;
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
            foreach (var kvp in staticBindings)
            {
                if (kvp.Value.Equals(binding))
                    return kvp.Key;
            }

            foreach (var kvp in dynamicBindings)
            {
                if (kvp.Value.Equals(binding))
                    return kvp.Key;
            }

            return null;
        }

        public string GetActionNameByKey(KeyCode key)
        {
            return GetActionNameByBinding(InputBinding.FromKeyCode(key));
        }

        public InputBinding GetBinding(string name)
        {
            if (staticBindings.TryGetValue(name, out InputBinding binding))
                return binding;

            if (dynamicBindings.TryGetValue(name, out binding))
                return binding;

            Debug.LogWarning($"Action {name} not found.");
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
                Debug.LogError("InputConfig is not assigned!");
                return;
            }

            // Reset static bindings
            foreach (var keyConfig in config.keyConfigs)
            {
                staticBindings[keyConfig.name] = keyConfig.binding;
                OnKeyRebinded?.Invoke(keyConfig.name, keyConfig.binding);
            }

            // Reset dynamic bindings
            foreach (var kvp in dynamicDefaults)
            {
                dynamicBindings[kvp.Key] = InputBinding.FromKeyCode(kvp.Value);
                OnKeyRebinded?.Invoke(kvp.Key, InputBinding.FromKeyCode(kvp.Value));
            }

            ClearSavedBindings();
            Debug.Log("All key bindings reset to defaults.");
        }

        public void ResetKeyToDefault(string name)
        {
            // Static action
            if (staticBindings.ContainsKey(name))
            {
                if (config == null)
                {
                    Debug.LogError("InputConfig is not assigned!");
                    return;
                }

                var keyConfig = config.keyConfigs.Find(k => k.name == name);
                if (!string.IsNullOrEmpty(keyConfig.name) && keyConfig.name == name)
                {
                    staticBindings[name] = keyConfig.binding;
                    OnKeyRebinded?.Invoke(name, keyConfig.binding);
                    SaveBindings();
                    Debug.Log($"Action {name} reset to default: {keyConfig.binding.GetDisplayName()}");
                }
                else
                {
                    Debug.LogWarning($"Action {name} not found in static config.");
                }
                return;
            }

            // Dynamic action
            if (dynamicBindings.ContainsKey(name))
            {
                if (dynamicDefaults.TryGetValue(name, out KeyCode defaultKey))
                {
                    dynamicBindings[name] = InputBinding.FromKeyCode(defaultKey);
                    OnKeyRebinded?.Invoke(name, InputBinding.FromKeyCode(defaultKey));
                    SaveBindings();
                    Debug.Log($"Dynamic action {name} reset to default: {defaultKey}");
                }
                else
                {
                    Debug.LogWarning($"Dynamic action {name} has no saved default.");
                }
                return;
            }

            Debug.LogWarning($"Action {name} not found.");
        }

        public void SaveBindings()
        {
            try
            {
                KeyBindingData data = new KeyBindingData();

                foreach (var kvp in staticBindings)
                {
                    data.bindings.Add(new KeyBindingEntry(kvp.Key, kvp.Value, false));
                }

                foreach (var kvp in dynamicBindings)
                {
                    data.bindings.Add(new KeyBindingEntry(kvp.Key, kvp.Value, true));
                }

                // Save dynamic defaults
                data.dynamicDefaults = new List<DynamicDefaultEntry>();
                foreach (var kvp in dynamicDefaults)
                {
                    data.dynamicDefaults.Add(new DynamicDefaultEntry(kvp.Key, kvp.Value));
                }

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(savePath, json);

                Debug.Log($"Key bindings saved to: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save key bindings: {e.Message}");
            }
        }

        public void LoadBindings()
        {
            if (!File.Exists(savePath))
            {
                Debug.Log("No saved key bindings found, using defaults.");
                return;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                KeyBindingData data = JsonUtility.FromJson<KeyBindingData>(json);

                if (data?.bindings == null || data.bindings.Count == 0)
                {
                    Debug.LogWarning("Save file is empty or corrupted.");
                    return;
                }

                List<string> loadedKeys = new List<string>();

                // Restore dynamic defaults
                if (data.dynamicDefaults != null)
                {
                    foreach (var entry in data.dynamicDefaults)
                    {
                        dynamicDefaults[entry.name] = (KeyCode)entry.defaultKeyCode;
                    }
                }

                foreach (var entry in data.bindings)
                {
                    InputBinding savedBinding = entry.GetBinding();

                    if (entry.isDynamic)
                    {
                        // Restore dynamic binding
                        dynamicBindings[entry.name] = savedBinding;
                        OnActionAdded?.Invoke(entry.name);
                        OnKeyRebinded?.Invoke(entry.name, savedBinding);
                        loadedKeys.Add(entry.name);
                    }
                    else if (staticBindings.ContainsKey(entry.name))
                    {
                        // Restore static binding
                        if (IsBindingAlreadyBound(savedBinding, entry.name))
                        {
                            Debug.LogWarning($"Conflict: Action {entry.name} binding {savedBinding.GetDisplayName()} is already in use.");
                            continue;
                        }

                        staticBindings[entry.name] = savedBinding;
                        OnKeyRebinded?.Invoke(entry.name, savedBinding);
                        loadedKeys.Add(entry.name);
                    }
                }

                if (loadedKeys.Count > 0)
                {
                    Debug.Log($"Loaded {loadedKeys.Count} key bindings: {string.Join(", ", loadedKeys)}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load key bindings: {e.Message}");
            }
        }

        public void ClearSavedBindings()
        {
            try
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log("Saved key bindings cleared.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to clear saved bindings: {e.Message}");
            }
        }

        public bool GetKey(string name)
        {
            InputBinding binding = GetBinding(name);
            if (binding.IsEmpty && !staticBindings.ContainsKey(name) && !dynamicBindings.ContainsKey(name))
                return false;
            return binding.IsPressed();
        }

        public bool GetKeyDown(string name)
        {
            InputBinding binding = GetBinding(name);
            if (binding.IsEmpty && !staticBindings.ContainsKey(name) && !dynamicBindings.ContainsKey(name))
                return false;
            return binding.WasPressedThisFrame();
        }

        public bool GetKeyUp(string name)
        {
            InputBinding binding = GetBinding(name);
            if (binding.IsEmpty && !staticBindings.ContainsKey(name) && !dynamicBindings.ContainsKey(name))
                return false;
            return binding.WasReleasedThisFrame();
        }

        public List<string> GetAllActionNames()
        {
            var names = new List<string>(staticBindings.Keys);
            names.AddRange(dynamicBindings.Keys);
            return names;
        }

        public List<string> GetDynamicActionNames()
        {
            return new List<string>(dynamicBindings.Keys);
        }

        public List<string> GetStaticActionNames()
        {
            return new List<string>(staticBindings.Keys);
        }

        public Dictionary<string, InputBinding> GetAllBindings()
        {
            var all = new Dictionary<string, InputBinding>(staticBindings);
            foreach (var kvp in dynamicBindings)
            {
                all[kvp.Key] = kvp.Value;
            }
            return all;
        }

        [Serializable]
        private class KeyBindingData
        {
            public List<KeyBindingEntry> bindings = new List<KeyBindingEntry>();
            public List<DynamicDefaultEntry> dynamicDefaults = new List<DynamicDefaultEntry>();
        }

        [Serializable]
        private class KeyBindingEntry
        {
            public string name;
            public int keyCode;
            public int modifiers;
            public int comboKey;
            public bool isDynamic;

            public KeyBindingEntry(string name, InputBinding binding, bool isDynamic)
            {
                this.name = name;
                keyCode = (int)binding.key;
                modifiers = (int)binding.modifiers;
                comboKey = (int)binding.comboKey;
                this.isDynamic = isDynamic;
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

        [Serializable]
        private class DynamicDefaultEntry
        {
            public string name;
            public int defaultKeyCode;

            public DynamicDefaultEntry(string name, KeyCode defaultKeyCode)
            {
                this.name = name;
                this.defaultKeyCode = (int)defaultKeyCode;
            }
        }
    }
}