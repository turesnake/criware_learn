/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CriWare {
	public class EditorCriLipsDeformer : UnityEditor.Editor {
		protected void OnInspectorGUICriLipsDeformer() {
			var lipsMorphProp = serializedObject.FindProperty("lipsMorph");
			var types = CriEditorUtilitiesInternal.GetSubclassesOf(typeof(ICriLipsMorph));
			var index = types.Select(t => string.Format("{0} {1}", t.Assembly.ToString().Split(',')[0], t.FullName)).ToList().IndexOf(lipsMorphProp.managedReferenceFullTypename);
			var newindex = EditorGUILayout.Popup("Morph Target", index, types.Select(t => t.Name).ToArray());
			if (newindex != index)
			{
				index = newindex;
				lipsMorphProp.managedReferenceValue = Activator.CreateInstance(types.ToList()[index]);
			}
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(lipsMorphProp, new GUIContent(lipsMorphProp.managedReferenceFullTypename.Split('.').Last()), true);
			EditorGUI.indentLevel--;
			OnInspectorGUICriLipsMouthDeformer();
		}
		
		protected void OnInspectorGUICriLipsMouthDeformer() {
			var lipsMorphProp = serializedObject.FindProperty("lipsMouthMorph");
			var types = CriEditorUtilitiesInternal.GetSubclassesOf(typeof(ICriLipsMorph));
			var index = types.Select(t => string.Format("{0} {1}", t.Assembly.ToString().Split(',')[0], t.FullName)).ToList().IndexOf(lipsMorphProp.managedReferenceFullTypename);
			var newindex = EditorGUILayout.Popup("Morph Mouth Target", index, types.Select(t => t.Name).ToArray());
			if (newindex != index)
			{
				index = newindex;
				lipsMorphProp.managedReferenceValue = Activator.CreateInstance(types.ToList()[index]);
			}
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(lipsMorphProp, new GUIContent(lipsMorphProp.managedReferenceFullTypename.Split('.').Last()), true);
			EditorGUI.indentLevel--;
		}
	}

} //namespace CriWare
#endif