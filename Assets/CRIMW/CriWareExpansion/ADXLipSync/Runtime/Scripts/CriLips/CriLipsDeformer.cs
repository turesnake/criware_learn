/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using UnityEngine;

/**
 * \addtogroup CRILIPS_UNITY_COMPONENT
 * @{
 */

namespace CriWare {
	/**
	 * <summary>Class that passes LipSync analysis results to ICriLipsMorph</summary>
	 * <remarks>
	 * <para header='Description'>This is a base class that passes LipSync analysis results to the registered ICriLipsMorph interface. <br/>
	 * By inheriting from this class, it is possible pass the LipSync analysis results based on the sound input method. <br/>
	 * Since the only thing this component does is passing the LipSync analysis results to a class that inherits from ICriLipsMorph, 
	 * nothing will be displayed by simply attaching it. <br/>
	 * In most cases, you can use the CriLipsDeformerForAtomSource component. <br/></para>
	 * </remarks>
	 * <seealso cref='ICriLipsMorph'/>
	*/
	[AddComponentMenu("CRIWARE/CriLipsDeformer")]
	public class CriLipsDeformer : CriMonoBehaviour
	{
		#region Properties

		/**
		 * <summary>Registering the interface for the module using the LipSync analysis results</summary>
		 * <remarks>
		 * <para header='Description'>Registers the interface for the module using the LipSync analysis results. <br/>
		 * If an interface has already been registered, ICriLipsMorph::Reset is called to perform the unregistration process. <br/></para>
		 * </remarks>
		 * <seealso cref='ICriLipsMorph::Reset'/>
		*/
		public ICriLipsMorph LipsMorph {
			get	{ return this.lipsMorph; }
			set {
				if (this.lipsMorph != null) {
					this.lipsMorph.Reset();
				}
				this.lipsMorph = value;
				if (this.lipsMorph != null) {
					this.lipsMorph.SilenceInfo = this.silicenInfo;
				}
			}
		}
		
		public ICriLipsMorph LipsMouthMorph {
			get	{ return this.lipsMouthMorph; }
			set {
				if (this.lipsMouthMorph != null) {
					this.lipsMouthMorph.Reset();
				}
				this.lipsMouthMorph = value;
				if (this.lipsMouthMorph != null) {
					this.lipsMouthMorph.SilenceInfo = this.silicenInfo;
				}
			}
		}

		/**
		 * <summary>Definition of the delegate used to modify the analysis result</summary>
		 */
		public delegate void UserModifyDelegateFunction(
				ref CriLipsMouth.Info info,
				ref CriLipsMouth.MorphTargetBlendAmountAsJapanese morph,
				ICriLipsAnalyzeModule analyzeModule
			);

		/**
		 * <summary>Delegate for change in analysis result</summary>
		 */
		public UserModifyDelegateFunction UserModifyDelegate = null;
		#endregion

		#region Internal Variables
		[field: SerializeReference]
		private ICriLipsMorph lipsMorph = null;
		[field: SerializeReference]
		private ICriLipsMorph lipsMouthMorph = null;
		
		protected CriLipsMouth.Info info;
		protected CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount;
		protected ICriLipsAnalyzeModule analyzeModule = null;
		private CriLipsMouth.Info silicenInfo;
		#endregion

		#region Functions
		/**
		 * <summary>Passes the shape information of a closed mouth to ICriLipsMorph.</summary>
		 * <remarks>
		 * <para header='Description'>This is the closed mouth shape information returned by ICriLipsMorph . <br/>
		 * To express the mouth movement, use the difference between the mouth shape information obtained in real-time <br/>
		 * and the closed mouth information. <br/></para>
		 * </remarks>
		 <seealso cref='CriLipsMouth::GetInfoAtSilence'/>
		*/
		protected virtual void StartForMorphing(CriLipsMouth.Info silenceInfo) {
			if (LipsMorph != null) {
				LipsMorph.SilenceInfo = silenceInfo;
			}
			if (LipsMouthMorph != null) {
				LipsMouthMorph.SilenceInfo = silenceInfo;
			}
			this.silicenInfo = silenceInfo;
		}

		/**
		 * <summary>Passes the LipSync analysis result value to ICriLipsMorph.</summary>
		 * <remarks>
		 * <para header='Description'>Pass the LipSync analysis result value to the registered ICriLipsMorph. <br/>
		 * If CriLipsDeformer::UserModifyDelegate is registered, the LipSync analysis result value is passed to the registered ICriLipsMorph <br/>
		 * after being processed by the analysis result modification delegate. <br/></para>
		 * </remarks>
		 * <seealso cref='CriLipsDeformer::UserModifyDelegate'/>
		 */
		protected virtual void UpdateLipsParameter() {
			if (UserModifyDelegate != null && analyzeModule != null) {
				UserModifyDelegate(ref info, ref blendAmount, analyzeModule);
			}
			if (LipsMorph == null) {
				return;
			}
			LipsMorph.Update(ref info, ref blendAmount);
			if (LipsMouthMorph == null) {
				return;
			}
			LipsMouthMorph.Update(ref info, ref blendAmount);
		}

		public override void CriInternalUpdate() { }
		public override void CriInternalLateUpdate() { }
		#endregion
	}

} //namespace CriWare
/**
 * @}
 */

#endif

/* end of file */
