/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System;
using System.Runtime.InteropServices;

/**
 * \addtogroup CRILIPS_UNITY_INTEGRATION
 * @{
 */

namespace CriWare {

public static partial class CriLipsPlugin {
	private const string scriptVersionString = "1.02.04";
	private const int scriptVersionNumber = 0x01020400;

	/**
	 * <summary>Initializing the Lips library</summary>
	 * \par 説明:
	 * Lipsライブラリを初期化します。<br/>
	 * 解析器のみを作成し口形状解析を行う場合は、本関数でライブラリの初期化を
	 * 行ってください。
	 * また、本関数を呼び出してライブラリを初期化を行った場合は、必ず
	 * ::CriLipsPlugin::FinalizeLibrary を呼び出してライブラリを終了してください。
	 * \sa CriLipsPlugin::FinalizeLibrary
	 */
	public static void InitializeLibrary() {
		CriLipsPlugin.initializationCount++;
		if (CriLipsPlugin.initializationCount != 1) {
			return;
		}

		if(CriLipsPlugin.IsLibraryInitialized() == true){
			CriLipsPlugin.FinalizeLibrary();
			CriLipsPlugin.initializationCount = 1;
		}

		CriLipsPlugin.criLipsUnity_SetPluginErrorCallbackFunc(CriLipsPlugin.criWareUnity_GetPluginErrorCallbackFunc());

		CriLipsPlugin.criLips_SetExternalBridgeInterface(
			CriLipsPlugin.criBase_GetLipsBaseBridgeInterface(),
			CriLipsPlugin.criAfx_GetLipsAfxBridgeInterface(),
			CriLipsPlugin.criDsp_GetLipsDspBridgeInterface());

		CriLipsPlugin.criLipsUnity_Initialize();
	}


	/**
	 * <summary>Finalizing the Lips library</summary>
	 * \par 説明:
	 * Lipsライブラリを初期化します。
	 * \sa CriLipsPlugin::InitializeLibrary
	 */
	public static void FinalizeLibrary() {
		CriLipsPlugin.initializationCount--;
		if (CriLipsPlugin.initializationCount < 0) {
			CriLipsPlugin.initializationCount = 0;
			if (CriLipsPlugin.IsLibraryInitialized() == false) {
				return;
			}
		}
		if (CriLipsPlugin.initializationCount != 0) {
			return;
		}
		CriDisposableObjectManager.DisposeAll(CriDisposableObjectManager.ModuleType.Lips);
		CriLipsPlugin.criLipsUnity_Finalize();
	}


	public static bool IsLibraryInitialized(){
		return criLipsUnity_IsInitialized();
	}

	#region Internal Members
	private static int initializationCount = 0;
	#endregion

	#region DLL Import
#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern void criLipsUnity_SetPluginErrorCallbackFunc(IntPtr func);
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern IntPtr criWareUnity_GetPluginErrorCallbackFunc();
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern void criLips_SetExternalBridgeInterface(IntPtr _base, IntPtr _afx, IntPtr _dsp);
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern IntPtr criBase_GetLipsBaseBridgeInterface();
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern IntPtr criAfx_GetLipsAfxBridgeInterface();
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern IntPtr criDsp_GetLipsDspBridgeInterface();
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern void criLipsUnity_Initialize();
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern void criLipsUnity_Finalize();
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	internal static extern bool criLipsUnity_IsInitialized();
#else
	internal static void criLipsUnity_SetPluginErrorCallbackFunc(IntPtr func) { }
	internal static IntPtr criWareUnity_GetPluginErrorCallbackFunc() { return IntPtr.Zero; }
	internal static void criLips_SetExternalBridgeInterface(IntPtr _base, IntPtr _afx, IntPtr _dsp) { }
	internal static IntPtr criBase_GetLipsBaseBridgeInterface() { return IntPtr.Zero; }
	internal static IntPtr criAfx_GetLipsAfxBridgeInterface() { return IntPtr.Zero; }
	internal static IntPtr criDsp_GetLipsDspBridgeInterface() { return IntPtr.Zero; }
	internal static void criLipsUnity_Initialize() { }
	internal static void criLipsUnity_Finalize() { }
	internal static bool criLipsUnity_IsInitialized() { return false; }
#endif
	#endregion
}


/**
 * <summary>ADX LipSync analysis module interface.</summary>
 */
public interface ICriLipsAnalyzeModule {

