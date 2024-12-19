using System;
using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace SFEscape.Runtime.Utilities
{
    public static class RandomEx
    {
        /// <summary>
        /// 重複なし乱数取得メソッド（UnityEngine.Random.Rangeを使用）
        /// </summary>
        /// <param name="minInclusive">最小値（含む）</param>
        /// <param name="maxExclusive">最大値（含まない）</param>
        /// <param name="count">取得する乱数の個数</param>
        /// <returns>重複のない乱数配列</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] GetUniqueRandomNumbers(int minInclusive, int maxExclusive, int count)
        {
            if (count > maxExclusive - minInclusive)
            {
                throw new ArgumentException("Requested count is larger than the available range");
            }

            var numbers = (Span<int>)stackalloc int[maxExclusive - minInclusive];
            for (int i = 0; i < numbers.Length; i++)
            {
                numbers[i] = minInclusive + i;
            }
            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, numbers.Length);
                result[i] = numbers[index];
                numbers[index] = numbers[^1];
                numbers = numbers[..^1];
            }

            return result;
        }

        /// <summary>
        /// int乱数取得メソッド（UnityEngine.Random.Rangeを使用）
        /// </summary>
        /// <param name="minInclusive">最小値（含む）</param>
        /// <param name="maxExclusive">最大値（含まない）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Range(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        /// <summary>
        /// float乱数取得メソッド（UnityEngine.Random.Rangeを使用）
        /// </summary>
        /// <param name="minInclusive">最小値（含む）</param>
        /// <param name="maxInclusive">最大値（含む）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Range(float minInclusive, float maxInclusive)
        {
            return Random.Range(minInclusive, maxInclusive);
        }
    }
}
