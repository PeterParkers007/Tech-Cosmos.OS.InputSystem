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
                Debug.LogError("InputManager not found in scene.");
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

        public void SetActionName(string newActionName)
        {
            actionName = newActionName;
            UpdateKeyDisplay();
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
                keyText.text = "Waiting for input...";

            yield return null;

            while (!Input.anyKeyDown && !IsMouseButtonDown())
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log("Key binding cancelled.");
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
                Debug.LogWarning("Failed to capture valid input, ignoring.");
            }

            UpdateKeyDisplay();
            SetListeningState(false);

            if (bindButton != null)
                bindButton.interactable = true;
        }

        private bool IsMouseButtonDown()
        {
            return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
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
                        Debug.LogWarning($"Key binding blocked: {key}");
                        continue;
                    }

                    if (InputBinding.KeyCodeToModifier(key) != ModifierKey.None)
                    {
                        Debug.LogWarning("Modifier keys alone cannot be bound.");
                        return InputBinding.FromKeyCode(KeyCode.None);
                    }

                    pressedKey = key;
                    break;
                }
            }

            if (pressedKey == KeyCode.None)
            {
                if (Input.GetMouseButtonDown(0))
                    pressedKey = KeyCode.Mouse0;
                else if (Input.GetMouseButtonDown(1))
                    pressedKey = KeyCode.Mouse1;
                else if (Input.GetMouseButtonDown(2))
                    pressedKey = KeyCode.Mouse2;
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

        public void UnbindKey()
        {
            if (inputManager != null)
            {
                inputManager.UnbindKey(actionName);
            }
        }
    }
}