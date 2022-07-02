/****************************************************************************
 *
 * Copyright (c) 2012 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#pragma warning disable 0618

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/**
 * \addtogroup CRIWARE_UNITY_COMPONENT
 * @{
 */
 namespace CriWare {


/**
 * <summary>CRIWARE error handling object</summary>
 * <remarks>
 * <para header='Description'>This component is used to initialize the CRIWARE library.<br/></para>
 * </remarks>
 */
[AddComponentMenu("CRIWARE/Error Handler")]
public class CriWareErrorHandler : CriMonoBehaviour
{

	/**
	 * <summary>Whether to enable Coroutine debug output</summary>

	Whether to enable console debug output along with Unity debug window output [deprecated]
	 * It will only output to the Unity debug window on PC.

	 */
	public bool enableDebugPrintOnTerminal = false;

	/** Whether to force crashing on error (for debug only) */
	public bool enableForceCrashOnError = false;

	/** Whether to remove the error handler at scene change */
	public bool dontDestroyOnLoad = true;

	/** Error message */
	public static string errorMessage { get; set; }

	/**
		Error callback delegate
	
	 * A callback delegate that will be called when an error occurs in the CRIWARE native library.
	 * The argument string contains the message in the format "Error ID: Error details".</para>
	
	 */
	public delegate void Callback(string message);

	/**
	 * <summary>Error callback event</summary>

	 * <para header='Description'>A callback event that will be called when an error occurs in the CRIWARE native library. <br/>

	 * If not set, the default log output function defined in this class will be called. <br/>
	 * If you want to write your own rror handling based on the error message,
	 * register a delegate and handle the error inside the callback function. <br/></para>
	 * <para header='Note'>The registered callback may be called at any time while CriWareErrorHandler is active. <br/>
	 * Therefore, be careful not to release the instance of the callback before CriWareErrorHandler .</para>

	 */
	public static event Callback OnCallback = null;

	/**
	 * \deprecated
	 * 削除予定の非推奨APIです。
	 * CriWareErrorHandler.OnCallback event の使用を検討してください。
	 * <summary>Error callback</summary>
	 * <remarks>
	 * <para header='Description'>A callback that will be called when an error occurs in the CRIWARE native library. <br/>
	 * If not set, the default log output function defined in this class will be called. <br/>
	 * If a user defined process based on the error message is preferred,
	 * register a delegate and perform the process inside the callback function. <br/>
	 * Set the callback to null to unregister.</para>
	 * <para header='Note'>The registered callback may be called at any time while CriWareErrorHandler is active. <br/>
	 * Therefore, be careful not to release the instance of the callback before CriWareErrorHandler .</para>
	 * </remarks>
	 */
	 /*
	[Obsolete("CriWareErrorHandler.callback is deprecated. Use CriWareErrorHandler.OnCallback event", false)]
	public static Callback callback = null;
	*/


	/**
		Setting the number of the error message buffers;

	 * <para header='Description'>Sets the maximum number of errors that can be handled within one frame of the application.

	 * If more errors occur within one frame, their log output etc. will be skipped.
	 * The default value is 8.
	 * The valid range is from 0 to 32.

	 */
	public uint messageBufferCounts = 8;


	/* 创建对象时的处理 */
	void Awake() 
	{
		/* 更新初始化计数器 */
		initializationCount++;
		if (initializationCount != 1) {
			/* 不允许多重初始化 */
			GameObject.Destroy(this);
			return;
		}

		/* 初始化错误处理 */
		CRIWARE124812B3();
		CRIWARED6FD5630(enableForceCrashOnError);
		CRIWAREF90E02C5(messageBufferCounts);

		/* 切换到终端的日志输出显示 */
		CRIWARE63F3EA68(enableDebugPrintOnTerminal);

		CRIWARE028EA0DA(ErrorCallbackFromNative);

		/* 设置场景更改后是否仍保留对象 */
		if (dontDestroyOnLoad) {
			DontDestroyOnLoad(transform.gameObject);
		}
	}



	/* Execution Order の設定を確実に有効にするために OnEnable をオーバーライド */
	protected override void OnEnable() {
		base.OnEnable();
		CRIWARE028EA0DA(ErrorCallbackFromNative);
	}

	protected override void OnDisable() {
		base.OnDisable();
		CRIWARE028EA0DA(null);
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	public override void CriInternalUpdate() {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS || UNITY_TVOS)
		if (enableDebugPrintOnTerminal == false){
			/* iOS/Androidの場合、エラーメッセージの出力先が重複してしまうため、*/
			/* ターミナル出力が無効になっている場合のみ、Unity出力を有効にする。*/
			DequeueErrorMessages();
		}
#else
		DequeueErrorMessages();
#endif
	}

	public override void CriInternalLateUpdate() { }

	void OnDestroy() {
		/* 初期化カウンタの更新 */
		initializationCount--;
		if (initializationCount != 0) {
			return;
		}
		CRIWARE028EA0DA(null);
		CRIWARE317CBB7E();

		/* エラー処理の終了処理 */
		CRIWAREA6735A0E();
	}

	/* エラーメッセージのポーリングと出力 */
	private void DequeueErrorMessages()
	{
		string message = null;
		while (true) {
			IntPtr ptr = CRIWARE5F6C6F32();
			if (ptr == IntPtr.Zero) {
				break;
			}
			try {
				message = Marshal.PtrToStringAnsi(ptr);
			} 
			catch (Exception) {
				Debug.LogError("[CRIWARE] Failed to parse error message.");
			}
			finally {
				if (message != null && message != string.Empty) {
					HandleMessage(message);
				}
			}
		}
	}

	private static void HandleMessage(string errmsg)
	{
		if (errmsg == null) {
			return;
		}

		if (OnCallback == null && callback == null) {
			OutputDefaultLog(errmsg);
		} else {
			if (OnCallback != null) {
				OnCallback(errmsg);
			}
			if (callback != null) {
				callback(errmsg);
			}
		}
	}

	/** Default log output */
	private static void OutputDefaultLog(string errmsg)
	{
		if (errmsg.StartsWith("E")) {
			Debug.LogError("[CRIWARE] Error:" + errmsg);
		} else if (errmsg.StartsWith("W")) {
			Debug.LogWarning("[CRIWARE] Warning:" + errmsg);
		} else {
			Debug.Log("[CRIWARE]" + errmsg);
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void ErrorCallbackFunc(string errmsg);

	[AOT.MonoPInvokeCallback(typeof(ErrorCallbackFunc))]
	private static void ErrorCallbackFromNative(string errmsg)
	{
		HandleMessage(errmsg);
	}

	/** Initialization counter */
	private static int initializationCount = 0;

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWAREF90E02C5(uint length);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARE124812B3();

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWAREA6735A0E();

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARE63F3EA68(bool sw);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARE317CBB7E();

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr CRIWARE5F6C6F32();

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARED6FD5630(bool sw);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARE028EA0DA(ErrorCallbackFunc callback);
	#else
	private static void CRIWAREF90E02C5(uint length) { }
	private static void CRIWARE124812B3() { }
	private static void CRIWAREA6735A0E() { }
	private static System.IntPtr criWareUnity_GetFirstError() { return System.IntPtr.Zero; }
	private static void CRIWARE63F3EA68(bool sw) { }
	private static void CRIWARE317CBB7E() { }
	private static System.IntPtr CRIWARE5F6C6F32() { return System.IntPtr.Zero; }
	private static void CRIWARED6FD5630(bool sw) { }
	private static void CRIWARE028EA0DA(ErrorCallbackFunc callback) { }
	#endif
	#endregion
} // end of class

} //namespace CriWare
/** @} */

/* --- end of file --- */
