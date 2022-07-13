/****************************************************************************
 *
 * Copyright (c) CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
#define CRIWAREPLUGIN_SUPPORT_OUTPUTDEVICE_OBSERVER
#endif

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/**
 * \addtogroup CRIWARE_COMMON_CLASS
 * @{
 */

namespace CriWare {


/*
	A component that monitors the connection status of audio output devices.
	监听 音频输出设备 连接状态的组件;

	Use it by adding it to any GameObject. 
	用法: 将其添加到任何 go 身上

	You can monitor the connection status of the audio output device on your smartphone device and acquire the status externally.
	By registering a delegate, you can also receive callbacks when the connection status changes.
	To use this component, it is necessary to initialize the Atom library first.
	---
	可以在 移动设备上 监听 音频输出设备 的连接状态, 并从外部获得 状态信息;
	通过登记 委托, 你还可在 连接状态发生改变时, 接收 callbacks;
	想要使用本组件, 需要先 初始化 atom library;

 	Currently, the functions of this component only work on smartphones (Android / iOS).
 	Please wait for future updates for support for other platforms.
	---
	目前本组函数 仅支持 安卓/ios 移动平台;
	
	"Observer" 观察者

 */
public class CriAtomOutputDeviceObserver : CriMonoBehaviour
{
	
	/**
	 * Types of device to which audio is output from the application.

	 */
	public enum OutputDeviceType 
	{
		BuiltinSpeaker,     /**< Internal Speaker  内置扬声器 */
		WiredDevice,        /**< Wired device (wired headset, etc.) 有线设备 */
		WirelessDevice,     /**< Wireless device (Bluetooth headset, etc.) 无线设备 */
	}


	/**
	 * Connection state change callback delegate type;
	 * This is the type of the callback function that is called when the connection status of the audio output device changes.
	 	当 audio output device 的连接状态发生变化时, 将调用的回调函数 的类型;

	 * <param name='isConnected'>   Output device connection status (false = disconnected, true = connected)</param>
	 * <param name='deviceType'>    Output device type           </param>

	 * <seealso cref='CriAtomOutputDeviceObserver::OnDeviceConnectionChanged'/>
	 */
	public delegate void DeviceConnectionChangeCallback(bool isConnected, OutputDeviceType deviceType);


	/**
	 * Connection state change callback delegate
	
	 * This is a callback function that is called when the connection status of the audio output device changes.
	 * Called from the application's main thread.
	 ---
	 从 app 的主线程调用;

		按照官方技术人员的描述, 此 event 的两个参数中, 只需监听参数2: deviceType 就可以了;
		当设备从 外放 -> 有线耳机, (或者从 有线耳机 -> 外放) 的那一刻, 只会触发一次 event, 
		此时它提供的参数 deviceType 就表示 在新的状态中, 使用的设备类型;

		参数1: isConnected 是个历史遗留问题, 是个旧参数, true 表示已经连接了 有线/无线耳机, false 表示 外放模式;
	
	 * <seealso cref='CriAtomExOutputDeviceObserver::DeviceConnectionChangeCallback'/>
	 */
	public static event DeviceConnectionChangeCallback OnDeviceConnectionChanged {
		add {
			_onDeviceConnectionChanged += value;
			if (instance) {
				value(IsDeviceConnected, DeviceType);
			}
		}
		remove {
			_onDeviceConnectionChanged -= value;
		}
	}


	/**
	 * Gets device connection status
	 * Whether connected (false = disconnected, true = connected)

	 猜测: 如果外联设备类型为 BuiltinSpeaker, 也是要返回 false 的;
	 ( 可能是因为, BuiltinSpeaker 不属于外连设备 ) 
	
	 * Returns whether an audio output device is connected to the machine.
	 * Returns true if the output destination is a device other than the internal speaker.
	 */
	public static bool IsDeviceConnected {
		get {
			if (instance == null) {
				return false;
			}
#if !UNITY_EDITOR && UNITY_IOS
			return UnsafeNativeMethods.criAtomUnity_IsOutputDeviceConnected_IOS();
#elif !UNITY_EDITOR && UNITY_ANDROID
			return instance.isConnected;
#else
			return false;
#endif
		}
	}



	/**
	 * Gets the output device type
	 * Gets the type of the current audio output device.

	返回:
	Output device type	
	 */
	public static OutputDeviceType DeviceType 
	{
		get {
			if (instance == null) {
				return OutputDeviceType.BuiltinSpeaker;
			}
#if !UNITY_EDITOR && UNITY_IOS
			return UnsafeNativeMethods.criAtomUnity_GetOutputDeviceType_IOS();
#elif !UNITY_EDITOR && UNITY_ANDROID
			return instance.deviceType;
#else
			return OutputDeviceType.BuiltinSpeaker;
#endif
		}
	}

	// =============================================== 下面的几乎不用看 ====================================================== //

	#region Internal Members
	[SerializeField] bool dontDestroyOnLoad = false;
	bool lastIsConnected = false;
	bool isConnected = false;
	OutputDeviceType lastDeviceType = OutputDeviceType.BuiltinSpeaker;
	OutputDeviceType deviceType = OutputDeviceType.BuiltinSpeaker;
	static CriAtomOutputDeviceObserver instance = null;
	static event DeviceConnectionChangeCallback _onDeviceConnectionChanged = null;
#if !UNITY_EDITOR && UNITY_ANDROID
	static UnityEngine.AndroidJavaObject checker = null;
#endif
	#endregion

