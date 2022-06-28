// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Add wireframe mesh generation info to project window items with a <see cref="ModelImporter"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class WireframeRenderingProjectWindowItems
    {
        static WireframeRenderingProjectWindowItems()
        {
            EditorApplication.projectWindowItemOnGUI -= OnItemCallback;
            EditorApplication.projectWindowItemOnGUI += OnItemCallback;
        }

        private const string DoNotGenerateText = "No Wireframe";
        private const string DoGenerateText = "Wireframe";
        private const string AutoGenerateText = "Wireframe (Auto)";

        /// <summary>
        /// Draws icons and additional tooltips for matched assets.
        /// </summary>
        private static void OnItemCallback(string guid, Rect selectionRect)
        {
            WireframeRenderingSettings settings = WireframeRenderingSettings.Settings;
            if (settings.ShowProjectItemInfo)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(path);
                //Since guid is the same for all sub-assets for now, we can't distinguish between them.
                if (assetImporter is ModelImporter modelImporter)
                {
                    Color prevContentColor = GUI.contentColor;
                    Color newContentColor = prevContentColor;
                    newContentColor.a = Mathf.Min(newContentColor.a, MinAlpha);
                    GUI.contentColor = newContentColor;
                    bool useDirectory = WireframeMeshPostprocessor.UseDirectoryForWireframe(path, settings.DirectorySuffixForAutomaticGeneration);
                    WireframeInfo wireframeInfo = WireframeMeshPostprocessor.GetWireframeInfoFromUserData(modelImporter, out bool couldNotParse);
                    if (couldNotParse)
                    {
                        if (useDirectory)
                        {
                            AddTextAtRight(selectionRect, AutoGenerateText);
                        }
                    }
                    else
                    {
                        if (wireframeInfo.ShouldGenerate)
                        {
                            AddTextAtRight(selectionRect, DoGenerateText);
                        }
                        else
                        {
                            AddTextAtRight(selectionRect, DoNotGenerateText);
                        }
                    }
                    GUI.contentColor = prevContentColor;
                }
            }
        }

        /// <summary>
        /// Adds <paramref name="text"/> to the right ot the <paramref name="selectionRect"/>.
        /// </summary>
        /// <param name="selectionRect">The <see cref="Rect"/>.</param>
        /// <param name="text">The text.</param>
        private static void AddTextAtRight(Rect selectionRect, string text)
        {
            GUILayout.BeginArea(selectionRect);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(text);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private const float MinAlpha = 0.3f;
    }
}
