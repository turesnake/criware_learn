/****************************************************************************
 *
 * Copyright (c) 2019 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System;
using System.Runtime.InteropServices;
using UnityEngine;

/*==========================================================================
 *      CRI Lips Unity Integration
 *=========================================================================*/
/**
 * \addtogroup CRILIPS_UNITY_INTEGRATION
 * @{
 */

namespace CriWare {

/**
 * <summary>Global class of the LipsAtom library.</summary>
 * <remarks>
 * <para header='Description'>A class containing the initialization functions for the LipsAtom library and the variable types shared in the library.<br/></para>
 * </remarks>
 */
public static class CriLipsAtomPlugin {

#if UNITY_EDITOR
	public const string pluginName = "cri_lips_unity";
#elif UNITY_IOS || UNITY_TVOS || UNITY_WEBGL || UNITY_SWITCH
		public const string pluginName = "__Internal";
#else
		public const string pluginName = "cri_lips_unity";
#endif

#if ENABLE_IL2CPP && (UNITY_STANDALONE_WIN || UNITY_WINRT)
		public const CallingConvention pluginCallingConvention = CallingConvention.Cdecl;
#else
	public const CallingConvention pluginCallingConvention = CallingConvention.Winapi;  /* default */
#endif

	/**
	 * <summary>Initializes the LipsAtom library</summary>
	 * <param name='maxHandles'>Maximum number of AtomAnalyzer handles to use at the same time (default is 2)</param>
	 * <remarks>
	 * <para header='Description'>Initializes the LipsAtom library.<br/>
	 * If you want to analyze the mouth shape in cooperation with Atom,
	 * initialize the library using this function.
	 * In addition, if you initialize the library by calling this function,
	 * be sure to call ::CriLipsAtomPlugin::FinalizeLibrary to terminate the library.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomPlugin::FinalizeLibrary'/>
	 */
	public static void InitializeLibrary(uint maxHandles = 2u) {
		CriLipsAtomPlugin.initializationCount++;
		if (CriLipsAtomPlugin.initializationCount != 1) {
			return;
		}

		if (CriLipsAtomPlugin.IsLibraryInitialized() == true) {
			CriLipsAtomPlugin.FinalizeLibrary();
			CriLipsAtomPlugin.initializationCount = 1;
		}

		CriAtomPlugin.InitializeLibrary();
		CriLipsPlugin.InitializeLibrary();

		criLipsAtom_SetExternalBridgeInterface(criAtomEx_GetLipsAtomBridgeInterface());

		CriLipsAtomPlugin.criLipsAtomUnity_Initialize(maxHandles);
	}


	/**
	 * <summary>Terminates the LipsAtom library</summary>
	 * <remarks>
	 * <para header='Description'>Initializes the LipsAtom library.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomPlugin::InitializeLibrary'/>
	 */
	public static void FinalizeLibrary() {
		CriLipsAtomPlugin.initializationCount--;
		if (CriLipsAtomPlugin.initializationCount < 0) {
			CriLipsAtomPlugin.initializationCount = 0;
			if (CriLipsAtomPlugin.IsLibraryInitialized() == false) {
				return;
			}
		}
		if (CriLipsAtomPlugin.initializationCount != 0) {
			return;
		}

		CriDisposableObjectManager.DisposeAll(CriDisposableObjectManager.ModuleType.Lips);
		CriLipsAtomPlugin.criLipsAtomUnity_Finalize();
		CriAtomPlugin.FinalizeLibrary();
	}


	/**
	 * <summary>Gets memory usage</summary>
	 * <returns>Memory usage [byte]</returns>
	 * <remarks>
	 * <para header='Description'>Returns the memory usage of LipsAtom.</para>
	 * </remarks>
	 */
	public static uint GetMemoryUsage() {
		return criLipsAtomUnity_GetAllocatedHeapSize();
	}


