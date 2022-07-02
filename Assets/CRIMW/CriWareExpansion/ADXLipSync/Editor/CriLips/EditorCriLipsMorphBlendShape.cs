/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace CriWare {
	static class EditorCriLipsMorphBlendShapeImplement
	{
		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, string[] indexFields)
		{
			position.height = EditorGUIUtility.singleLineHeight;

			var targetProp = property.FindPropertyRelative(CriEditorUtilitiesInternal.BackingFieldNameOf("target"));
			EditorGUI.PropertyField(position, targetProp, new GUIContent("Target"));
			position.y += CriEditorUtilitiesInternal.SingleReturnHeight;

			foreach (var name in indexFields)
			{
				var prop = property.FindPropertyRelative(name);

				if (targetProp.objectReferenceValue != null)
				{
					var mesh = (targetProp.objectReferenceValue as SkinnedMeshRenderer).sharedMesh;
					if (mesh != null)
					{
						EditorGUI.IntPopup(
							position, prop,
							Enumerable.Range(0, mesh.blendShapeCount).Select( i =>
								new GUIContent(mesh.GetBlendShapeName(i))
							).Prepend<GUIContent>(
								new GUIContent("　") //empty popup
							).ToArray(),
							Enumerable.Range(0, mesh.blendShapeCount)
							.Prepend<int>(
								-1
							).ToArray());
						position.y += CriEditorUtilitiesInternal.SingleReturnHeight;
						continue;
					}
				}

				EditorGUI.PropertyField(position, prop);
				position.y += CriEditorUtilitiesInternal.SingleReturnHeight;
			}
		}

		public static float GetPropertyHeight(SerializedProperty property, GUIContent label, string[] indexFields) =>
			CriEditorUtilitiesInternal.SingleReturnHeight * (1 + indexFields.Length);
	}

	[CustomPropertyDrawer(typeof(CriLipsMorphBlendShapeWidthHeight))]
	public class EditorCriLipsMorphBlendShapeWidthHeight : PropertyDrawer
	{
		static readonly string[] indexFields = {
			"lipHeightIndex",
			"lipWidthOpenIndex",
			"lipWidthCloseIndex",
			"tongueUpIndex"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphBlendShapeImplement.OnGUI(position, property, label, indexFields);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphBlendShapeImplement.GetPropertyHeight(property, label, indexFields);
	}

	[CustomPropertyDrawer(typeof(CriLipsMorphBlendShapeJapaneseVowel))]
	public class EditorCriLipsMorphBlendShapeJapaneseVowel : PropertyDrawer
	{
		static readonly string[] indexFields = {
			"aIndex",
			"iIndex",
			"uIndex",
			"eIndex",
			"oIndex"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphBlendShapeImplement.OnGUI(position, property, label, indexFields);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphBlendShapeImplement.GetPropertyHeight(property, label, indexFields);
	}
} //namespace CriWare
#endif