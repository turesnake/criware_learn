/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using UnityEngine;

/**
 * \addtogroup CRILIPS_UNITY_INTEGRATION
 * @{
 */

namespace CriWare {
	internal static class CriLipsMorphBlendShapeImplement
	{
		public static void SetBlendShapeWeightSafety(this SkinnedMeshRenderer skinnedMeshRenderer, int index, float weight)
		{
			if (index < 0 || skinnedMeshRenderer == null) {
				return;
			}
			skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
		}
	}

	/**
	 * <summary>This is the class used for morphing with blend shapes (using mouth shape information)</summary>
	 * <remarks>
	 * <para header='Description'>This class inherits from ICriLipsMorph and can be used for morphing with UnityEngine.SkinnedMeshRenderer, by using mouth shape information. <br/>
	 * Please register with a component that inherits from CriLipsDeformer, such as CriLipsDeformerForAtomSource . <br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsDeformer::LipsMorph'/>
	*/
	[System.Serializable]
	public class CriLipsMorphBlendShapeWidthHeight : ICriLipsMorph
	{
		[field: SerializeField]
		private SkinnedMeshRenderer target { get; set; }

		/**
		 * <summary>Morphing target object</summary>
		 * <remarks>
		 * <para header='Description'>Sets a SkinnedMeshRenderer as the morphing target.<br/></para>
		 * </remarks>
		*/
		public SkinnedMeshRenderer Target {
			get { return this.target; }
			set {
				this.target = value;
			}
		}

		/**
		 * <summary>Index to which the mouth height parameter is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the mouth height parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipHeightIndex;

		/**
		 * <summary>Index to which the mouth width parameter (in the opening direction) is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the mouth width (in the opening direction) parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipWidthOpenIndex;

		/**
		 * <summary>Index to which the mouth width parameter (in the closing direction) is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the mouth width (in the closing direction) parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipWidthCloseIndex;

		/**
		 * <summary>Index to which the tongue height parameter is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the tongue height parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int tongueUpIndex;

		public CriLipsMouth.Info SilenceInfo { get; set; }

		public void Update(ref CriLipsMouth.Info info, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount)
		{
			float lipWidthOpen = 0.0f;
			float lipWidthClose = 0.0f;
			if (info.lipWidth > this.SilenceInfo.lipWidth) {
				lipWidthOpen = (info.lipWidth - this.SilenceInfo.lipWidth) / (1.0f - this.SilenceInfo.lipWidth);
			} else {
				lipWidthClose = (this.SilenceInfo.lipWidth - info.lipWidth) / (this.SilenceInfo.lipWidth);
			}

			Target.SetBlendShapeWeightSafety(this.lipHeightIndex, info.lipHeight * 100.0f);
			Target.SetBlendShapeWeightSafety(this.lipWidthOpenIndex, lipWidthOpen * 100.0f);
			Target.SetBlendShapeWeightSafety(this.lipWidthCloseIndex, lipWidthClose * 100.0f);
			Target.SetBlendShapeWeightSafety(this.tongueUpIndex, info.tonguePosition * 100.0f);
		}

		public void Reset()
		{
			Target.SetBlendShapeWeightSafety(this.lipHeightIndex, SilenceInfo.lipHeight * 100.0f);
			Target.SetBlendShapeWeightSafety(this.lipWidthOpenIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(this.lipWidthCloseIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(this.tongueUpIndex, SilenceInfo.tonguePosition * 100.0f);
		}
	}

	/**
	 * <summary>This is the class used for morphing with blend shapes (using blend amounts of the Japanese 5-vowels morph targets)</summary>
	 * <remarks>
	 * <para header='Description'>This class inherits from ICriLipsMorph and can be used for morphing with UnityEngine.SkinnedMeshRenderer, by using the Japanese 5-vowels morph target blend amounts. <br/>
	 * Please register with a component that inherits from CriLipsDeformer, such as CriLipsDeformerForAtomSource . <br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsDeformer::LipsMorph'/>
	*/
	[System.Serializable]
	public class CriLipsMorphBlendShapeJapaneseVowel : ICriLipsMorph
	{
		[field: SerializeField]
		private SkinnedMeshRenderer target { get; set; }

		/**
		 * <summary>Morphing target object</summary>
		 * <remarks>
		 * <para header='Description'>Sets a SkinnedMeshRenderer as the morphing target.<br/></para>
		 * </remarks>
		*/
		public SkinnedMeshRenderer Target {
			get { return this.target; }
			set {
				this.target = value;
			}
		}

		/**
		 * <summary>Index to which the blend amount of the Japanese vowel "A" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int aIndex;

		/**
		 * <summary>Index to which the blend amount of the Japanese vowel "I" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int iIndex;

		/**
		 * <summary>Index to which the blend amount of the Japanese vowel "U" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int uIndex;

		/**
		 * <summary>Index to which the blend amount of the Japanese vowel "E" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int eIndex;

		/**
		 * <summary>Index to which the blend amount of the Japanese vowel "O" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the index to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * This index is returned by SkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int oIndex;

		public CriLipsMouth.Info SilenceInfo { get; set; }

		public void Update(ref CriLipsMouth.Info info, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount)
		{
			Target.SetBlendShapeWeightSafety(aIndex, blendAmount.a * 100);
			Target.SetBlendShapeWeightSafety(iIndex, blendAmount.i * 100);
			Target.SetBlendShapeWeightSafety(uIndex, blendAmount.u * 100);
			Target.SetBlendShapeWeightSafety(eIndex, blendAmount.e * 100);
			Target.SetBlendShapeWeightSafety(oIndex, blendAmount.o * 100);
		}

		public void Reset()
		{
			Target.SetBlendShapeWeightSafety(aIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(iIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(uIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(eIndex, 0.0f);
			Target.SetBlendShapeWeightSafety(oIndex, 0.0f);
		}
	}
} //namespace CriWare

/**
 * @}
 */

#endif