/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace CriWare {
	static class EditorCriLipsMorphAnimatorImplement
	{
		private static List<string> GetAnimatorControllerLayerStateNames(Animator animator)
		{
			if (animator == null || animator.runtimeAnimatorController == null) {
				return null;
			}

			UnityEditor.Animations.AnimatorController animatorController = null;
			animatorController = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
			if (animatorController == null) {
				var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
				animatorController = overrideController?.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
				if (animatorController == null) {
					return null;
				}
			}
			List<string> layerStateNameList = new List<string>();
			foreach (var layer in animatorController.layers) {
				foreach (var childAnimatorState in layer.stateMachine.states) {
					layerStateNameList.Add(layer.name + "." + childAnimatorState.state.name);
				}
			}
			return layerStateNameList;
		}

		public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, string[] indexFields)
		{
			position.height = EditorGUIUtility.singleLineHeight;

			var targetProp = property.FindPropertyRelative(CriEditorUtilitiesInternal.BackingFieldNameOf("target"));
			EditorGUI.PropertyField(position, targetProp, new GUIContent("Target"));
			position.y += CriEditorUtilitiesInternal.SingleReturnHeight;

			foreach (var name in indexFields)
			{
				var prop = property.FindPropertyRelative(name);
				if (targetProp.objectReferenceValue != null) {
					var animator = targetProp.objectReferenceValue as Animator;
					var layerStateNameList = GetAnimatorControllerLayerStateNames(animator);
					if (layerStateNameList != null)
					{
						EditorGUI.IntPopup(
							position, prop,
							Enumerable.Range(0, layerStateNameList.Count).Select(i =>
								new GUIContent(layerStateNameList[i])
							).Prepend<GUIContent>(
								new GUIContent("　") //empty popup
							).ToArray(),
							Enumerable.Range(0, layerStateNameList.Count).Select(i =>
							   Animator.StringToHash(layerStateNameList[i])
							).Prepend<int>(
								0
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

	[CustomPropertyDrawer(typeof(CriLipsMorphAnimatorWidthHeight))]
	public class EditorCriLipsMorphAnimatorWidthHeight : PropertyDrawer
	{
		static readonly string[] indexFields = {
			"lipHeightStateHash",
			"lipWidthOpenStateHash",
			"lipWidthCloseStateHash",
			"tongueUpStateHash"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphAnimatorImplement.OnGUI(position, property, label, indexFields);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphAnimatorImplement.GetPropertyHeight(property, label, indexFields);
	}

	[CustomPropertyDrawer(typeof(CriLipsMorphAnimatorJapaneseVowel))]
	public class EditorCriLipsMorphAnimatorJapaneseVowel : PropertyDrawer
	{
		static readonly string[] indexFields = {
			"aStateHash",
			"iStateHash",
			"uStateHash",
			"eStateHash",
			"oStateHash"
		};

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphAnimatorImplement.OnGUI(position, property, label, indexFields);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
			EditorCriLipsMorphAnimatorImplement.GetPropertyHeight(property, label, indexFields);
	}
} //namespace CriWare
#endif