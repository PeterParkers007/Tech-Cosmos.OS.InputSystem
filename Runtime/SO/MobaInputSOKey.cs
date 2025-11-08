using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TechCosmos.InputSystem.Runtime.SO
{
    [CreateAssetMenu(fileName ="New MobaInput Config",menuName ="TechCosmos/Input")]
    public class MobaInputSOKey : ScriptableObject
    {
        public string id;

        [Header("选择指令")]
        public KeyCode selectKey = KeyCode.Mouse0;

        [Header("移动指令")]
        public KeyCode moveKey = KeyCode.Mouse1;

        [Header("停止指令")]
        public KeyCode stopKey = KeyCode.S;

        [Header("保持指令")]
        public KeyCode holdKey = KeyCode.H;

        [Header("技能快捷键 - 动态分配")]
        public List<KeyCode> abilityHotkeys = new List<KeyCode>
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R,
        KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.Z,
        KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B
    };

        [Header("编队快捷键")]
        public KeyCode[] controlGroupKeys = new KeyCode[] {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3
    };
        [Header("框选设置")]
        [Tooltip("最小框选距离，避免误触")]
        public float minSelectionDistance = 10f;
    }
}

