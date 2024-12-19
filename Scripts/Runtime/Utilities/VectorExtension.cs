using UnityEngine;
using System.Runtime.CompilerServices;

namespace SFEscape.Runtime.Utilities
{
    /// <summary>
    /// Vectorの拡張メソッドクラス
    /// </summary>
    public static class VectorExtension
    {
        /// <summary>
        /// Vector2をVector3のXZに分配し、変換するメソッド
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3XZ(in this Vector2 vector2, float y = 0)
        {
            return new Vector3(vector2.x, y, vector2.y);
        }
    }
}
