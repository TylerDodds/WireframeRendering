// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Editor.MeshProcessing;
using PixelinearAccelerator.WireframeRendering.Editor.Settings;
using PixelinearAccelerator.WireframeRendering.Runtime.Enums;
using PixelinearAccelerator.WireframeRendering.Runtime.Mesh;
using System.IO;
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
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathClearWireframeInformation)]
        internal static void ClearWireframeGenerationInformationWithError()
        {
            ModelImporter[] modelImporters = GetSelectedModelImporters();
            if (modelImporters.Length > 0)
            {
                Undo.RecordObjects(modelImporters, WireframeRenderingSettingsUtil.MenuNameClearWireframeInformation);
                foreach (ModelImporter modelImporter in modelImporters)
                {
                    ClearWireframeGenerationInformation(modelImporter, true, out _);
                }
            }
        }

        /// <summary>
        /// Clears <see cref="AssetImporter.userData"/> for the given <paramref name="modelImporter"/>.
        /// </summary>
        /// <param name="modelImporter">The <see cref="ModelImporter"/>.</param>
        /// <param name="logErrorIfCannotParse">If Debug.LogError should be called when wireframe data cannot be parsed.</param>
        /// <param name="couldNotParse">If wireframe data couldn't be pared from <see cref="AssetImporter.userData"/>.</param>
        internal static void ClearWireframeGenerationInformation(ModelImporter modelImporter, bool logErrorIfCannotParse, out bool couldNotParse)
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
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetWireframeInformationGenerate)]
        internal static void SetDoGenerateWireframeInformation()
        {
            SetGenerateWireframeInformation(true, WireframeRenderingSettingsUtil.MenuNameSetWireframeWireframeInformationGenerate);
        }

        /// <summary>
        /// Sets selected model to not generate wireframe uv coordinates.
        /// </summary>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetWireframeInformationDoNotGenerate)]
        internal static void SetDoNotGenerateWireframeInformation()
        {
            SetGenerateWireframeInformation(false, WireframeRenderingSettingsUtil.MenuNameSetWireframeInformationDoNotGenerate);
        }

        /// <summary>
        /// If models in this directory should be considered for automatically having wireframe uvs generated.
        /// </summary>
        /// <param name="assetPath">The asset path.</param>
        /// <param name="suffix">The wireframe directory suffix.</param>
        /// <returns>If uvs should be generated.</returns>
        internal static bool UseDirectoryForWireframe(string assetPath, string suffix)
        {
            string directoryName = Path.GetDirectoryName(assetPath);
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
            return UserData.GetWireframeInfoFromUserData(assetImporter.userData, out couldNotParse);
        }

        /// <summary>
        /// Sets the userData if the model importer should use wireframe generation or not.
        /// </summary>
        /// <param name="generateInformation">If wireframe information should be geenerated.</param>
        /// <param name="undoName">Name used for undo command.</param>
        private static void SetGenerateWireframeInformation(bool generateInformation, string undoName)
        {
            ModelImporter[] modelImporters = GetSelectedModelImporters();
            if (modelImporters.Length > 0)
            {
                Undo.RecordObjects(modelImporters, undoName);
                foreach (ModelImporter modelImporter in modelImporters)
                {
                    if (generateInformation)
                    {
                        SetUserDataToGenerateWireframeInformation(modelImporter);
                    }
                    else
                    {
                        WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(modelImporter, out bool couldNotParse);
                        if (wireframeInfo.ShouldGenerate || string.IsNullOrEmpty(modelImporter.userData))
                        {
                            wireframeInfo.ShouldGenerate = false;
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
        private static void SetUserDataToGenerateWireframeInformation(AssetImporter assetImporter)
        {
            if (string.IsNullOrEmpty(assetImporter.userData))
            {
                assetImporter.userData = JsonUtility.ToJson(UserData.GetNewWireframeInfo(true));
                assetImporter.SaveAndReimport();
            }
            else
            {
                WireframeInfo wireframeInfo = GetWireframeInfoFromUserData(assetImporter, out bool couldNotParse);
                if (!couldNotParse)
                {
                    if (!wireframeInfo.ShouldGenerate)
                    {
                        wireframeInfo.ShouldGenerate = true;
                    }
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
            bool generateInformation = GetIfShouldGenerateWireframeInfo(wireframeRenderingSettings);
            if (generateInformation)
            {
                switch (wireframeRenderingSettings.WireframeTypeToUse)
                {
                    case WireframeType.TextureCoordinates:
                        if (wireframeRenderingSettings.DoNotWeldVertices)
                        {
                            (assetImporter as ModelImporter).weldVertices = false;
                        }
                        break;
                    case WireframeType.GeometryShader:
                        (assetImporter as ModelImporter).preserveHierarchy = true;
                        break;
                }
            }
        }

        /// <summary>
        /// AssetPostprocessor message on postprocessing of the model.
        /// </summary>
        void OnPostprocessModel(GameObject go)
        {
            WireframeRenderingSettings wireframeRenderingSettings = WireframeRenderingSettings.Settings;
            bool generateInformation = GetIfShouldGenerateWireframeInfo(wireframeRenderingSettings);
            if (generateInformation)
            {
                switch(wireframeRenderingSettings.WireframeTypeToUse)
                {
                    case WireframeType.TextureCoordinates:
                        SetWireframeCoordinates(go, wireframeRenderingSettings.UvChannel, wireframeRenderingSettings.AngleCutoffDegrees);
                        break;
                }
            }
        }

        /// <summary>
        /// Generates a wireframe mesh asset if needed for the given path of an imported model.
        /// </summary>
        /// <param name="modelAssetPath">The asset path of the imported model.</param>
        private static void GenerateWireframeMeshAssetIfNeeded(string modelAssetPath)
        {
            string assetGuid = AssetDatabase.AssetPathToGUID(modelAssetPath);

            string[] wireframeGeneratedMeshHolderGuids = AssetDatabase.FindAssets($"t:{nameof(Runtime.Mesh.WireframeGeneratedMeshInfo)}");

            bool alreadyGenerated = wireframeGeneratedMeshHolderGuids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => AssetDatabase.LoadAssetAtPath<WireframeGeneratedMeshInfo>(p))
                .Where(p => p != null)
                .Any(w => w != null && w.ReferenceGuid.Equals(assetGuid));

            if (!alreadyGenerated)
            {
                string newFileName = $"{Path.GetFileNameWithoutExtension(modelAssetPath)}_Wire{WireframeMeshScriptedImporter.FileExtension}";
                string newPath = Path.Combine(Path.GetDirectoryName(modelAssetPath), newFileName);
                newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                WireframeGeneratedMeshData wireframeGeneratedMesh = new WireframeGeneratedMeshData(assetGuid);
                string json = JsonUtility.ToJson(wireframeGeneratedMesh);
                File.WriteAllText(newPath, json);
                AssetDatabase.ImportAsset(newPath);
            }

        }

        /// <summary>
        /// AssetPostprocessor message on postprocessing of all assets.
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            WireframeRenderingSettings wireframeRenderingSettings = WireframeRenderingSettings.Settings;
            if(wireframeRenderingSettings.WireframeTypeToUse == WireframeType.GeometryShader)
            {
                AssetDatabase.StartAssetEditing();
                foreach(string assetPath in importedAssets)
                {
                    if (GetIfShouldGenerateWireframeInfo(wireframeRenderingSettings, AssetImporter.GetAtPath(assetPath), assetPath))
                    {
                        //NB It seems that this needs to be done in OnPostprocessAllAssets so that AssetDatabase.FindAssets can find the dependent assets.
                        GenerateWireframeMeshAssetIfNeeded(assetPath);
                    }
                }
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// From the <paramref name="wireframeRenderingSettings"/>, determines if the current <see cref="ModelImporter"/> should have wireframe information generated for it.
        /// </summary>
        /// <param name="wireframeRenderingSettings">The <see cref="WireframeRenderingSettings"/>.</param>
        /// <returns>If wireframe texture coordinates should be generated.</returns>
        private bool GetIfShouldGenerateWireframeInfo(WireframeRenderingSettings wireframeRenderingSettings)
        {
            return GetIfShouldGenerateWireframeInfo(wireframeRenderingSettings, assetImporter, assetPath);
        }

        /// <summary>
        /// From the <paramref name="wireframeRenderingSettings"/>, determines if the current <see cref="ModelImporter"/> should have wireframe information generated for it.
        /// </summary>
        /// <param name="wireframeRenderingSettings">The <see cref="WireframeRenderingSettings"/>.</param>
        /// <param name="assetImporter">The Asset Importer.</param>
        /// <param name="assetPath">The asset path.</param>
        /// <returns>If wireframe texture coordinates should be generated.</returns>
        private static bool GetIfShouldGenerateWireframeInfo(WireframeRenderingSettings wireframeRenderingSettings, AssetImporter assetImporter, string assetPath)
        {
            bool generateCoordinates = false;
            if (assetImporter is ModelImporter)
            {
                if (wireframeRenderingSettings.AllowUserConfigurationOfWireframeInfoGeneration)
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
                        if (wireframeInfo.ShouldGenerate)
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
        /// Gets if user configuration is allowed based on <see cref="WireframeRenderingSettings"/>.
        /// </summary>
        /// <returns>If user configuration is allowed.</returns>
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetWireframeInformationGenerate, true)]
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathSetWireframeInformationDoNotGenerate, true)]
        [MenuItem(WireframeRenderingSettingsUtil.MenuPathClearWireframeInformation, true)]
        private static bool IsUserConfigurationAllowed()
        {
            return WireframeRenderingSettings.Settings.AllowUserConfigurationOfWireframeInfoGeneration;
        }
    }
}
