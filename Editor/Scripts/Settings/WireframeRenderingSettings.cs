// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using PixelinearAccelerator.WireframeRendering.Runtime.RenderFeature;
using PixelinearAccelerator.WireframeRendering.Runtime.Enums;
using PixelinearAccelerator.WireframeRendering.Runtime.Layer;
using System;

namespace PixelinearAccelerator.WireframeRendering.Editor.Settings
{
    /// <summary>
    /// Settings asset for wireframe rendering.
    /// </summary>
    internal class WireframeRenderingSettings : ScriptableObject
    {
        /// <summary>
        /// The path to the custom settings.
        /// </summary>
        private const string k_MyCustomSettingsPath = "Assets/Settings/WireframeRenderingSettings.asset";

        /// <summary>
        /// The type of wireframe rendering to use (after resolving any Default choice).
        /// </summary>
        public WireframeType WireframeTypeToUse => WireframeTypeChosen.ResolveDefault();

        /// <summary>
        /// The user's choice of type of wireframe rendering to use (potentially Default).
        /// </summary>
        public WireframeType WireframeTypeChosen = WireframeType.Default;

        /// <summary>
        /// The directory suffix that signals automatic generation of wireframe information.
        /// </summary>
        [Delayed]
        public string DirectorySuffixForAutomaticGeneration = "_Wireframe";

        /// <summary>
        /// If per-model configuration of wireframe information generation should be enabled.
        /// </summary>
        public bool AllowUserConfigurationOfWireframeInfoGeneration = true;

        /// <summary>
        /// The UV channel used for <see cref="WireframeType.TextureCoordinates"/>.
        /// </summary>
        [Delayed]
        public int UvChannel = 3;

        /// <summary>
        /// The angle cutoff in degrees used for <see cref="WireframeType.TextureCoordinates"/>.
        /// </summary>
        [Delayed]
        public float AngleCutoffDegrees = 45;

        /// <summary>
        /// If importers should not weld vertices for <see cref="WireframeType.TextureCoordinates"/>.
        /// </summary>
        public bool DoNotWeldVertices = true;

        /// <summary>
        /// If wireframe information generation should be shown for project items.
        /// </summary>
        public bool ShowProjectItemInfo = true;

        /// <summary>
        /// The default Layer for wireframe models.
        /// </summary>
        public SingleLayer DefaultLayer = 0;

        /// <summary>
        /// If object normals should be imported when <see cref="WireframeTypeExtensions.DrawSegmentsAsQuads(WireframeType)"/>.
        /// </summary>
        public bool ImportObjectNormals = false;

        /// <summary>
        /// If contour edge information should be imported when <see cref="WireframeTypeExtensions.DrawSegmentsAsQuads(WireframeType)"/>.
        /// </summary>
        public bool ImportContourEdges = false;

        /// <summary>
        /// The default weld distance for mesh importers when <see cref="WireframeTypeExtensions.DrawSegmentsAsQuads(WireframeType)"/>.
        /// </summary>
        public float ImporterDefaultWeldDistance = 0.001f;

        internal static WireframeRenderingSettings Settings
        {
            get
            {
                if(_settings == null)
                {
                    _settings = GetOrCreateSettings();
                }
                return _settings;
            }
        }
        private static WireframeRenderingSettings _settings = null;