	/**
	 * <summary>Gets the mouth shape information</summary>
	 * <param name='info'>Mouth shape information</param>
	 */
	void GetInfo(out CriLipsMouth.Info info);

	/**
	 * <summary>Gets the Japanese 5 vowel morph target blend amount</summary>
	 * <param name='morph'>Japanese 5 vowel morph target blend amount</param>
	 */
	void GetMorphTargetBlendAmountAsJapanese(out CriLipsMouth.MorphTargetBlendAmountAsJapanese morph);

	/**
	 * <summary>Gets volume</summary>
	 * <returns>Volume of the analyzed sample (dB)</returns>
	 */
	float GetVolume();

	/**
	 * <summary>Check if there is no voice and the mouth is closed</summary>
	 * <returns>true when the mouth is closed; false otherwise</returns>
	 */
	bool IsAtSilence();

	/**
	 * <summary>Gets the silence determination volume threshold</summary>
	 * <returns>Maximum volume (dB)</returns>
	 */
	float GetSilenceThreshold();

	/**
	 * <summary>Gets lips information of no sound situation</summary>
	 * <param name='info'>Lips shape information at no sound situation</param>
	 */
	 void GetInfoAtSilence(out CriLipsMouth.Info info);
}

} //namespace CriWare

/**
 * @}
 */

/*==========================================================================
 *      CRI Lips Native Wrapper
 *=========================================================================*/
/**
 * \addtogroup CRILIPS_NATIVE_WRAPPER
 * @{
 */

namespace CriWare {

/**
 * <summary>Mouth shape analysis module</summary>
 * <remarks>
 * <para header='Description'>An analysis module class used for sound analysis.<br/>
 *  The mouth shape information can be acquired by inputting PCM sample data and performing analysis processing. <br/></para>
 * </remarks>
 */
public partial class CriLipsMouth : CriDisposable, ICriLipsAnalyzeModule {
	/**
	 * <summary>Morph target type</summary>
	 * <remarks>
	 * <para header='Description'>Morph target type included in the mouth shape information structure.</para>
	 * </remarks>
	 */
	public enum MorphTargetType {
		Japanese_AIUEO = 0,     /**< Japanese “A[ɑ]、I[i]、U[u]、E[ɛ]、O[ɔ]” */
		MAX_NUM                 /**< The number of morph target types */
	}

	/**
	 * <summary>Presets of behaviour parameters</summary>
	 * <remarks>
	 * <para header='Description'>Presets of parameters related to the behaviour of the object.</para>
	 * </remarks>
	 */
	public enum BehaviourParamsPreset {
		Default = 0,             /**< Default preset */
		NoBlend,                 /**< Preset for models that do not support blending */
	}

	/**
	 * <summary>Mouth shape information structure</summary>
	 * <remarks>
	 * <para header='Description'>The mouth shape information obtained by analyzing the input sound.<br/>
	 * It is passed as an argument of the ::CriLipsMouth::GetInfo function.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::GetInfo'/>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct Info {
		public float lipWidth;          /**< Mouth width ((0.0f to 1.0f)) */
		public float lipHeight;         /**< Mouth height ((0.0f to 1.0f)) */
		public float tonguePosition;    /**< Tongue position (0.0f to 1.0f) */
		bool isLipWidthReleased;        /**< Whether the width of the mouth is transitioning to closed state */
		bool isLipHeightReleased;       /**< Whether the height of the mouth is transitioning to closed state */
		bool isLipToungueReleased;      /**< Whether the tongue is transitioning to the closed state */
	}


