// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing
{
    /// <summary>
    /// Utility functions for dealing with period lists.
    /// </summary>
    internal static class PeriodicUtilities
    {
        /// <summary>
        /// Get an <paramref name="index"/> wrapped cyclically to a given <paramref name="period"/>.
        /// </summary>
        /// <param name="index">The unwrapped index.</param>
        /// <param name="period">The period of wrapping.</param>
        /// <returns>The wrapped index.</returns>
        internal static int GetIndexPeriodic(int index, int period) => (index + period) % period;

        /// <summary>
        /// Gets the periodically-wrapped distance from the <paramref name="first"/> index to the <paramref name="second"/>, 
        /// where distance is measured by the number of increasing steps from <paramref name="first"/> to <paramref name="second"/>,
        /// wrapping at the given <paramref name="period"/> if necessary.
        /// </summary>
        /// <param name="first">The first index.</param>
        /// <param name="second">The second index.</param>
        /// <param name="period">The period of wrapping.</param>
        /// <returns>The distance between indices.</returns>
        internal static int GetIndexDistance(int first, int second, int period) => second > first ? second - first : period - (first - second);
    }
}
