
using UnityEngine;

namespace Assets.Scripts
{
    public static class ArrayExtensions
    {
        public static T[] Shuffle<T>(this T[] array)
        {
            for (int i = 0; i < array.Length - 1; i++)
            {
                var temp = array[i];
                array[i] = array[i + Random.Range(0, array.Length - i)];
            }

            return array;
        }
    }
}