	#region Internal Functions
	private void Awake() {
		if (instance != null) {
			Destroy(this);
			return;
		}

		if (!CriAtomPlugin.IsLibraryInitialized()) {
			Debug.LogError("[CRIWARE] Atom library is not initialized. Cannot setup CriAtomExOutputDeviceObserver.");
			Destroy(this);
			return;
		}

		instance = this;

#if CRIWAREPLUGIN_SUPPORT_OUTPUTDEVICE_OBSERVER
#if !UNITY_EDITOR && UNITY_IOS
		bool isStarted = UnsafeNativeMethods.criAtomUnity_StartOutputDeviceObserver_IOS();
		if (!isStarted) {
			Debug.LogError("[CRIWARE] CriAtomOutputDeviceObserver cannot start while Atom library is not initialized.");
		}
#elif !UNITY_EDITOR && UNITY_ANDROID
		UnityEngine.AndroidJavaClass jc = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer");
		UnityEngine.AndroidJavaObject activity = jc.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity");
		
		if (checker == null) {
			checker = new UnityEngine.AndroidJavaObject("com.crimw.crijavaclasses.CriOutputDeviceObserver", activity, this.gameObject.name, "CallbackFromObserver_ANDROID");
		}
		if (checker == null) {
			Debug.LogError("[CRIWARE] Cannot load CriOutputDeviceObserver class in library.");
		}
		checker.Call("Start", activity);
		CheckOutputDevice_ANDROID();
#endif
		isConnected = lastIsConnected = IsDeviceConnected;
		deviceType = lastDeviceType = DeviceType;
		if (_onDeviceConnectionChanged != null) {
			_onDeviceConnectionChanged(isConnected, deviceType);
		}
#elif !UNITY_EDITOR
		Debug.Log("[CRIWARE] CriAtomOutputDeviceObserver is not supported on this platform.");
#endif
		if (this.dontDestroyOnLoad) {
			GameObject.DontDestroyOnLoad(this.gameObject);
		}
	}


	private void OnDestroy() {
		if (instance != this) {
			return;
		}
		instance = null;

#if CRIWAREPLUGIN_SUPPORT_OUTPUTDEVICE_OBSERVER
#if !UNITY_EDITOR && UNITY_IOS
		UnsafeNativeMethods.criAtomUnity_StopOutputDeviceObserver_IOS();
#elif !UNITY_EDITOR && UNITY_ANDROID
		UnityEngine.AndroidJavaClass jc = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer");
		UnityEngine.AndroidJavaObject activity = jc.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity");
		if (activity != null && checker != null) {
			checker.Call("Stop", activity);
		}
		checker = null;
#endif
#endif
	}


	public override void CriInternalUpdate() {
		isConnected = IsDeviceConnected;
		deviceType = DeviceType;

		if ((isConnected != lastIsConnected ||
			deviceType != lastDeviceType) &&
			_onDeviceConnectionChanged != null) {
			_onDeviceConnectionChanged(isConnected, deviceType);
		}
		lastIsConnected = isConnected;
		lastDeviceType = deviceType;
	}


	public override void CriInternalLateUpdate() {
	}

#if !UNITY_EDITOR && UNITY_ANDROID
	/* [ANDROID] Callback from CriOutputDeviceObserver class */
	private void CallbackFromObserver_ANDROID(string message) {
		if (message[0] == 'a') {
			CheckOutputDevice_ANDROID();
		} else if (message[0] == 'b') {
			StartCoroutine("CoroutineForCheck_ANDROID");
		}
	}

	private void CheckOutputDevice_ANDROID() {
		if (checker == null) {
			return;
		}

		UnityEngine.AndroidJavaClass jc = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer");
		UnityEngine.AndroidJavaObject activity = jc.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity");
		int device = checker.Call<int>("CheckOutputDeviceType", activity);
		deviceType = (OutputDeviceType)device;
		isConnected = (deviceType != OutputDeviceType.BuiltinSpeaker);

	}

	private IEnumerator CoroutineForCheck_ANDROID() {
		const float waitSec = 2.0f;
		float time = 0.0f;
		while (time < waitSec) {
			yield return null;
			time += Time.deltaTime;
		}
		CheckOutputDevice_ANDROID();
	}

#endif
	#endregion

	#region Dll Import
	private static class UnsafeNativeMethods
	{
#if !CRIWARE_ENABLE_HEADLESS_MODE
#if !UNITY_EDITOR && UNITY_IOS
		[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
		internal static extern bool criAtomUnity_StartOutputDeviceObserver_IOS();
		[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
		internal static extern void criAtomUnity_StopOutputDeviceObserver_IOS();
		[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
		internal static extern bool criAtomUnity_IsOutputDeviceConnected_IOS();
		[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
		internal static extern OutputDeviceType criAtomUnity_GetOutputDeviceType_IOS();
#endif
#else
#if !UNITY_EDITOR && UNITY_IOS
		internal static bool criAtomUnity_StartOutputDeviceObserver_IOS() { return false; }
		internal static void criAtomUnity_StopOutputDeviceObserver_IOS() { }
		internal static bool criAtomUnity_IsOutputDeviceConnected_IOS() { return false; }
		internal static OutputDeviceType criAtomUnity_GetOutputDeviceType_IOS() { return OutputDeviceType.BuiltinSpeaker; }
#endif
#endif
	}
	#endregion

} // end of class

} //namespace CriWare
/** @} */
/* end of file */
