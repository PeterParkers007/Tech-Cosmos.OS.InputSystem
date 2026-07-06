using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.InputSystem.Runtime
{
    [CreateAssetMenu(menuName = "Tech-Cosmos/Input/Config", fileName = "New Input Config")]
    public class InputConfig : ScriptableObject
    {
        public List<KeyConfig> keyConfigs = new List<KeyConfig>();
    }
}