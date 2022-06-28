// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: © 2022 Tyler Dodds

using PixelinearAccelerator.WireframeRendering.Runtime.Layer;
using UnityEditor;
using UnityEngine;

namespace PixelinearAccelerator.WireframeRendering.Editor.Layer
{
    /// <summary>
    /// <see cref="PropertyDrawer"/> for <see cref="SingleLayer"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(SingleLayer))]
    public class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty layerProp = property.FindPropertyRelative("m_layer");
            layerProp.intValue = EditorGUI.LayerField(position, label, layerProp.intValue);
        }
    }
}