	/**
	 * <summary>Japanese 5 vowel morph target blend amount structure</summary>
	 * <remarks>
	 * <para header='Description'>The amount of Japanese 5 vowel morph target blend as a result of analyzing the input sound.<br/>
	 * It is passed as an argument of the GetMorphTargetBlendAmountAsJapanese function.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::GetMorphTargetBlendAmountAsJapanese'/>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct MorphTargetBlendAmountAsJapanese {
		public float a;                 /**< Blend amount of morph target "A" (0.0f to 1.0f) */
		public float i;                 /**< Blend amount of morph target "I" (0.0f to 1.0f) */
		public float u;                 /**< Blend amount of morph target "U" (0.0f to 1.0f) */
		public float e;                 /**< Blend amount of morph target "E" (0.0f to 1.0f) */
		public float o;                 /**< Blend amount of morph target "O" (0.0f to 1.0f) */
	}


	/**
	 * <summary>Gets the silence determination volume threshold</summary>
	 * <returns>Maximum volume (dB)</returns>
	 * <remarks>
	 * <para header='Description'>Gets the volume (dB) below which the sample to be analyzed<br/>
	 * by the CriLipsMouth::Process function is considered as silence.<br/></para>
	 * <para header='Note'>Returns a value greater than 0 if an error occurred.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::Process'/>
	 */
	public float GetSilenceThreshold() {
		return criLipsMouth_GetSilenceThreshold(this.handle);
	}


	/**
	 * <summary>Gets the mouth shape information</summary>
	 * <param name='info'>Mouth shape information</param>
	 * <remarks>
	 * <para header='Description'>Gets the mouth shape method obtained by analyzing the input PCM sample.<br/>
	 * When analysis is not performed using the CriLipsMouth::Process function,
	 * the mouth shape information in the closed state is acquired.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::Process'/>
	 */
	public void GetInfo(out Info info) {
		info = new CriLipsMouth.Info();
		criLipsMouth_GetInfo(this.handle, ref info);
	}


	/**
	 * <summary>Gets the Japanese 5 vowel morph target blend amount</summary>
	 * <param name='morph'>Japanese 5 vowel morph target blend amount</param>
	 * <remarks>
	 * <para header='Description'>Gets the Japanese 5 vowel morph target blend amount obtained by analyzing the input PCM sample.<br/>
	 * When analysis is not performed using the CriLipsMouth::Process function,
	 * the blend amount information in the closed state is acquired.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::Process'/>
	 */
	public void GetMorphTargetBlendAmountAsJapanese(out MorphTargetBlendAmountAsJapanese morph) {
		morph = new MorphTargetBlendAmountAsJapanese();
		criLipsMouth_GetMorphTargetBlendAmountJapanese(this.handle, ref morph);
	}


	/**
	 * <summary>Converts the morph target blend amounts for the Japanese 5-vowels into an array</summary>
	 * <param name='inputMorph'>Japanese 5 vowel morph target blend amount</param>
	 * <param name='outputArray'>The array that stores the converted information</param>
	 * <remarks>
	 * <para header='Description'>Converts the morph target blend amounts for the Japanese 5-vowels into an array.</para>
	 * <para header='Note'>The float array passed as an argument should have a length greater or equal to 5.</para>
	 * </remarks>
	 */
	public static void ConvertMorphTargetBlendAmountToArray(MorphTargetBlendAmountAsJapanese inputMorph, float[] outputArray) {
		UnityEngine.Debug.Assert(outputArray != null && outputArray.Length >= 5);
		outputArray[0] = inputMorph.a;
		outputArray[1] = inputMorph.i;
		outputArray[2] = inputMorph.u;
		outputArray[3] = inputMorph.e;
		outputArray[4] = inputMorph.o;
	}


	/**
	 * <summary>Conversion of the Japanese 5 vowel morph target blend amounts from an array to a structure</summary>
	 * <param name='inputArray'>Array containing information of the Japanese 5-vowels morph target blend amounts</param>
	 * <param name='outputMorph'>The structure that stores the converted information</param>
	 * <remarks>
	 * <para header='Description'>Converts the Japanese 5 vowels morph target blend amounts (which are input as a float array) into a structure.</para>
	 * </remarks>
	 */
	public static void ConvertArrayToMorphTargetBlendAmount(float[] inputArray, ref MorphTargetBlendAmountAsJapanese outputMorph) {
		UnityEngine.Debug.Assert(inputArray != null && inputArray.Length >= 5);
		outputMorph.a = inputArray[0];
		outputMorph.i = inputArray[1];
		outputMorph.u = inputArray[2];
		outputMorph.e = inputArray[3];
		outputMorph.o = inputArray[4];
	}


	/**
	 * <summary>Gets volume</summary>
	 * <returns>Volume of the analyzed sample (dB)</returns>
	 * <remarks>
	 * <para header='Description'>Gets the volume (dB) of the sample analyzed by the CriLipsMouth::Process function.</para>
	 * <para header='Note'>Returns a value greater than 0 if an error occurred.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::Process'/>
	 */
	public float GetVolume() {
		return criLipsMouth_GetVolumeDecibel(this.handle);
	}


	/**
	 * <summary>Gets the mouth shape information in the closed state</summary>
	 * <param name='info'>Mouth shape information</param>
	 * <remarks>
	 * <para header='Description'>Gets the mouth shape information in the closed state that can be acquired immediately after creating a handle or when silence is entered.</para>
	 * </remarks>
	 */
	public void GetInfoAtSilence(out Info info) {
		info = new Info();
		criLipsMouth_GetInfoAtSilence(this.handle, ref info);
	}


	/**
	 * <summary>Check if there is no voice and the mouth is closed</summary>
	 * <returns>true when the mouth is closed; false otherwise</returns>
	 * <remarks>
	 * <para header='Description'>Whether the current state is "not being pronounced" (i.e. the mouth is closed).<br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::Process'/>
	 */
	public bool IsAtSilence() {
		return criLipsMouth_IsAtSilence(this.handle);
	}


	/**
	 * <summary>Whether the blend amount is interpolative</summary>
	 * <returns>CRI_TRUE if interpolation is used, CRI_FALSE otherwise</returns>
	 * <remarks>
	 * <para header='Description'>Whether the acquired blend amount is generated by interpolating between morph targets.</para>
	 * </remarks>
	 */
	public bool IsMorphTargetBlendAmountInterpolative() {
		return criLipsMouth_IsMorphTargetBlendAmountInterpolative(this.handle);
	}


	/**
	 * <summary>Discards the LipsMouth handle</summary>
	 * <remarks>
	 * <para header='Description'>Discards the LipsMouth handle.</para>
	 * </remarks>
	 * <seealso cref='CriLipsMouth::CriLipsMouth'/>
	 */
	public override void Dispose() {
		CriDisposableObjectManager.Unregister(this);
		if (hasExistingNativeHandle == false &&
			handle != IntPtr.Zero) {
			criLipsMouth_Destroy(this.handle);
		}
		handle = IntPtr.Zero;
		GC.SuppressFinalize(this);
	}


	#region Internal Members

	public IntPtr nativeHandle { get { return this.handle; } }
	public bool isAvailable { get { return this.handle != IntPtr.Zero; } }

	private bool hasExistingNativeHandle = false;
	private IntPtr handle = IntPtr.Zero;

	public CriLipsMouth(IntPtr existingNativeHandle) {
		if (!CriLipsAtomPlugin.IsLibraryInitialized()) {
			throw new Exception("CriLipsAtomPlugin is not initialized.");
		}

		hasExistingNativeHandle = (existingNativeHandle != IntPtr.Zero);
		if (!hasExistingNativeHandle) {
			throw new Exception("Null handle was set for CriLipsMouth.");
		}
		this.handle = existingNativeHandle;
		CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Lips);
	}

	~CriLipsMouth() {
		Dispose();
	}

	#endregion

	#region DLL Import
