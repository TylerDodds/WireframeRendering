// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Utility functions for <see cref="Dictionary{TKey, TValue}"/>.
    /// </summary>
    internal static class DictionaryUtil
    {
        /// <summary>
        /// Adds an item to a dictionary where values are <see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TItem">The type of items in the value collection.</typeparam>
        /// <typeparam name="TCollection">The type of the value, an <see cref="ICollection{TItem}"/>. </typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key of the item.</param>
        /// <param name="value">The value of the item.</param>
        public static void AddListItem<TKey, TItem, TCollection>(this Dictionary<TKey, TCollection> dictionary, TKey key, TItem value) where TCollection : ICollection<TItem>, new()
        {
            if (!dictionary.ContainsKey(key))
            {
                TCollection newCollection = new TCollection();
                newCollection.Add(value);
                dictionary[key] = newCollection;
            }
            else
            {
                dictionary[key].Add(value);
            }
        }
    }
}
