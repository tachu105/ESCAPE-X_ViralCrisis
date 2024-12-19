using UnityEngine;

namespace SFEscape.Runtime.Utilities.Debugger
{
    /// <summary>
    /// デバッグ変数を定義するための属性
    /// </summary>
    [System.Serializable]
    public class DebugVariableAttribute : PropertyAttribute
    {
        public string Description { get; private set; }

        public DebugVariableAttribute(string description = "")
        {
            Description = description;
        }
    }
}
