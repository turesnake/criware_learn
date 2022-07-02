/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
#define CRI_ENABLE_LIPS_DEFORMER
#endif
using UnityEngine;

#if CRI_ENABLE_LIPS_DEFORMER
#pragma warning disable 0618 // obsoleted for CriLipsShape
#endif

namespace CriWare {

#if CRI_ENABLE_LIPS_DEFORMER
[System.Obsolete("Use ICriLipsMorph")]
#endif
public class CriLipsMeshMorph {
	#region Enumlators
	public enum BlendShapeNameMappingIndex {
		WidthOpen,
		HeightOpen,
		TonguePosition,
		WidthClose,
		A,
		I,
		U,
		E,
		O,
		MAXNUM
	}
	#endregion

	[System.Serializable]
	public struct BlendShapeNameMapping {
		public string lipWidthOpenName;
		public string lipHeightOpenName;
		public string tonguePosition;
		public string lipWidthCloseName;
		public string a;
		public string i;
		public string u;
		public string e;
		public string o;
	}

	#region Internal Variables
	private CriLipsShape.MorphingTargetType morphingTargetType;
	public SkinnedMeshRenderer skinnedMeshRenderer;
	public UnityEngine.Animator animator;
	private int[] nameMappingGetBlendShapeIndexs = new int[(int)BlendShapeNameMappingIndex.MAXNUM];
	private float silenceWidthPosition = 0.0f;
	#endregion

	#region Functions
	public CriLipsMeshMorph(SkinnedMeshRenderer skinnedMeshRenderer, BlendShapeNameMapping nameMapping, float silenceWidthPosition) {
		this.morphingTargetType = CriLipsShape.MorphingTargetType.BlendShape;
		this.skinnedMeshRenderer = skinnedMeshRenderer;
		this.silenceWidthPosition = silenceWidthPosition;

		var nameMappingArray = BlendShapeNameMappingToArray(nameMapping);
		for (int i = 0; i < (int)BlendShapeNameMappingIndex.MAXNUM; i++) {
			this.nameMappingGetBlendShapeIndexs[i] = this.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(nameMappingArray[i]);
		}
	}

	public CriLipsMeshMorph(Animator animator, BlendShapeNameMapping nameMapping, float silenceWidthPosition) {
		this.morphingTargetType = CriLipsShape.MorphingTargetType.Animation;
		this.animator = animator;
		this.silenceWidthPosition = silenceWidthPosition;

		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthOpen] = Animator.StringToHash(nameMapping.lipWidthOpenName);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.HeightOpen] = Animator.StringToHash(nameMapping.lipHeightOpenName);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.TonguePosition] = Animator.StringToHash(nameMapping.tonguePosition);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthClose] = Animator.StringToHash(nameMapping.lipWidthCloseName);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.A] = Animator.StringToHash(nameMapping.a);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.I] = Animator.StringToHash(nameMapping.i);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.U] = Animator.StringToHash(nameMapping.u);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.E] = Animator.StringToHash(nameMapping.e);
		this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.O] = Animator.StringToHash(nameMapping.o);
	}

	public void Update(ref CriLipsMouth.Info info) {
		float lipWidthOpen = 0.0f;
		float lipWidthClose = 0.0f;

		if (info.lipWidth> this.silenceWidthPosition) {
			lipWidthOpen = (info.lipWidth - this.silenceWidthPosition) / (1.0f - this.silenceWidthPosition);
		} else {
			lipWidthClose = (this.silenceWidthPosition - info.lipWidth) / (this.silenceWidthPosition);
		}

		switch (this.morphingTargetType) {
			case CriLipsShape.MorphingTargetType.BlendShape:
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthOpen], lipWidthOpen * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthClose], lipWidthClose * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.HeightOpen], info.lipHeight * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.TonguePosition], info.tonguePosition * 100.0f);
				break;
			case CriLipsShape.MorphingTargetType.Animation:
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthOpen], -1, Mathf.Max(0.001f, Mathf.Min(lipWidthOpen, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.WidthClose], -1, Mathf.Max(0.001f, Mathf.Min(lipWidthClose, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.HeightOpen], -1, Mathf.Max(0.001f, Mathf.Min(info.lipHeight, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.TonguePosition], -1, Mathf.Max(0.001f, Mathf.Min(info.tonguePosition, 1.0f)));
				break;
			default:
				break;
		}
	}

	public void Update(ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount) {
		switch (this.morphingTargetType) {
			case CriLipsShape.MorphingTargetType.BlendShape:
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.A], blendAmount.a * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.I], blendAmount.i * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.U], blendAmount.u * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.E], blendAmount.e * 100.0f);
				BlendShapeWeightIndex(this.skinnedMeshRenderer, this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.O], blendAmount.o * 100.0f);
				break;
			case CriLipsShape.MorphingTargetType.Animation:
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.A], -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.a, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.I], -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.i, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.U], -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.u, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.E], -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.e, 1.0f)));
				animator.Play(this.nameMappingGetBlendShapeIndexs[(int)BlendShapeNameMappingIndex.O], -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.o, 1.0f)));
				break;
			default:
				break;
		}
	}

	private void BlendShapeWeightIndex(SkinnedMeshRenderer skinnedMeshRenderer, int index, float weight) {
		if (index < 0 || skinnedMeshRenderer == null) {
			return;
		}
		skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
	}

	public static string[] BlendShapeNameMappingToArray(BlendShapeNameMapping nameMapping) {
		string[] blendNameMappingArray = new string[(int)BlendShapeNameMappingIndex.MAXNUM];

		blendNameMappingArray[(int)BlendShapeNameMappingIndex.WidthOpen] = nameMapping.lipWidthOpenName;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.HeightOpen] = nameMapping.lipHeightOpenName;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.TonguePosition] = nameMapping.tonguePosition;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.WidthClose] = nameMapping.lipWidthCloseName;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.A] = nameMapping.a;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.I] = nameMapping.i;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.U] = nameMapping.u;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.E] = nameMapping.e;
		blendNameMappingArray[(int)BlendShapeNameMappingIndex.O] = nameMapping.o;

		return blendNameMappingArray;
	}

	public static bool SetBlendShapeNameMappingArrayToStruct(string[] nameMappingArray,ref BlendShapeNameMapping nameMapping) {
		if (nameMappingArray == null || nameMappingArray.Length != (int)BlendShapeNameMappingIndex.MAXNUM) {
			Debug.LogError("[CRIWARE] nameMappingArray is invalid.");
			return false;
		}

		nameMapping.lipWidthOpenName = nameMappingArray[(int)BlendShapeNameMappingIndex.WidthOpen];
		nameMapping.lipHeightOpenName = nameMappingArray[(int)BlendShapeNameMappingIndex.HeightOpen];
		nameMapping.tonguePosition = nameMappingArray[(int)BlendShapeNameMappingIndex.TonguePosition];
		nameMapping.lipWidthCloseName = nameMappingArray[(int)BlendShapeNameMappingIndex.WidthClose];
		nameMapping.a = nameMappingArray[(int)BlendShapeNameMappingIndex.A];
		nameMapping.i = nameMappingArray[(int)BlendShapeNameMappingIndex.I];
		nameMapping.u = nameMappingArray[(int)BlendShapeNameMappingIndex.U];
		nameMapping.e = nameMappingArray[(int)BlendShapeNameMappingIndex.E];
		nameMapping.o = nameMappingArray[(int)BlendShapeNameMappingIndex.O];

		return true;
	}
	#endregion
}

} //namespace CriWare
/* end of file */