	/**
	 * <summary>Attaches the LipsAtom analyzer to the AtomExPlayer</summary>
	 * <param name='player'>AtomExPlayer handle</param>
	 * <param name='analyzer'>LipsAtomAnalyzer handle</param>
	 * <remarks>
	 * <para header='Description'>Attaches the LipsAtom analyzer to the AtomExPlayer to be analyzed.<br/>
	 * After calling this function, the sound played on by the AtomExPlayer is analyzed,
	 * and the mouth shape information can be get through the attached LipsAtom analyzer.<br/>
	 * This function can be called only for players that are stopped (with status Stop).<br/>
	 * If you call the function for a player that is not stopped, it fails with an error callback.<br/>
	 * Calling this function clears the internal state.</para>
	 * <para header='Note'>Before discarding the AtomExPlayer or LipsAtom analyzer attached by this function,
	 * be sure to detach them using the ::CriLipsAtomPlugin::DetachAnalyzerFromPlayer function.<br/>
	 * Invalid memory access may occur if you discard the analyzer.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomPlugin::DetachAnalyzerFromPlayer'/>
	 */
	public static void AttachAnalyzerToPlayer(CriAtomExPlayer player, CriLipsAtomAnalyzer analyzer) {
		if (player != null && player.isAvailable &&
			analyzer != null && analyzer.isAvailable) {
			criLipsAtom_AttachAnalyzerToPlayer(player.nativeHandle, analyzer.nativeHandle);
		}
	}


	/**
	 * <summary>Detaches the LipsAtom analyzer from the AtomExPlayer</summary>
	 * <param name='player'>AtomExPlayer handle</param>
	 * <param name='analyzer'>LipsAtomAnalyzer handle</param>
	 * <remarks>
	 * <para header='Description'>Detaches (removes) the analyzer from the attached AtomExPlayer.<br/>
	 * This function can be called only for players that are stopped (with status Stop).<br/>
	 * If you call the function for a player that is not stopped, it fails with an error callback.<br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomPlugin::AttachAnalyzerToPlayer'/>
	 */
	public static void DetachAnalyzerFromPlayer(CriAtomExPlayer player, CriLipsAtomAnalyzer analyzer) {
		if (player == null || !player.isAvailable) {
			DetachAnalyzerFromInvalidPlayer(analyzer);
			return;
		}
		if (analyzer != null && analyzer.isAvailable) {
			criLipsAtom_DetachAnalyzerFromPlayer(player.nativeHandle, analyzer.nativeHandle);
		}
	}


	/**
	 * <summary>Detaches the LipsAtom analyzer from the discarded AtomExPlayer</summary>
	 * <param name='analyzer'>LipsAtomAnalyzer handle</param>
	 * <remarks>
	 * <para header='Description'>Detaches (removes) the analyzer from the discarded AtomExPlayer.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomPlugin::AttachAnalyzerToPlayer'/>
	 */
	public static void DetachAnalyzerFromInvalidPlayer(CriLipsAtomAnalyzer analyzer) {
		if (analyzer != null && analyzer.isAvailable) {
			criLipsAtom_DetachAnalyzerFromInvalidPlayer(analyzer.nativeHandle);
		}
	}


	public static bool isInitialized { get { return initializationCount > 0; } }
	public static bool IsLibraryInitialized() {
		return CriLipsAtomPlugin.criLipsAtomUnity_IsInitialized();
	}

