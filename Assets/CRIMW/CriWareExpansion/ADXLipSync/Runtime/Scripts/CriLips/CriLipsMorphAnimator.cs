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
	internal static class CriLipsMorphAnimatorImplement
	{
		public static void PlaySafety(this Animator animator, int stateNameHash, int layer, float normalizedTime)
		{
			if (stateNameHash == 0 || animator == null) {
				return;
			}
			animator.Play(stateNameHash, layer , normalizedTime);
		}
	}

	/**
	 * <summary>Class for morphing with Animator (mouth shape information)</summary>
	 * <remarks>
	 * <para header='Description'>This class inherits ICriLipsMorph and can be used for morphing with UnityEngine.Animator, by using mouth shape information. <br/>
	 * Please register with a component that inherits from CriLipsDeformer, such as CriLipsDeformerForAtomSource . <br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsDeformer::LipsMorph'/>
	*/
	[System.Serializable]
	public class CriLipsMorphAnimatorWidthHeight : ICriLipsMorph
	{
		[field: SerializeField]
		private Animator target { get; set; }

		/**
		 * <summary>Morphing target object</summary>
		 * <remarks>
		 * <para header='Description'>Sets an Animator as the morphing target. <br/>
		 * The Animator set by this property must have an AnimationController with layers that are set to \"Additive\". <br/>
		 * For the recommended data configuration of an AnimationController, please refer to "Control method by Animator" in the manual. <br/></para>
		 * </remarks>
		*/
		public Animator Target {
			get { return this.target; }
			set {
				this.target = value;
			}
		}

		/**
		 * <summary>Hash value to which the mouth height parameter is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the mouth height parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipHeightStateHash;

		/**
		 * <summary>Hash value to which the mouth width parameter (in the opening direction) is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the mouth width (in the opening direction) parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipWidthOpenStateHash;

		/**
		 * <summary>Hash value to which the mouth width parameter (in the closing direction) is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the mouth width (in the closing direction) parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int lipWidthCloseStateHash;

		/**
		 * <summary>Hash value to which the tongue height parameter is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the tongue height parameter of the mouth shape information (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int tongueUpStateHash;

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

			Target.PlaySafety(lipHeightStateHash, -1, Mathf.Max(0.001f, Mathf.Min(lipWidthOpen, 1.0f)));
			Target.PlaySafety(lipWidthOpenStateHash, -1, Mathf.Max(0.001f, Mathf.Min(lipWidthClose, 1.0f)));
			Target.PlaySafety(lipWidthCloseStateHash, -1, Mathf.Max(0.001f, Mathf.Min(info.lipHeight, 1.0f)));
			Target.PlaySafety(tongueUpStateHash, -1, Mathf.Max(0.001f, Mathf.Min(info.tonguePosition, 1.0f)));
		}

		public void Reset()
		{
		}
	}

	/**
	 * <summary>Class for morphing with Animator (Japanese 5 vowel morph target blend amounts)</summary>
	 * <remarks>
	 * <para header='Description'>This class inherits from ICriLipsMorph and can be used for morphing with UnityEngine.Animator, by using the Japanese 5-vowels morph target blend amounts. <br/>
	 * Please register with a component that inherits from CriLipsDeformer, such as CriLipsDeformerForAtomSource . <br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsDeformer::LipsMorph'/>
	*/
	[System.Serializable]
	public class CriLipsMorphAnimatorJapaneseVowel : ICriLipsMorph
	{
		[field: SerializeField]
		private Animator target { get; set; }

		/**
		 * <summary>Morphing target object</summary>
		 * <remarks>
		 * <para header='Description'>Sets an Animator as the morphing target. <br/>
		 * The Animator set by this property must have an AnimationController with layers that are set to \"Additive\". <br/>
		 * For the recommended data configuration of an AnimationController, please refer to "Control method by Animator" in the manual. <br/></para>
		 * </remarks>
		*/
		public Animator Target {
			get { return this.target; }
			set {
				this.target = value;
			}
		}

		/**
		 * <summary>Hash value to which the blend amount of the Japanese vowel "A" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int aStateHash;

		/**
		 * <summary>Hash value to which the blend amount of the Japanese vowel "I" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int iStateHash;

		/**
		 * <summary>Hash value to which the blend amount of the Japanese vowel "U" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int uStateHash;

		/**
		 * <summary>Hash value to which the blend amount of the Japanese vowel "E" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int eStateHash;

		/**
		 * <summary>Hash value to which the blend amount of the Japanese vowel "O" is applied</summary>
		 * <remarks>
		 * <para header='Description'>Specifies the hash value to which the Japanese 5 vowel morph target blend amounts (updated by ICriLipsMorph.Update) is applied. <br/>
		 * The hash value is the LayerName.StateName string in the AnimationController, once converted by calling UnityEngine.Animator.StringToHash() . <br/></para>
		 * </remarks>
		*/
		[SerializeField]
		public int oStateHash;

		public CriLipsMouth.Info SilenceInfo { get; set; }

		public void Update(ref CriLipsMouth.Info info, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount)
		{
			Target.PlaySafety(aStateHash, -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.a, 1.0f)));
			Target.PlaySafety(iStateHash, -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.i, 1.0f)));
			Target.PlaySafety(uStateHash, -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.u, 1.0f)));
			Target.PlaySafety(eStateHash, -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.e, 1.0f)));
			Target.PlaySafety(oStateHash, -1, Mathf.Max(0.001f, Mathf.Min(blendAmount.o, 1.0f)));
		}

		public void Reset()
		{
		}
	}
} //namespace CriWare

/**
 * @}
 */

#endif