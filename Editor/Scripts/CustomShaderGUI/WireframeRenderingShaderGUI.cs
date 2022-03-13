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
            FindAndShowProperty("_EdgeColor", materialEditor, properties);

            EditorGUILayout.Space();

            FindAndShowProperty("_WireframeClip", materialEditor, properties, out float wireframeClipValue);
            if(wireframeClipValue > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_WireframeCutoff");
            }

            EditorGUILayout.Space();

            FindAndShowProperty("_WireframeDash", materialEditor, properties, out float wireframeDashValue);
            if (wireframeDashValue > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_DashLength", "_EmptyLength");
            }

            EditorGUILayout.Space();

            FindAndShowProperty("_ApplyTexture", materialEditor, properties, out float wireframeTextureValue);
            if(wireframeTextureValue >= 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_WireframeTex", "_TexLength");
            }

            EditorGUILayout.Space();

            FindAndShowProperty("_Cull", materialEditor, properties);

            EditorGUILayout.Space();

            FindAndShowProperty("_WorldSpaceReference", materialEditor, properties);

            EditorGUILayout.Space();

            FindAndShowProperty("_BehindDepthFade", materialEditor, properties, out float depthFadeValue);
            if(depthFadeValue > 0.5)
            {
                ShowPropertiesIndented(materialEditor, properties, "_DepthFadeDistance");
            }

            EditorGUILayout.Space();

            FindAndShowProperty("_WireframeFresnelEffect", materialEditor, properties);
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

        private static void FindAndShowProperty(string propertyName, MaterialEditor materialEditor, MaterialProperty[] properties, out float value)
        {
            MaterialProperty property = FindProperty(propertyName, properties);
            materialEditor.ShaderProperty(property, property.displayName);
            value = property.floatValue;
        }
    }
}
