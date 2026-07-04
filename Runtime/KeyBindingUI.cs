using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TechCosmos.InputSystem.Runtime
{
    public class KeyBindingUI : MonoBehaviour
    {
        [SerializeField] private string actionName = "Jump";
        [SerializeField] private Text keyText;
        [SerializeField] private Button bindButton;

        [SerializeField]
        private KeyCode[] ignoredKeys = { KeyCode.None };

        public UnityEvent<string, InputBinding> onKeyRebinded;
        public UnityEvent<bool> onListeningStateChanged;

        private Coroutine rebindCoroutine;
        private bool isListening = false;
        private InputManager inputManager;

        private void Start()
        {
            inputManager = InputManager.Instance;

            if (inputManager == null)
            {
                Debug.LogError("场景中未找到 InputManager！");
                return;
            }

            inputManager.OnKeyRebinded += HandleKeyRebinded;
            UpdateKeyDisplay();

            if (bindButton != null)
            {
                bindButton.onClick.AddListener(StartListening);
            }
        }

        private void OnDestroy()
        {
            if (inputManager != null)
            {
                inputManager.OnKeyRebinded -= HandleKeyRebinded;
            }
        }

        private void HandleKeyRebinded(string name, InputBinding newBinding)
        {
            if (name == actionName)
            {
                UpdateKeyDisplay();
                onKeyRebinded?.Invoke(name, newBinding);
            }
        }

        public void StartListening()
        {
            if (isListening) return;

            if (rebindCoroutine != null)
                StopCoroutine(rebindCoroutine);

            rebindCoroutine = StartCoroutine(WaitForKeyPress());
        }

        public void CancelListening()
        {
            if (rebindCoroutine != null)
            {
                StopCoroutine(rebindCoroutine);
                rebindCoroutine = null;
            }

            SetListeningState(false);

            if (bindButton != null)
                bindButton.interactable = true;
        }

        private void SetListeningState(bool listening)
        {
            isListening = listening;
            onListeningStateChanged?.Invoke(listening);
        }

        private IEnumerator WaitForKeyPress()
        {
            SetListeningState(true);

            if (bindButton != null)
                bindButton.interactable = false;

            if (keyText != null)
                keyText.text = "等待按键...";

            yield return null;

            while (!Input.anyKeyDown)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("取消按键绑定");
                    CancelListening();
                    UpdateKeyDisplay();
                    yield break;
                }

                yield return null;
            }

            InputBinding capturedBinding = CaptureInputBinding();
            if (!capturedBinding.IsEmpty)
            {
                inputManager.RebindKey(actionName, capturedBinding);
                inputManager.SaveBindings();
            }
            else
            {
                Debug.LogWarning("未能捕获有效按键组合，请重试。");
            }

            UpdateKeyDisplay();
            SetListeningState(false);

            if (bindButton != null)
                bindButton.interactable = true;
        }

        private InputBinding CaptureInputBinding()
        {
            KeyCode pressedKey = KeyCode.None;

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (IsIgnoredKey(key))
                    {
                        Debug.LogWarning($"不支持绑定按键: {key}");
                        continue;
                    }

                    if (InputBinding.KeyCodeToModifier(key) != ModifierKey.None)
                    {
                        Debug.LogWarning("不能只绑定修饰键，请同时按下主键。");
                        return InputBinding.FromKeyCode(KeyCode.None);
                    }

                    pressedKey = key;
                    break;
                }
            }

            if (pressedKey == KeyCode.None)
                return InputBinding.FromKeyCode(KeyCode.None);

            KeyCode comboKey = KeyCode.None;
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == pressedKey || key == KeyCode.None)
                    continue;

                if (InputBinding.IsModifierKeyCode(key))
                    continue;

                if (IsIgnoredKey(key))
                    continue;

                if (Input.GetKey(key))
                {
                    comboKey = key;
                    break;
                }
            }

            return new InputBinding
            {
                key = pressedKey,
                modifiers = InputBinding.GetHeldModifiers(),
                comboKey = comboKey
            };
        }

        private bool IsIgnoredKey(KeyCode key)
        {
            foreach (var ignoredKey in ignoredKeys)
            {
                if (key == ignoredKey) return true;
            }
            return false;
        }

        public void UpdateKeyDisplay()
        {
            if (keyText != null && inputManager != null)
            {
                InputBinding currentBinding = inputManager.GetBinding(actionName);
                keyText.text = currentBinding.GetDisplayName();
            }
        }

        public void ResetToDefault()
        {
            if (inputManager != null)
            {
                inputManager.ResetKeyToDefault(actionName);
            }
        }
    }
}