	#region Internal Members
	private static int initializationCount = 0;
	#endregion

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtom_SetExternalBridgeInterface(IntPtr atom);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criAtomEx_GetLipsAtomBridgeInterface();

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomUnity_Initialize(uint maxHandles);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsAtomUnity_IsInitialized();

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomUnity_Finalize();

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern uint criLipsAtomUnity_GetAllocatedHeapSize();

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtom_AttachAnalyzerToPlayer(IntPtr player, IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtom_DetachAnalyzerFromPlayer(IntPtr player, IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtom_DetachAnalyzerFromInvalidPlayer(IntPtr analyzer);

#else
	private static void criLipsAtom_SetExternalBridgeInterface(IntPtr atom) { }
	private static IntPtr criAtomEx_GetLipsAtomBridgeInterface() { return IntPtr.Zero; }
	private static void criLipsAtomUnity_Initialize(uint maxHandles) { }
	private static bool criLipsAtomUnity_IsInitialized() { return false; }
	private static void criLipsAtomUnity_Finalize() { }
	private static uint criLipsAtomUnity_GetAllocatedHeapSize() { return 0u; }
	private static void criLipsAtom_AttachAnalyzerToPlayer(IntPtr player, IntPtr analyzer) { }
	private static void criLipsAtom_DetachAnalyzerFromPlayer(IntPtr player, IntPtr analyzer) { }
	private static void criLipsAtom_DetachAnalyzerFromInvalidPlayer(IntPtr analyzer) { }

#endif
	#endregion
}

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

/**
 * <summary>Atom linked mouth shape analysis module</summary>
 * <remarks>
 * <para header='Description'>The module to analyze sound in cooperation with the AtomExPlayer and DSP bus.<br/>
 * You can analyze the sound played back using the Atom library and get the mouth shape information.</para>
 * </remarks>
 */
public class CriLipsAtomAnalyzer : CriDisposable, ICriLipsAnalyzeModule {
	/**
	 * <summary>External data read operation type</summary>
	 * <remarks>
	 * <para header='Description'>Data type used to specify the behavior of reading lip-sync data included in an Atom Cue Sheet binary. <br/>
	 * It is used to decide whether to perform real-time analysis according to the presence of pre-analyzed
	 * lip-sync data corresponding to the waveform data being played. <br/>
	 * - ProcessIfNoData: Reads lip-sync data if exists; performs real-time analysis if data does not exist. <br/>
	 * - SilentIfNoData: Reads lip-sync data if exists; treats as silence otherwise. No real-time analysis will be performed. <br/>
	 * - ProcessAlways: Real-time analysis will always be performed regardless of the presence of lip-sync data. <br/></para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomAnalyzer::Config'/>
	 * <seealso cref='CriLipsAtomAnalyzer::CriLipsAtomAnalyzer'/>
	 */
	public enum DataReadoutMode {
		ProcessIfNoData = 0,
		SilentIfNoData,
		ProcessAlways,
	}


	/**
	 * <summary>Configuration for creating Atom sound analysis module</summary>
	 * <remarks>
	 * <para header='Description'>A structure for specifying operating specifications when creating CriLipsAtomAnalyzer. <br/>
	 * It is used as an argument of the CriLipsAtomAnalyzer::CriLipsAtomAnalyzer function. <br/>
	 * The created LipsAtomAnalyzer reserves as many internal resources as necessary
	 * depending to the settings specified in this structure when the handle is created. <br/>
	 * The size of the work area required varies according to the parameters specified in this structure.</para>
	 * </remarks>
	 * <para header='Note'>Be sure to use CriLipsAnalyzer::Config::Default for initialization
	 * as new members may be added to the structure in the future. <br/>
	 * (Be careful not to specify undefined values to the members of the structure.)</para>
	 * <seealso cref='CriLipsAtomAnalyzer::CriLipsAtomAnalyzer'/>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct Config {
		public int maxInputSamplingRate;
		public CriLipsMouth.MorphTargetType morphTargetType;
		public DataReadoutMode dataReadoutMode;
		public CriLipsMouth.BehaviourParamsPreset behaviourParamsPreset;

		/**
		 * <summary>Default values used when creating an Atom audio analysis module</summary>
		 */
		public static Config Default {
			get {
				Config config = new Config();
				config.maxInputSamplingRate = 48000;
				config.morphTargetType = CriLipsMouth.MorphTargetType.Japanese_AIUEO;
				config.dataReadoutMode = DataReadoutMode.ProcessIfNoData;
				config.behaviourParamsPreset = CriLipsMouth.BehaviourParamsPreset.Default;
				return config;
			}
		}
	}


	/**
	 * <summary>Creating AtomAnalyzer handle (specifying the maximum sampling rate)</summary>
	 * <param name='maxSamplingRate'>Maximum sampling rate</param>
	 * <returns>AtomAnalyzer handle</returns>
	 * <remarks>
	 * <para header='Description'>Creates LipAtomAnalyzer which is a mouth shape analysis module that works with Atom. <br/>
	 * It is created in a state where the waveform data of the sampling rate specified by the argument
	 * can be analyzed.<br/>
	 * When passing the waveform data with a sampling rate lower than that specified when creating the handle,
	 * call ::CriLipsAtomAnalyzer::SetSamplingRate .<br/></para>
	 * <para header='Note'>Calling ::CriLipsAtomAnalyzer::GetInfo without attaching to the AtomExPlayer etc.
	 * obtains the shape information with "mouth closed.</para>
	 * <para header='Note'>Be sure to initialize the plug-in before calling this function.<br/>
	 * <br/>
	 * The lip-sync analysis uses the ADX2 filter callback inside the plug-in.<br/>
	 * Therefore, if there are any filter callbacks registered with CriAtomSource, they are unregistered.<br/>
	 * In addition, if a filter callback is registered with CriAtomSource while performing the lip-sync analysis,<br/>
	 * the lip-sync analysis stops.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomAnalyzer::Dispose'/>
	 * <seealso cref='CriLipsAtomAnalyzer::GetInfo'/>
	 */
	public CriLipsAtomAnalyzer(int maxSamplingRate = 48000) {
		if (!CriLipsAtomPlugin.IsLibraryInitialized()) {
			throw new Exception("CriLipsAtomPlugin is not initialized.");
		}

		Config config = Config.Default;
		config.maxInputSamplingRate = maxSamplingRate;

		this.handle = criLipsAtomAnalyzer_Create(ref config, IntPtr.Zero, 0);
		Debug.Assert(this.handle != IntPtr.Zero, "[CRIWARE] Invalid native handle of CriLipsAtomAnalyzer.");
		this.mouth = new CriLipsMouth(GetNativeMouthHandle());
		this.maxSamplingRate = maxSamplingRate;
		CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Lips);
	}


