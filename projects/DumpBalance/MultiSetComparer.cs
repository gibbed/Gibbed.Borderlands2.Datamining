/* Copyright (c) 2019 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace DumpBalance
{
    public class MultiSetComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        private readonly IEqualityComparer<T> _Comparer;

        public MultiSetComparer(IEqualityComparer<T> comparer = null)
        {
            _Comparer = comparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first == null)
            {
                return second == null;
            }

            if (second == null)
            {
                return false;
            }

            if (ReferenceEquals(first, second) == true)
            {
                return true;
            }

            if (first is ICollection<T> firstCollection && second is ICollection<T> secondCollection)
            {
                if (firstCollection.Count != secondCollection.Count)
                {
                    return false;
                }

                if (firstCollection.Count == 0)
                {
                    return true;
                }
            }

            return HaveMismatchedElement(first, second) == false;
        }

        private bool HaveMismatchedElement(IEnumerable<T> first, IEnumerable<T> second)
        {
            int firstNullCount;
            int secondNullCount;

            var firstElementCounts = GetElementCounts(first, out firstNullCount);
            var secondElementCounts = GetElementCounts(second, out secondNullCount);

            if (firstNullCount != secondNullCount || firstElementCounts.Count != secondElementCounts.Count)
            {
                return true;
            }

            foreach (var kvp in firstElementCounts)
            {
                var firstElementCount = kvp.Value;
                int secondElementCount;
                secondElementCounts.TryGetValue(kvp.Key, out secondElementCount);

                if (firstElementCount != secondElementCount)
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<T, int> GetElementCounts(IEnumerable<T> enumerable, out int nullCount)
        {
            var dictionary = new Dictionary<T, int>(_Comparer);
            nullCount = 0;

            foreach (T element in enumerable)
            {
                if (element == null)
                {
                    nullCount++;
                }
                else
                {
                    int num;
                    dictionary.TryGetValue(element, out num);
                    num++;
                    dictionary[element] = num;
                }
            }

            return dictionary;
        }

        public int GetHashCode(IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            int hash = 17;

            foreach (T val in enumerable.OrderBy(x => x))
            {
                hash = hash * 23 + (val?.GetHashCode() ?? 42);
            }

            return hash;
        }
    }
}
