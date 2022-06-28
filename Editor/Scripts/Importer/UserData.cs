// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using System;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Class for accessing stored model import user data.
    /// </summary>
    internal static class UserData
    {
        /// <summary>
        /// Parses <see cref="WireframeInfo"/> from <see cref="AssetImporter.userData"/>.
        /// </summary>
        /// <param name="userData">The userData.</param>
        /// <param name="couldNotParse">If parsing failed.</param>
        /// <returns>The <see cref="WireframeInfo"/>.</returns>
        internal static WireframeInfo GetWireframeInfoFromUserData(string userData, out bool couldNotParse)
        {
            WireframeInfo info;
            couldNotParse = false;
            if (!string.IsNullOrEmpty(userData))
            {
                try
                {
                    info = JsonUtility.FromJson<WireframeInfo>(userData);
                }
                catch (ArgumentException)
                {
                    info = GetNewWireframeInfo(false);
                    couldNotParse = true;
                }
            }
            else
            {
                info = GetNewWireframeInfo(false);
                couldNotParse = true;
            }
            return info;
        }

        /// <summary>
        /// Gets a <see cref="WireframeInfo"/> using default UV channel settings.
        /// </summary>
        /// <param name="shouldGenerate">If wireframe information should be generated.</param>
        /// <returns>A <see cref="WireframeInfo"/>.</returns>
        internal static WireframeInfo GetNewWireframeInfo(bool shouldGenerate) => new WireframeInfo(shouldGenerate);
    }
}
