/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using UnityEditor;

namespace CriWare {

	[CustomEditor(typeof(CriLipsDeformerForAtomSource))]
	public class EditorCriLipsDeformerForAtomSource : EditorCriLipsDeformer {
		public CriLipsDeformerForAtomSource criLipsShapeForAtomSource;
		private SerializedProperty m_atomSource = null;

		void OnEnable() {
			if (target != null) {
				criLipsShapeForAtomSource = target as CriLipsDeformerForAtomSource;
				m_atomSource = serializedObject.FindProperty("source");
			}
		}
		public override void OnInspectorGUI() {
			if (criLipsShapeForAtomSource == null) {
				return;
			}

			Undo.RecordObject(target, "Update CriLipsShapeForAtomSource");
			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.ObjectField(this.m_atomSource, typeof(CriAtomSource), new UnityEngine.GUIContent("CriAtomSource"));

			base.OnInspectorGUICriLipsDeformer();

			criLipsShapeForAtomSource.silenceThreshold
				= EditorGUILayout.Slider("SilenceThreshold(dB)", criLipsShapeForAtomSource.silenceThreshold, -96, 0);

			criLipsShapeForAtomSource.samplingRate
				= EditorGUILayout.IntField("SamplingRate(Hz)", criLipsShapeForAtomSource.samplingRate);

			serializedObject.ApplyModifiedProperties();
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(criLipsShapeForAtomSource);
			}
		}
	}

} //namespace CriWare
#endif