	/**
	 * <summary>Creating AtomAnalyzer handle (specifying configurations)</summary>
	 * <param name='config'>Config structure</param>
	 * <returns>AtomAnalyzer handle</returns>
	 * <remarks>
	 * <para header='Description'>Creates LipAtomAnalyzer which is a mouth shape analysis module that works with Atom. <br/>
	 * When passing the waveform data with a sampling rate lower than that specified in the configuration
	 * when creating the handle, please call ::CriLipsAtomAnalyzer::SetSamplingRate . <br/></para>
	 * <para header='Note'>Calling ::CriLipsAtomAnalyzer::GetInfo without attaching to the AtomExPlayer etc.
	 * obtains the shape information with "mouth closed.</para>
	 * <para header='Note'>Be sure to initialize the plug-in before calling this function.<br/>
	 * <br/>
	 * The lip-sync analysis uses the ADX2 filter callback inside the plug-in.<br/>
	 * Therefore, if there are any filter callbacks registered with CriAtomSource, they are unregistered.<br/>
	 * In addition, if a filter callback is registered with CriAtomSource while performing the lip-sync analysis,<br/>
	 * the lip-sync analysis stops.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomAnalyzer::Dispose'/>
	 * <seealso cref='CriLipsAtomAnalyzer::GetInfo'/>
	 */
	public CriLipsAtomAnalyzer(Config config) {
		if (!CriLipsAtomPlugin.IsLibraryInitialized()) {
			throw new Exception("CriLipsAtomPlugin is not initialized.");
		}

		this.handle = criLipsAtomAnalyzer_Create(ref config, IntPtr.Zero, 0);
		Debug.Assert(this.handle != IntPtr.Zero, "[CRIWARE] Invalid native handle of CriLipsAtomAnalyzer.");
		this.mouth = new CriLipsMouth(GetNativeMouthHandle());
		this.maxSamplingRate = config.maxInputSamplingRate;
		CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Lips);
	}


	/**
	 * <summary>Discards the LipsAtomAnalyzer handle</summary>
	 * <remarks>
	 * <para header='Description'>Discards the LipsAtomAnalyzer handle.<br/>
	 * When this function is called, all the resources allocated in the DLL
	 * when creating the LipsAtomAnalyzer handle are released.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomAnalyzer::CriLipsAtomAnalyzer'/>
	 */
	public override void Dispose() {
		CriDisposableObjectManager.Unregister(this);
		if (isAvailable) {
			DetachFromDspBus();
			DetachFromAtomExPlayer(true);
			criLipsAtomAnalyzer_Destroy(this.handle);
		}
		this.handle = IntPtr.Zero;
		GC.SuppressFinalize(this);
	}


