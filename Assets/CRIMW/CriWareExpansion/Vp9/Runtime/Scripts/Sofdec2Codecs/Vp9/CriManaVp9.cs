#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS || UNITY_ANDROID
	#define CRIMANA_VP9_SUPPORTED_BY_EXPANSION
#endif

#if !UNITY_EDITOR && UNITY_SWITCH
	#define CRIMANA_VP9_SUPPORTED_BY_PLUGIN
#endif

using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System;

/**
 * \addtogroup CriManaVp9
 * @{
 */

namespace CriWare {

/**
 * <summary>CRI Sofdec2 Codec VP9 Expansion plugin</summary>
 */
public static class CriManaVp9
{
	private const string scriptVersionString = "1.01.01";
	private const int scriptVersionNumber = 0x01010100;

	/* ネイティブライブラリ */
	#if UNITY_EDITOR
		public const string cri_mana_vp9_name = "cri_mana_vpx";
	#elif UNITY_IOS
		public const string cri_mana_vp9_name = "__Internal";
	#else
		public const string cri_mana_vp9_name = "cri_mana_vpx";
	#endif

	static public bool SupportCurrentPlatform()
	{
#if CRIMANA_VP9_SUPPORTED_BY_EXPANSION || CRIMANA_VP9_SUPPORTED_BY_PLUGIN
		return true;
#else
		return false;
#endif
	}

	/**
	 * <summary>Initializes the plug-in (for manual initialization)</summary>
	 * <remarks>
	 * <para header='Description'>Initializes the plugin.<br/>
	 * When deploying ::CriManaVP9Initializer in scene, this function will be called in the 'Awake' function automatically,
	 * hence no need to call this function in application.<br/>
	 * <br/>
	 * If you want to initialize from script and not using ::CriManaVP9Initializer , call this function BEFORE
	 * initializing the CRIWARE plugin.<br/></para>
	 * <para header='Note'>This function must be called BEFORE the initialization of the CRIWARE plugin.<br/>
	 * Please use it in a script with high Script Execution Order.</para>
	 * </remarks>
	 */
	static public void SetupVp9Decoder()
	{
#if CRIMANA_VP9_SUPPORTED_BY_EXPANSION
		if (CriManaPlugin.IsLibraryInitialized()) {
			Debug.LogError("[CRIWARE][VP9] Mana library is already initialized.");
			return;
		}

		{
			/* VP9ユーザアロケータの設定 */
			IntPtr alloc_func   = criWareUnity_GetAllocateFunc();
			IntPtr free_func    = criWareUnity_GetDeallocateFunc();
			IntPtr usr_obj      = criManaUnity_GetAllocatorManager();

			criVvp9_SetUserAllocator(alloc_func, free_func, usr_obj);
		}

		{
			/* VP9コーデックインターフェースのアタッチ */
			IntPtr codec_if      = criVvp9_GetInterface();
			IntPtr codecalpha_if = criVvp9_GetAlphaInterface();

			criMvPly_AttachCodecInterface((int)CriMana.CodecType.VP9, codec_if, codecalpha_if);
		}
#endif
	}

	#region Native API Definition (DLL)
#if CRIMANA_VP9_SUPPORTED_BY_EXPANSION
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criWareUnity_GetAllocateFunc();
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criWareUnity_GetDeallocateFunc();
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criManaUnity_GetAllocatorManager();
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criMvPly_AttachCodecInterface(int codec_type, IntPtr codec_if, IntPtr codecalpha_if);

	[DllImport(cri_mana_vp9_name, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criVvp9_SetUserAllocator(IntPtr alloc_func, IntPtr free_func, IntPtr usr_obj);
	[DllImport(cri_mana_vp9_name, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criVvp9_GetInterface();
	[DllImport(cri_mana_vp9_name, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criVvp9_GetAlphaInterface();
#endif

	#endregion
}

} //namespace CriWare
/** @} */
