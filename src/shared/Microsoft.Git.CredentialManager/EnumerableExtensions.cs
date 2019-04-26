// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Git.CredentialManager
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Concatenates multiple sequences.
        /// </summary>
        /// <param name="first">Initial sequence to concatenate other sequences with.</param>
        /// <param name="others">Other sequences to concatenate together with <paramref name="first"/>.</param>
        /// <typeparam name="TSource">Type of the sequence elements.</typeparam>
        /// <returns>Concatenated sequence.</returns>
        public static IEnumerable<TSource> ConcatMany<TSource>(this IEnumerable<TSource> first, params IEnumerable<TSource>[] others)
        {
            IEnumerable<TSource> result = first;

            foreach (IEnumerable<TSource> other in others)
            {
                result = result.Concat(other);
            }

            return result;
        }

        public static IEnumerable<(T, T)> TakePairs<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                while (true)
                {
                    if (enumerator.MoveNext())
                    {
                        T first = enumerator.Current;

                        if (enumerator.MoveNext())
                        {
                            T second = enumerator.Current;

                            yield return (first, second);
                        }
                        else
                        {
                            throw new InvalidOperationException("Sequence contains an odd number of elements");
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }
            }
        }
    }
}