	/**
	 * <summary>Attaches to the AtomExPlayer</summary>
	 * <param name='player'>CriAtomExPlayer handle</param>
	 * <returns>True if the attachment succeeds, False if it fails</returns>
	 * <remarks>
	 * <para header='Description'>Attaches the analyzer to the AtomExPlayer to be analyzed.<br/>
	 * After calling this function, the sound played on the AtomExPlayer is analyzed
	 * and the mouth shape information can be acquired.<br/>
	 * This function can be called only for stopped players.<br/>
	 * If you call the function on a player that is playing, it will fail with an error callback.<br/>
	 * Calling this function clears the internal state.</para>
	 * <para header='Note'>If it is already attached to the AtomExPlayer or the DSP bus at the time of calling this function,
	 * it is detached internally.<br/>
	 * If the attached AtomExPlayer is being played, this function fails
	 * because the detachment cannot be done .</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::DetachFromAtomExPlayer'/>
	 */
	public bool AttachToAtomExPlayer(CriAtomExPlayer player) {
		if (player.GetStatus() != CriAtomExPlayer.Status.Stop) {
			Debug.LogError("[CRIWARE] Cannot attach to player that is not stopped.");
			return false;
		}

		attachedPlayer = player;
		CriLipsAtomPlugin.AttachAnalyzerToPlayer(player, this);
		return true;
	}


	/**
	 * <summary>Detaches from the AtomExPlayer</summary>
	 * <param name='forceStop'>Whether to stop the player and force detachment</param>
	 * <returns>True if the detachment succeeds, or False if it fails</returns>
	 * <remarks>
	 * <para header='Description'>Detaches (removes) the analyzer from the attached AtomExPlayer.<br/>
	 * This function can be called only for stopped players.<br/>
	 * When the function is called by passing True as an argument, playback is stopped and the detachment is forced.<br/>
	 * If you call the function with False as an argument for the player being played, an error callback
	 * occurs and it fails.<br/>
	 * Calling this function clears the internal state.</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::AttachToAtomExPlayer'/>
	 */
	public bool DetachFromAtomExPlayer(bool forceStop = false) {
		bool ret = true;
		if (attachedPlayer != null && attachedPlayer.isAvailable) {
			if (forceStop) {
				attachedPlayer.Stop(true);
			}
			if (attachedPlayer.GetStatus() != CriAtomExPlayer.Status.Stop) {
				Debug.LogError("[CRIWARE] Cannot detach from player that is not stopped.");
				return false;
			}
			CriLipsAtomPlugin.DetachAnalyzerFromPlayer(attachedPlayer, this);
			attachedPlayer = null;
			return true;
		} else {
			attachedPlayer = null;
			CriLipsAtomPlugin.DetachAnalyzerFromInvalidPlayer(this);
		}
		return ret;
	}


	/**
	 * <summary>Sets the sampling frequency</summary>
	 * <param name='samplingRate'>Sampling frequency</param>
	 * <returns>True if the data was set successfully, or False if failed</returns>
	 * <remarks>
	 * <para header='Description'>Sets the sampling frequency of the sound data to be analyzed.<br/>
	 * Set the sampling frequency of the waveform data before playing the sound
	 * on the AtomExPlayer.<br/>
	 * Calling this function clears the internal state.<br/>
	 * The sampling frequency that can be analyzed is 16000Hz or more and
	 * less than the maximum sampling rate specified at initialization.</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::AttachToAtomExPlayer'/>
	 */
	public bool SetSamplingRate(int samplingRate) {
		if (samplingRate > maxSamplingRate) {
			Debug.LogErrorFormat("[CRIWARE] Sampling rate should be equal to or less than {0}.", maxSamplingRate);
			return false;
		} else if (samplingRate < 16000) {
			Debug.LogErrorFormat("[CRIWARE] Sampling rate should be equal to or more than {0}.", 16000);
			return false;
		}
		criLipsAtomAnalyzer_SetSamplingRate(this.handle, samplingRate);
		return true;
	}


	/**
	 * <summary>Sets the silence determination volume threshold</summary>
	 * <param name='volume'>Maximum volume (dB)</param>
	 * <returns>True if the data was set successfully, or False if failed</returns>
	 * <remarks>
	 * <para header='Description'>Set the maximum volume (dB) equal to or smaller than 0, for which the sample to be analyzed that is output
	 * from the AtomExPlayer is considered as silent.<br/>
	 * The default when creating a handle is -40dB.</para>
	 * </remarks>
	 */
	public bool SetSilenceThreshold(float volume) {
		if (volume > 0.0f) {
			Debug.LogErrorFormat("[CRIWARE] Silence threshold should be equal to or less than {0}.", 0.0f);
			return false;
		}
		criLipsAtomAnalyzer_SetSilenceThreshold(this.handle, volume);
		return true;
	}