#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsMouth_Destroy(IntPtr mouth);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern float criLipsMouth_GetSilenceThreshold(IntPtr mouth);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsMouth_GetInfo(IntPtr mouth, ref Info info);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern float criLipsMouth_GetVolumeDecibel(IntPtr mouth);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsMouth_GetMorphTargetBlendAmountJapanese(
		IntPtr mouth, ref MorphTargetBlendAmountAsJapanese blend_amount);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsMouth_IsAtSilence(IntPtr mouth);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsMouth_IsMorphTargetBlendAmountInterpolative(IntPtr mouth);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsMouth_GetInfoAtSilence(IntPtr mouth, ref Info info);
#else
	private static void criLipsMouth_Destroy(IntPtr mouth) { }
	private static float criLipsMouth_GetSilenceThreshold(IntPtr mouth) { return 1.0f; }
	private static bool criLipsMouth_GetInfo(IntPtr mouth, ref CriLipsMouth.Info info) { return false; }
	private static float criLipsMouth_GetVolumeDecibel(IntPtr mouth){ return 1.0f; }
	private static bool criLipsMouth_GetMorphTargetBlendAmountJapanese(
		IntPtr mouth, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blend_amount) { return false; }
	private static bool criLipsMouth_IsAtSilence(IntPtr mouth) { return false; }
	private static bool criLipsMouth_IsMorphTargetBlendAmountInterpolative(IntPtr mouth) { return false; }
	private static void criLipsMouth_GetInfoAtSilence(IntPtr mouth, ref Info info) { }
#endif
	#endregion
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */
