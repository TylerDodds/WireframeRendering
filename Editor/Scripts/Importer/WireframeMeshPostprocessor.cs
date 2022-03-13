// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.Importer
{
    /// <summary>
    /// Model postprocessor that assigns wireframe uvs.
    /// </summary>
    public class WireframeMeshPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// Clears selected model importer wireframe user data.
        /// </summary>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathClearUVInformation)]
        internal static void ClearWireframeUVCoordinateInformationWithError()
        {
            ModelImporter[] modelImporters = GetSelectedModelImporters();
            if (modelImporters.Length > 0)
            {
                Undo.RecordObjects(modelImporters, WireframeRenderingSettingsUtil.MenuNameClearUVInformation);
                foreach (ModelImporter modelImporter in modelImporters)
                {
                    ClearWireframeUVCoordinateInformation(modelImporter, true, out _);
                }
            }
        }

        /// <summary>
        /// Clears <see cref="AssetImporter.userData"/> for the given <paramref name="modelImporter"/>.
        /// </summary>
        /// <param name="modelImporter">The <see cref="ModelImporter"/>.</param>
        /// <param name="logErrorIfCannotParse">If Debug.LogError should be called when wireframe data cannot be parsed.</param>
        /// <param name="couldNotParse">If wireframe data couldn't be pared from <see cref="AssetImporter.userData"/>.</param>
        internal static void ClearWireframeUVCoordinateInformation(ModelImporter modelImporter, bool logErrorIfCannotParse, out bool couldNotParse)
        {
            couldNotParse = false;
            if(modelImporter != null)
            {
                if (!string.IsNullOrEmpty(modelImporter.userData))
                {
                    WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(modelImporter, out couldNotParse);
                    if(couldNotParse)
                    {
                        if (logErrorIfCannotParse)
                        {
                            Debug.LogError($"{modelImporter.assetPath}: Wireframe Rendering could not parse {nameof(AssetImporter)}.{nameof(modelImporter.userData)} and will therefore not clear it, in case it contains other custom data.\n" +
                            "Disable user configuration of uv generation in the settings to turn off use of this field.");
                        }
                    }
                    else
                    {
                        modelImporter.userData = string.Empty;
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Sets selected model to generate wireframe uv coordinates.
        /// </summary>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetUvInformationGenerate)]
        internal static void SetApplyWireframeUVCoordinates()
        {
            SetUseWireframeUVCoordinates(true, WireframeRenderingSettingsUtil.MenuNameSetUvInformationGenerate);
        }

        /// <summary>
        /// Sets selected model to not generate wireframe uv coordinates.
        /// </summary>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetUvInformationDoNotGenerate)]
        internal static void SetRemoveWireframeUVCoordinates()
        {
            SetUseWireframeUVCoordinates(false, WireframeRenderingSettingsUtil.MenuNameSetUvInformationDoNotGenerate);
        }

        /// <summary>
        /// Tries to update wireframe information stored in the <paramref name="modelImporter"/>.
        /// /// </summary>
        /// <param name="channel">The new UV channel.</param>
        /// <param name="modelImporter">The <see cref="ModelImporter"/>.</param>
        /// <returns>If wireframe information was stored in the <paramref name="modelImporter"/> to be updated.</returns>
        internal static bool TryUpdateWireframeInfo(int channel, ModelImporter modelImporter)
        {
            WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(modelImporter, out bool couldNotParse);
            if (!couldNotParse)
            {
                wireframeInfo.WireframeUvChannel = channel;
                modelImporter.userData = JsonUtility.ToJson(wireframeInfo);
                modelImporter.SaveAndReimport();
            }
            return !couldNotParse;
        }

        /// <summary>
        /// If models in this directory should be considered for automatically having wireframe uvs generated.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <param name="suffix">The wireframe directory suffix.</param>
        /// <returns>If uvs should be generated.</returns>
        internal static bool UseDirectoryForWireframe(string assetPath, string suffix)
        {
            string directoryName = System.IO.Path.GetDirectoryName(assetPath);
            return !string.IsNullOrWhiteSpace(suffix) && directoryName.EndsWith(suffix);
        }

        /// <summary>
        /// Parses <see cref="WireframeInfo"/> from <see cref="AssetImporter.userData"/>.
        /// </summary>
        /// <param name="assetImporter">The asset importer.</param>
        /// <param name="couldNotParse">If parsing failed.</param>
        /// <returns>The <see cref="WireframeInfo"/>.</returns>
        internal static WireframeInfo GetWireframeInfoFromUserData(AssetImporter assetImporter, out bool couldNotParse)
        {
            return GetWireframeInfoFromUserData(assetImporter.userData, out couldNotParse);
        }

        /// <summary>
        /// Sets the userData if the model importer should use wireframe generation or not.
        /// </summary>
        /// <param name="generateCoordinates">If UV coordinates should be geenerated.</param>
        /// <param name="undoName">Name used for undo command.</param>
        private static void SetUseWireframeUVCoordinates(bool generateCoordinates, string undoName)
        {
            ModelImporter[] modelImporters = GetSelectedModelImporters();
            if (modelImporters.Length > 0)
            {
                Undo.RecordObjects(modelImporters, undoName);
                foreach (ModelImporter modelImporter in modelImporters)
                {
                    if (generateCoordinates)
                    {
                        SetUserDataToUseWireframeCoordinates(modelImporter);
                    }
                    else
                    {
                        WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(modelImporter, out bool couldNotParse);
                        if (wireframeInfo.GenerateWireframeUvs || string.IsNullOrEmpty(modelImporter.userData))
                        {
                            wireframeInfo.GenerateWireframeUvs = false;
                            modelImporter.userData = JsonUtility.ToJson(wireframeInfo);
                            modelImporter.SaveAndReimport();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="ModelImporter"/> of the selected asset (if it's a model, null otherwise).
        /// </summary>
        /// <returns>The selected <see cref="ModelImporter"/>.</returns>
        private static ModelImporter[] GetSelectedModelImporters()
        {
            return Selection.objects.Where(o => o != null).Select(o => AssetDatabase.GetAssetPath(o)).Where(p => !string.IsNullOrEmpty(p))
                .Select(p => AssetImporter.GetAtPath(p)).Where(ai => ai != null).Select(ai => ai as ModelImporter).Where(m => m != null).ToArray();
        }

        /// <summary>
        /// Sets the <see cref="AssetImporter.userData"/> to designate if the importer should generate wireframe uv coordinates.
        /// </summary>
        /// <param name="assetImporter">The <see cref="AssetImporter"/>.</param>
        private static void SetUserDataToUseWireframeCoordinates(AssetImporter assetImporter)
        {
            if (string.IsNullOrEmpty(assetImporter.userData))
            {
                assetImporter.userData = JsonUtility.ToJson(GetNewWireframeInfo(true));
                assetImporter.SaveAndReimport();
            }
            else
            {
                WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(assetImporter, out bool couldNotParse);
                if (!couldNotParse)
                {
                    wireframeInfo.GenerateWireframeUvs = true;
                    assetImporter.userData = JsonUtility.ToJson(wireframeInfo);
                    assetImporter.SaveAndReimport();
                }
                else
                {
                    Debug.LogError($"{assetImporter.assetPath}: Wireframe Rendering expects {nameof(AssetImporter)}.{nameof(assetImporter.userData)} to be empty in order to add custom metadata regarding the import state of the model." +
                        "Disable user configuration of uv generation in the settings to turn off use of this field.");
                }
            }
        }

        /// <summary>
        /// AssetPostprocessor message on preprocessing of the model.
        /// </summary>
        void OnPreprocessModel()
        {
            WireframeRenderingSettings wireframeRenderingSettings = WireframeRenderingSettings.Settings;
            if (wireframeRenderingSettings.DoNotWeldVertices)
            {
                bool generateCoordinates = GetIfShouldGenerateWireframeCoordinates(wireframeRenderingSettings);
                if (generateCoordinates)
                {
                    (assetImporter as ModelImporter).weldVertices = false;
                }
            }
        }

        /// <summary>
        /// AssetPostprocessor message on postprocessing of the model.
        /// </summary>
        void OnPostprocessModel(GameObject go)
        {
            WireframeRenderingSettings wireframeRenderingSettings = WireframeRenderingSettings.Settings;
            bool generateCoordinates = GetIfShouldGenerateWireframeCoordinates(wireframeRenderingSettings);
            if (generateCoordinates)
            {
                SetWireframeCoordinates(go, wireframeRenderingSettings.UvChannel, wireframeRenderingSettings.AngleCutoffDegrees);
            }
        }

        /// <summary>
        /// From the <paramref name="wireframeRenderingSettings"/>, determines if the current <see cref="ModelImporter"/> should have wireframe texture coordinates generated for it.
        /// </summary>
        /// <param name="wireframeRenderingSettings">The <see cref="WireframeRenderingSettings"/>.</param>
        /// <returns>If wireframe texture coordinates should be generated.</returns>
        private bool GetIfShouldGenerateWireframeCoordinates(WireframeRenderingSettings wireframeRenderingSettings)
        {
            bool generateCoordinates = false;
            if (assetImporter is ModelImporter modelImporter)
            {
                if (wireframeRenderingSettings.AllowUserConfigurationOfUvGeneration)
                {
                    WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(assetImporter, out bool couldNotParse);
                    if (couldNotParse)
                    {
                        if (UseDirectoryForWireframe(assetPath, wireframeRenderingSettings.DirectorySuffixForAutomaticGeneration))
                        {
                            generateCoordinates = true;
                        }
                    }
                    else
                    {
                        if (wireframeInfo.GenerateWireframeUvs)
                        {
                            generateCoordinates = true;
                        }
                    }
                }
                else
                {
                    if (UseDirectoryForWireframe(assetPath, wireframeRenderingSettings.DirectorySuffixForAutomaticGeneration))
                    {
                        generateCoordinates = true;

                    }
                }
            }

            return generateCoordinates;
        }

        /// <summary>
        /// Sets wireframe uv coordinates for meshes on the given <see cref="GameObject"/>.
        /// </summary>
        /// <param name="go">The <see cref="GameObject"/>.</param>
        /// <param name="channel">The UV channel.</param>
        /// <param name="angleCutoffDegrees">Number of degrees between edges before they are considered to have different angles for wireframe texture generation.</param>
        private void SetWireframeCoordinates(GameObject go, int channel, float angleCutoffDegrees)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    WireframeCoordinateGenerator.DecoupleDisconnectedPortions(meshFilter.sharedMesh, channel, angleCutoffDegrees, s => RaiseWarningDuringCoordinateGeneration(s, meshFilter));
                }
            }
        }

        /// <summary>
        /// Raises a warning that occurs during wireframe texture coordinate generation.
        /// </summary>
        /// <param name="meshFilter">The <see cref="MeshFilter"/> whose mesh is having coordinates generated.</param>
        /// <param name="warning">The warning to raise.</param>
        private void RaiseWarningDuringCoordinateGeneration(string warning, MeshFilter meshFilter)
        {
            Debug.LogWarning($"Warning while importing mesh at path {assetPath} in MeshFilter {meshFilter.name}: {warning}");
        }

        /// <summary>
        /// Parses <see cref="WireframeInfo"/> from <see cref="AssetImporter.userData"/>.
        /// </summary>
        /// <param name="userData">The userData.</param>
        /// <param name="couldNotParse">If parsing failed.</param>
        /// <returns>The <see cref="WireframeInfo"/>.</returns>
        private static WireframeInfo GetWireframeInfoFromUserData(string userData, out bool couldNotParse)
        {
            WireframeInfo info;
            couldNotParse = false;
            if (!string.IsNullOrEmpty(userData))
            {
                try
                {
                    info = JsonUtility.FromJson<WireframeInfo>(userData);
                }
                catch(ArgumentException)
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
        /// A <see cref="WireframeTextureCoordinateGenerator"/> for generating wireframe uvs.
        /// </summary>
        private WireframeTextureCoordinateGenerator WireframeCoordinateGenerator
        {
            get
            {
                if(_wireframeCoordinateGenerator == null)
                {
                    _wireframeCoordinateGenerator = new WireframeTextureCoordinateGenerator();
                }
                return _wireframeCoordinateGenerator;
            }
        }
        private WireframeTextureCoordinateGenerator _wireframeCoordinateGenerator = null;

        /// <summary>
        /// Gets a <see cref="WireframeInfo"/> using default UV channel settings.
        /// </summary>
        /// <param name="includeWireframeChannel">If wireframe channel should be included.</param>
        /// <returns>A <see cref="WireframeInfo"/>.</returns>
        private static WireframeInfo GetNewWireframeInfo(bool includeWireframeChannel) => new WireframeInfo(includeWireframeChannel, WireframeRenderingSettings.Settings.UvChannel);

        /// <summary>
        /// Gets if user configuration is allowed based on <see cref="WireframeRenderingSettings"/>.
        /// </summary>
        /// <returns>If user configuration is allowed.</returns>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetUvInformationGenerate, true)]
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetUvInformationDoNotGenerate, true)]
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathClearUVInformation, true)]
        private static bool IsUserConfigurationAllowed()
        {
            return WireframeRenderingSettings.Settings.AllowUserConfigurationOfUvGeneration;
        }

        /// <summary>
        /// Information about wireframe uv generation to be serialized as custom data for the asset importer.
        /// </summary>
        [Serializable]
        internal struct WireframeInfo
        {
            public bool GenerateWireframeUvs;
            public int WireframeUvChannel;

            public WireframeInfo(bool addWireframeUvs, int uvChannel)
            {
                GenerateWireframeUvs = addWireframeUvs;
                WireframeUvChannel = uvChannel;
            }
        }
    }
}
