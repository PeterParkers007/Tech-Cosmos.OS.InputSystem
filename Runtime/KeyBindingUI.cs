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
        private KeyCode[] ignoredKeys = {
        KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Mouse2, KeyCode.None
    };

        public UnityEvent<string, KeyCode> onKeyRebinded;
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

        private void HandleKeyRebinded(string name, KeyCode newKey)
        {
            if (name == actionName)
            {
                UpdateKeyDisplay();
                onKeyRebinded?.Invoke(name, newKey);
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

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    if (IsIgnoredKey(key))
                    {
                        Debug.LogWarning($"不支持绑定按键: {key}");
                        continue;
                    }

                    inputManager.RebindKey(actionName, key);
                    inputManager.SaveBindings();
                    break;
                }
            }

            UpdateKeyDisplay();
            SetListeningState(false);

            if (bindButton != null)
                bindButton.interactable = true;
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
                KeyCode currentKey = inputManager.GetKeyCode(actionName);
                keyText.text = currentKey.ToString();
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
