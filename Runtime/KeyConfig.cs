using System;
using UnityEngine;

namespace TechCosmos.InputSystem.Runtime
{
    [Serializable]
    public struct KeyConfig
    {
        public string name;
        public KeyCode keyCode;
        public InputBinding binding;

        public InputBinding GetEffectiveBinding()
        {
            if (!binding.IsEmpty)
                return binding;

            return InputBinding.FromKeyCode(keyCode);
        }
    }
}