        /// <summary>
        /// Gets or creates the serialized <see cref="WireframeRenderingSettings"/>.
        /// </summary>
        /// <returns></returns>
        private static WireframeRenderingSettings GetOrCreateSettings()
        {
            WireframeRenderingSettings settings = AssetDatabase.LoadAssetAtPath<WireframeRenderingSettings>(k_MyCustomSettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<WireframeRenderingSettings>();
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }

    /// <summary>
    /// Utility class for <see cref="WireframeRenderingSettings"/>, including settings and menu paths.
    /// </summary>
    static class WireframeRenderingSettingsUtil
    {
        [MenuItem(MenuPathOpenSettings, priority = -100)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(WireframeRenderingSettingsPath);
        }

        /// <summary>The base Menu path for all Wireframe Rendering items.</summary>
        public const string MenuPathBase = "Tools/Wireframe Rendering/";
        /// <summary>The menu name for Open Settings.</summary>
        public const string MenuNameOpenSettings = "Open Settings";
        /// <summary>The menu path for Open Settings.</summary>
        public const string MenuPathOpenSettings = MenuPathBase + MenuNameOpenSettings;
        /// <summary>The menu name for Clear Wireframe Generation Information.</summary>
        public const string MenuNameClearWireframeInformation = "Clear Wireframe Generation Information For Selected Models";
        /// <summary>The menu path for Clear Wireframe Generation Information.</summary>
        public const string MenuPathClearWireframeInformation = MenuPathBase + MenuNameClearWireframeInformation;
        /// <summary>The menu name for Generate Wireframe Generation Information.</summary>
        public const string MenuNameSetWireframeWireframeInformationGenerate = "Generate Wireframe Information For Selected Models";
        /// <summary>The menu path for Generate Wireframe Generation Information.</summary>
        public const string MenuPathSetWireframeInformationGenerate = MenuPathBase + MenuNameSetWireframeWireframeInformationGenerate;
        /// <summary>The menu name for Do Not Generate Wireframe Generation Information.</summary>
        public const string MenuNameSetWireframeInformationDoNotGenerate = "Do Not Generate Wireframe Information For Selected Models";
        /// <summary>The menu path for Do Not Generate Wireframe Generation Information.</summary>
        public const string MenuPathSetWireframeInformationDoNotGenerate = MenuPathBase + MenuNameSetWireframeInformationDoNotGenerate;
        /// <summary>The menu name for Select Renderer Feature.</summary>
        public const string MenuNameSelectRendererFeature = "Select Renderer Feature";
        /// <summary>The menu path for Select Renderer Feature.</summary>
        public const string MenuPathSelectRendererFeature = MenuPathBase + MenuNameSelectRendererFeature;

        /// <summary>
        /// SettingsProvider path for wireframe rendering settings.
        /// </summary>
        public const string WireframeRenderingSettingsPath = "Project/WireframeRenderingSettings";
    }

    /// <summary>
    /// Class to register <see cref="WireframeRenderingSettings"/> with the IMGUI drawing framework.
    /// </summary>
    static class WireframeRenderingSettingsIMGUIRegister
    {
        /// <summary>
        /// Creates the WireframeRendering <see cref="SettingsProvider"/>.
        /// </summary>
        /// <returns>The <see cref="SettingsProvider"/>.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateWireframeRenderingSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider(WireframeRenderingSettingsUtil.WireframeRenderingSettingsPath, SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "Wireframe Rendering",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    GUIStyle largeLabelStyle = new GUIStyle(EditorStyles.largeLabel)
                    {
                        fontStyle = FontStyle.Bold,
                    };

                    bool reimportModelsPressed = false;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox("Settings changes will only affect newly-imported or reimported models.", MessageType.Info);
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Space(12);
                            reimportModelsPressed = GUILayout.Button("Reimport Models");
                        }
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    WireframeRenderingSettings settings = WireframeRenderingSettings.Settings;
                    SerializedObject serializedSettings = new SerializedObject(settings);
                    EditorGUILayout.LabelField("System Used to Perform Wireframe Rendering", largeLabelStyle);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.WireframeTypeChosen)), new GUIContent("Wireframe Method"));
                    WireframeType prevType = settings.WireframeTypeChosen;
                    bool typeChanged = EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Automated Wireframe Information Generation", largeLabelStyle);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.DirectorySuffixForAutomaticGeneration)), new GUIContent("Directory Suffix", "Directories Ending With This Will Automatically Add Wireframe Information to Imported Models."));
                    bool suffixChanged = EditorGUI.EndChangeCheck();
                    string previousSuffix = settings.DirectorySuffixForAutomaticGeneration;
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(UserConfigurationMessage, MessageType.Info);
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("User Configurable");
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.AllowUserConfigurationOfWireframeInfoGeneration)), GUIContent.none);
                        GUILayout.FlexibleSpace();
                    }
                    bool userConfigurationSettingChanged = EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Wireframe Information Generation Status in Project Window", largeLabelStyle);
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.ShowProjectItemInfo)), new GUIContent("Show"));
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    bool channelChanged, angleCutoffChanged, weldVerticesChanged;
                    int previousChannel = settings.UvChannel;
                    channelChanged = angleCutoffChanged = weldVerticesChanged = false;
                    if (settings.WireframeTypeToUse == WireframeType.TextureCoordinates)
                    {
                        EditorGUILayout.LabelField("Texture Coordinate Generation Details", largeLabelStyle);
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.UvChannel)), new GUIContent($"UV Channel ({MinChannel} to {MaxChannel})", "The UV Channel Where Wireframe Texture Coordinates Will Be Generated"));
                        channelChanged = EditorGUI.EndChangeCheck();
                        if (channelChanged)
                        {
                            serializedSettings.FindProperty(nameof(settings.UvChannel)).intValue = Mathf.Clamp(serializedSettings.FindProperty(nameof(settings.UvChannel)).intValue, MinChannel, MaxChannel);
                        }
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)), new GUIContent($"Sharp Edge Angle (\u00B0)",
                            "Adjacent edges that differ in angle greater less than this value are treated as a group for wireframe texture coordinate generation."));
                        angleCutoffChanged = EditorGUI.EndChangeCheck();
                        if (angleCutoffChanged)
                        {
                            serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)).floatValue = Mathf.Clamp(serializedSettings.FindProperty(nameof(settings.AngleCutoffDegrees)).floatValue, 0f, 90f);
                        }
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.DoNotWeldVertices)), new GUIContent($"Do not Weld Vertices", "Disable Weld Vertices in model import settings for models with Wireframe Texture Coordinates generated."));
                        weldVerticesChanged = EditorGUI.EndChangeCheck();
                    }

                    bool defaultLayerChanged = false;
                    bool importObjectNormalsChanged = false;
                    bool importContourEdgesChanged = false;
                    if (settings.WireframeTypeToUse.DrawSegmentsAsQuads())
                    {
                        EditorGUILayout.LabelField("Line Segment Mesh Generation", largeLabelStyle);
                        EditorGUILayout.LabelField("Segment Generation", EditorStyles.boldLabel);
                        if (settings.WireframeTypeToUse == WireframeType.GeometryShader)
                        {
                            EditorGUILayout.HelpBox("Importing additional segment information will prevent wireframe meshes from sharing vertices.", MessageType.None, true);
                        }
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.ImportObjectNormals)), new GUIContent("Import Normals", "Encodes object-space normals for potential use in aligning segments when rendering in world space."));
                        importObjectNormalsChanged = EditorGUI.EndChangeCheck();
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.ImportContourEdges)), new GUIContent("Import Contour Edges", "Imports all edges of a model and encodes neighbouring face normals, so that contour edges can be rendered."));
                        importContourEdgesChanged = EditorGUI.EndChangeCheck();
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Importer", EditorStyles.boldLabel);
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.DefaultLayer)), new GUIContent("Wireframe Layer", "Generated Prefabs will have Wireframe MeshRenderers set to this Layer."));
                        defaultLayerChanged = EditorGUI.EndChangeCheck();
                        EditorGUILayout.PropertyField(serializedSettings.FindProperty(nameof(settings.ImporterDefaultWeldDistance)), new GUIContent("Default Weld Distance", "Default value for weld distance for new wireframe mesh Importers. Can be modified per Importer."));
                    }


                    serializedSettings.ApplyModifiedProperties();


                    if (typeChanged && prevType != settings.WireframeTypeChosen)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new Wireframe Type behavior?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if (reimportModelsPressed)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Do you want to reimport all models?", DialogOk, DialogCancel))
                        {
                            ReimportAllModels();
                        }
                    }
                    else if(channelChanged && previousChannel != settings.UvChannel)
                    {
                        SetRendererFeatureUvChannel(settings.UvChannel);
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new channel of UV coordinate generation?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(angleCutoffChanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new Angle Cutoff behavior?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(weldVerticesChanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new Weld Vertices behavior?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if (defaultLayerChanged)
                    {
                        SetRendererFeatureWireframeLayer(settings.DefaultLayer);
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new default layer?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if (importObjectNormalsChanged)
                    {
                        SetRendererFeatureWireframeImportObjectNormals(settings.ImportObjectNormals);
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new segment information?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if (importContourEdgesChanged)
                    {
                        SetRendererFeatureWireframeImportContourEdges(settings.ImportContourEdges);
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new segment information?", DialogOk, DialogCancel))
                        {
                            ReimportWireframeModels(settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(suffixChanged && previousSuffix != settings.DirectorySuffixForAutomaticGeneration)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Do you want to reimport all models affected by the directory suffix change?", DialogOk, DialogCancel))
                        {
                            ReimportAllModels(previousSuffix, settings.DirectorySuffixForAutomaticGeneration);
                        }
                    }
                    else if(userConfigurationSettingChanged)
                    {
                        if (EditorUtility.DisplayDialog("Model Reimport", "Should relevant models be reimported to reflect the new method of wireframe generation?", DialogOk, DialogCancel))
                        {
                            ReimportModelsOnUserConfigurationAllowed(settings);
                        }
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>()
            };

            return provider;
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and sets the wireframe layer to be used.
        /// </summary>
        /// <param name="layer">The wireframe layer</param>
        private static void SetRendererFeatureWireframeLayer(SingleLayer layer)
        {
            ForEachRendererFeatureSettings(settingsProperty =>
            {
                SerializedProperty layerMaskProperty = settingsProperty.FindPropertyRelative(nameof(WireframeRenderingFeatureSettings.LayerMask));
                layerMaskProperty.intValue = layer.Mask;
            });
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and sets if object normals are imported.
        /// </summary>
        /// <param name="importObjectNormals">If object normals are imported.</param>
        private static void SetRendererFeatureWireframeImportObjectNormals(bool importObjectNormals)
        {
            ForEachRendererFeatureSettings(settingsProperty =>
            {
                SerializedProperty objectNormalsImportedProperty = settingsProperty.FindPropertyRelative("_objectNormalsImported");
                objectNormalsImportedProperty.boolValue = importObjectNormals;
            });
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and sets if contour edge information is imported.
        /// </summary>
        /// <param name="importContourEdges">If contour edge information is imported.</param>
        private static void SetRendererFeatureWireframeImportContourEdges(bool importContourEdges)
        {
            ForEachRendererFeatureSettings(settingsProperty =>
            {
                SerializedProperty contourEdgesImportedProperty = settingsProperty.FindPropertyRelative("_contourEdgesImported");
                contourEdgesImportedProperty.boolValue = importContourEdges;
            });
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and sets the wireframe uv channel to be used.
        /// </summary>
        /// <param name="uvChannel">The wireframe uv channel to be used.</param>
        private static void SetRendererFeatureUvChannel(int uvChannel)
        {
            ForEachRendererFeatureSettings(settingsProperty =>
            { 
                SerializedProperty channelProperty = settingsProperty.FindPropertyRelative("_uvChannel"); 
                channelProperty.intValue = uvChannel;
            });
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and performs the given action on the feature's Settings <see cref="SerializedProperty"/>.
        /// </summary>
        /// <param name="actionOnWireframeRenderingFeatureSettings">The action to take.</param>
        private static void ForEachRendererFeatureSettings(Action<SerializedProperty> actionOnWireframeRenderingFeatureSettings)
        {
            ForEachRendererFeature(feature =>
            {
                SerializedObject serializedObject = new SerializedObject(feature);
                SerializedProperty settingsProperty = serializedObject.FindProperty(nameof(feature.Settings));
                actionOnWireframeRenderingFeatureSettings(settingsProperty);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        /// <summary>
        /// Finds all <see cref="WireframeRenderingFeature"/> and performs the given action.
        /// </summary>
        /// <param name="action">The action to take.</param>
        private static void ForEachRendererFeature(Action<WireframeRenderingFeature> action)
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(WireframeRenderingFeature)}");
            string[] paths = guids.Select(g => AssetDatabase.GUIDToAssetPath(g)).ToArray();
            foreach (string path in paths)
            {
                WireframeRenderingFeature feature = AssetDatabase.LoadAssetAtPath<WireframeRenderingFeature>(path);
                if (feature != null)
                {
                    action(feature);
                }
            }
        }

        /// <summary>
        /// Reimports relevant models with wireframe info or with appropriate <paramref name="directorySuffix"/>.
        /// </summary>
        private static void ReimportWireframeModels(string directorySuffix)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if (assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    Importer.WireframeInfo wireframeInfo = Importer.WireframeMeshPostprocessor.GetWireframeInfoFromUserData(assetImporter, out bool couldNotParse);
                    if((!couldNotParse && wireframeInfo.ShouldGenerate) || Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, directorySuffix))
                    {
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Reimport all models from user request (those in relevant directories or with wireframe information).
        /// </summary>
        private static void ReimportAllModels(params string[] directorySuffixRestrictions)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if (assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    if(directorySuffixRestrictions.Length == 0 || directorySuffixRestrictions.Any(ds => Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, ds)))
                    {
                        modelImporter.SaveAndReimport();
                    }
                }
            }
        }

        /// <summary>
        /// Reimport models based on changes to setting allowing user configuration of UV generation.
        /// </summary>
        /// <param name="settings">The <see cref="WireframeRenderingSettings"/>.</param>
        private static void ReimportModelsOnUserConfigurationAllowed(WireframeRenderingSettings settings)
        {
            string[] modelGuids = AssetDatabase.FindAssets(ModelSearchFilter);
            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                if(assetImporter != null && assetImporter is ModelImporter modelImporter)
                {
                    if(settings.AllowUserConfigurationOfWireframeInfoGeneration)
                    {
                        //We want to re-import files in the directories that used to have UVS auto-generated, to raise any issues immediately (if assetData field is already being used).
                        if (Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, settings.DirectorySuffixForAutomaticGeneration))
                        {
                            modelImporter.SaveAndReimport();
                        }
                    }
                    else
                    {
                        //In this case, we want to go through *every* ModelImporter with userData, try to parse the WireframeInfo, and clear the field if we could successfully parse.
                        //This will allow other tools to start to use that field.
                        Importer.WireframeMeshPostprocessor.ClearWireframeGenerationInformation(modelImporter, false, out bool couldNotParse);
                        if (couldNotParse)
                        {
                            //There's already information in that field, so we don't clear it, but we still need to reimport the model, since it won't be done above.
                            if (Importer.WireframeMeshPostprocessor.UseDirectoryForWireframe(assetPath, settings.DirectorySuffixForAutomaticGeneration))
                            {
                                modelImporter.SaveAndReimport();
                            }
                        }
                    }
                }
            }
        }

        private const int MinChannel = 0;
        private const int MaxChannel = 7;
        private const string ModelSearchFilter = "t:Model";
        private const string DialogOk = "Yes";
        private const string DialogCancel = "No";
        private const string UserConfigurationMessage = "In addition to automatic generation from directory suffix, user configuration of wireframe information generation from the Tools menu requires use of the AssetImporter userData field.\n" +
                                "If other packages, assets, or code requires this field, you can disable User Configurable to restrict wireframe information generation to only check directory suffix.";
    }
}
