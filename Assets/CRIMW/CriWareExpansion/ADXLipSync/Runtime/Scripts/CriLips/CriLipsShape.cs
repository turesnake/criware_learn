/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
#define CRI_ENABLE_LIPS_DEFORMER
#endif

using UnityEngine;

/**
 * \addtogroup CRILIPS_UNITY_COMPONENT
 * @{
 */

namespace CriWare {

/**
 * \deprecated
 * Unity2019.3 〜 では削除予定の非推奨APIです。
 * CriLipsDeformer コンポーネントの使用を検討してください。
 * <summary>A component for sending the LipSync analysis results into the blend shape.</summary>
 * <remarks>
 * <para header='Description'>A component for sending the LipSync analysis results into the blend shape.<br/>
 * A base class that passes the analysis result to the blend shape set on the editor.<br/>
 * By inheriting this class, it is possible to reflect the LipSync analysis result according to the sound input method.<br/>
 * This component only passes the LipSync analysis result to the blend shape, so nothing is displayed when it is attached and used.<br/>
 * In general, use the CriLipsShapeForAtomSource component, which is the inherited component.<br/></para>
 * <para header='Note'>In this class, only the combination of configure blend shapes is shaped.<br/>
 * When controlling the blend shape by combining multiple LipSync analysis results,
 * operate the blend shape directly from the analysis results.<br/></para>
 * </remarks>
 */
#if CRI_ENABLE_LIPS_DEFORMER
[System.Obsolete("Use CriLipsDeformer Component")]
#else
[AddComponentMenu("CRIWARE/CriLipsShape")]
#endif
public class CriLipsShape : CriMonoBehaviour
{
	#region Properties
	public enum MorphingTargetType {
		BlendShape = 0,
		Animation,
	}
	public MorphingTargetType morphingTargetType {
		get {
			return _morphingTargetType;
		}
		set {
			_morphingTargetType = value;
		}
	}

	/**
	 * <summary>The blend shape type to which the LipSync analysis result is applied</summary>
	 */
	public enum BlendShapeType {
		WidthHeight = 0,    /**< Vertical and horizontal type */
		JapaneseAIUEO,      /**< Japanese 5 vowel type */
	}

	public BlendShapeType blendShapeType {
		get {
			return _blendShapeType;
		}
		set {
			_blendShapeType = value;
		}
	}

	/**
	 * <summary>Delegate for change in analysis result</summary>
	 */
	public delegate void UserModifyDelegateFunction(ref CriLipsMouth.Info info, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese morph, ICriLipsAnalyzeModule analyzeModule);

	public UserModifyDelegateFunction UserModifyDelegate = null;

	#endregion

	#region Internal Variables
	[SerializeField]
	private MorphingTargetType _morphingTargetType = MorphingTargetType.BlendShape;
	[SerializeField]
	private BlendShapeType _blendShapeType = CriLipsShape.BlendShapeType.WidthHeight;
	[SerializeField]
	public SkinnedMeshRenderer skinnedMeshRenderer;
	
	/// <summary>
	/// 添加修改，因项目需求需要另一套参数控制
	/// </summary>
	[SerializeField]
	public SkinnedMeshRenderer mouthMeshRenderer;
	protected CriLipsMeshMorph meshMouthMorphing = null;
	[SerializeField]
	public CriLipsMeshMorph.BlendShapeNameMapping nameMouthMapping;
	
	[SerializeField]
	public CriLipsMeshMorph.BlendShapeNameMapping nameMapping;
	[SerializeField]
	public Animator animator;
	[SerializeField]
	public CriLipsMeshMorph.BlendShapeNameMapping animationStateNameMapping;
	protected CriLipsMeshMorph meshMorphing = null;
	protected CriLipsMouth.Info info;
	protected CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount;
	protected ICriLipsAnalyzeModule analyzeModule = null;
	#endregion

	#region Functions

	protected void initialMouthMorphing(CriLipsMouth.Info silenceInfo)
	{
		if (mouthMeshRenderer != null)
		{
			this.meshMouthMorphing = new CriLipsMeshMorph(mouthMeshRenderer, nameMouthMapping, silenceInfo.lipWidth);
		}
	}
	/**
	 * <summary>Generates a morphing class from SkinnedMeshRenderer.</summary>
	 * <remarks>
	 * <para header='Description'>Generates a morphing class from SkinnedMeshRenderer set on the Inspector.<br/></para>
	 * </remarks>
	 */
	protected virtual void StartForMorphing(CriLipsMouth.Info silenceInfo) {
		if (skinnedMeshRenderer == null &&
			animator == null) {
			return;
		}
		if (meshMorphing != null) {
			Debug.LogError("[CRIWARE] There is already existed CriLipsMeshMorph instance.");
			return;
		}
		switch (morphingTargetType) {
			case MorphingTargetType.BlendShape:
				if (skinnedMeshRenderer == null) {
					Debug.LogError("[CRIWARE] skinnedMeshRenderer is not found.");
					return;
				}
				this.meshMorphing = new CriLipsMeshMorph(skinnedMeshRenderer, nameMapping, silenceInfo.lipWidth);
				initialMouthMorphing(silenceInfo);
				break;
			case MorphingTargetType.Animation:
				if (animator == null) {
					Debug.LogError("[CRIWARE] animator is not found.");
					return;
				}
				this.meshMorphing = new CriLipsMeshMorph(animator, animationStateNameMapping, silenceInfo.lipWidth);
				initialMouthMorphing(silenceInfo);
				break;
			default:
				break;
		}
	}

	/**
	 * <summary>Perform vertically/horizontally type of morphing.</summary>
	 * <remarks>
	 * <para header='Description'>Morph the BlendShape vertically and horizontally. <br/>
	 * The BlendShape target is specified when CriLipsShape.StartForMorphing is being called.<br/></para>
	 * </remarks>
	 */
	protected virtual void UpdateLipsParamerterForBelndShape(ref CriLipsMouth.Info info) {
		if (meshMorphing == null) {
			return;
		}
		meshMorphing.Update(ref info);
		meshMouthMorphing?.Update(ref info);
	}

	/**
	 * <summary>Use the Japanese 5-vowel type of morphing.</summary>
	 * <remarks>
	 * <para header='Description'>Use the 5 vowels of Japanese to do morphing on the BlendShape.<br/>
	 * The BlendShape target is specified when CriLipsShape.StartForMorphing is being called.<br/></para>
	 * </remarks>
	 */
	protected virtual void UpdateLipsParamerterForBelndShape(ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount) {
		if (meshMorphing == null) {
			return;
		}
		meshMorphing.Update(ref blendAmount);
		meshMouthMorphing?.Update(ref blendAmount);
	}

	protected virtual void UpdateLipsParameter() {
		if (UserModifyDelegate != null && analyzeModule != null) {
			UserModifyDelegate(ref info, ref blendAmount, analyzeModule);
		}

		switch (blendShapeType) {
			case CriLipsShape.BlendShapeType.WidthHeight: {
				UpdateLipsParamerterForBelndShape(ref info);
			}
			break;
			case CriLipsShape.BlendShapeType.JapaneseAIUEO: {
				UpdateLipsParamerterForBelndShape(ref blendAmount);
			}
			break;
			default:
				break;
		}
	}

	public override void CriInternalUpdate() { }
	public override void CriInternalLateUpdate() { }

	#endregion
}

} //namespace CriWare
/**
 * @}
 */

/* end of file */
