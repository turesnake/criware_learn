/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
/**
 * \addtogroup CRILIPS_UNITY_INTEGRATION
 * @{
 */
namespace CriWare {
	/**
	 * <summary>Interface for the module using the LipSync analysis results</summary>
	 * <remarks>
	 * <para header='Description'>LipSync analysis result module interface used by the CriLipsDeformer component. <br/>
	 * By inheriting this interface and registering it in the CriLipsDeformer component, <br/>
	 * you can implement your own morphing process based on LipSync analysis results. <br/></para>
	 * </remarks>
	 <seealso cref='CriLipsDeformer::LipsMorph'/>
	*/
	public interface ICriLipsMorph {
		/**
		 * <summary>Mouth shape information when the mouth is closed</summary>
		 * <remarks>
		 * <para header='Description'>This is the closed mouth shape information returned by CriLipsDeformer . <br/>
		 * To express the mouth movement, use the difference between the mouth shape information obtained in real-time <br/>
		 * and the closed mouth information. <br/></para>
		 * </remarks>
		*/
		CriLipsMouth.Info SilenceInfo { set; }

		/**
		 * <summary>Update the LipSync analysis result value</summary>
		 * <remarks>
		 * <para header='Description'>Called from CriLipsDeformer when the LipSync analysis result value is updated. <br/>
		 * Custom morphing can be achieved by referring to the analysis results returned by this function. <br/></para>
		 * </remarks>
		*/
		void Update(ref CriLipsMouth.Info info, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount);

		/**
		 * <summary>Processing when unregistering the interface</summary>
		 * <remarks>
		 * <para header='Description'>It is called when the interface registered in CriLipsDeformer is being unregistered. <br/></para>
		 * </remarks>
		*/
		void Reset();
	}
} //namespace CriWare
/**
 * @}
 */
#endif