// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using UnityEditor;

namespace PixelinearAccelerator.WireframeRendering.Editor.CustomShaderGUI
{
    /// <summary>
    /// <see cref="ShaderGUI"/> to be used with wireframe rendering shader.
    /// </summary>
    public class WireframeRenderingShaderGUI : ShaderGUI
    {
        /// <inheritdoc/>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            materialEditor.SetDefaultGUIWidths();

            FindAndShowProperty("_Width", materialEditor, properties);
            FindAndShowProperty("_FalloffWidth", materialEditor, properties);
            FindAndShowProperty("_Overshoot", materialEditor, properties, out float? overshootValue);
            if(overshootValue.HasValue && overshootValue > 0.5)
            {
                FindAndShowProperty("_OvershootLength", materialEditor, properties);
            }

            FindAndShowProperty("_WorldSpaceReference", materialEditor, properties, out float? worldSpaceReferenceValue);
            if (worldSpaceReferenceValue.HasValue && worldSpaceReferenceValue.Value > 0.5)
            {
                FindAndShowProperty("_UseObjectNormals", materialEditor, properties);
            }
            FindAndShowProperty("_WireframeFresnelEffect", materialEditor, properties);

            FindAndShowProperty("_EdgeColor", materialEditor, properties);

            FindAndShowProperty("_WireframeDash", materialEditor, properties, out float? wireframeDashValue);
            if (wireframeDashValue.HasValue && wireframeDashValue.Value > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_DashLength", "_EmptyLength");
            }

            FindAndShowProperty("_ApplyTexture", materialEditor, properties, out float? wireframeTextureValue);
            if(wireframeTextureValue.HasValue && wireframeTextureValue.Value >= 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_WireframeTex", "_TexLength");
            }

            FindAndShowProperty("_BehindDepthFade", materialEditor, properties, out float? depthFadeValue);
            if (depthFadeValue.HasValue && depthFadeValue.Value > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_DepthFadeDistance");
            }

            FindAndShowProperty("_Wireframe_Contour_Edges", materialEditor, properties);

            FindAndShowProperty("_Cull", materialEditor, properties);
            FindAndShowProperty("_WireframeClip", materialEditor, properties, out float? wireframeClipValue);
            if (wireframeClipValue.HasValue && wireframeClipValue.Value > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_WireframeCutoff");
            }
            FindAndShowProperty("_Wireframe_Depth", materialEditor, properties);

            bool hasAdvancedProperties = HasAnyOfProperties(materialEditor, properties, "_InFrontDepthCutoff", "_NdcMinMaxCutoffForWidthAndAlpha");
            if (hasAdvancedProperties)
            {
                EditorGUILayout.Space();
                _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true, EditorStyles.foldoutHeader);
                if (_showAdvanced)
                {
                    EditorGUI.indentLevel++;
                    FindAndShowProperty("_InFrontDepthCutoff", materialEditor, properties);
                    FindAndShowProperty("_NdcMinMaxCutoffForWidthAndAlpha", materialEditor, properties);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private static void ShowPropertiesIndented(MaterialEditor materialEditor, MaterialProperty[] properties, params string[] propertyNames)
        {
            EditorGUI.indentLevel++;
            foreach(string propertyName in propertyNames)
            {
                FindAndShowProperty(propertyName, materialEditor, properties);
            }
            EditorGUI.indentLevel--;
        }

        private static void FindAndShowProperty(string propertyName, MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindAndShowProperty(propertyName, materialEditor, properties, out _);
        }

        private static void FindAndShowProperty(string propertyName, MaterialEditor materialEditor, MaterialProperty[] properties, out float? value)
        {
            MaterialProperty property = FindProperty(propertyName, properties, false);
            if (property != null)
            {
                materialEditor.ShaderProperty(property, property.displayName);
                value = property.floatValue;
            }
            else
            {
                value = null;
            }
        }

        private static bool HasAnyOfProperties(MaterialEditor materialEditor, MaterialProperty[] properties, params string[] propertyNames)
        {
            bool hasAny = false;
            foreach(string propertyName in propertyNames)
            {
                if(FindProperty(propertyName, properties, false) != null)
                {
                    hasAny = true;
                    break;
                }
            }
            return hasAny;
        }

        private bool _showAdvanced = false;
    }
}
