using UnityEngine;
using System.Runtime.CompilerServices;

namespace SFEscape.Runtime.Utilities
{
    /// <summary>
    /// 角度情報に関するユーティリティクラス
    /// </summary>
    public static class AngleUtility
    {
        /// <summary>
        /// 0-360度の角度を-180から180度の範囲に変換します
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float To180(float angle0To360)
        {
            float normalizedAngle = angle0To360 % 360f;
            if (normalizedAngle < 0)
            {
                normalizedAngle += 360f;
            }

            if (normalizedAngle > 180f)
            {
                normalizedAngle -= 360f;
            }

            return normalizedAngle;
        }

        /// <summary>
        /// -180から180度の角度を0-360度の範囲に変換します
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float To360(float angleMinus180To180)
        {
            float normalizedAngle = angleMinus180To180;
            while (normalizedAngle <= -180f)
            {
                normalizedAngle += 360f;
            }
            while (normalizedAngle > 180f)
            {
                normalizedAngle -= 360f;
            }

            if (normalizedAngle < 0)
            {
                normalizedAngle += 360f;
            }

            return normalizedAngle;
        }

        /// <summary>
        /// Vector3の各成分の角度を0-360から-180から180の範囲に変換します
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RotationTo180(Vector3 rot)
        {
            return new Vector3(
                To180(rot.x),
                To180(rot.y),
                To180(rot.z)
            );
        }

        /// <summary>
        /// Vector3の各成分の角度を-180から180から0-360の範囲に変換します
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RotationTo360(Vector3 rot)
        {
            return new Vector3(
                To360(rot.x),
                To360(rot.y),
                To360(rot.z)
            );
        }
    }
}