	/**
	 * <summary>Gets the mouth shape information</summary>
	 * <param name='info'>Mouth shape information</param>
	 * <remarks>
	 * <para header='Description'>Gets the mouth shape information obtained by analyzing the sound data
	 * played back by the attached AtomExPlayer.</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::AttachToAtomExPlayer'/>
	 */
	public void GetInfo(out CriLipsMouth.Info info) {
		info = new CriLipsMouth.Info();
		criLipsAtomAnalyzer_GetInfo(this.handle, ref info);
	}


	/**
	 * <summary>Gets the Japanese 5 vowel morph target blend amount</summary>
	 * <param name='morph'>Japanese 5 vowel morph target blend amount</param>
	 * <remarks>
	 * <para header='Description'>Gets the Japanese 5 vowel morph target blend amount obtained by
	 * analyzing the sound data played back by the attached AtomExPlayer.</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::AttachToAtomExPlayer'/>
	 */
	public void GetMorphTargetBlendAmountAsJapanese(out CriLipsMouth.MorphTargetBlendAmountAsJapanese morph) {
		morph = new CriLipsMouth.MorphTargetBlendAmountAsJapanese();
		criLipsAtomAnalyzer_GetMorphTargetBlendAmountJapanese(this.handle, ref morph);
	}


	/**
	 * <summary>Gets volume</summary>
	 * <returns>Volume of the analyzed sample (dB)</returns>
	 * <remarks>
	 * <para header='Description'>Gets the volume (dB) of the analyzed sample.</para>
	 * <para header='Note'>Returns a positive value if an error occurred.</para>
	 * </remarks>
	 * <seealso cref='CriLipsAtomAnalyzer::AttachToAtomExPlayer'/>
	 */
	public float GetVolume() {
		return criLipsAtomAnalyzer_GetVolumeDecibel(this.handle);
	}


	/**
	 * <summary>Gets the silence determination volume threshold</summary>
	 * <returns>Maximum volume (dB)</returns>
	 * <remarks>
	 * <para header='Description'>Acquires the maximum volume (dB) that determines the voice data
	 * analyzed by the function as silence.<br/></para>
	 * <para header='Note'>Returns a value greater than 0 if an error occurred.</para>
	 * </remarks>
	 */
	public float GetSilenceThreshold() {
		return criLipsAtomAnalyzer_GetSilenceThreshold(this.handle);
	}


	/**
	 * <summary>Gets the mouth shape information in the closed state</summary>
	 * <param name='info'>Mouth shape information</param>
	 * <remarks>
	 * <para header='Description'>Gets lips shape information after creating the handle or starting silent playback.</para>
	 * </remarks>
	 */
	public void GetInfoAtSilence(out CriLipsMouth.Info info) {
		info = new CriLipsMouth.Info();
		criLipsAtomAnalyzer_GetInfoAtSilence(this.handle, ref info);
	}


	/**
	 * <summary>Check if there is no voice and the mouth is closed</summary>
	 * <returns>true when the mouth is closed; false otherwise</returns>
	 * <remarks>
	 * <para header='Description'>Whether the current state is "not being pronounced" (i.e. the mouth is closed).<br/></para>
	 * </remarks>
	 */
	public bool IsAtSilence() {
		return criLipsAtomAnalyzer_IsAtSilence(this.handle);
	}


	/**
	 * <summary>Attaches to the DSP bus</summary>
	 * <param name='busName'>DSP bus name</param>
	 * <returns>True if the attachment succeeds, False if it fails</returns>
	 * <remarks>
	 * <para header='Description'>Attaches the DSP bus analyzer to be analyzed.<br/>
	 * After calling this function, the sound sent to the DSP bus is analyzed
	 * and the mouth shape information can be acquired.<br/>
	 * Calling this function clears the internal state.</para>
	 * <para header='Note'>If it is already attached to the AtomExPlayer or the DSP bus at the time of calling this function,
	 * it is detached internally.<br/>
	 * If the attached AtomExPlayer is being played, this function fails
	 * because the detachment cannot be done .</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::DetachFromDspBus'/>
	 */
	public bool AttachToDspBus(string busName) {
		return criLipsAtomAnalyzer_AttachToDspBus(this.handle, busName);
	}


