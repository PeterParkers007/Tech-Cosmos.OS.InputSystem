using System;
using UnityEngine;

namespace TechCosmos.InputSystem.Runtime
{
    [Flags]
    public enum ModifierKey
    {
        None = 0,
        Control = 1 << 0,
        Alt = 1 << 1,
        Shift = 1 << 2,
    }

    [Serializable]
    public struct InputBinding : IEquatable<InputBinding>
    {
        public KeyCode key;
        public ModifierKey modifiers;
        public KeyCode comboKey;

        public static InputBinding FromKeyCode(KeyCode keyCode)
        {
            return new InputBinding { key = keyCode };
        }

        public bool IsEmpty => key == KeyCode.None;

        public bool Equals(InputBinding other)
        {
            return key == other.key && modifiers == other.modifiers && comboKey == other.comboKey;
        }

        public override bool Equals(object obj)
        {
            return obj is InputBinding other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)key;
                hash = hash * 31 + (int)modifiers;
                hash = hash * 31 + (int)comboKey;
                return hash;
            }
        }

        public static bool IsModifierKeyCode(KeyCode key)
        {
            return key == KeyCode.LeftControl || key == KeyCode.RightControl
                || key == KeyCode.LeftAlt || key == KeyCode.RightAlt
                || key == KeyCode.LeftShift || key == KeyCode.RightShift;
        }

        public static ModifierKey KeyCodeToModifier(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return ModifierKey.Control;
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return ModifierKey.Alt;
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return ModifierKey.Shift;
                default:
                    return ModifierKey.None;
            }
        }

        public static ModifierKey GetHeldModifiers()
        {
            ModifierKey mods = ModifierKey.None;

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                mods |= ModifierKey.Control;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                mods |= ModifierKey.Alt;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                mods |= ModifierKey.Shift;

            return mods;
        }

        public bool IsPressed()
        {
            if (key == KeyCode.None)
                return false;

            if (!AreModifiersHeld())
                return false;

            if (!Input.GetKey(key))
                return false;

            if (comboKey != KeyCode.None && !Input.GetKey(comboKey))
                return false;

            return true;
        }

        public bool WasPressedThisFrame()
        {
            if (key == KeyCode.None)
                return false;

            if (!AreModifiersHeld())
                return false;

            if (Input.GetKeyDown(key))
            {
                if (comboKey == KeyCode.None || Input.GetKey(comboKey))
                    return true;
            }

            if (comboKey != KeyCode.None && Input.GetKeyDown(comboKey))
            {
                if (Input.GetKey(key))
                    return true;
            }

            return false;
        }

        public bool WasReleasedThisFrame()
        {
            if (key == KeyCode.None)
                return false;

            if (Input.GetKeyUp(key))
                return true;

            if (comboKey != KeyCode.None && Input.GetKeyUp(comboKey))
                return true;

            if (ModifiersReleasedWhileKeysHeld())
                return true;

            return false;
        }

        public string GetDisplayName()
        {
            if (IsEmpty)
                return "None";

            var parts = new System.Collections.Generic.List<string>();

            if ((modifiers & ModifierKey.Control) != 0)
                parts.Add("Ctrl");

            if ((modifiers & ModifierKey.Alt) != 0)
                parts.Add("Alt");

            if ((modifiers & ModifierKey.Shift) != 0)
                parts.Add("Shift");

            if (comboKey != KeyCode.None)
                parts.Add(FormatKeyCode(comboKey));

            parts.Add(FormatKeyCode(key));

            return string.Join(" + ", parts);
        }

        private bool AreModifiersHeld()
        {
            ModifierKey held = GetHeldModifiers();
            return (held & modifiers) == modifiers;
        }

        private bool ModifiersReleasedWhileKeysHeld()
        {
            if (modifiers == ModifierKey.None)
                return false;

            bool modifierReleased = false;

            if ((modifiers & ModifierKey.Control) != 0)
                modifierReleased |= Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl);

            if ((modifiers & ModifierKey.Alt) != 0)
                modifierReleased |= Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt);

            if ((modifiers & ModifierKey.Shift) != 0)
                modifierReleased |= Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift);

            if (!modifierReleased)
                return false;

            return Input.GetKey(key) && (comboKey == KeyCode.None || Input.GetKey(comboKey));
        }

        private static string FormatKeyCode(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Mouse0: return "Left Mouse";
                case KeyCode.Mouse1: return "Right Mouse";
                case KeyCode.Mouse2: return "Middle Mouse";
                default: return keyCode.ToString();
            }
        }
    }

    [Serializable]
    public struct KeyConfig
    {
        public string name;
        public InputBinding binding;
    }
}