	/**
	 * <summary>Detaches from the DSP bus</summary>
	 * <returns>True if the detachment succeeds, or False if it fails</returns>
	 * <remarks>
	 * <para header='Description'>Detaches (removes) the analyzer from the attached DSP bus.<br/>
	 * Calling this function clears the internal state.</para>
	 * </remarks>
	 * <seealso cref='::CriLipsAtomAnalyzer::AttachToDspBus'/>
	 */
	public bool DetachFromDspBus() {
		return criLipsAtomAnalyzer_DetachFromDspBus(this.handle);
	}


	#region Internal Members
	private IntPtr GetNativeMouthHandle() {
		return criLipsAtomAnalyzer_GetLipsMouthHandle(this.handle);
	}

	~CriLipsAtomAnalyzer() {
		this.Dispose();
	}

	public IntPtr nativeHandle { get { return this.handle; } }
	public bool isAvailable { get { return this.handle != IntPtr.Zero; } }
	public CriLipsMouth mouth = null;
	private IntPtr handle = IntPtr.Zero;
	private CriAtomExPlayer attachedPlayer = null;
	private int maxSamplingRate;

	#endregion

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criLipsAtomAnalyzer_Create(ref Config config, IntPtr work, Int32 size);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_Destroy(IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsAtomAnalyzer_AttachToDspBus(IntPtr analyzer, string busName);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsAtomAnalyzer_DetachFromDspBus(IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_GetInfo(IntPtr analyzer, ref CriLipsMouth.Info info);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_GetInfoAtSilence(IntPtr analyzer, ref CriLipsMouth.Info info);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_SetSamplingRate(IntPtr analyzer, Int32 samplingRate);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_SetSilenceThreshold(IntPtr analyzer, float volumeDb);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern float criLipsAtomAnalyzer_GetSilenceThreshold(IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern float criLipsAtomAnalyzer_GetVolumeDecibel(IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criLipsAtomAnalyzer_IsAtSilence(IntPtr analyzer);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criLipsAtomAnalyzer_GetMorphTargetBlendAmountJapanese(IntPtr analyzer, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount);

	[DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criLipsAtomAnalyzer_GetLipsMouthHandle(IntPtr analyzer);

	#else
	private static IntPtr criLipsAtomAnalyzer_Create(ref Config config, IntPtr work, Int32 size) { return IntPtr.Zero; }
	private static void criLipsAtomAnalyzer_Destroy(IntPtr analyzer) { }
	private static bool criLipsAtomAnalyzer_AttachToDspBus(IntPtr analyzer, string busName) { return false; }
	private static bool criLipsAtomAnalyzer_DetachFromDspBus(IntPtr analyzer) { return false; }
	private static void criLipsAtomAnalyzer_GetInfo(IntPtr analyzer, ref CriLipsMouth.Info info) { }
	private static void criLipsAtomAnalyzer_GetInfoAtSilence(IntPtr analyzer, ref CriLipsMouth.Info info) { }
	private static void criLipsAtomAnalyzer_SetSamplingRate(IntPtr analyzer, Int32 samplingRate) { }
	private static void criLipsAtomAnalyzer_SetSilenceThreshold(IntPtr analyzer, float volumeDb) { }
	private static float criLipsAtomAnalyzer_GetSilenceThreshold(IntPtr analyzer) { return 1.0f; }
	private static float criLipsAtomAnalyzer_GetVolumeDecibel(IntPtr analyzer) { return 1.0f; }
	private static bool criLipsAtomAnalyzer_IsAtSilence(IntPtr analyzer) { return false; }
	private static void criLipsAtomAnalyzer_GetMorphTargetBlendAmountJapanese(IntPtr analyzer, ref CriLipsMouth.MorphTargetBlendAmountAsJapanese blendAmount) { }
	private static IntPtr criLipsAtomAnalyzer_GetLipsMouthHandle(IntPtr analyzer) { return IntPtr.Zero; }
	#endif
	#endregion
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */
