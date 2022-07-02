/****************************************************************************
 *
 * Copyright (c) 2011 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/*==========================================================================
 *      CRI Atom Native Wrapper
 *=========================================================================*/
/**
 * \addtogroup CRIATOM_NATIVE_WRAPPER
 * @{
 */

namespace CriWare {



/**
	AtomExPlayer

		在那个 CriAtomSource 中, 存储了对应的 player 实例, 可以直接拿出来用;
		source 和 player 实例是一一对应的;

	A player class used for sound playback control.
	Performs control such as setting data, starting playback and getting status.

 */
public class CriAtomExPlayer : CriDisposable
{
	public IntPtr nativeHandle {get {return this.handle;} }
	public bool isAvailable {get {return this.handle != IntPtr.Zero;} }
	private event CriAtomExBeatSync.CbFunc _onBeatSyncCallback = null;



	/**
	Registers the beat synchronization callback;   BeatSync: 节拍同步;

	Register a callback function that receives the beat sync position information embedded in the Cue being played by the player.
	The registered callback function is executed when the application's main thread is updated, immediately after processing the callback event.
	---

	 */
	public event CriAtomExBeatSync.CbFunc OnBeatSyncCallback 
	{
		add {
			if (_onBeatSyncCallback == null) {
				CriAtom.OnBeatSyncCallback += OnBeatSyncCallbackChainInternal;
			}
			_onBeatSyncCallback += value;
		}
		remove {
			_onBeatSyncCallback -= value;
			if (_onBeatSyncCallback == null) {
				CriAtom.OnBeatSyncCallback -= OnBeatSyncCallbackChainInternal;
			}
		}
	}

	private event CriAtomExSequencer.EventCallback _onSequenceCallback = null;
	
	/**
		Registers the Sequence event callback;

		Registers a callback function to be executed when the callback marker embedded in a Cue is reached (during playback on the player).
		The registered callback function is executed when the application's main thread is updated, immediately after processing the callback event.
		---
		当 cue 正在被播放, 然后到达时间轨道的一个 marker, 这个 marker 上登记的一个 回调函数就会被调用;
		调用时间: 当 app 的主线程已经 updated, 然后执行了 callback event 之后的一瞬间;
	 */
	public event CriAtomExSequencer.EventCallback OnSequenceCallback
	{
		add {
			if (_onSequenceCallback == null) {
				CriAtomExSequencer.OnCallback += OnSequenceCallbackChainInternal;
			}
			_onSequenceCallback += value;
		}
		remove {
			_onSequenceCallback -= value;
			if (_onSequenceCallback == null) {
				CriAtomExSequencer.OnCallback -= OnSequenceCallbackChainInternal;
			}
		}
	}

	private bool hasExistingNativeHandle = false;
	private IntPtr entryPoolHandle = IntPtr.Zero;
	private int _entryPoolCapacity = 0;
    private int max_path = 0;

	/**
		Player status;   ---   player 的状态;
	 
		A value indicating the playback status of the AtomExPlayer;
		It can get using the "::CriAtomExPlayer::GetStatus()" function.

	 * The playback status usually changes in the following order.<br/>
	 * -# Stop
	 * -# Prep
	 * -# Playing
	 * -# PlayEnd
	
	 * The status immediately after creating the AtomExPlayer is Stop.

	 * When you set the data using the ::CriAtomExPlayer::SetCue() function etc.,
	 * and call the ::CriAtomExPlayer::Start function, the status changes to playback preparation state ( Prep ) to prepare playback.

	 * When enough data is supplied and the playback is ready, the status changes to ( Playing ) and the sound output starts.

	 * When all the data set was played back, the status changes to
	 * playback completed ( PlayEnd );
	---
	-1- AtomExPlayer 实例一旦创建完毕, 状态为 stop;
	-2- 当调用 CriAtomExPlayer::SetCue(), 和 CriAtomExPlayer::Start(), 状态切换为 playback;
	-3- 当准备了足够的数据, 状态切换为 playing, 然后发出声音;
	-4- 当数据都播放完毕, 状态切换为 playend;

	注意:
		当一个 player 同时播放多个声音时:

	 * One AtomExPlayer can play multiple sounds.
	 * If you call the ::CriAtomExPlayer::Start function for the AtomExPlayer being played,
	 * the two sounds are played back at the same time.
	 * If the ::CriAtomExPlayer::Stop function is called during playback,
	 * all sounds currently being played in the AtomExPlayer are stopped and the status returns to Stop.
	 * (Depending on the timing of calling the ::CriAtomExPlayer::Stop function,
	 * it may take some time to change to Stop.)
	 ---
	 一个 player 可同时播放数个声音, 如果调用 "::CriAtomExPlayer::Start()", 数个声音会一起播放;
	 如果在播放时调用 "::CriAtomExPlayer::Stop()", 本 player 的所有正在播放的声音 都会停止, 然后状态会改为 stop;
	 ( 在某些时间调用 Stop() 函数时, 可能会消耗一点时间来将状态更改为 stop )
	
	 * When the ::CriAtomExPlayer::Start function is called multiple times for one AtomExPlayer,
	 * the status changes to Prep if at least one sound is being prepared.<br/>
	 * (The status does not change to Playing until all the sounds
	 * are playing.)<br/>
	 * If you call the ::CriAtomExPlayer::Start function again for the player
	 * in Playing state, the status temporarily returns to Prep.<br/>
	 ---
	 若对着一个 player 调用数次 Start() 函数, 只要有一个声音准备好了, 状态就会切换为 prep;
	 (直到所有声音都开始播放了, 状态才会切换为 playing)
	 
	 若对着一个 playing 状态的 player 再次调用 start(), 状态会临时回到 prep; 
	
	 * If invalid data is read during playback or file access fails,
	 * the status changes to Error.<br/>
	 * If an error occurs in a sound while playing multiple sounds, the player status
	 * changes to Error regardless of the status of other sounds.<br/></para>
	 ---
	 在播放多个声音时, 若一个声音状态变为 error, 则这个 player 的状态也变为 error, 哪怕其他声音都是正常的;
	
	 * <seealso cref='CriAtomExPlayer::GetStatus'/>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer'/>
	 */
	public enum Status 
	{
		Stop = 0,       /**< Stopped */
		Prep,           /**< Preparing for playback */
		Playing,        /**< Playing */
		PlayEnd,        /**< Playback completed */
		Error,          /**< Error occurred */
	}


	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	private struct Config 
	{
		public CriAtomEx.VoiceAllocationMethod voiceAllocationMethod;
		public int maxPathStrings;
		public int maxPath;
		public int maxAisacs;
		public bool updatesTime;
		public bool enableAudioSyncedTimer;
	}

	/**
	Creates an AtomExPlayer  -- 构造函数;

	 * The steps for playing back the sound data using the created AtomExPlayer are as follows:
	 * -# Use the ::CriAtomExPlayer::SetCue function to set the data to be played in the AtomExPlayer.
	 * -# Call the ::CriAtomExPlayer::Start function to start playback.<br/></para>
	 * The library must be initialized before calling this function.

	 * When playing back a sound file that is not packed by specifying it in the ::CriAtomExPlayer::SetFile() function,
	 * ::CriAtomExPlayer::CriAtomExPlayer(int, int) function must be used instead of this function.
	 ---
	 如果要播放的声音文件 没有用 CriAtomExPlayer::SetFile() 打包时, 
	 此时改改用 CriAtomExPlayer(int, int) 构造函数, 而不是本函数;


	 * This function is a return-on-complete function. -- 完成时返回函数
	 * The time it takes for creating the AtomExPlayer depends on the platform.
	 * If this function is called at a timing when the screen needs to be updated such as in a game loop,
	 * the process is blocked in the unit of milliseconds, resulting in dropped frames.
	 * Create/discard AtomExPlayers at a timing when load fluctuations(波动) is accepted
	 * such as when switching scenes.
	 ---
	 按照平台的不同, 调用本函数消耗的时间也不同, 若在 屏幕被更新时调用本函数, 会阻塞数毫秒, 导致帧丢失;
	 可以在 场景切换阶段 创建/discard player 对象;

	 * <seealso cref='CriAtomExPlayer::Dispose'/>
	 * <seealso cref='CriAtomExPlayer::SetCue'/>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(int, int)'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(bool)'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(int, int, bool)'/>
	 */
	public CriAtomExPlayer() : this(0, 0, false, IntPtr.Zero)
	{
	}

	/**
		Creates an AtomExPlayer (for single file playback)

		参数:
	 		maxPath 		-- Maximum path string length
	 		maxPathStrings 	-- The number of files that can be played simultaneously (同时)
	
	 
	 * The basic specification is the same as the ::CriAtomExPlayer::CriAtomExPlayer() function,
	 * except that a single file can be played.

	 * The AtomExPlayer created by this function
	 * allocates maxPathxmaxPathStrings memory areas for single file playback.
	 * This area is not used when playing back ACB or AWB files.
	 ---
	 分配内存, 在播放 acb, awb 文件时这个分配的内存不会被使用到;

	 * When the ::CriAtomExPlayer::SetFile function is not used, it is possible to
	 * save memory usage by using the ::CriAtomExPlayer::CriAtomExPlayer()
	 * function instead of this function.
	 --
	 若不需要用 SetFile(), 则该改用另一个 构造函数

	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer()'/>
	 */
	public CriAtomExPlayer(int maxPath, int maxPathStrings) : this(maxPath, maxPathStrings, false, IntPtr.Zero)
	{
	}

	/**
	Creates an AtomExPlayer (using sound synchronization timer)
	Create an AtomExPlayer which supports sound synchronization

	参数:
		enableAudioSyncedTimer -- Sound synchronization timer enabled flag 启用声音同步计时器;
	
	 * The basic specification is the same as the ::CriAtomExPlayer::CriAtomExPlayer() function
	 * except that the playback time synchronized with the sound can be obtained.
	---
	除了可以获得与 声音同步的播放时间。

	 * For the sound played using the AtomExPlayer created by specifying True for
	 * the argument of this function, the time is updated in sync with the number of played samples;
	 ---
	 若使用参数 true, 时间 与播 放的样本数 同步更新；

	 * When the ::CriAtomExPlayback::GetTimeSyncedWithAudio() function is not used,
	 * it is possible to suppress the increase in load by specifying False
	 * in the argument of this function or by using ::CriAtomExPlayer::CriAtomExPlayer()
	 * instead of this function.
	 ---
	 若不会用到 GetTimeSyncedWithAudio(), 则该传入 false, 或用另一个 构造函数;

	 * For the AtomExPlayer created by specifying True for the argument of this function,
	 * the playback pitch cannot be changed using the ::CriAtomExPlayer::SetPitch function.
	 ---
	 若传入参数 true, 不能调用 SetPitch() 来修改 playback pitch;
	
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer()'/>
	 * <seealso cref='CriAtomExPlayback::GetTimeSyncedWithAudio()'/>
	 */
	public CriAtomExPlayer(bool enableAudioSyncedTimer) : this(0, 0, enableAudioSyncedTimer, IntPtr.Zero)
	{
	}

	/**
	Creates an AtomExPlayer (single file playback, using sound synchronization timer)

	 * <param name='maxPath'>Maximum path string length</param>
	 * <param name='maxPathStrings'>The number of files that can be played simultaneously</param>
	 * <param name='enableAudioSyncedTimer'>Sound synchronization timer enabled flag</param>

	
	 * Create an AtomExPlayer for single file playback.
	 * By specifying the flag to enableAudioSyncedTimer, it is possible to get the playback time synchronized with the sound.
	
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer()'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(int, int)'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(bool)'/>
	 */
	public CriAtomExPlayer(int maxPath, int maxPathStrings, bool enableAudioSyncedTimer) : this(maxPath, maxPathStrings, enableAudioSyncedTimer, IntPtr.Zero)
	{
	}

	public CriAtomExPlayer(IntPtr existingNativeHandle) : this(0, 0, false, existingNativeHandle)
	{
	}

	public CriAtomExPlayer(int maxPath, int maxPathStrings, bool enableAudioSyncedTimer, IntPtr existingNativeHandle)
	{
		//  初始化库  
		if (!CriAtomPlugin.IsLibraryInitialized()) {
			throw new Exception("CriAtomPlugin is not initialized.");
		}

		// 创建句柄 
		Config config;
		config.voiceAllocationMethod = CriAtomEx.VoiceAllocationMethod.Once;
		config.maxPath = maxPath;
		config.maxPathStrings = maxPathStrings;
		config.enableAudioSyncedTimer = enableAudioSyncedTimer;
		// 默认值 
		config.maxAisacs = 8; // 最多 8 个 aisac 值;
		config.updatesTime = true;

		// 是否已传递现有（外部创建）CriAtomExPlayerHn？ 
		hasExistingNativeHandle = (existingNativeHandle != IntPtr.Zero);
		if (hasExistingNativeHandle) {
			this.handle = existingNativeHandle;
		} else {
			// 新建 CriAtomExPlayerHn
			this.handle = criAtomExPlayer_Create(ref config, IntPtr.Zero, 0);
            this.max_path = config.maxPath;
		}

		CriDisposableObjectManager.Register(this, CriDisposableObjectManager.ModuleType.Atom);
	}



	/**
	Discards an AtomExPlayer

	
	 * When this function is called, all the resources allocated in the DLL when creating the AtomExPlayer are released.

	 * This function is a return-on-complete function. 完成后再返回, 意味着会阻塞;
	 * If you try to discard an AtomExPlayer that is playing sound, this function waits for
	 * the playback to be stopped and then releases the resources.
	 * (If the playback is from a file, it also waits for the completion of reading.)
	 * Therefore, the process may be blocked in this function for a long time (for several frames).
	 * Create/discard AtomExPlayers at a timing when load fluctuations波动 is accepted such as when switching scenes.
	 ---
	 如果要 discard 的 player 正在播放, 本函数会等到 播放被停止后 再释放资源; 若从一个文件播放, 本函数会等到这个 文件的读操作完成后再执行;
	 所有调用本函数可能会阻塞很长时间, 数帧;

	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer'/>
	 */
	public override void Dispose()
	{
		CriDisposableObjectManager.Unregister(this);
		if (this.entryPoolHandle != IntPtr.Zero) {
			this.StopWithoutReleaseTime();
			CRIWAREC97D3429(this.entryPoolHandle);
		}
		this.entryPoolHandle = IntPtr.Zero;
		this._entryPoolCapacity = 0;
		if (hasExistingNativeHandle == false && isAvailable) {
			// 新建CriAtomExPlayerHn时 
			// 删除句柄 
			criAtomExPlayer_Destroy(this.handle);
		}
		if (_onBeatSyncCallback != null) {
			_onBeatSyncCallback = null;
			CriAtom.OnBeatSyncCallback -= OnBeatSyncCallbackChainInternal;
		}
		this.handle = IntPtr.Zero;
		GC.SuppressFinalize(this);
	}


	/**
	Sets the sound data (Cue name specified)

	参数:
		acb -- ACB object</param>
		name -- Cue name</param>
	
	 * Associates the Cue name with the AtomExPlayer.
	 * After specifying the Cue name using this function, when you start the playback
	 * using the ::CriAtomExPlayer::Start function, the specified Cue is played.

	 * When a Cue is set using the ::CriAtomExPlayer::SetCue function,
	 * the parameters set with the following functions are ignored.
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate
	 * (Information such as sound format, number of channels, sampling rate, etc.
	 * is automatically set based on the information in the ACB file.)
	 ---
	 调用本函数后, 那几个 set 函数设置的参数将被忽略, 因为 acb 文件自己就有这些参数;
	
	示范代码:
	 *  // 创建播放器
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 注册ACF文件
	 *  CriAtomEx.RegisterAcf(null, "sample.acf");
	 *  // 加载ACB文件
	 *  CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, "sample.acb", "sample.awb");
	 *  // 指定要播放的队列的名称
	 *  player.SetCue(acb, "gun_shot");
	 *  // 播放设置的声音数据
	 *  player.Start();

	 * Once set, the information on the data is retained in the AtomExPlayer until other data is set.
	 * Therefore, when playing the same data many times, it is not necessary to reset the data for each playback.
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)
	 ---
	 调用本函数一次, 设置的数据将一直保留, 直到下次改设别的 cue;
	 所以在此期间, 可调用多次 start() 函数;

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetCue(CriAtomExAcb acb, string name)
	{
		criAtomExPlayer_SetCueName(this.handle,
			(acb != null) ? acb.nativeHandle : IntPtr.Zero, name);
	}


	/**
		Sets the sound data (Cue ID specified)

	 * <param name='acb'>ACB object</param>
	 * <param name='id -- Cue ID
	 
	 * Associates the Cue ID with the AtomExPlayer.<br/>
	 * After specifying the Cue ID using this function, when you start the playback
	 * using the ::CriAtomExPlayer::Start function, the specified Cue is played.

	 * When a Cue is set using the ::CriAtomExPlayer::SetCue function,
	 * the parameters set with the following functions are ignored.<br/>
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate
	 * (Information such as sound format, number of channels, sampling rate, etc.
	 * is automatically set based on the information in the ACB file.)
	
	示范代码:
	 *  // 创建播放器
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 注册ACF文件
	 *  CriAtomEx.RegisterAcf(null, "sample.acf");
	 *  // 加载ACB文件
	 *  CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, "sample.acb", "sample.awb");
	 *  // 指定要播放的队列的名称
	 * player.SetCue(acb, 100);
	 *  // 播放设置的声音数据
	 *  player.Start();


	 * Once set, the information on the data is retained in the AtomExPlayer until other data is set.
	 * Therefore, when playing the same data many times, it is not necessary to reset the data for each playback.
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetCue(CriAtomExAcb acb, int id)
	{
		criAtomExPlayer_SetCueId(this.handle,
			(acb != null) ? acb.nativeHandle : IntPtr.Zero, id);
	}


	/**
	Sets the sound data (Cue index specified)

	 * <param name='acb'>ACB object</param>
	 * <param name='index -- Cue index
	
	
	 * After specifying the Cue index using this function, when you start the playback
	 * using the ::CriAtomExPlayer::Start function, the specified Cue is played.</para>

	 * When a Cue is set using the ::CriAtomExPlayer::SetCueIndex function,
	 * the parameters set with the following functions are ignored.<br/>
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate
	 *  .
	 * (Information such as sound format, number of channels, sampling rate, etc.
	 * is automatically set based on the information in the ACB file.)

	 * By using this function, it is possible to set sound to the player without specifying the Cue name or Cue ID.
	 * (Even if you don't know the Cue name or Cue ID, the contents in the ACB file can be played back, so it can be used for debugging purposes.)
	 ---
	 猜测是通过 cue 的idx 来播放, 常用于 debug, 比如逐个播放列表中所有 cue;

	示范代码:
	 *  // 创建播放器
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 注册ACF文件
	 *  CriAtomEx.RegisterAcf(null, "sample.acf");
	 *  // 加载ACB文件
	 *  CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, "sample.acb", "sample.awb");
	 *  // 指定要播放的队列的索引
	 *  player.SetCueIndex(acb, 300);
	 *  // 播放设置的声音数据
	 *  player.Start();


	 * Once set, the information on the data is
	 * retained in the AtomExPlayer until other data is set.
	 * Therefore, when playing the same data many times,
	 * it is not necessary to reset the data for each playback.
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetCueIndex(CriAtomExAcb acb, int index)
	{
		criAtomExPlayer_SetCueIndex(this.handle,
			(acb != null) ? acb.nativeHandle : IntPtr.Zero, index);
	}



	/**
		Sets the sound data (CPK content ID specified)

	 * <param name='binder    -- Binder
	 * <param name='contentId -- Content ID
	
	 * Associates the content with the AtomExPlayer. -- 将内容与AtomExPlayer关联

	 * Used to play the content file in the CPK file by specifying the ID using the CRI File System library.
	 * By specifying the binder and content ID for the player
	 * using this function and calling the ::CriAtomExPlayer::Start function,
	 * the specified content file is streamed.

	 * Note that the file is not loaded when this function is called.
	 * Loading of the file starts after calling the ::CriAtomExPlayer::Start function.
	---
	调用本函数不会加载文件内容, 只有调用 start() 后才会;

	 * When setting the sound data using the ::CriAtomExPlayer::SetContentId function,
	 * it is necessary to separately specify the information of the sound data to be played using the following functions.
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate
	 ----
	 和 acb 文件不同, cpk 文件需要再手动设置这些 参数
	
	示范代码:


	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 创建绑定
	 *  CriFsBinder binder = new CriFsBinder();
	 *  // 开始绑定CPK文件
	 *  CriFsBindRequest bindRequest = CriFsUtility.BindCpk(binder, "sample.cpk");
	 *  // バインドの完了を待つ
	 *  yield return bindRequest.WaitForDone(this);
	 *  // 设置音频文件
	 *  // sample.设置cpk中的第一个内容
	 *  player.SetContentId(binder, 1);     ----- 本函数
	 *  // 指定要播放的声音数据的格式
	 *  player.SetFormat(CriAtomEx.Format.ADX);
	 *  player.SetNumChannels(2);
	 *  player.SetSamplingRate(44100);
	 *  // セットされた音声データを再生
	 *  plaeyr.Start();
	

	 * Once set, the information on the file is
	 * retained in the AtomExPlayer until other data is set.
	 * Therefore, when playing the same data many times,
	 * it is not necessary to reset the data for each playback.
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::SetFormat'/>
	 * <seealso cref='CriAtomExPlayer::SetNumChannels'/>
	 * <seealso cref='CriAtomExPlayer::SetSamplingRate'/>
	 */
	public void SetContentId(CriFsBinder binder, int contentId)
	{
		criAtomExPlayer_SetContentId(this.handle,
			(binder != null) ? binder.nativeHandle : IntPtr.Zero, contentId);
	}


	/**
	Sets the sound data (file name specified)

	 * <param name='binder -- Binder object<
	 * <param name='path   -- File path
 
	 * Associates the sound file with the AtomExPlayer. -- 将内容与AtomExPlayer关联

	 * After specifying a file using this function, the file is streamed
	 * by calling the ::CriAtomExPlayer::Start function to start playback.
	 * Note that the file is not loaded when this function is called.
	 * Loading of the file starts after calling the ::CriAtomExPlayer::Start function.

	 * When using this function, it is necessary to create an AtomExPlayer for single file playback.
	 * Specifically, ::CriAtomExPlayer::CriAtomExPlayer(int, int) must be used
	 * instead of ::CriAtomExPlayer::CriAtomExPlayer().
	 ---
	 新建一个 player 来使用本函数;

	 * When setting the sound data using the ::CriAtomExPlayer::SetFile function,
	 * it is necessary to separately specify the information of the sound data to be played using the following functions.<br/>
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate</para>


	 * <example><code>
	 *      :
	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 音声ファイルをセット
	 *  player.SetFile(null, "sample.hca");
	 *  // 再生する音声データのフォーマットを指定
	 *  player.SetFormat(CriAtomEx.Format.HCA);
	 *  player.SetNumChannels(2);
	 *  player.SetSamplingRate(48000);
	 *
	 *  // セットされた音声データを再生
	 *  player.Start();


	 * </code>Once set, the information on the file is
	 * retained in the AtomExPlayer until other data is set.<br/>
	 * Therefore, when playing the same data many times,
	 * it is not necessary to reset the data for each playback.<br/>
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)</example>

	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(int, int)'/>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::SetFormat'/>
	 * <seealso cref='CriAtomExPlayer::SetNumChannels'/>
	 * <seealso cref='CriAtomExPlayer::SetSamplingRate'/>
	 */
	public void SetFile(CriFsBinder binder, string path)
	{
		criAtomExPlayer_SetFile(this.handle,
			(binder != null) ? binder.nativeHandle : IntPtr.Zero, path);
	}



	/**
	Sets the sound data (on-memory byte array specified)   内存中的字节数组;

	 * <param name='buffer'>Byte array</param>
	 * <param name='size'>Buffer size</param>

	 * Associates the sound data placed on the memory with the AtomExPlayer.
	 * After specifying the memory address and size using this function, when you start playback
	 * using the ::CriAtomExPlayer::Start function, the specified data is played.

	 * <para header='Note'>When using this function, it is necessary to create "an AtomExPlayer for single file playback".
	 * Specifically, ::CriAtomExPlayer::CriAtomExPlayer(int, int) must be used
	 * instead of ::CriAtomExPlayer::CriAtomExPlayer().<br/>

	 * When setting the sound data using the ::CriAtomExPlayer::SetData function,
	 * it is necessary to separately specify the information of the sound data to be played using the following functions.<br/>
	 *  - ::CriAtomExPlayer::SetFormat
	 *  - ::CriAtomExPlayer::SetNumChannels
	 *  - ::CriAtomExPlayer::SetSamplingRate</para>
	 * <para header='Note'>The buffer address passed to SetData should be fixed beforehand by the application
	 * so that it is not moved by the garbage collector.<br/></para>


	 * <example><code>
	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 音声ファイルをセット
	 *  player.SetData(null, buffer, size);
	 *  // 再生する音声データのフォーマットを指定
	 *  player.SetFormat(CriAtomEx.Format.HCA);
	 *  player.SetNumChannels(2);
	 *  player.SetSamplingRate(48000);
	 *  // セットされた音声データを再生
	 *  player.Start();


	 * </code>Once set, the information on the file is
	 * retained in the AtomExPlayer until other data is set.<br/>
	 * Therefore, when playing the same data many times,
	 * it is not necessary to reset the data for each playback.<br/>
	 * (You can call the ::CriAtomExPlayer::Start function repeatedly.)</example>

	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(int, int)'/>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::SetFormat'/>
	 * <seealso cref='CriAtomExPlayer::SetNumChannels'/>
	 * <seealso cref='CriAtomExPlayer::SetSamplingRate'/>
	 */
	public void SetData(byte[] buffer, int size)
	{
		criAtomExPlayer_SetData(this.handle,
			buffer, size);
	}


	/**
	 * Sets the sound data (on-memory buffer address specified)

	 * <param name='buffer'>Buffer address</param>
	 * <param name='size'>Buffer size</param>
	 * <remarks>
	 * <para header='Description'>For details, refer to ::CriAtomExPlayer::SetData(byte[], int).</para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::SetData(byte[], int)'/>
	 */
	public void SetData(IntPtr buffer, int size) {
		criAtomExPlayer_SetData(this.handle,
			buffer, size);
	}


	/**
	 * Specifies the format

	 * <param name='format'>Format</param>

	 * Specifies the sound format to be played by the AtomExPlayer.
	 * When the sound is played using the ::CriAtomExPlayer::Start function, the AtomExPlayer
	 * gets a Voice that can play the data with the format specified by this function from the Voice Pool.

	 * The default before calling the function is the ADX format.

	 * This function needs to be set only when playing sound without using the ACB file.
	 * When playing a Cue, the format is automatically acquired from the Cue Sheet,
	 * so it is not necessary to call this function separately.
	 ---
	 当使用 acb 文件, cue 时, 无需设置本变量;

	 */
	public void SetFormat(CriAtomEx.Format format)
	{
		criAtomExPlayer_SetFormat(this.handle, format);
	}


	/**
	 * Specifies the number of channels
	 * <param name='numChannels'>The number of channels</param>

	 * Specifies the number of sound channels to be played by the AtomExPlayer.
	 * When the sound is played using the ::CriAtomExPlayer::Start function, the AtomExPlayer
	 * gets a Voice that can play the data with the channel count specified by this function from the Voice Pool.<br/>
	 * The default before calling the function is 2 channels.<br/></para>

	 * This function needs to be set only when playing sound without using the ACB file.
	 * When playing a Cue, the format is automatically acquired from the Cue Sheet,
	 * so it is not necessary to call this function separately.<br/></para>

	 */
	public void SetNumChannels(int numChannels)
	{
		criAtomExPlayer_SetNumChannels(this.handle, numChannels);
	}



	/**
	 * Specifies the sampling rate<

	 * <param name='samplingRate'>Sampling rate</param>

	 * Specifies the sampling rate of the sound played by the AtomExPlayer.
	 * When the sound is played using the ::CriAtomExPlayer::Start function, the AtomExPlayer
	 * gets a Voice that can play the data with the sampling rate specified by this function from the Voice Pool.

	 * The default before calling the function is 32000Hz.

	 * This function needs to be set only when playing sound without using the ACB file.<br/>
	 * When playing a Cue, the format is automatically acquired from the Cue Sheet,
	 * so it is not necessary to call this function separately.<br/></para>
	 */
	public void SetSamplingRate(int samplingRate)
	{
		criAtomExPlayer_SetSamplingRate(this.handle, samplingRate);
	}


	/**
	 * Creates an entry pool for concatenated playback -- 创建用于 连接播放 的 入口池;

	 * <param name='capacity'>		The number of data that can be input;
	 * <param name='stopOnEmpty'>	Whether to stop if the entry pool is empty; 	若 entry pool 为空, 是否 stop;

	 * Creates an entry pool for linked playback using the functions such as ::CriAtomExPlayer::EntryData();

	 注意:
	 * Calling this function puts the player in linked playback mode.
	 * If the stopOnEmpty flag is set to False, even if the data supplied by the ::CriAtomExPlayer::SetData
	 * or ::CriAtomExPlayer::EntryData reached its end, the player stays in playback state and waits for the next data input.<br/>
	 * If set to True, the player stops playback when the supplied data is exhausted,
	 * so make sure to feed data with a margin.
	 ---
	若参数 stopOnEmpty 设为 false, 即使在 "使用 SetData() 或 EntryData() 提供的数据 到达了结尾", player 的状态将继续为 playback,
	然后等着下一个数据的 input;

	若参数 stopOnEmpty 设为 true, 那么在输入的数据使用完毕后, player 状态进入 stop, 所以一定要给数据留一个空白。

	 *
	 */
	public void PrepareEntryPool(int capacity, bool stopOnEmpty)
	{
		if (this.entryPoolHandle != IntPtr.Zero) {
			CRIWAREC97D3429(this.entryPoolHandle);
			this._entryPoolCapacity = 0;
			this.entryPoolHandle = IntPtr.Zero;
		}

		if (capacity <= 0)
			return;

		this.entryPoolHandle = CRIWARE01E006CC(this.handle, capacity, this.max_path, stopOnEmpty);
		if (this.entryPoolHandle != IntPtr.Zero) {
			this._entryPoolCapacity = capacity;
		}
	}


	/**
	 * The number of concatenated playback data entries;  串联播放数据项 的数量；

	 * 返回值: Number of entries
	
	 * Gets the number of entries entered for concatenated playback.
	
	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 * <seealso cref='CriAtomExPlayer::EntryData'/>
	 */
	public int GetNumEntries()
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return 0;
		}
		return CRIWARE2DCE75C8(this.entryPoolHandle);
	}


	/**
	 * Number of entries "consumed by the playback process" -- 播放过程消耗的条目数

	 * 返回值: Number of entries
	
	 * Gets the number of entries consumed by the playback process when fetching concatenated playback data to the player.
	 * This number will increase each time an entry is consumed during playback, and it will be reset when CriAtomExPlayer::Start is called.
	 * It does not increase if the same entry is being played in a loop.
	 ---
	 当同一个 entry 循环播放时, 本值不会变大;

	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 * <seealso cref='CriAtomExPlayer::EntryData'/>
	 */
	public int GetNumConsumedEntries()
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return 0;
		}
		return CRIWAREB4B05A20(this.entryPoolHandle);
	}


	/**
	 * The number of concatenated playback data that can be input  ---  可以输入的 串联播放数据 的数量
	 */
	public int entryPoolCapacity { get { return _entryPoolCapacity; } }


	/**
	 * Inputs the data for linked playback (file name specified)

	 * <param name='binder'>	-- Binder object</param>
	 * <param name='path'>		-- File path</param>
	 * <param name='repeat'>	-- Whether to repeat playback when the next data is not entered

	 * 返回: Whether the data was successfully entered.

	 * Inputs a sound file for linked playback.
	 * When performing linked playback, set the start data using the ::CriAtomExPlayer::SetData function etc.
	 * start playback, then input additional playback data using this function.
	
	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 * <seealso cref='CriAtomExPlayer::EntryContentId'/>
	 * <seealso cref='CriAtomExPlayer::EntryData'/>
	 */
	public bool EntryFile(CriFsBinder binder, string path, bool repeat)
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return false;
		}
		return CRIWAREC15C851D(this.entryPoolHandle,
								(binder != null) ? binder.nativeHandle : IntPtr.Zero,
								path,
								repeat,
								this.max_path);
	}



	/**
	 * <summary>Inputs the data for linked playback (CPK content ID specified)
	 * <param name='binder'>Binder object</param>
	 * <param name='contentId'>Content ID</param>
	 * <param name='repeat'>Whether to repeat playback when the next data is not entered</param>
	 * <returns>Whether the data was successfully entered.</returns>
	 * <remarks>
	 * <para header='Description'>Enter the content for linked playback.<br/>
	 * Used to play the content file in the CPK file by specifying the ID
	 * using the CRI File System library.<br/>
	 * When performing linked playback, set the start data using the ::CriAtomExPlayer::SetData function etc.
	 * start playback, then input additional playback data using this function.</para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 * <seealso cref='CriAtomExPlayer::EntryFile'/>
	 * <seealso cref='CriAtomExPlayer::EntryData'/>
	 */
	public bool EntryContentId(CriFsBinder binder, int contentId, bool repeat)
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return false;
		}
		return CRIWAREC08B2D9C(this.entryPoolHandle,
								(binder != null) ? binder.nativeHandle : IntPtr.Zero,
								contentId,
								repeat);
	}

	/**
	 * <summary>Enters the concatenated playback data (on-memory byte array specified)
	 * <param name='buffer'>On-memory byte array</param>
	 * <param name='size'>Buffer size</param>
	 * <param name='repeat'>Whether to repeat playback when the next data is not entered</param>
	 * <returns>Whether the data was successfully entered.</returns>
	 * <remarks>
	 * <para header='Description'>Inputs the data for linked playback.<br/>
	 * When performing linked playback, set the start data using the ::CriAtomExPlayer::SetData function etc.
	 * start playback, then input data using this function.</para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 * <seealso cref='CriAtomExPlayer::EntryFile'/>
	 * <seealso cref='CriAtomExPlayer::EntryContentId'/>
	 */
	public bool EntryData(byte[] buffer, int size, bool repeat)
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return false;
		}
		return CRIWAREE07795D8(this.entryPoolHandle, buffer, size, repeat);
	}

	/**
	 * <summary>Enters the concatenated playback data (on-memory buffer address specified)
	 * <param name='buffer'>Buffer address</param>
	 * <param name='size'>Buffer size</param>
	 * <param name='repeat'>Whether to repeat playback when the next data is not entered</param>
	 * <returns>Whether the data was successfully entered.</returns>
	 * <remarks>
	 * <para header='Description'>Inputs the data for linked playback.<br/>
	 * When performing linked playback, set the start data using the ::CriAtomExPlayer::SetData function etc.
	 * start playback, then input data using this function.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 */
	public bool EntryData(IntPtr buffer, int size, bool repeat)
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return false;
		}
		return CRIWAREE07795D8(this.entryPoolHandle, buffer, size, repeat);
	}


	/**
	 * Inputs the data for linked playback (Cue name specified)

	 * <param name='acb'>	-- ACB handle</param>
	 * <param name='name'>	-- Cue name</param>
	 * <param name='repeat'>-- Whether to repeat playback when the next data is not entered</param>

	 * 返回值: Whether the data was successfully entered.

	 * Inputs the data for linked playback.
	 * When performing linked playback, set the start data using the ::CriAtomExPlayer::SetData function etc.
	 * start playback, then input data using this function.

	 * <seealso cref='CriAtomExPlayer::PrepareEntryPool'/>
	 */
	public bool EntryCue(CriAtomExAcb acb, string name, bool repeat)
	{
		if (this.entryPoolHandle == IntPtr.Zero) {
			return false;
		}
		return CRIWARE089F6DAB(this.entryPoolHandle,
			(acb != null) ? acb.nativeHandle : IntPtr.Zero, name, repeat);
	}



	/**
	 * Starts playback

	 返回:
	 * CriAtomExPlayback object
	
	 * Starts playing the sound data.
	 * Before calling this function, it is necessary to set the sound data to be played
	 * to the AtomExPlayer in advance using the functions such as ::CriAtomExPlayer::SetCue .
	 * For example, when playing a Cue, it is necessary to set the sound data
	 * as follows using the ::CriAtomExPlayer::SetCue function in advance,
	 * before calling this function.


	 *  // ACFファイルの登録
	 *  CriAtomEx.RegisterAcf(null, "sample.acf");
	 *  // ACBファイルのロード
	 *  CriAtomExAcb acb = CriAtomExAcb.LoadAcbFile(null, "sample.acb", "sample.awb");
	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 再生するキューの名前を指定
	 *  player.SetCue(acb, "gun_shot");
	 *  // セットされた音声データを再生
	 *  player.Start();


	 * 本函数执行后，再生的进展情况（发音开始还是再生完成等）
	 * 可以通过检索状态来确认是怎样的。
	 * 要获取状态，请使用 ::CriAtomExPlayer::GetStatus函数。
	 * ::CriAtomExPlayer::GetStatus 返回以下 5 种状态:
	    -# Stop
	    -# Prep
	    -# Playing
	    -# PlayEnd
	    -# Error
	 
	 * When you create an AtomExPlayer, the AtomExPlayer's status is Stop.

	 * By calling this function after setting the sound data to be played,
	 * the status of the AtomExPlayer changes to ready ( Prep ).

	 * (Prep is the status waiting for the data to be supplied or start of decoding.)<br/>
	 * When enough data is supplied for starting playback, the AtomExPlayer changes the status to
	 * Playing and starts to output sound.<br/>

	 * When all the set data have been played back, AtomExPlayer
	 * changes its status to PlayEnd.<br/>

	 * If an error occurs during playback, AtomExPlayer
	 * changes the status to Error.<br/>

	 * By checking the status of AtomExPlayer and switching the processing depending on the status,
	 * it is possible to create a program linked to the sound playback status.<br/>

	 * For example, if you want to wait for the completion of sound playback before proceeding, use the following code.
	

	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 再生するキューの名前を指定
	 *  player.SetCue(acb, "gun_shot");
	 *  // セットされた音声データを再生
	 *  player.Start();
	 *  // 再生完了待ち
	 *  while (player.GetStatus() != CriAtomExPlayer.Status.PlayEnd) {
	 *      yield return null;
	 *  }


	 * <seealso cref='CriAtomExPlayer::SetCue'/>
	 * <seealso cref='CriAtomExPlayer::GetStatus'/>
	 */
	public CriAtomExPlayback Start()
	{
		if (this.entryPoolHandle != IntPtr.Zero) {
			CRIWAREE835C03B(this.entryPoolHandle);
		}
		return new CriAtomExPlayback(criAtomExPlayer_Start(this.handle));
	}



	/**
	 * Prepare for playback

	 返回:
	 CriAtomExPlayback object;

	 * Prepares for playing sound data.
	 * Before calling this function, it is necessary to set the sound data to be played
	 * to the AtomExPlayer in advance using the functions such as ::CriAtomExPlayer::SetData .

	 * When this function is called, the sound playback starts in the paused state.
	 * When the function is called, the resources required for sound playback are allocated
	 * and the data starts to be buffered (reading the streamed file),
	 * but the sound is not played after finished buffering.
	 * (It stands by in paused state even if it is ready to be played.)

	 * In the case of playing only one sound, this function behaves the same as the code below.
	 
	 *  // 将播放器设置为暂停状态
	 *  player.Pause();
	 *  // 开始播放声音
	 *  CriAtomExPlayback playback = player.Start();


	 * 为了发音用本函数进行了再现准备声音,
	 * 对于本函数返回 CriAtomExPlayback对象
	 * 必须运行 ::CriAtomExPlayback::Resume  

	 * In streaming playback, there is a time lag after starting playback using
	 * the ::CriAtomExPlayer::Start function before the sound playback actually starts.<br/>
	 * (Because it takes time to buffer the sound data.)
	 ---
	 在 streaming playback 中, 就算调用了 CriAtomExPlayer::Start(), 也需要一段时间来缓存声音数据, 然后再播放;

	 * By performing the following operations, it is possible to control the timing of sound playback
	 * even for sound in stream playback.
	 *  -# Call the ::CriAtomExPlayer::Prepare function to start preparation.
	 *  -# Check the status of the CriAtomExPlayback obtained acquired in step 1. using the ::CriAtomExPlayback::GetStatus function.
	 *  -# When the status becomes Playing, unpause the playback using the ::CriAtomExPlayback::Resume function.
	 *  -# After unpausing, the sound starts at the next server processing timing.
	---
	通过使用如下步骤, 就算是处理 streaming playback, 也能精确地控制它的时间;
	-- 调用 Prepare(); 返回一个 playback 实例
	-- 调用 playback.GetStatus() 来检查状态, 
	-- 当状态变为 Playing, 调用 CriAtomExPlayback::Resume() 解除暂停;
	-- 在下一个 server processing timing, 这个声音开始发声;

	代码示范:
	 *  // プレーヤの作成
	 *  CriAtomExPlayer player = new CriAtomExPlayer();
	 *  // 再生するキューの名前を指定
	 *  player.SetCue(acb, "gun_shot");
	 *  // セットされた音声データの再生準備を開始
	 *  CriAtomExPlayback playback = player.Prepare();
	 *  // 再生準備完了待ち
	 *  while (playback.GetStatus() != CriAtomExPlayback.Status.Playing) {
	 *      yield return null;
	 *  }
	 *  // ポーズを解除
	 *  playback.Resume(CriAtomEx.ResumeMode.PreparedPlayback);

	
	 * If PausedPlayback is specified when unpausing,
	 * both the pausing for preparing playback by this function and the
	 * pausing by the ::CriAtomExPlayer::Pause function are canceled.
	---
	若在调用 Resume() 时使用参数 PausedPlayback, 则不管是用本函数暂停的, 还是用 CriAtomExPlayer::Pause() 暂停的, 都会被重放;

	 * If you want to play the sound prepared for playback using this function
	 * while stopping the sound paused by the ::CriAtomExPlayer::Pause function,
	 * specify PreparedPlayback when unpausing.</para>
	---
	何时该使用 参数 PreparedPlayback ...


	 * <seealso cref='CriAtomExPlayback::GetStatus'/>
	 * <seealso cref='CriAtomExPlayback::Resume'/>
	 */
	public CriAtomExPlayback Prepare()
	{
		return new CriAtomExPlayback(criAtomExPlayer_Prepare(this.handle));
	}


	/**
	 * Stop playing

	 * <param name='ignoresReleaseTime'>Whether to ignore release time
	 * (False = perform release process, True = ignore release time and stop immediately)
	
	 * Issues a request to stop playback.
	 * If this function is called for the AtomExPlayer that is playing sound,
	 * the AtomExPlayer stops playing (stops reading files or playback),
	 * and transitions to the Stop state.
	
	 * If the argument is set to True,
	 * the release time is ignored and the sound is stopped immediately,
	 * even if the sound being played has an envelope release time. 信封释放时间

	 * If this function is called for an AtomExPlayer that has already stopped (AtomExPlayer
	 * whose status is PlayEnd or Error), the status of the AtomExPlayer is changed to Stop.

	 * If this function is called for the AtomExPlayer that is playing sound,
	 * the status may not change to Stop immediately. (It may take some time for it to change to Stop.)
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::GetStatus'/>
	 */
	public void Stop(bool ignoresReleaseTime)
	{
		if (!this.isAvailable) {
			return;
		}

		if (ignoresReleaseTime == false) {
			criAtomExPlayer_Stop(this.handle);
		} else {
			criAtomExPlayer_StopWithoutReleaseTime(this.handle);
		}
		if (this.entryPoolHandle != IntPtr.Zero) {
			CRIWAREC9642C99(this.entryPoolHandle);
		}
	}


	/**
	 * Pause

	 * <para header='Description'> -- Pauses playback.<br/></para>
	 * <para header='Note'>        -- In the default state (state immediately after creating the player), the player is not paused.<br/></para>
	 * <para header='Note'>        -- When this function is called, "all" the sounds being played by the player are paused.<br/>

	 * Use the ::CriAtomExPlayback::Pause function to pause
	 * each sound being played separately.</para>
	
	 * <seealso cref='CriAtomExPlayer::Resume'/>
	 * <seealso cref='CriAtomExPlayer::IsPaused'/>
	 * <seealso cref='CriAtomExPlayback::Pause'/>
	 */
	public void Pause()
	{
		criAtomExPlayer_Pause(this.handle, true);
	}

	/**
	 * Cancels the pausing

	 * <param name='mode'>Unpausing target
	
	 * When this function is called by specifying "PausedPlayback" in the argument (mode),
	 * the playback of the sound paused by the user using the ::CriAtomExPlayer::Pause function
	 * (or the ::CriAtomExPlayback::Pause function) is resumed.

	 * When this function is called by specifying "PreparedPlayback" in the argument (mode),
	 * the playback of the sound prepared by the user using the::CriAtomExPlayer::Prepare starts.

	 * If you prepare a playback using the ::CriAtomExPlayer::Prepare function for the player paused using the ::CriAtomExPlayer::Pause function,
	 * the sound is not played back until it is unpaused using PausedPlayback
	 * as well as unpaused with PreparedPlayback.

	 * If you want to always start playback regardless of whether
	 * the sound is paused using the ::CriAtomExPlayer::Pause or ::CriAtomExPlayer::Prepare function, call this function by specifying "AllPlayback" in the argument (mode).
	 * When this function is called, "all" the sounds being played by the player are unpaused.<br/>

	 * Use the ::CriAtomExPlayback::Resume function to unpause
	 * each sound being played separately.</para>
	
	 * <seealso cref='CriAtomExPlayer::Pause'/>
	 * <seealso cref='CriAtomExPlayback::Resume'/>
	 */
	public void Resume(CriAtomEx.ResumeMode mode)
	{
		criAtomExPlayer_Resume(this.handle, mode);
	}


	/**
	 * Gets the pausing status;  只有到所有声音都 paused 时, 才返回 true;

	 * <returns>Whether the playback is paused (False = not paused, True = paused)</returns>
	
	 * This function returns True only when "all sounds are paused".

	 * After calling the ::CriAtomExPlayer::Pause function, if the sounds were unpaused by specifying the playback ID
	 * (the ::CriAtomExPlayback::Pause function was called), this function returns
	 * False.

	 * This function does not distinguish between the sounds paused by the
	 * ::CriAtomExPlayer::Pause function and the those paused by the ::CriAtomExPlayer::Prepare function.
	 * (Regardless of the pausing method, it only determines whether all the Voices are paused.)
	
	 * <seealso cref='CriAtomExPlayer::Pause'/>
	 * <seealso cref='CriAtomExPlayback::Pause'/>
	 */
	public bool IsPaused()
	{
		return criAtomExPlayer_IsPaused(this.handle);
	}


	/**
	 * Sets the volume

	 * <param name='volume'>Volume value</param>
	
	 * After setting the volume using this function, when you start the playback
	 * using the ::CriAtomExPlayer::Start function, the sound is played with the specified volume.
	 * It is also possible to update the volume of the sound already played by calling
	 * the ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function after setting the volume.
	 ---
	 调用本函数后, 只要再调用 Update, 或 UpdateAll, 可以将新的音量值 同步给那些已经在播放的 声音;

	 * The volume value is a scale factor for the amplitude of the sound data (the unit is not decibel分贝).

	 * For example, if you specify 1.0f, the original sound is played at its unmodified volume.
	 * If you specify 0.5f, the sound is played at the volume by halving the amplitude (-6dB)
	 * of the original waveform.

	 * If you specify 0.0f, the sound is muted (silent).
	 * The default value for volume is 1.0f.

	 * You can set the volume value to 0.0f or higher.
	 * If you set a value higher than 1.0f, the waveform data may be played
	 * at a louder volume than the original Material "depending on the platform";

	 * If you specify a volume value lower than 0.0f, the value is clipped to 0.0f.
	 * (Even if you set a negative volume value,
	 * the phase of the waveform data is not inverted.)

	 * When playing a Cue, if this function is called when the volume is set on the data,
	 * the value set on the data and the setting in this function is "multiplied" and the result is applied.
	 * For example, if the volume on the data is 0.8f and the volume of the AtomExPlayer is 0.5f,
	 * the volume actually applied is 0.4f.
	 ---
	 猜测: 本函数设置的值, 会和 cue 的值 相乘;

	 * If you want to set it in decibel, convert it with the following formula before setting it.<


			volume = Math.Pow(10.0f, db_vol / 20.0f);
	 		※ db_vol是分贝值，volume是卷值。

	
	 * When specifying a volume larger than 1.0f, note the followings:
	 *  - Behavior may differ depending on the platform.
	 *  - Clipping sound may be heard. -- 可能会听到剪切声。

	 * Even if a volume value exceeding 1.0f is set in this function,
	 * whether the sound is played back at a volume higher than the original waveform data
	 * depends on the platform and the sound compression codec type.
	 * Therefore, it is recommended that you do not use volume values higher than 1.0f
	 * when adjusting volume for multi-platform titles.
	 * (If you specify a volume value higher than 1.0f, the sound may be played back at a different volume
	 * depending on the model even if the same waveform data is played back.)

	 * Even on models in which volume can be raised,
	 * there is a limit to the volume that can be output by hardware,
	 * so noise may occur due to clicking sound.

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // ボリュームの設定
	 * player.SetVolume(0.5f);
	 * // 再生の開始
	 * // 備考）ボリュームはプレーヤに設定された値（＝0.5f）で再生される。
	 * CriAtomExPlayback playback = player.Start();
	 * // ボリュームの変更
	 * // 注意）この時点では再生中の音声のボリュームは変更されない。
	 * player.SetVolume(0.3f);
	 * // プレーヤに設定されたボリュームを再生中の音声にも反映
	 * player.Update(playback);


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetVolume(float volume)
	{
		criAtomExPlayer_SetVolume(this.handle, volume);
	}



	/**
	 * Sets the pitch   指定输出声音的 "音调"; 音高

	 * <param name='pitch'>Pitch (in cents 分)
	 
	 * Specifies the pitch of the output sound.
	 * After setting the pitch using this function, when you start the playback
	 * using the ::CriAtomExPlayer::Start function, the sound is played with the specified pitch.
	 * It is possible to update the pitch of the sound already played by calling
	 * the ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function after setting the pitch.
	
	 * The pitch is specified in cents.
	 * One cent is 1/1200 of one octave. A semitone is 100 cents.

	 * For example, if you specify 100.0f, the pitch increases by a semitone(半音). If you specify -100.0f,
	 * the pitch decreases by a semitone.

	 * The default pitch is 0.0f.

	 * When playing a Cue, if this function is called when the pitch is set on the data,
	 * the value set on the data and the setting in this function is added and the result is applied.
	 * For example, if the pitch on the data is -100.0f and the pitch of the AtomExPlayer is 200.0f,
	 * the actual pitch applied is 100.0f.
	 ---
	 数据的 音调 和 player 的 音调 可以线性叠加;
	
	 * If you want to set the frequency ratio of the sampling rate, convert it with the following formula before setting it.

			pitch = 1200.0 * Math.Log(freq_ratio, 2.0);
			※ freq_ratio 是频率比，pitch 是间距值。
	
	 * The pitch of the sound data encoded for HCA-MX cannot be changed.
	 * (The pitch does not change even if you call this function.)
	 * For the sound whose pitch you want to change, encode it with a different codec such as ADX or HCA.
	 ---
	 使用 HCA-MX 格式的声音, 不会被改写音调, 就是调用本函数 也无法改写;

	 * The maximum pitch that can be set depends on the sampling rate of the sound data and the maximum sampling rate of the Voice Pool.
	 * For example, if the sampling rate of the sound data is 24kHz and the maximum sampling rate of the Voice Pool is 48kHz,
	 * the maximum pitch that can be set is 1200(double frequency ratio).

	 * Since the pitch is implemented by increasing or decreasing the playback sampling rate,
	 * changing the pitch also changes the playback speed along with the pitch.
	 改变音高也会随着音高改变播放速度;

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // ピッチの設定
	 * player.SetPitch(100.0f);
	 * // 再生の開始
	 * // 備考）ピッチはプレーヤに設定された値（＝100セント）で再生される。
	 * CriAtomExPlayback playback = player.Start();
	 * // ピッチの変更
	 * // 注意）この時点では再生中の音声のピッチは変更されない。
	 * player.SetPitch(-200.0f);
	 * // プレーヤに設定されたピッチを再生中の音声にも反映
	 * player.Update(playback);


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetPitch(float pitch)
	{
		criAtomExPlayer_SetPitch(this.handle, pitch);
	}


	/**
	 * Sequence "playback ratio" setting

	 * Sets the playback ratio of the player's playing sequence. 

	 * <param name='ratio'>Sequence playback ratio
	 
	 * Playback ratio can be set between 0.0f ~ 2.0f.
	 * If the input value is outside the range, the lower or upper limit will be set.

	 * After specifying playback ratio with this function, start with ::CriAtomExPlayer::Start to play back at the specified ratio.
	 * If you want to change the playback ratio during the playback,
	 * please call ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function.

	 * The value set by this function is applied only when playing a sequence type Cue.
	 * It cannot be used for the playback ratio of waveform data in a sequence. 
	 * If you want to change the playback ratio of waveform, use the time stretch function.
	 ---
	 本函数不能作用于 waveform data in a sequence,  只能作用于 sequence type Cue;

	 * <seealso cref='CriAtomPlayer::Update'/>
	 * <seealso cref='CriAtomPlayer::UpdateAll'/>
	 */
	public void SetPlaybackRatio(float ratio)
	{
		criAtomExPlayer_SetPlaybackRatio(this.handle, ratio);
	}

	/**
	 * Sets the Panning 3D angle  左右声道

	 * <param name='angle'>Panning 3D angle (-180.0f to 180.0f: in degrees)</param>
	 
	 * <para header='Description'>Specifies the Panning 3D angle.<br/>
	 * After setting the Panning 3D angle using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified Panning 3D angle.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Panning 3D angle of the sound already played.<br/>
	 
	 * The angle is specified in degrees.<br/>
	 * With front being 0 degree, you can set up to 180.0f in the right direction (clockwise) and -180.0f in the left direction (counterclockwise).<br/>
	 * For example, if you specify 45.0f, the localization will be 45 degrees to the front right. If you specify -45.0f, the localization will be 45 degree to the front left.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the Panning 3D angle is set on the data,
	 * the value set on the data and the setting in this function is <b>added</b> and the result is applied.<br/>
	 * For example, if the Panning 3D angle on the data is 15.0f and the Panning 3D angle on the AtomExPlayer is 30.0f,
	 * the actual Panning 3D angle applied will be 45.0f.
	 
	 * If the actual applied Panning 3D angle exceeds 180.0f, -360.0f is added to the value so that it is within the range.<br/>
	 * Similarly, if the actual applied volume value is less than -180.0f, +360.0f is add so that it is within the range.<br/>
	 * (Since the localization does not change even when +360.0f, or -360.0f is added, you can effectively set a value outside the range of -180.0f to 180.0f.)</para>
	

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // パンニング3D角度の設定
	 * player.SetPan3dAngle(45.0f);
	 * // 再生の開始
	 * // 備考）パンニング3D角度はプレーヤに設定された値（＝45.0f）で再生される。
	 * CriAtomExPlayback playback = player.Start();
	 * // パンニング3D角度の変更
	 * // 注意）この時点では再生中の音声のパンニング3D角度は変更されない。
	 * player.SetPan3dAngle(-45.0f);
	 * // プレーヤに設定されたパンニング3D角度を再生中の音声にも反映
	 * player.Update(playback);


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetPan3dAngle(float angle)
	{
		criAtomExPlayer_SetPan3dAngle(this.handle, angle);
	}


	/**
	 * Sets the Panning 3D distance<

	 * <param name='distance'>Panning 3D distance (-1.0f to 1.0f)</param>
	
	 * <para header='Description'>Specifies the distance for doing interior Panning in Panning 3D.<br/>
	 * After setting the Panning 3D distance using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified Panning 3D distance.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Panning 3D distance of the sound already played.<br/>
	 * <br/>
	 * The distance is specified in the range of -1.0f to 1.0f, with the listener position being 0.0f and the circumference of the speaker position being 1.0f.<br/>
	 * If you specify a negative value, the Panning 3D angle inverts 180 degrees and the localization is reversed.</para>
	 * <para header='Note'>When playing a Cue, if this function is called when the Panning 3D distance is set on the data,
	 * the value set on the data and the setting in this function is <b>multiplied</b> and the result is applied.<br/>
	 * For example, if the Panning 3D distance on the data is 0.8f and the Panning 3D distance on the AtomExPlayer is 0.5f,
	 * the actual Panning 3D distance applied will be 0.4f.
	 * <br/>
	 * If the actual applied Panning 3D distance exceeds 1.0f, the value is clipped to 1.0f.<br/>
	 * Similarly, if the actual applied Panning 3D distance is smaller than -1.0f, the value is clipped to -1.0f.<br/></para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *  ：
	 * // パンニング3D距離の設定
	 * player.SetPan3dInteriorDistance(0.5f);
	 *
	 * // 再生の開始
	 * // 備考）パンニング3D距離はプレーヤに設定された値（＝0.5f）で再生される。
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // パンニング3D距離の変更
	 * // 注意）この時点では再生中の音声のパンニング3D距離は変更されない。
	 * // 備考）以下の処理はパン3D角度を180度反転するのと等価
	 * player.SetPan3dInteriorDistance(-0.5f);
	 *
	 * // プレーヤに設定されたパンニング3D距離を再生中の音声にも反映
	 * player.Update(playback);
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetPan3dInteriorDistance(float distance)
	{
		criAtomExPlayer_SetPan3dInteriorDistance(this.handle, distance);
	}



	/**
	 * Sets the Panning 3D volume

	 * <param name='volume'>Panning 3D volume (0.0f to 1.0f)</param>

	 * <para header='Description'>Specifies the volume of the Panning 3D.<br/>
	 * After setting the Panning 3D volume using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified Panning 3D volume.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Panning 3D volume of the sound already played.<br/>
	 * <br/>
	 * The Panning 3D volume is used when controlling the
	 * Panning 3D component and the output level to the center/LFE separately.<br/>
	 * For example, when outputting a sound from LFE using the send level at a fixed volume,
	 * and Panning is controlled by Panning 3D.
	 * <br/>
	 * The range and handling of the value are the same as for normal volume. Refer to the ::CriAtomExPlayer::SetVolume function.</para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *  ：
	 * // パンニング3Dボリュームの設定
	 * player.SetPan3dVolume(0.8f);
	 *
	 * // 再生の開始
	 * // 備考）パンニング3Dボリュームはプレーヤに設定された値（＝0.5f）で再生される。
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // パンニング3Dボリュームの変更
	 * // 注意）この時点では再生中の音声のパンニング3Dボリュームは変更されない。
	 * player.SetPan3dVolume(0.7f);
	 *
	 * // プレーヤに設定されたパンニング3Dボリュームを再生中の音声にも反映
	 * player.Update(playback);
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetVolume'/>
	 */
	public void SetPan3dVolume(float volume)
	{
		criAtomExPlayer_SetPan3dVolume(this.handle, volume);
	}


	/**
	 * Sets the Panning type

	 * <param name='panType'>Panning type</param>

	 * <para header='Description'>Specifies Panning type.<br/>
	 * After setting the Panning type using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified Panning type.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Panning type of the sound already played.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called, the Panning type set on the
	 * data is <b>overridden</b> (the setting on the data is ignored).<br/>
	 * Normally, it is not necessary to call this function because the Panning type is set on the data.<br/>
	 * To enable 3D Positioning when playing back sound without using an ACB file,
	 * set Pos3d using this function.
	 * <br/></para>
	 * <para header='Note'>An Error will occur if CriAtomEx.PanType.Unknown is specified when executing.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomEx.PanType'/>
	 */
	public void SetPanType(CriAtomEx.PanType panType)
	{
		criAtomExPlayer_SetPanType(this.handle, panType);
	}


	/**
	 * Sets the send level

	 Specifies the send level.

	 * <param name='channel'>	Channel number	</param>
	 * <param name='id'>		Speaker ID	</param>
	 * <param name='level'>		Send level value (0.0f to 1.0f)	</param>

	 * Specifies the send level.
	 * The send level is a mechanism for specifying which speaker outputs the sound of each channel of the sound data at which volume.
	 ---
	"send level": 指定: 哪个扬声器以哪个音量输出每个声道的声音数据。


	 * After setting the send level using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified send level.<br/>

	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the send level of the sound already played.<br/>
	
	 * The first argument, the channel number, specifies the "channel number of the sound data".<br/>
	 * The second argument, the speaker ID, specifies which speaker outputs the data of the specified channel number,
	 * and the third argument specifies the level (volume) when sending data.<br/>
	 * For example, if you want to output the sound data of channel 0 from
	 * the right speaker with full volume (1.0f), specify as follows:<code>
	 * player.SetSendLevel(0, CriAtomEx.Speaker.FrontRight, 1.0f);
	 
	 * センドレベル値の範囲や扱いは、ボリュームと同等です。 ::CriAtomExPlayer::SetVolume 関数を参照してください。<br/>
	 
	 * <para header='Note'>There are two types of send level settings: "automatic setting" and "manual setting".<br/>
	 * Immediately after creating the AtomExPlayer, or when
	 * the parameters are cleared using the ::CriAtomExPlayer::ResetParameters function, the send level setting becomes "automatic setting".<br/>
	 * But when this function is called, the send level setting becomes "manual setting".<br/>
	 * (The user must control the send level to each speaker and perform Panning.)<br/>
	
	 * In the "automatic setting", the AtomExPlayer routes sound as follows:<br/>

	 * [When playing mono sound]<br/>
	 * The sound from channel 0 is output from the left and right speakers at the volume of about 0.7f (-3dB).<br/>
	
	 * [When playing stereo sound]<br/>
	 * The sound from channel 0 is output from the left speaker,
	 * and the sound from channel 1 is output from the right speaker.<br/>
	
	 * [When playing 4ch sound]<br/>
	 * The sound from channel 0 is output from the left speaker, the sound from channel 1 is output from the right speaker,
	 * the sound from channel 2 is output from the surround left speaker,
	 * and the sound from channel 3 is output from the surround right speaker.<br/>

	 * [When playing 5.1ch sound]<br/>
	 * The sound from channel 0 is output from the left speaker, the sound from channel 1 is output from the right speaker,
	 * the sound from channel 2 is output from the center speaker, the sound from channel 3 is output from LFE,
	 * the sound from channel 4 is output from the surround left speaker,
	 * and the sound from channel 5 is output from the surround right speaker.<br/>
	 
	 * [When playing 7.1ch sound]<br/>
	 * The sound from channel 0 is output from the left speaker, the sound from channel 1 is output from the right speaker,
	 * the sound from channel 2 is output from the center speaker, the sound from channel 3 is output from LFE,
	 * the sound from channel 4 is output from the surround left speaker,
	 * and the sound from channel 5 is output from the surround right speaker.<br/>
	 * The sound from channel 6 is output from the surround back left speaker,
	 * and the sound from channel 7 is output from the surround back right speaker.<br/>

	 * On the other hand, when "manual setting" is selected using this function, the sound is output
	 * with the specified send level setting regardless of the number of channels in the sound data.<br/>
	 * (It is necessary to switch the send level setting appropriately according to the number of channels in the sound data.)<br/>
	
	 * If you want to clear the send level specified previously and return to the "automatic setting" routing,
	 * call the ::CriAtomExPlayer::ResetParameters function.<br/>
	
	 * This parameter cannot be set on the data, so the setting in this function is always applied.<br/></para>
	 * <para header='Note'>Sound is not output for channels for which the send level is not set.<br/>
	 * For example, if the sound data to be played is stereo, but the send level is set
	 * only for one of the channels, the sound of the channel for which the send level
	 * is not set is muted.<br/>
	 * When controlling the send level, be sure to set the send level for all channels
	 * you want to output.<br/></para>
	

	 * CriSint32 ch = 0;    // channel number 0
	 * CriAtomEx.Speaker spk = CriAtomEx.Speaker.FrontCenter;
	 * CriFloat32 level = 1.0f;
	 * // Set send level(ch0 to center)
	 * player.SetSendLevel(ch, spk, level);
	 * // Start playback
	 * CriAtomExPlayback playback = player.Start();
	 *                :
	 * // Change send level
	 * level = 0.7f;
	 * player.SetSendLevel(ch, spk, level);
	 * player.Update(playback);
	

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetVolume'/>
	 */
	public void SetSendLevel(int channel, CriAtomEx.Speaker id, float level)
	{
		criAtomExPlayer_SetSendLevel(this.handle, channel, id, level);
	}



	/**
	 * Sets the parameters of the Biquad Filter    双二次滤波器

	 * <param name='type'>Filter type</param>
	 * <param name='frequency'>Normalized frequency (0.0f to 1.0f)</param>
	 * <param name='gain'>Gain (decibels)</param>
	 * <param name='q'>Q value</param>
	 * <remarks>
	 * <para header='Description'>Specifies various parameters of the Biquad Filter.<br/>
	 * After setting the parameters using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the Biquad Filter is activated using the specified parameters.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Biquad Filter parameters for the sound already played.<br/>
	 * <br/>
	 * The normalized frequency is a value obtained by normalizing 24Hz to 24000Hz on the logarithmic axis to 0.0f to 1.0f.<br/>
	 * The gain is specified in decibels.<br/>
	 * Gain is valid only when the filter type is either of the following types.<br/>
	 * - LowShelf   : Low shelf filter
	 * - HighShelf  : High shelf filter
	 * - Peaking    : Peaking filter
	 * .</para>
	 * <para header='Note'>- type<br/>
	 *  Overwrites the value set in the data.
	 * - frequency<br/>
	 *  Added to the value set in the data.
	 * - gain<br/>
	 *  The value set in the data is multiplied.
	 * - q<br/>
	 *  Added to the value set in the data.
	 * .
	 * <br/>
	 * If the normalization cutoff frequency which is actually applied exceeds 1.0f, the value is clipped to 1.0f.<br/>
	 * Similarly, if the normalization cutoff frequency actually applied is less than 0.0f, the value is clipped to 0.0f.<br/></para>
	 * <para header='Note'>The Biquad Filter is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use the Biquad Filter for a sound, encode it with a different codec such as ADX or HCA.<br/></para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *  ：
	 * // フィルタのパラメータを設定
	 * CriAtomEx.BiquadFilterType type = CriAtomEx.BiquadFilterType.LowPass;
	 * float frequency = 0.5f;
	 * float gain = 1.0f;
	 * float q = 3.0f;
	 * player.SetBiquadFilterParameters(type, frequency, gain, q);
	 *
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // パラメータの変更
	 * // 注意）この時点では再生中の音声のパラメータは変更されない。
	 * frequency = 0.7f;
	 * player.SetBiquadFilterParameters(type, frequency, gain, q);
	 *
	 * // プレーヤに設定されたパラメータを再生中の音声にも反映
	 * player.Update(playback);
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetBiquadFilterParameters(
		CriAtomEx.BiquadFilterType type, float frequency, float gain, float q)
	{
		criAtomExPlayer_SetBiquadFilterParameters(this.handle, type, frequency, gain, q);
	}


	/**
	 * Sets BandPass Filter parameters   带通滤波器

	 * <param name='cofLow'>Normalized low frequency cutoff frequency (0.0f to 1.0f)</param>
	 * <param name='cofHigh'>Normalized high frequency cutoff frequency (0.0f to 1.0f)</param>
	 * <remarks>
	 * <para header='Description'>Specifies the cutoff frequency of the BandPass Filter.<br/>
	 * After setting the cutoff frequency using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the BandPass Filter is activated using the specified cutoff frequency.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the cutoff frequency of the BandPass Filter for the sound already played.<br/>
	 * <br/>
	 * The normalized cutoff frequency is a value obtained by normalizing 24Hz to 24000Hz on the logarithmic axis to 0.0f to 1.0f.<br/>
	 * For example, if the normalized low cutoff frequency is specified as 0.0f and the normalized high cutoff frequency as 1.0f,
	 * the BandPass Filter will pass the entire range, and the higher the normalized low cutoff frequency,
	 * or the lower the normalized high cutoff frequency, the narrower the passband.<br/></para>
	 * <para header='Note'>When playing a Cue, if the function is called when the bandpass filter parameter is set on the data,
	 * the settings will be as follows.
	 * - cofLow<br/>
	 *  The value set on the data is multiplied after calculating "cofLowRev = 1.0f - cofLow",
	 *  and the result of calculating "cofLow = 1.0f - cofLowRev" is applied.<br/>
	 *  In other words, 0.0f is considered as "the filter is opened most to the low frequency side", and the degree of opening is multiplied and applied.
	 * - cofHigh<br/>
	 *  The value set in the data is multiplied and applied.<br/>
	 *  In other words, 1.0f is considered as "the filter is opened most to the high frequency side", and the degree of opening is multiplied and applied.
	 * .
	 * <br/>
	 * If the normalization cutoff frequency which is actually applied exceeds 1.0f, the value is clipped to 1.0f.<br/>
	 * Similarly, if the normalization cutoff frequency actually applied is less than 0.0f, the value is clipped to 0.0f.<br/></para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *  ：
	 * // フィルタのパラメータを設定
	 * float cof_low = 0.0f;
	 * float cof_high = 0.3f;
	 * player.SetBandpassFilterParameter(cof_low, cof_high);
	 *
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // パラメータの変更
	 * // 注意）この時点では再生中の音声のパラメータは変更されない。
	 * cof_low = 0.7f;
	 * cof_high = 1.0f;
	 * player.SetBandpassFilterParameter(cof_low, cof_high);
	 *
	 * // プレーヤに設定されたパラメータを再生中の音声にも反映
	 * player.Update(playback);
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void SetBandpassFilterParameters(float cofLow, float cofHigh)
	{
		criAtomExPlayer_SetBandpassFilterParameters(this.handle, cofLow, cofHigh);
	}


	/**
	 * Sets the "Bus Send Level" (bus name specified)

	 * <param name='busName'>Bus name -- "channel number of the sound data";
	 * <param name='level'>Send level value (0.0f to 1.0f) -- the level (volume) when sending;
	

	 * Specifies the Bus Send Level.
	 * The Bus Send Level is a mechanism for specifying how much sound should be sent to which bus.
	---
	"Bus Send Level": 指定应向哪条总线发送多少声音。

	 * After setting the Bus Send Level using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified Bus Send Level.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the Bus Send Level of the sound already played.<br/>

	
	 * The range and handling of the send level values are the same as for volume. Refer to ::CriAtomExPlayer::SetVolume function.</para>
	 * By calling this function multiple times, you can send sound to multiple buses.
	 ---
	 多次调用本函数, 可将声音发送给多个 buses;

	
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // バスセンドレベルを設定
	 * int bus_id = 1;  // ex. reverb, etc...
	 * float level = 0.3f;
	 * player.SetBusSendLevel(bus_id, level);
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 * // パラメータの変更
	 * // 注意）この時点では再生中の音声のパラメータは変更されない。
	 * level = 0.5f;
	 * player.SetBusSendLevel(bus_id, level);
	 * // プレーヤに設定されたパラメータを再生中の音声にも反映
	 * player.Update(playback);


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetVolume'/>
	 */
	public void SetBusSendLevel(string busName, float level)
	{
		criAtomExPlayer_SetBusSendLevelByName(this.handle, busName, level);
	}


	/**
	 * Obtaining the bus send level (specifying the bus name)

	 * <param name='busName'>Bus name</param>
	 * <param name='level'>Send level value (0.0f to 1.0f)</param>

	返回值:
	 * Whether obtained or not (Obtained: TRUE / Unable to obtain: FALSE)
	
	 * Gets the specific bus send level set for the player.<br/>
	 * In the following cases, the acquisition of the bus send level will fail.<br/>
	 *  - A bus with the specified name does not exist in the current DSP bus configuration.
	 *  - The send level has not been set for the bus specified by the player.</para>
	
	 * <seealso cref='CriAtomExPlayer::SetBusSendLevel'/>
	 */
	public bool GetBusSendLevel(string busName, out float level) 
	{
		return criAtomExPlayer_GetBusSendLevelByName(this.handle, busName, out level);
	}


	/**
	 * \deprecated
	 * This is a deprecated API that will be removed.
	 * Please consider using CriAtomExPlayer.SetBusSendLevel(string busName, float level) instead.
	*/
	/*
	[System.Obsolete("Use CriAtomExPlayer.SetBusSendLevel(string busName, float level)")]
	public void SetBusSendLevel(int busId, float level)
	{
		criAtomExPlayer_SetBusSendLevel(this.handle, busId, level);
	}
	*/


	/**
	 * Sets the Bus Send Level by offset (bus name specified)

	 * <param name='busName'>Bus name</param>
	 * <param name='levelOffset'>Send level value (0.0f to 1.0f)
	
	 * Specifies the Bus Send Level as an offset.<br/>
	 * When playing a Cue, if this function is called when the Bus Send Level is set on the data,
	 * the value set on the data and the setting in this function is "added" and the result is applied.
	 * Other specifications are the same as the ::CriAtomExPlayer::SetBusSendLevel function.</para>

	 * By setting 0.0f in the ::CriAtomExPlayer::SetBusSendLevel function and an offset value in this function,
	 * you can set the value ignoring the Bus Send Level set on the data. (Overridden)
	 ---
	 猜测: 就是在 SetBusSendLevel() 设置的值的基础上, 再 offset 一个值;

	 * <seealso cref='CriAtomExPlayer::SetBusSendLevel'/>
	 */
	public void SetBusSendLevelOffset(string busName, float levelOffset)
	{
		criAtomExPlayer_SetBusSendLevelOffsetByName(this.handle, busName, levelOffset);
	}


	/**
	 * Obtaining the offset of the bus send level (specifying the bus name)

	 * <param name='busName'>Bus name</param>
	 * <param name='level'>Send level offset value (0.0f to 1.0f)</param>

	返回:
	 * Whether obtained or not (Obtained: TRUE / Unable to obtain: FALSE)
	
	 * Gets the offset for a particular bus send level set on the player.
	 * In the following cases, the acquisition of the bus send level will fail.
	 *  - A bus with the specified name does not exist in the current DSP bus configuration.
	 *  - The send level has not been set for the bus specified by the player.

	 * <seealso cref='CriAtomExPlayer::SetBusSendLevelOffset'/>
	 */
	public bool GetBusSendLevelOffset(string busName, out float level) 
	{
		return criAtomExPlayer_GetBusSendLevelOffsetByName(this.handle, busName, out level);
	}

	/**
	 * \deprecated
	 * This is a deprecated API that will be removed.
	 * Please consider using CriAtomExPlayer.SetBusSendLevelOffset(int busId, float levelOffset) instead.
	*/
	/*
	[System.Obsolete("Use CriAtomExPlayer.SetBusSendLevelOffset(int busId, float levelOffset)")]
	public void SetBusSendLevelOffset(int busId, float levelOffset)
	{
		criAtomExPlayer_SetBusSendLevelOffset(this.handle, busId, levelOffset);
	}
	*/


	/**
	 * Attaches AISAC to the player; --- 将 AISAC 连接到 player;

	 * <param name='globalAisacName'>-- Global AISAC name to attach
	
	 * By attaching AISAC, you can get AISAC effect even if you didn't set AISAC for the Cue or the Track.
	 * After the AISAC is attached using this function, when the playback is started using the ::CriAtomExPlayer::Start function ,
	 * various parameters are applied considering the attached AISAC;
	 ---
	 * After attachment, by calling the ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function,
	 * you can apply various parameter settings by the attached AISAC to the sound already played.

	注意:
	 * Only the global ACF included in the global settings (AISAC file) can be attached.
	 * To get the effect of AISAC , it is necessary to set the appropriate AISAC control value
	 * in the same way as the AISAC set for Cues or Tracks.
	 
	 * This parameter is cleared using the ::CriAtomExPlayer::ResetParameters function.
	 ---
	 使用 CriAtomExPlayer::ResetParameters() 来清理;

	 注意:
	 * Even if "an AISAC to change AISAC control value" is set for the Cue or Track,
	 * the applied AISAC control value does not affect the AISAC attached to the player.
	 * Currently, it does not support the AISAC attachment of control type "Automodulation" or "Random".
	 * Currently, the maximum number of AISACs that can be attached to a player is fixed to 8.
	 ---
	 一个 player 可绑定 8 个 aisac;
	
	 */
	public void AttachAisac(string globalAisacName)
	{
		criAtomExPlayer_AttachAisac(this.handle, globalAisacName);
	}


	/**
	 * Detach (separate) AISAC from the player.

	 * <param name='globalAisacName'>Global AISAC name to detach</param>
	
	 * After detaching AISAC using this function, when you start the playback using
	 * the ::CriAtomExPlayer::Start function, the detached AISAC has no effect.

	 * After detachment, by calling the ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function,
	 * the detached AISAC has no effect on the sound already played.
	 */
	public void DetachAisac(string globalAisacName)
	{
		criAtomExPlayer_DetachAisac(this.handle, globalAisacName);
	}


	/**
	 	Sets the AISAC control value (specifying the control name)
		指定名称来设置针对 aisac 控制器值;

	 	参数:
			controlName -- Control name
	 		value       -- Control value (0.0f to 1.0f)
	 
	 
	 * Up to eight AISAC control values can be set for one player.
	 * After setting the AISAC control value using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified AISAC control value.
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the AISAC control value of the sound already played.
	 	---
		一个 player 最多可设置 8 个 aisac 控制值;
		当使用本函数设置了 aisac 控制值之后, 然后你调用 ::CriAtomExPlayer::Start() 开始 playback 之后, 声音会使用特殊的 aisac控制值 来播放;
		此外, 在设置之后, 通过调用 ::CriAtomExPlayer::Update() 和 ::CriAtomExPlayer::UpdateAll(), 你可以更改那些已经播放了的 声音的 aisac 的控制值;

	 * The AISAC control value is handled in the same way as in the ::CriAtomExPlayer::SetAisacControl function. (怎么感觉没意义...)

	示范代码:
	 
	 	// -- 创建播放器
		CriAtomExPlayer player = new CriAtomExPlayer();
	 	// -- 设置AISAC控制值
	 	float control_value = 0.5f;
	 	player.SetAisacControl("Any", control_value);
	 	// -- 开始播放
	 	CriAtomExPlayback playback = player.Start();
	 	// -- 修改参数
	 	// -- 注意）此时，正在播放的声音的参数不会改变。
	 	control_value = 0.3f;
	 	player.SetAisacControl("Any", control_value);
	 	// -- 播放器设置的参数也反映在播放中的声音中
	 	player.Update(playback);


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetAisacControl(uint, float)'/>
	 */
	public void SetAisacControl(string controlName, float value)
	{
		criAtomExPlayer_SetAisacControlByName(this.handle, controlName, value);
	}



	/**
	 * \deprecated
	 * This is a deprecated API that will be removed.
	 * Please consider using CriAtomExPlayer.SetAisacControl instead.
	*/
	/*
	[System.Obsolete("Use CriAtomExPlayer.SetAisacControl")]
	public void SetAisac(string controlName, float value)
	{
		SetAisacControl(controlName, value);
	}
	*/



	/**
	 Sets the AISAC control value (specifying the control ID)

	 * <param name='controlId'>Control ID
	 * <param name='value'>Control value (0.0f to 1.0f)
	
	 
	 * Up to eight AISAC control values can be set for one player.
	 * After setting the AISAC control value using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified AISAC control value.
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the AISAC control value of the sound already played.
	 	---
		一个 player 最多可设置 8 个 aisac 控制值;
		当使用本函数设置了 aisac 控制值之后, 然后你调用 ::CriAtomExPlayer::Start() 开始 playback 之后, 声音会使用特殊的 aisac控制值 来播放;
		此外, 在设置之后, 通过调用 ::CriAtomExPlayer::Update() 和 ::CriAtomExPlayer::UpdateAll(), 你可以更改那些已经播放了的 声音的 aisac 的控制值;


	 * Specify a real value in the range of 0.0f to 1.0f for the AISAC control value.
	 * The behavior changes as follows depending on the control type of AISAC.
	 *  - Off
	 *      - If the AISAC control value is not set using this function, the AISAC is not activated.
	 *      .
	 *  - Automatic modulation
	 *      - The AISAC control value changes automatically over time without being affected by the setting in this function.
	 *      .
	 *  - Random
	 *      - With the AISAC control value set by this function etc. as the median value, 
	 			the final AISAC control value is determined by randomizing it with the random width set to the data.
	 *      - The randomization is done only by applying parameters when starting playback, and the AISAC control value cannot be changed for the sound being played.
	 *      - If the AISAC control value is not set when starting playback, randomization is done using 0.0f as the median value.
	---
	根据 AISAC 的控件类型，行为会发生以下变化。
	-- Off:
		此模式的 aisac 值, 如果不使用本函数来设置, 就会处于 非激活状态;

	-- Automatic modulation:
		此模式的 aisac 值会随着时间自动切换, 不需要使用崩函数来设置;

	-- Random:
		-- 将此函数设置的 AISAC控制值 作为 中值，使用这个中值, 再配合 "随机宽度" 随机出来的值, 就是最终的 AISAC 控制值。
		-- 只有在开始播放时 才能进行随机化，一旦 声音开始播放,  AISAC控制值 就不能再被更改了;
		-- 若在开始播放时 没有设置 AISAC控制值，则使用 0.0f 作为 中值 进行随机化。

	示范代码:

	 	// -- 创建播放器
	 	CriAtomExPlayer player = new CriAtomExPlayer();

	 	// -- 设置AISAC控制值
	 	CriAtomExAisacControlId control_id = 0;
	 	float control_value = 0.5f;
	 	player.SetAisacControl(control_id, control_value);
	
	 	// -- 开始播放
	 	CriAtomExPlayback playback = player.Start();
	 
	 	// -- 修改参数
	 	// -- 注意）此时，正在播放的声音的参数不会改变。
	 	control_value = 0.3f;
	 	player.SetAisacControl(control_id, control_value);
	 
	 	// -- 播放器设置的参数也反映在播放中的声音中
	 	player.Update(playback);

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetAisacControl(string, float)'/>
	 */
	public void SetAisacControl(uint controlId, float value)
	{
		criAtomExPlayer_SetAisacControlById(this.handle, (ushort)controlId, value);
	}




	/**
	 * \deprecated
	 * This is a deprecated API that will be removed.
	 * Please consider using CriAtomExPlayer.SetAisacControl instead.
	*/
	/*
	[System.Obsolete("Use SetAisacControl")]
	public void SetAisac(uint controlId, float value)
	{
		criAtomExPlayer_SetAisacControlById(this.handle, (ushort)controlId, value);
	}
	*/



	/**
	 * Gets information on AISAC attached to the player;

	参数:
	 * 'aisacAttachedIndex  -- Index of the attached AISAC
	 * aisacInfo			-- A structure for getting the AISAC information
	
	 * <para header='Description'>Gets information on AISAC attached to the player.<br/></para>
	 * <para header='Note'>Returns False if you specify an invalid index.<br/></para>
	 */
	public bool GetAttachedAisacInfo(int aisacAttachedIndex, out CriAtomEx.AisacInfo aisacInfo)
	{
		using (var mem = new CriStructMemory<CriAtomEx.AisacInfo>()) {
			bool result = criAtomExPlayer_GetAttachedAisacInfo(this.handle, aisacAttachedIndex, mem.ptr);
			if (result) {
				aisacInfo = new CriAtomEx.AisacInfo(mem.bytes, 0);
			} else {
				aisacInfo = new CriAtomEx.AisacInfo();
			}
			return result;
		}
	}


	/**
	 * Sets a 3D sound source object

	 * <param name='source'> CriAtomEx3dSource object </param>
	
	 * Set the 3D sound source object to realize the 3D Positioning.
	 * By setting the 3D listener object and the 3D sound source object,
	 * localization, volume, pitch, etc. are automatically applied based on the positional relationship between the 3D listener object and 3D sound source object.<br/>
	 * After setting the 3D sound source object using this function, if you start playback using the ::CriAtomExPlayer::Start function,
	 * the 3D sound source object that has been set will be referenced for playback.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can change the 3D sound source object referenced by the already played sound.<br/>
	 * If source is set to null, the 3D sound source object already set is cleared.<br/></para>
	 * <para header='Note'>To change or update the parameters of the 3D sound source object, use the functions of the 3D sound source object
	 * instead of the functions of the AtomExPlayer.<br/>
	 * By default, 3D Positioning calculations are done in the left-handed coordinate system.<br/>

	 *  ：
	 * // リスナの作成
	 * CriAtomEx3dListener listener = new CriAtomEx3dListener();
	 * // ソースの作成
	 * CriAtomEx3dSource source = new CriAtomEx3dSource();
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // ソース、リスナをプレーヤに設定
	 * player.Set3dListener(listener);
	 * player.Set3dSource(source);
	 *  ：
	 * // 音源の位置を初期化
	 * source.SetPosition(0.0f, 0.0f, 0.0f);
	 * source.Update();
	 *  ：
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // 音源の位置を変更
	 * source.SetPosition(10.0f, 0.0f, 0.0f);
	 * source.Update();

	
	 * <seealso cref='CriAtomEx3dListener'/>
	 * <seealso cref='CriAtomExPlayer::Set3dSource'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 */
	public void Set3dSource(CriAtomEx3dSource source)
	{
		criAtomExPlayer_Set3dSourceHn(this.handle, (source == null) ? IntPtr.Zero : source.nativeHandle);
	}


	/**
	 * Sets the 3D listener object

	 * <param name='listener'>3D listener object</param>
	 * <remarks>
	 * <para header='Description'>Sets the 3D listener object for realizing the 3D Positioning.<br/>
	 * By setting the 3D listener object and the 3D sound source object,
	 * localization, volume, pitch, etc. are automatically applied based on the positional relationship between the 3D listener and 3D sound source.<br/>
	 * After setting the 3D listener object using this function, if you start playback using the ::CriAtomExPlayer::Start function,
	 * the 3D listener object that has been set will be referenced for playback.<br/>
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can change the 3D listener object referenced by the already played sound.<br/>
	 * If listener is set to null, the 3D listener object already set is cleared.<br/></para>
	 * <para header='Note'>To change or update the parameters of the 3D listener object, use the functions of the 3D listener object
	 * instead of the functions of the AtomExPlayer.<br/>
	 * By default, 3D Positioning calculations are done in the left-handed coordinate system.<br/>
	 * <br/></para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // リスナの作成
	 * CriAtomEx3dListener listener = new CriAtomEx3dListener();
	 *
	 * // ソースの作成
	 * CriAtomEx3dSource source = new CriAtomEx3dSource();
	 *
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *
	 * // ソース、リスナをプレーヤに設定
	 * player.Set3dListener(listener);
	 * player.Set3dSource(source);
	 *  ：
	 * // 音源の位置を初期化
	 * source.SetPosition(0.0f, 0.0f, 0.0f);
	 * source.Update();
	 *  ：
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * // リスナの位置を変更
	 * listener.SetPosition(-10.0f, 0.0f, 0.0f);
	 * listener.Update();
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomEx3dListener'/>
	 * <seealso cref='CriAtomExPlayer::Set3dSource'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 */
	public void Set3dListener(CriAtomEx3dListener listener)
	{
		criAtomExPlayer_Set3dListenerHn(this.handle, ((listener == null) ? IntPtr.Zero : listener.nativeHandle));
	}


	/**
	 * Specifies the playback start position  -- 猜测: 从一个声音的中间位置开始播放;

	 * <param name='startTimeMs'> Playback start position (in milliseconds)</param>
	 
	 * Specifies the position to start playing the sound played by the AtomExPlayer.
	 * If you want to play the sound data from the middle, you need to specify the playback start position
	 * using this function before starting playback.
	
	 * The playback start position is specified in milliseconds.
	 * For example, if you set start_time_ms to 10000 and call this function,
	 * the sound data to be played next will be played from the position of 10 seconds.

	 注意:
	 * When playing a sound from the middle of the data, the play timing delays compared to when playing it from the beginning.
	 * This is because the system must analyze the header of the sound data,
	 * jump to the specified position, and rereads the data to start playback.</para>
	---
	若想从中间位置播放一个声音, 将比从头播放 更加延迟; 因为系统必须解析 声音数据的 header, 跳到特定位置, 再重读数据, 以开始播放;


	 * A 64bit value can be set for startTimeMs, but currently it is not possible to specify a playback time exceeding 32bit value.
	 
	 * The encrypted ADX data must be sequentially decrypted from the beginning of the data.<br/>
	 * Therefore, if the encrypted ADX data is played back from the middle,
	 * decryption must be done up to the seek position when starting playback,
	 * which may result in a significant load.<br/>
	 ---
	 加密的ADX数据必须从数据开始按顺序解密; 所以此时若想从中间位置播放这个声音, 它的加载时间会明显变长;
	 
	 * When the Sequence is played back specifying the playback start position,
	 * the waveform data placed before the specified position is not played.
	 * (Individual waveforms in the Sequence are not played from the middle.)
	
	 */
	public void SetStartTime(long startTimeMs)
	{
		criAtomExPlayer_SetStartTime(this.handle, startTimeMs);
	}


	/**
	 * Sets the playback start Block (Block index specified)

	 * <param name='index'>Block index</param>

	 * Associates the playback start Block index with the AtomExPlayer.

	 * After specifying the playback start Block index with this function, when the Block Sequence Cue
	 * is started using the ::CriAtomExPlayer::Start function, playback starts from the specified Block.</para>
	---
	当使用本函数设置了 block idx 后, 再调用 start() 开始播放声音, 此时会从指定的 block 开始播放;

	注意:
	 * The default Block index of the AtomExPlayers is 0;
	 * If the Cue set in the player at the start of playback by the ::CriAtomExPlayer::Start function
	 * is not a Block sequence, the value set by this function is not used.
	---
	若目标 cue 不是一个 block sequence, 那么使用本函数设置的东西 将不会被用到;

	 * If there is no Block corresponding to the specified index, playback starts from the first Block.
	 * At this time, a warning is issued indicating that there is no Block at the specified index.
	 ---
	 如果声音数据中 不存在参数指定的那个 idx block, 播放将从第一个 block 开始;
	 此时会报出 warning;

	注意:
	 * Use the ::CriAtomExPlayback::SetNextBlockIndex function to move Block after starting playback,
	 * and use the ::CriAtomExPlayback::GetCurrentBlockIndex function to get the Block index during playback.
	---
	 
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // 音声データをセット
	 * player.SetCue(acb, 300);
	 * // 開始ブロックをセット
	 * player.SetFirstBlockIndex(1);
	 * // セットされた音声データを再生
	 * player.Start();
	 

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayback::SetNextBlockIndex'/>
	 * <seealso cref='CriAtomExPlayback::GetCurrentBlockIndex'/>
	 */
	public void SetFirstBlockIndex(int index)
	{
		criAtomExPlayer_SetFirstBlockIndex(this.handle, index);
	}



	/**
	 * Sets the selector information

	 * <param name='selector'>Selector name</param>
	 * <param name='label'>Label name</param>
	 
	 * Assigns the Selector name and Label name to the player. Up to 8 can be assigned to each player.
	 * If a Cue that has Selector Labels assigned to its tracks is played, only the track that matches the Selector Label
	 * specified with this function will be played.<br/>

	 * Call ::CriAtomExPlayer::UnsetSelectorLabel 	to unassign a specific label from the player.<br/>
	 * Call ::CriAtomExPlayer::ClearSelectorLabels 	to unassign all labels.<br/>
	 * Call ::CriAtomExPlayer::ResetParameters 		to remove all the player's settings, including label information.<br/></para>
	 
	 * <seealso cref='CriAtomExPlayer::UnsetSelectorLabel'/>
	 * <seealso cref='CriAtomExPlayer::ClearSelectorLabels'/>
	 * <seealso cref='CriAtomExPlayer::ResetParameters'/>
	 */
	public void SetSelectorLabel(string selector, string label)
	{
		criAtomExPlayer_SetSelectorLabel(this.handle, selector, label);
	}



	/**
	 * Removes the selector information that is set.

	 * <param name='selector'>Selector name</param>
	
	 * Removes the information related to a selector name from the player, as well as any label names associated with it. 
	 * After removal, you can call ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll ,
	 * to also remove the selector information from voices that are already playing. However, the voices themselves will not stop.

	 * <seealso cref='CriAtomExPlayer::SetSelectorLabel'/>
	 * <seealso cref='CriAtomExPlayer::ClearSelectorLabels'/>
	 */
	public void UnsetSelectorLabel(string selector)
	{
		criAtomExPlayer_UnsetSelectorLabel(this.handle, selector);
	}


	/**
	 * Deletes all selector information
	
	 * <para header='Description'>Delete all selector name and label name information set in the player.<br/>
	 * Also, after deleting, by calling ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll ,
	 * you can delete the selector information for the sound that is already being played, but the sound does not stop.</para>

	 * <seealso cref='CriAtomExPlayer::ResetParameters'/>
	 */
	public void ClearSelectorLabels()
	{
		criAtomExPlayer_ClearSelectorLabels(this.handle);
	}


	/**
	 * Sets the Category (ID specified)

	 * <param name='categoryId'>Category ID</param>
	
	 * Sets the Category by specifying the Category ID.
	 * To remove the configured Category information, the ::CriAtomExPlayer::UnsetCategory function.

	 注意:
	 * When playing a Cue, if this function is called, the Category setting set on the
	 * data is "overridden" (the setting on the data is ignored).
	---
	本函数的设置 会覆盖 cue 数据自己的设置; (数字自己的设置会被忽略)

	 * The Category information set by this function is cleared when ACF is registered and unregistered.
	 * The default Category is available when ACF is not registered.

	 注意:
	 * Make the Category settings before starting playback. If you update the Category settings for the sound being played,
	 * the playback count of the Category may be incorrect.
	---
	必须先调用本函数, 再调用 start()

	 * <seealso cref='CriAtomExPlayer::UnsetCategory'/>
	 * <seealso cref='CriAtomExPlayer::SetCategory(string)'/>
	 */
	public void SetCategory(int categoryId)
	{
		criAtomExPlayer_SetCategoryById(this.handle, (uint)categoryId);
	}


	/**
	 * Sets Category (Category name specified)

	 * <param name='categoryName'>Category name</param>

	 * <para header='Description'>Set the Category by specifying the Category name.<br/>
	 * To remove the configured Category information, the ::CriAtomExPlayer::UnsetCategory function.<br/></para>
	 * <para header='Note'>The basic specification is the same as the ::CriAtomExPlayer::SetCategory(int) function, except that the Category is specified by name.</para>

	 * <seealso cref='CriAtomExPlayer::UnsetCategory'/>
	 * <seealso cref='CriAtomExPlayer::SetCategory(int)'/>
	 */
	public void SetCategory(string categoryName)
	{
		criAtomExPlayer_SetCategoryByName(this.handle, categoryName);
	}



	/**
	 * >Removes Category
	 * Deletes the Category information set in the player handle.

	 * <seealso cref='CriAtomExPlayer::SetCategory'/>
	 */
	public void UnsetCategory()
	{
		criAtomExPlayer_UnsetCategory(this.handle);
	}


	/**
	 * Sets the Cue priority to the AtomExPlayer.

	 * <param name='priority'>Cue priority</param>

	 * After setting the Cue priority using this function, when a sound is played using the ::CriAtomExPlayer::Start function,
	 * the sound is played with the Cue priority set in this function.<br/>

	 * The default before calling the function is 0.

	 注意:
	 * When the AtomExPlayer plays a Cue, if the category to which the Cue belongs has already reached
	 * its Voice count limit, Voice control is done based on the priority.
	 * Specifically, if the AtomExPlayer's play request has higher priority than that of the Cue being played,
	 * the AtomExPlayer stops the Cue being played and starts playing the requested Cue.<br/>
	 * (The sound being played is stopped and another sound is played.)<br/>
	---

	 * Conversely, if the AtomExPlayer's playback request is lower than the priority of the Cue currently being played,
	 * the AtomExPlayer's playback request is rejected.<br/>
	 * (The requested Cue will not be played.)<br/>
	 ---

	 * When the playback request of the AtomExPlayer is equal to the priority of the Cue currently being played,
	 * the AtomExPlayer controls the Voice with the last-arrival priority.
	 ---
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::ResetParameters'/>
	 */
	public void SetCuePriority(int priority)
	{
		criAtomExPlayer_SetCuePriority(this.handle, priority);
	}


	/**
	 * Sets the Voice Priority

	 * <param name='priority'> Voice Priority (-255 to 255) </param>
	
	 * Sets the Voice Priority to the AtomExPlayer.
	 * After setting the priority using this function, when a sound is played using the ::CriAtomExPlayer::Start function,
	 * the sound is played with the priority set in this function.
	 * In addition, by calling the ::CriAtomExPlayer::Update and ::CriAtomExPlayer::UpdateAll functions after setting,
	 * you can update the priority of the sound already played.
	
	 * For Voice Priority, specify an integer in the range of -255 to 255.
	 * If you set a value outside the range, it will be clipped to fit the range.
	 * The default before calling the function is 0.

	注意:
	 * When the AtomExPlayer tries to play waveform data,
	 * if the number of Voices in the Voice limit group to which the waveform data belongs has reached the upper limit,
	 * or if all the Voices in the Voice Pool are in use,
	 * the Voices are controlled based on the priority.
	 * (The Voice Priority is used to determine whether to play the specified waveform data.)
	
	 
	 * Specifically, if the priority of the waveform data to be played is
	 * higher than that of the waveform data currently being played in the Voice,
	 * the AtomExPlayer steals the Voice being played and starts playing the requested waveform data.
	 * (The sound being played is stopped and another sound is played.)
	
	 * Conversely, if the priority of the waveform data to be played is
	 * lower than that of the waveform data currently being played in the Voice,
	 * the AtomExPlayer does not play the requested waveform data.
	 * (The requested sound is not played, and the sound being played continues to be played.)
	 
	 * If the priority of the waveform data to be played is the same as
	 * that of the waveform data currently being played in the Voice,
	 * the AtomExPlayer performs the following control according to the
	 * Voice control method (first-priority or last-priority).

	 * - When in the first-priority mode, the priority is given to the waveform data being played, and the requested waveform data is not played.
	 * - When in the last-priority mode, the priority is given to the requested waveform data and the Voice is stolen.

	
	 * When playing a Cue, if this function is called when the Voice Priority is set on the data,
	 * the "sum" of the value set on the data and the setting in this function is applied.
	 * For example, if the priority of the data is 255 and the priority of the AtomExPlayer is 45,
	 * the priority actually applied is 300.

	 * The range of values that can be set using this function is -255 to 255, but since the calculation inside the library
	 * is done within the range of int, the result of adding the values on data may exceed the range of -255 to 255.

	 * This function controls the "Voice Priority" set in the waveform data.
	 * Note that it does not affect the "Category Cue Priority" set for the Cue
	 * on Atom Craft.
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 * <seealso cref='CriAtomExPlayer::SetVoiceControlMethod'/>
	 */
	public void SetVoicePriority(int priority)
	{
		criAtomExPlayer_SetVoicePriority(this.handle, priority);
	}



	/**
	 * Specifies the Voice control method
	 * <param name='method'>Voice control method
	 
	 * Sets the Voice control method to the AtomExPlayer.
	 * After setting the Voice control method using this function, when you play a sound using the ::CriAtomExPlayer::Start function,
	 * the control method specified by this function is applied to the waveform data played by the player.
	 * The default before calling the function is last-priority ( PreferLast ).

	 * When the AtomExPlayer tries to play waveform data,
	 * if the number of Voices in the Voice limit group to which the waveform data belongs has reached the upper limit,
	 * or if all the Voices in the Voice Pool are in use, the Voices are controlled based on the Voice Priority.

	 
	 * The Voice control method set by this function is considered in the Voice control
	 * when the priority of the waveform data to be played back is
	 * the same as that of the waveform data being played back.<br/>
	 * (For details on the Voice control by Voice Priority,
	 * see the Description of the ::CriAtomExPlayer::SetVoicePriority function.)

	 * <para header='Known defects'>Currently, even if a Voice control method is specified using this function,
	 * the Voice control method set on the authoring tool is applied preferentially when playing a Cue.<br/></para>
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::SetVoicePriority'/>
	 * <seealso cref='CriAtomEx.VoiceControlMethod'/>
	 */
	public void SetVoiceControlMethod(CriAtomEx.VoiceControlMethod method)
	{
		criAtomExPlayer_SetVoiceControlMethod(this.handle, method);
	}


	/**
	 * Set the pre-delay time   ---   预延迟时间;

	 * <param name='time'>Pre-delay time (0.0f to 2000.0f)
	
	 * After setting the pre-delay time using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the system waits for the pre-delay time before playback.<br/>

	 * For the pre-delay time, specify a real value in the range of 0.0f to 10000.0f. The unit is ms (millisecond).<br/>
	 * The default for pre-delay time is 0.0f.

	 * When playing a Cue, if this function is called when the pre-delay time is set on the data,
	 * the value set on the data and the setting in this function is "added" and the result is applied.

	 * It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function during playback.

	 * Pre-delay is not applied to the sound data encoded for HCA-MX.
	 --- 
	 HCA-MX 格式不支持本功能;

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetPreDelayTime(float time)
	{
		criAtomExPlayer_SetPreDelayTime(this.handle, time);
	}


	/**
	 * Sets the Envelope Attack Time;  --- 波封起音时间;

	 * <param name='time'>Attack time (0.0f to 2000.0f)</param>

	 * <para header='Description'>Sets the attack time of the envelope.<br/>

	 * After setting the attack time using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified attack time.<br/>

	 * For the attack time, specify a real value from 0.0f to 2000.0f. The unit is ms (millisecond).<br/>
	 * The default attack time is 0.0f.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the attack time is set on the data,
	 * the value set on the data is <b>overridden</b> (the setting on the data is ignored).<br/></para>
	 * <para header='Note'>It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function during playback.<br/>

	 * The envelope is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use the envelope, encode the data using another codec such as ADX or HCA.<br/></para>

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // エンベロープの設定
	 * player.SetEnvelopeAttackTime(10.0f);
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetEnvelopeAttackTime(float time)
	{
		criAtomExPlayer_SetEnvelopeAttackTime(this.handle, time);
	}


	/**
	 * Sets the envelope hold time; --- 波封 hold 时间;

	 * <param name='time'>Hold time (0.0f to 2000.0f)</param>
	
	 * <para header='Description'>Sets the hold time of the envelope.<br/>
	 * After setting the hold time using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified hold time.<br/>
	
	 * For the hold time, specify a real value in the range of 0.0f to 2000.0f. The unit is ms (millisecond).<br/>
	 * The default hold time is 0.0f.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the hold time is set on the data,
	 * the value set on the data is <b>overridden</b> (the setting on the data is ignored).<br/></para>
	 * <para header='Note'>It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function
	 * during playback.<br/>

	 * Envelope is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use envelope, encode the data using another codec such as ADX or HCA.<br/></para>

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // エンベロープの設定
	 * player.SetEnvelopeHoldTime(player, 20.0f);
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetEnvelopeHoldTime(float time)
	{
		criAtomExPlayer_SetEnvelopeHoldTime(this.handle, time);
	}


	/**
	 * Sets the envelope decay time --- 波封 hold 时间;

	 * <param name='time'>Decay time (0.0f to 2000.0f)</param>
	 * <remarks>
	 * <para header='Description'>Sets the decay time of the envelope.<br/>
	 * After setting the decay time using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified decay time.<br/>
	 * <br/>
	 * For the decay time, specify a real value in the range of 0.0f to 2000.0f. The unit is ms (millisecond).<br/>
	 * The default decay time is 0.0f.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the decay time is set on the data,
	 * the value set on the data is <b>overridden</b> (the setting on the data is ignored).<br/></para>
	 * <para header='Note'>It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function
	 * during playback.<br/>
	 * <br/>
	 * Envelope is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use envelope, encode the data using another codec such as ADX or HCA.<br/></para>


	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // エンベロープの設定
	 * player.SetEnvelopeDecayTime(player, 10.0f);
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetEnvelopeDecayTime(float time)
	{
		criAtomExPlayer_SetEnvelopeDecayTime(this.handle, time);
	}


	/**
	 * Sets the envelope release time --- 波封 释音时间;

	 * <param name='time'>Release time (0.0f to 10000.0f)</param>

	 * <para header='Description'>Sets the release time of the envelope.<br/>
	 * After setting the release time using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified release time.<br/>

	 * For the release time, specify a real value in the range of 0.0f to 10000.0f. The unit is ms (millisecond).<br/>
	 * The default release time value is 0.0f.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the release time is set on the data,
	 * the value set on the data is <b>overridden</b> (the setting on the data is ignored).<br/></para>
	 * <para header='Note'>It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function
	 * during playback.<br/>

	 * Envelope is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use envelope, encode the data using another codec such as ADX or HCA.<br/></para>


	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // エンベロープの設定
	 * player.SetEnvelopeReleaseTime(player, 3000.0f);
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetEnvelopeReleaseTime(float time)
	{
		criAtomExPlayer_SetEnvelopeReleaseTime(this.handle, time);
	}


	/**
	 * Sets the envelope sustain level -- 波封 延迟时间

	 * <param name='level'>Sustain level (0.0f to 2000.0f)</param>
	 * <remarks>
	 * <para header='Description'>Sets the sustain level of the envelope.<br/>
	 * After setting the sustain level using this function, when you start the playback using the ::CriAtomExPlayer::Start function,
	 * the sound is played back using the specified sustain level.<br/>
	 * <br/>
	 * Specify a real value in the range of 0.0f to 1.0f for the sustain level.<br/>
	 * The default sustain level is 0.0f.<br/></para>
	 * <para header='Note'>When playing a Cue, if this function is called when the sustain level is set on the data,
	 * the value set on the data is <b>overridden</b> (the setting on the data is ignored).<br/></para>
	 * <para header='Note'>It cannot be updated using ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function
	 * during playback.<br/>
	 * <br/>
	 * Envelope is not applied to the sound data encoded for HCA-MX.<br/>
	 * If you want to use envelope, encode the data using another codec such as ADX or HCA.<br/></para>
	 * </remarks>
	 * <example><code>
	 *  ：
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 *  ：
	 * // エンベロープの設定
	 * player.SetEnvelopeSustainLevel(0.5f);
	 *
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 *  ：
	 * </code></example>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public void SetEnvelopeSustainLevel(float level)
	{
		criAtomExPlayer_SetEnvelopeSustainLevel(this.handle, level);
	}



	/**
	 * Attach a fader to a player
	
	 * <para header='Description'>Attach a fader to a player and turn the
	 * CriAtomExPlayer into a player dedicated to crossfade.<br/>
	 * (Some functions of conventional CriAtomExPlayer such as simultaneous playback of multiple sounds will not be available.)<br/>
	 * <br/>
	 * The player to which the fader is attached by this function performs
	 * the following control every time the sound playback is started (every time the
	 * ::CriAtomExPlayer::Start or ::CriAtomExPlayer::Prepare function is called).<br/>
	 * - Forcibly stops sounds being faded out if any.
	 * - Fades out the sound currently being played (or faded in).
	 * - Fades in the sound newly started to be played.
	 * .
	 * <br/>
	 * The following controls are performed when stopping playback (when the
	 * ::CriAtomExPlayer::Stop function is called).<br/>
	 * - Forcibly stops sounds being faded out if any.
	 * - Fades out the sound currently being played (or faded in).</para>
	 * <para header='Note'>If the player to attach the fader is playing sounds, all the sounds being played by the player
	 * stops when this function is called.<br/>
	 * <br/>
	 * Each time the function ::CriAtomExPlayer::Start or ::CriAtomExPlayer::Stop
	 * is called for the player a fader is attached to,
	 * the fader does the following control for the sound being played by the player.<br/>
	 * <br/>
	 * -# If there are any sounds already faded out, it stops it immediately.
	 * -# If there are any sounds being faded in (or sounds being played),
	 * it fades out the sounds from the current volume over the time
	 * specified in the ::CriAtomExPlayer::SetFadeOutTime function.
	 * -# When the ::CriAtomExPlayer::Start function is called,
	 * the fader starts playing the sound data set to the player at volume 0,
	 * and fades it in over the time specified in the ::CriAtomExPlayer::SetFadeInTime function.
	 * .
	 * <br/>
	 * (When the ::CriAtomExPlayer::Prepare function is used instead of the
	 * ::CriAtomExPlayer::Start function, the above control is performed when unpaused.)<br/></para>
	 * <para header='Note'>When this function is called, the play/stop operation for CriAtomExPlayer changes significantly.<br/>
	 * (The behavior changes significantly before/after attaching a fader.)<br/>
	 * Specifically, the number of Voices that can be played simultaneously is limited to 1 (2 only during crossfading),
	 * and the control using ::CriAtomExPlayback will not be available.<br/>
	 * <br/>
	 * This function is necessary only when you want to perform crossfading.<br/>
	 * Use envelope or Tween for Fade-in/out of only one sound.<br/>
	 * <br/>
	 * Due to the operational specifications of the fader, the Fade-in/out process is
	 * only applicable to the past two sound playbacks.<br/>
	 * Sound played before that is forcibly stopped when the
	 * ::CriAtomExPlayer::Start or ::CriAtomExPlayer::Stop function is called.<br/>
	 * Unintended noise may occur at the timing of forced stop, so be careful that
	 * the number of simultaneous playbacks does not exceed 3 sounds.<br/>
	 * <br/>
	 * Fade-in/out works only for the "operation on the AtomExPlayer".<br/>
	 * If you call the ::CriAtomExPlayback::Stop function for the playback ID
	 * obtained by calling ::CriAtomExPlayer::Start function, Fade-out is not activated.<br/>
	 * (Fader settings are ignored and the playback stops immediately.)<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::DetachFader'/>
	 */
	public void AttachFader()
	{
		criAtomExPlayer_AttachFader(this.handle, IntPtr.Zero, IntPtr.Zero, 0);
	}


	/**
	 * Remove a fader from a player

	 * <para header='Description'>Detaches (remove) the fader from the player.<br/>
	 * Fade-in/out process is no longer performed on the player whose fader is detached using this function.<br/></para>
	 * <para header='Note'>If the player to detach the fader is playing sounds, all the sounds being played by the player
	 * stops when this function is called.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::AttachFader'/>
	 */
	public void DetachFader()
	{
		criAtomExPlayer_DetachFader(this.handle);
	}


	/**
	 * Sets the Fade-out time --- 淡出时间;

	 * <param name='ms'>Fade out time (in milliseconds)</param>

	 * >Specifies the Fade-out time for players with faders attached.

	 * The next time a sound is played back (when the ::CriAtomExPlayer::Start function is called),
	 * the sound being played is faded out over the time set in this function.<br/>
	 * <br/>
	 * The default for Fade-out time is 500 milliseconds.<br/></para>
	 * <para header='Note'>If the Fade-out time is set, CriAtomExPlayer stops the playback in the following order:<br/>
	 * <br/>
	 *  -# Lowers the volume of the sound to 0 over the specified time.
	 *  -# Continues the playback until delay time elapses with volume set to 0.
	 *  -# Stops the playback after the delay time elapses.
	 *  .
	 * <br/>
	 * The volume control during Fade-out is performed before the sound playback is stopped.<br/>
	 * Therefore, the envelope release time preset in the waveform data is ignored.<br/>
	 * (Strictly speaking, the envelope release process is applied after the volume reaches 0.)<br/>
	 * <br/>
	 * The behavior differs in the case where the second argument ( ms )
	 * is set to 0 from the case where it is set to -1 as follows:<br/>
	 * <br/>
	 *  - When 0 is specified: The volume is immediately lowered to 0 and the sound is stopped.
	 *  - When -1 is specified: The sound is stopped without changing the volume.
	 *  .
	 * <br/>
	 * If you want to enable the envelope release process preset in the waveform
	 * without performing Fade-out process when stopping playback,
	 * specify -1 for the second argument ( ms ).<br/>
	 * By specifying -1, volume control by Fade-out process will not be done,
	 * so after calling the ::CriAtomExPlayer::Stop function, normal stopping process is done.<br/>
	 * (If the envelope release is set to the waveform data, the release process is performed.)<br/></para>
	 * <para header='Note'>Before calling this function, you need to attach faders to the player
	 * in advance using the ::CriAtomExPlayer::AttachFader function.<br/>
	 * <br/>
	 * The value set by this function does not affect the sounds that being played.<br/>
	 * The fade time set by this function is applied when the ::CriAtomExPlayer::Start
	 * or ::CriAtomExPlayer::Stop function is called after calling this function.<br/>
	 * (For the sound that is being faded out, it is not possible to change
	 * Fade-out time afterward using this function.)<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::AttachFader'/>
	 * <seealso cref='CriAtomExPlayer::SetFadeInTime'/>
	 */
	public void SetFadeOutTime(int ms)
	{
		criAtomExPlayer_SetFadeOutTime(this.handle, ms);
	}


	/**
	 * Sets the Fade-in time  ---  淡入时间

	 * <param name='ms'>Fade-in time (in millisecond)</param>
	 * <remarks>
	 * <para header='Description'>Specifies the Fade-in time for players with faders attached.<br/>
	 * The next time a sound is played back (when the ::CriAtomExPlayer::Start function is called),
	 * it is newly faded in over the time set in this function.<br/>
	 * <br/>
	 * The default for Fade-in time is 0 second.<br/>
	 * Therefore, if this function is not used, Fade-in does not occur,
	 * and the playback of the sound starts immediately at full volume.<br/></para>
	 * <para header='Note'>Before calling this function, you need to attach faders to the player
	 * in advance using the ::CriAtomExPlayer::AttachFader function.<br/>
	 * <br/>
	 * The value set by this function does not affect the sounds that being played.<br/>
	 * The fade time set by this function is applied when the ::CriAtomExPlayer::Start function
	 * is called after calling this function.<br/>
	 * (For the sound that is being faded in, it is not possible to change
	 * Fade-in time afterward using this function.)<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::AttachFader'/>
	 * <seealso cref='CriAtomExPlayer::SetFadeInTime'/>
	 */
	public void SetFadeInTime(int ms)
	{
		criAtomExPlayer_SetFadeInTime(this.handle, ms);
	}


	/**
	 * Sets the Fade-in start offset

	 * <param name='ms'>Fade-in start offset (in millisecond)</param>
	
	 * <para header='Description'>Specifies the Fade-in start offset for players with faders attached.<br/>
	 * By using this function, the timing to start the Fade-in can be advanced or delayed
	 * for any time with respect to the Fade-out.<br/>
	 * For example, if you set the Fade-out time to 5 seconds and the Fade-in start offset to 5 seconds,
	 * you can fade in the next sound immediately after the Fade-out completes in 5 seconds.<br/>
	 * Conversely, if you set the Fade-in time to 5 seconds and the Fade-in start offset to -5 seconds,
	 * it is possible to start the Fade-out of the sound being played immediately after the Fade-in completes in 5 seconds.<br/>
	
	 * The default Fade-in start offset is 0 second.<br/>
	 * (Fade-in and Fade-out start at the same time.)<br/></para>
	 * <para header='Note'>The Fade-in starts when the sound to be faded in is ready to be played back.<br/>
	 * Therefore, even if the Fade-in start offset is set to 0 second, if it takes time
	 * to buffer the Fade-in sound, (such as in stream playback),
	 * it may take some time before the Fade-out starts.<br/>
	 * (This parameter is a relative value for adjusting the timing of Fade-in and Fade-out.)<br/></para>
	 * <para header='Note'>Before calling this function, you need to attach faders to the player
	 * in advance using the ::CriAtomExPlayer::AttachFader function.<br/>
	
	 * The value set by this function does not affect the sounds that being played.<br/>
	 * The fade time set by this function is applied when the ::CriAtomExPlayer::Start
	 * or ::CriAtomExPlayer::Stop function is called after calling this function.<br/>
	 * (For a Voice that has already started fading,
	 * the fading timing cannot be changed later with this function.)<br/></para>
	
	 * <seealso cref='CriAtomExPlayer::AttachFader'/>
	 * <seealso cref='CriAtomExPlayer::SetFadeInTime'/>
	 */
	public void SetFadeInStartOffset(int ms)
	{
		criAtomExPlayer_SetFadeInStartOffset(this.handle, ms);
	}


	/**
	 * Sets the delay time after Fade-out

	 * <param name='ms'>Delay time after Fade-out (in milliseconds)</param>
	
	 * <para header='Description'>You can set the delay time before discarding the Voice after the Fade-out completes.<br/>
	 * By using this function, you can set the timing when the Voice that was faded out is discarded.<br/>
	
	 * The default delay time is 500 milliseconds.<br/>
	 * (The Voices playing a Fade-out sound are discarded 500 milliseconds after the volume is set to 0.)<br/></para>
	 * <para header='Note'>It is not necessary to use this function except on platforms where the Voice is stopped
	 * before the sound is stopped before finishing Fade-out.<br/></para>
	 * <para header='Note'>Before calling this function, you need to attach faders to the player
	 * in advance using the ::CriAtomExPlayer::AttachFader function.<br/>
	 * <br/>
	 * The value set by this function does not affect the sounds that are being played.<br/>
	 * The fade time set by this function is applied when the ::CriAtomExPlayer::Start
	 * or ::CriAtomExPlayer::Stop function is called after calling this function.<br/>
	 * (For the sound that is being faded out, it is not possible to change
	 * delay time after Fade-out afterward using this function.)<br/>
	 * <br/>
	 * The timing when volume control and Voice stop are reflected depends on the platform.<br/>
	 * Therefore, if 0 is specified in this function, the Voice may be stopped before the volume change
	 * takes effect depending on the platform.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::AttachFader'/>
	 */
	public void SetFadeOutEndDelay(int ms)
	{
		criAtomExPlayer_SetFadeOutEndDelay(this.handle, ms);
	}


	/**
	 * Gets whether the fading is in process;

	 * <returns>Whether the fading is in progress</returns>
	
	 * <para header='Description'>Gets whether the fading process is in progress.<br/></para>
	 * <para header='Note'>This function returns True during the following processing period.<br/>
	 * - Waiting for synchronization for starting crossfade.
	 * - During Fade-in/Fade-out process (while changing volume).
	 * - During the delay period after completing Fade-out.</para>
	 */
	public bool IsFading()
	{
		return criAtomExPlayer_IsFading(this.handle);
	}


	/**
	 * Initializes the fader parameters

	 * <para header='Description'>Clears various parameters set in the fader and returns them to the initial values.<br/></para>
	 * <para header='Note'>Before calling this function, you need to attach faders to the player
	 * in advance using the CriAtomExPlayer::AttachFader function.<br/>

	 * Clearing the fader parameter using this function does not affect the sound that is being played.<br/>
	 * The fader parameters cleared by this function is applied when the CriAtomExPlayer::Start
	 * or CriAtomExPlayer::Stop function is called after calling this function.<br/>
	 * (For a Voice that has already started fading, the
	 * fader parameters cleared by this function cannot be changed)<br/></para>
	 * </remarks>
	 */
	public void ResetFaderParameters()
	{
		criAtomExPlayer_ResetFaderParameters(this.handle);
	}


	/**
	 * Specifies the group number
	
	 * Specifies from which Voice limit group the Voice is taken when playing.
	
	 * If group_no is set to -1, the player will not be restricted by the Voice limit group.<br/>
	 *  (If there are any free Voices or Voices with a lower priority,<br/>
	 *  the Voice is taken regardless of Voice Limit group.)<br/></para>
	 * <para header='Note'>When playback starts and all the Voices in the specified Voice limit group are in use,
	 * whether or not the sound is played is determined by the Voice Priority control.<br/>
	 * (For details on the Voice control by Voice Priority,
	 * see the Description of the ::CriAtomExPlayer::SetVoicePriority function.<br/>
	
	 * When playing a Cue, if this function is called, the Voice limit group setting<br/>
	 * set on the data is overridden (the setting on the data is ignored).<br/>
	 * However, if -1 is specified for group_no, the Voice limit group set on data is referenced.<br/>
	
	 */
	public void SetGroupNumber(int group_no)
	{
		criAtomExPlayer_SetGroupNumber(this.handle, group_no);
	}



	/**
	 * Updates the playback parameters (by CriAtomExPlayback object) -- 仅 update 某个特定的 playback;

	 * <param name='playback'>CriAtomExPlayback object</param>
	 
	 * Updates the parameters of the CriAtomExPlayback object using the playback parameters
	 * set in the AtomExPlayer (including the AISAC control values).

	 * <para header='Note'>The CriAtomExPlayer object used for parameter update must be
	 * the player that generated the CriAtomExPlayback object.
	
	
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 * // ボリュームの変更
	 * // 注意）この時点では再生中の音声のパラメータは変更されない。
	 * player.SetVolume(volume);
	 * // プレーヤに設定されたパラメータを再生中の音声にも反映
	 * player.Update(playback);

	
	 * <seealso cref='CriAtomExPlayer::UpdateAll'/>
	 */
	public void Update(CriAtomExPlayback playback)
	{
		criAtomExPlayer_Update(this.handle, playback.id);
	}


	/**
	 * Updates the playback parameters (for all sounds being played)
	 
	 * <para header='Description'>Updates the playback parameters for all sounds being played by this AtomExPlayer
	 * using the playback parameters set in the AtomExPlayer (including the AISAC control values).
	
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // 再生の開始
	 * CriAtomExPlayback playback = player.Start();
	 * // ボリュームの変更
	 * // 注意）この時点では再生中の音声のパラメータは変更されない。
	 * player.SetVolume(volume);
	 * // プレーヤに設定されたパラメータを再生中の全ての音声に反映
	 * player.UpdateAll();
	
	
	 * <seealso cref='CriAtomExPlayer::Update'/>
	 */
	public void UpdateAll()
	{
		criAtomExPlayer_UpdateAll(this.handle);
	}



	/**
	 * Initializes the playback parameters; -- 重置为初始值
	
	 * Resets the playback parameters (including AISAC control values) set in the AtomExPlayer
	 * to the initial state (unset state).

	 * After calling this function, if you start playback using the ::CriAtomExPlayer::Start function, 
	   the sound is played using the default playback parameters.

	注意:
	 * Even if the ::CriAtomExPlayer::Update or ::CriAtomExPlayer::UpdateAll function is called after calling this function,
	 * the parameters of the sound already being played does not return to the initial values.
	 * If you want to change the parameters of the sound that is already being played,
	 * call the ::CriAtomExPlayer::SetVolume function explicitly.
	 ---
	 调用本函数后, 就算使用 update 系列函数也是无法重置 那些 已经播放的声音的;
	 此时该调用 CriAtomExPlayer::SetVolume();
	
	 
	 * The following parameters are reset by this function.
	 * - Parameters defined in ::CriAtomEx::Parameter
	 * - AISAC control value (set by the ::CriAtomExPlayer::SetAisacControl function)
	 * - Cue priority (set by the ::CriAtomExPlayer::SetCuePriority function)
	 * - 3D sound source handle (set by the ::CriAtomExPlayer::Set3dSource function)
	 * - 3D listener handle (set by the ::CriAtomExPlayer::Set3dListener function)
	 * - Category setting (set by the ::CriAtomExPlayer::SetCategory function)
	 * - Playback starting Block (set by the ::CriAtomExPlayer::SetFirstBlockIndex function)
	 ---
	 上述这些参数 会被重置;
	
	 * Note that this function does not reset the parameters (position etc.) held by the 3D sound source handle or 3D listener handle itself.
	 * It only resets "What handle is set on the AtomExPlayer".
	 * If you want to reset the parameters of these handles themselves,
	 * call the parameter reset function of each handle.
	 ---
	 本函数没法重置 3d声源 3d接听;

	
	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // ボリュームの変更
	 * player.SetVolume(0.5f);
	 * // 音声を再生
	 * // 備考）ここで再生された音声は0.5fのボリュームで出力される。
	 * CriAtomExPlayback playback1 = player.Start();
	 * // プレーヤに設定されたパラメータをリセット
	 * // 備考）プレーヤのボリューム設定がデフォルト値（1.0f）に戻される。
	 * player.ResetParameters();
	 * // 別の音を再生
	 * // 備考）ここで再生された音声は1.0fのボリュームで出力される。
	 * CriAtomExPlayback playback2 = player.Start();

	
	 * <seealso cref='CriAtomEx3dSource::ResetParameters'/>
	 * <seealso cref='CriAtomEx3dListener::ResetParameters'/>
	 */
	public void ResetParameters()
	{
		criAtomExPlayer_ResetParameters(this.handle);
	}


	/**
	 *  Gets the playback time
	 	Gets the playback time of the sound that was played last by the AtomExPlayer. 
		--
		上次被 player 播放的声音 的播放时间; --- 播放开始后经过的时间;

	 * <returns>Playback time (in milliseconds)</returns>
	 
	 * This function returns a value of 0 or greater if the playback time can be obtained.
	 * This function returns a negative value when the playback time cannot be obtained (when the Voice cannot be obtained).

	 注意
	 * When multiple sounds are played in a player and this function is called,
	 * this function returns the time of the "last" played sound.
	 ---
	 若 player 正在播放多个声音, 此时调用本函数, 将返回 最后一个播放的声音的 时间;

	 * If you need to check the playback time for multiple sounds,
	 * create as many players as the number of sounds to play, or
	 * use the ::CriAtomExPlayback::GetTime function.
	 ---
	 若想知道每个声音的 播放时间, 要么为每个声音分配一个 player, 或调用 CriAtomExPlayback::GetTime();

	 * The playback time returned by this function is "the elapsed time from the start of playback". -- 播放开始后经过的时间;
	 * The time does not rewind depending on the playback position,
	 * even during loop playback or seamless linked playback.
	
	 * When the playback is paused using the ::CriAtomExPlayer::Pause function,
	 * the playback time count-up also stops.<br/>
	 * (If you unpause the playback, the count-up resumes.)
	
	 * The accuracy of the time that can be obtained by this function depends on the frequency of the server processing.
	 * (The time is updated for each server process.)

	 * If you need to get more accurate time, use the
	 * ::CriAtomExPlayback::GetNumPlayedSamples function instead of
	 * this function to get the number of samples played.<br/></para>

	 注意:
	 * The return type is long, but currently there is no precision over 32bit.
	 * When performing control based on the playback time, it should be noted that the playback time becomes incorrect in about 24 days.
	 * (The playback time overflows and becomes a negative value when it exceeds 2147483647 milliseconds.)

	 * If the sound being played is erased by the Voice control,
	 * the playback time count-up also stops at that point.
	 * In addition, if Voice couldn't be allocated by the Voice control at the start of playback,
	 * this function does not return the correct time.
	 * (Negative value is returned.)

	 * Even if the sound data supply is temporarily interrupted due to read retry etc. on the drive,
	 * the playback time count-up is not interrupted.

	 * (The time progresses even if the playback is stopped due to the stop of data supply.)
	 * Therefore, when synchronizing sound with the source video based on the time acquired by this function,
	 * the synchronization may be greatly deviated each time a read retry occurs.

	 * If it is necessary to strictly synchronize the waveform data and video,
	 * use the ::CriAtomExPlayback::GetNumPlayedSamples function instead of this function
	 * to synchronize with the number of played samples.


	 * <seealso cref='CriAtomExPlayback::GetTime'/>
	 * <seealso cref='CriAtomExPlayback::GetNumPlayedSamples'/>
	 */
	public long GetTime()
	{
		return criAtomExPlayer_GetTime(this.handle);
	}



	/**
	 * Gets the status of the AtomExPlayer

	 * The status is one of following 5 values of type ::CriAtomExPlayer::Status
	 * indicating the playback status of the AtomExPlayer.<br/>
	 * -# Stop
	 * -# Prep
	 * -# Playing
	 * -# PlayEnd
	 * -# Error


	 * When you create an AtomExPlayer, the AtomExPlayer's status is Stop.<br/>
	 * By calling the ::CriAtomExPlayer::Start function after setting the sound data to be played,
	 * the status of the AtomExPlayer changes to ready ( Prep ).<br/>
	 * (Prep is the status waiting for the data to be supplied or start of decoding.)<br/>
	 * When enough data is supplied for starting playback, the AtomExPlayer changes the status to
	 * Playing and starts to output sound.<br/>
	 * When all the set data have been played back, the AtomExPlayer changes the status to
	 * PlayEnd.<br/>
	 * If an error occurs during playback, the AtomExPlayer changes the status to Error.<br/>

	 * By checking the status of AtomExPlayer and switching the processing depending on the status,
	 * it is possible to create a program linked to the sound playback status.<br/>
	 * For example, if you want to wait for the completion of sound playback before proceeding, use the following code.<code>

	 * // プレーヤの作成
	 * CriAtomExPlayer player = new CriAtomExPlayer();
	 * // 音声データをセット
	 * player.SetCue(acb, cueId);
	 * // セットされた音声データを再生
	 * player.Start();
	 * // 再生完了待ち
	 * while (player.GetStatus() != CriAtomExPlayer.Status.PlayEnd) {
	 *  yield return null;
	 * }


	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public Status GetStatus()
	{
		return criAtomExPlayer_GetStatus(this.handle);
	}


	/**
	 * Gets parameters (floating point numbers)

	 * <param name='id'>Parameter ID</param>
	 * <returns>Parameter setting</returns>
	
	 * Gets the values of various parameters set in the AtomExPlayer.<br/>
	 * The value is obtained as a floating point number.
	
	 * <seealso cref='CriAtomEx.Parameter'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterUint32'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterSint32'/>
	 */
	public float GetParameterFloat32(CriAtomEx.Parameter id)
	{
		return criAtomExPlayer_GetParameterFloat32(this.handle, id);
	}


	/**
	 * Gets parameters (unsigned integer)

	 * <param name='id'>Parameter ID</param>
	 * <returns>Parameter setting</returns>
	 * <remarks>
	 * <para header='Description'>Gets the values of various parameters set in the AtomExPlayer.<br/>
	 * The value is obtained as an unsigned integer.</para>
	 * </remarks>
	 * <seealso cref='CriAtomEx.Parameter'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterFloat32'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterSint32'/>
	 */
	public uint GetParameterUint32(CriAtomEx.Parameter id)
	{
		return criAtomExPlayer_GetParameterUint32(this.handle, id);
	}


	/**
	 * Gets parameters (signed integer)

	 * <param name='id'>Parameter ID</param>
	 * <returns>Parameter setting</returns>
	 * <remarks>
	 * <para header='Description'>Gets the values of various parameters set in the AtomExPlayer.<br/>
	 * The value is obtained as a signed integer.</para>
	 * </remarks>
	 * <seealso cref='CriAtomEx.Parameter'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterFloat32'/>
	 * <seealso cref='CriAtomExPlayer::GetParameterUint32'/>
	 */
	public int GetParameterSint32(CriAtomEx.Parameter id)
	{
		return criAtomExPlayer_GetParameterSint32(this.handle, id);
	}


	/**
	 * Sets the output sound renderer type

	 * <param name='type'>Output destination sound renderer type</param>
	 
	 * <para header='Description'>Sets the type of sound renderer used to play the AtomExPlayer.<br/>
	 * If you start playback after making this setting, a playable Voice is acquired from the Voice Pool
	 * created with the specified sound renderer type.<br/>
	 * The sound renderer type can be set for each Cue playback unit.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomEx.SoundRendererType'/>
	 */
	public void SetSoundRendererType(CriAtomEx.SoundRendererType type)
	{
		criAtomExPlayer_SetSoundRendererType(this.handle, type);
	}


	/**
	 * Sets the random number seed
	 * <param name='seed'>Random number seed</param>

	 * Sets the random number seed in the pseudo random number generator in the AtomExPlayer.
	 * By setting the random number seed, it is possible to add reproducibility to various random playback processes.
	
	
	 * <seealso cref='CriAtomEx.SetRandomSeed'/>
	 */
	public void SetRandomSeed(uint seed)
	{
		criAtomExPlayer_SetRandomSeed(this.handle, seed);
	}


	/**
	 * Switches on/off the loop playback

	 * <param name='sw'>Loop switch (True: loop mode, False: cancel loop mode)</param>
	
	 * Switches the loop playback ON/OFF for the waveform data that does not have a loop point.
	 * By default, loop is OFF.

	 * When loop playback is turned ON, the playback does not end at the end of the sound, and the playback is repeated from the beginning.<br/></para>
	 
	 注意:
	 * The setting in this function is applied to the waveform data.
	 * When this function is called for Sequence data,
	 * the individual waveform data in the Sequence data is played back in a loop.
	 ---
	 
	 * The specification by this function is valid only for waveform data that does not have loop points.<br/>
	 * When playing back waveform data with loop points, loop playback is performed
	 * according to the loop position of the waveform data regardless of the specification of this function.
	 ---
	只有那些没有设置 loop points 的 waveform data, 调用本函数才会起效;

	 * This function internally uses the seamless link playback feature. -- 无缝连接
	 * Therefore, if you use an format that does not support seamless linked playback (such as HCA-MX),
	 * some amount of silence is inserted at the loop position.
	 ---
	 对于 HCA-MX 这种不支持 无缝连接 的格式, loop 过程中可能会存在缝隙;

	 * You can change the loop switch only before starting playback.
	 * You cannot change the loop switch for the player that is playing sound.
	 ----
	 只有在调用 start() 之前, 才能设置 loop 模式, 对于那些正在播放声音的 player, 不能修改 loop 模式;
	 */
	public void Loop(bool sw)
	{
		if (sw)
		{
			/*(-1) JP<ループ回数制限なし   */
			/*(-2) JP<ループ情報を無視      */
			/*(-3) JP<強制ループ再生     */
			criAtomExPlayer_LimitLoopCount(this.handle, -3);
		}else
		{
			ushort CRIATOMPARAMETER2_ID_LOOP_COUNT = CriAtomPlugin.GetLoopCountParameterId();
			IntPtr player_parameter = criAtomExPlayer_GetPlayerParameter(this.handle);
			criAtomExPlayerParameter_RemoveParameter(player_parameter, CRIATOMPARAMETER2_ID_LOOP_COUNT);
		}
	}



	/**
	 * Specification of ASR Rack(支架) ID   --- asr: 一种 sound renderer type, 猜测为: Atom Sound Renderer

	 * <param name='asr_rack_id'>ASR Rack ID</param>
	
	 * Specifies the Voice destination ASR Rack ID.

	注意:
	 * This function is effective only when ASR is used as the sound renderer type of the Voice.
	 * (When using other Voices, the setting in this function is ignored.)
	
	 * The ASR Rack ID must be set before starting playback.
	 * The ASR Rack ID cannot be changed later for the sound already being played.
	
	 * The setting in this function is not applied to the sound data encoded for HCA-MX.
	
	 */
	public void SetAsrRackId(int asr_rack_id)
	{
		criAtomExPlayer_SetAsrRackId(this.handle, asr_rack_id);
	}


	/**
	 * Sets the Voice Pool identifier

	 * <param name='identifier'> Voice Pool identifier </param>
	 
	 * Specifies from which Voice Pool the Voice is taken when playing.
	 * When this function is called, the player will get Voices only from the Voice Pool that matches
	 * the specified Voice Pool identifier.
	 ---
	 需要播放的声音, 从哪个 id 的 Voice Pool 中提取;
	 参数 id 为 Voice Pool 的 id;
	
	 * <seealso cref='CriAtomExStandardVoicePool'/>
	 * <seealso cref='CriAtomExWaveVoicePool'/>
	 */
	public void SetVoicePoolIdentifier(uint identifier)
	{
		criAtomExPlayer_SetVoicePoolIdentifier(this.handle, identifier);
	}


	/**
	 * Sets the DSP time stretch ratio;  -- (Digital Signal Processing) 数字信号处理 拉伸比;

	 * <param name='ratio'>Stretch ratio</param>
	
	 * Sets the scale factor for the playback time of the DSP time stretch.

	 * The value obtained by multiplying the playback time of the original data by ratio is the playback time of the stretch result.
	 * Acceptable value is between 0.5f to 2.0f.

	 注意
	 * The value to be set is not the playback speed but the scale factor for the "playback time".
	 * When specifying the stretch rate by the playback speed, set the reciprocal(倒数) of the playback speed scale factor.
	
	 * <seealso cref='CriAtomExVoicePool.AttachDspTimeStretch'/>
	 * <seealso cref='CriAtomExPlayer.SetVoicePoolIdentifier'/>
	 */
	public void SetDspTimeStretchRatio(float ratio)
	{
		SetDspParameter((int)TimeStretchParameterId.Ratio, ratio);
	}


	/**
	 * Sets the pitch shift amount of the DSP pitch shifter  --- 数字信号处理 的 音调偏移量	

	 猜测: 此处的 dsp 可能是 dsp bus 的该能...

	 * <param name='pitch'>Pitch shift amount</param>
	 * <remarks>
	 * <para header='Description'>Sets the pitch shift amount of the DSP pitch shifter.<br/>
	 * The unit is cents.<br/>
	 * Acceptable value is between -2400 to 2400.<br/>
	 * 1200 means 2 times and -1200 means 1/2 times pitch shift compared to the original sound.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExVoicePool.AttachDspPitchShifter'/>
	 * <seealso cref='CriAtomExPlayer.SetVoicePoolIdentifier'/>
	 */
	public void SetDspPitchShifterPitch(float pitch)
	{
		float value = pitch / 4800.0f + 0.5f;
		SetDspParameter((int)PitchShifterParameterId.Pitch, value);
	}


	/**
	 * Sets the DSP parameter

	 猜测: 此处的 dsp 可能是 dsp bus 的该能...

	 * <param name='id'>Parameter ID</param>
	 * <param name='value'>Parameter value</param>

	 * Sets the parameters of the DSP attached to the Voice.

	 * <seealso cref='CriAtomExVoicePool.AttachDspTimeStretch'/>
	 * <seealso cref='CriAtomExVoicePool.AttachDspPitchShifter'/>
	 * <seealso cref='SetVoicePoolIdentifier'/>
	 */
	public void SetDspParameter(int id, float value)
	{
		criAtomExPlayer_SetDspParameter(this.handle, id, value);
	}
	

	public void SetSequencePrepareTime(uint ms)
	{
		criAtomExPlayer_SetSequencePrepareTime(this.handle, ms);
	}


	/**
	 * Parameters for time stretch  -- 时间拉伸参数
	
	 * <para header='Description'>The parameter specified for the time stretch DSP.</para>
	
	 * <seealso cref='CriAtomExVoicePool.AttachDspTimeStretch'/>
	 * <seealso cref='CriAtomExPlayer.SetDspParameter'/>
	 */
	public enum TimeStretchParameterId : int {
		Ratio       = 0,        /**< Stretch ratio -- 拉伸比 */
		FrameTime   = 1,        /**< Frame time  -- 帧时间 */
		Quality     = 2         /**< Processing quality -- 工序质量 */
	}


	/**
	 * <summary>Parameters for pitch shifter</summary>
	
	 * <para header='Description'>The parameter specified for the pitch shifter DSP.</para>

	 * <seealso cref='CriAtomExVoicePool.AttachDspPitchShifter'/>
	 * <seealso cref='CriAtomExPlayer.SetDspParameter'/>
	 */
	public enum PitchShifterParameterId : int {
		Pitch       = 0,        /**< Pitch  音调 */
		Formant     = 1,        /**< Formant */
		Mode        = 2         /**< Pitch shift mode */
	}


    /**
     * Attaches an AtomExTween
   
     * Simple parameter animation is possible by attaching AtomExTween to the player. 
	 * If you want to apply the AtomExTween animation to the audio that started playing before the attachment, 
	 * You need to call CriAtomExPlayer.Update or CriAtomExPlayer.UpdateAll .
  
     * <param name='tween'>AtomExTween Object</param>
     * <seealso cref='CriAtomExTween'/>
     */
    public void AttachTween(CriAtomExTween tween)
    {
        criAtomExPlayer_AttachTween(this.handle, tween.nativeHandle);
    }



    /**
     * Detaches an AtomExTween

     * <param name='tween'>AtomExTween Object</param>
     * <seealso cref='CriAtomExTween'/>
     */
    public void DetachTween(CriAtomExTween tween)
    {
        criAtomExPlayer_DetachTween(this.handle, tween.nativeHandle);
    }


    /**
     * Detaches all AtomExTween
     * <seealso cref='CriAtomExTween'/>
     */
    public void DetachTweenAll()
    {
        criAtomExPlayer_DetachTweenAll(this.handle);
    }

	// -----------------------------------------------  下面的都不用看 --------------------------------------------------------------------- #
    /* Old APIs */
    public void Stop() {
		if (this.isAvailable) {
			criAtomExPlayer_Stop(this.handle);
			CRIWAREC9642C99(this.entryPoolHandle);
		}
	}
	public void StopWithoutReleaseTime() {
		if (this.isAvailable) {
			criAtomExPlayer_StopWithoutReleaseTime(this.handle);
			CRIWAREC9642C99(this.entryPoolHandle);
		}
	}
	public void Pause(bool sw) { criAtomExPlayer_Pause(this.handle, sw); }

	#region Internal Members

	~CriAtomExPlayer()
	{
		this.Dispose();
	}

	private IntPtr handle = IntPtr.Zero; // 可从构造函数传入, 也可在构造函数中自己生成;

	private void OnBeatSyncCallbackChainInternal(ref CriAtomExBeatSync.Info info)
	{
		if (info.playerHn != this.nativeHandle) {
			return;
		}
		_onBeatSyncCallback(ref info);
	}

	private void OnSequenceCallbackChainInternal(ref CriAtomExSequencer.CriAtomExSequenceEventInfo info)
	{
		if (info.playerHn != this.nativeHandle) {
			return;
		}
		_onSequenceCallback(ref info);
	}

	#endregion

	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criAtomExPlayer_Create(ref Config config, IntPtr work, int work_size);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Destroy(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCueId(
		IntPtr player, IntPtr acb_hn, int id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCueName(
		IntPtr player, IntPtr acb_hn, string cue_name);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCueIndex(
		IntPtr player, IntPtr acb_hn, int index);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFile(
		IntPtr player, IntPtr binder, string path);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetData(
		IntPtr player, byte[] buffer, int size);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetData(
		IntPtr player, IntPtr buffer, int size);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetContentId(
		IntPtr player, IntPtr binder, int id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetVoicePoolIdentifier(
		IntPtr player, uint identifier);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern uint criAtomExPlayer_Start(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern uint criAtomExPlayer_Prepare(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Stop(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_StopWithoutReleaseTime(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Pause(IntPtr player, bool sw);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Resume(IntPtr player, CriAtomEx.ResumeMode mode);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayer_IsPaused(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern Status criAtomExPlayer_GetStatus(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern long criAtomExPlayer_GetTime(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFormat(IntPtr player, CriAtomEx.Format format);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetNumChannels(IntPtr player, int num_channels);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetSamplingRate(IntPtr player, int sampling_rate);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr CRIWARE01E006CC(IntPtr player, int capacity, int max_path, bool stopOnEmpty);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWAREC97D3429(IntPtr pool);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern int CRIWARE2DCE75C8(IntPtr pool);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern int CRIWAREB4B05A20(IntPtr pool);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWAREE835C03B(IntPtr pool);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool CRIWAREC15C851D(IntPtr pool, IntPtr binder, string path, bool repeat, int max_path);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool CRIWAREC08B2D9C(IntPtr pool,  IntPtr binder, int id, bool repeat);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool CRIWAREE07795D8(IntPtr pool, byte[] buffer, int size, bool repeat);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool CRIWAREE07795D8(IntPtr pool, IntPtr buffer, int size, bool repeat);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool CRIWARE089F6DAB(IntPtr pool, IntPtr acbhn, string name, bool repeat);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWAREC9642C99(IntPtr pool);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetStartTime(
		IntPtr player, long start_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetSequencePrepareTime(
		IntPtr player, uint seq_prep_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_LimitLoopCount(IntPtr player, int count);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Update(IntPtr player, uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_UpdateAll(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_ResetParameters(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern float criAtomExPlayer_GetParameterFloat32(IntPtr player, CriAtomEx.Parameter id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern uint criAtomExPlayer_GetParameterUint32(IntPtr player, CriAtomEx.Parameter id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern int criAtomExPlayer_GetParameterSint32(IntPtr player, CriAtomEx.Parameter id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern IntPtr criAtomExPlayer_GetPlayerParameter(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayerParameter_RemoveParameter(IntPtr player_parameter, System.UInt16 id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetVolume(IntPtr player, float volume);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPitch(IntPtr player, float pitch);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPlaybackRatio(IntPtr player, float playback_ratio);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPan3dAngle(IntPtr player, float pan3d_angle);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPan3dInteriorDistance(
		IntPtr player, float pan3d_interior_distance);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPan3dVolume(IntPtr player, float pan3d_volume);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPanType(IntPtr player, CriAtomEx.PanType panType);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetSendLevel(IntPtr player, int channel, CriAtomEx.Speaker id, float level);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBusSendLevel(
		IntPtr player, int bus_id, float level);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBusSendLevelByName(
		IntPtr player, string bus_name, float level);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayer_GetBusSendLevelByName(
		IntPtr player, string bus_name, out float level);
	
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBusSendLevelOffset(
		IntPtr player, int bus_id, float level_offset);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBusSendLevelOffsetByName(
		IntPtr player, string bus_name, float level_offset);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayer_GetBusSendLevelOffsetByName(
		IntPtr player, string bus_name, out float level_offset);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBandpassFilterParameters(
		IntPtr player, float cof_low, float cof_high);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetBiquadFilterParameters(
		IntPtr player, CriAtomEx.BiquadFilterType type, float frequency, float gain, float q);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetVoicePriority(
		IntPtr player, int priority);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetVoiceControlMethod(
		IntPtr player, CriAtomEx.VoiceControlMethod method);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetAisacControlById(
		IntPtr player, ushort control_id, float control_value);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetAisacControlByName(
		IntPtr player, string control_name, float control_value);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Set3dSourceHn(
		IntPtr player, IntPtr source);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_Set3dListenerHn(
		IntPtr player, IntPtr listener);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCategoryById(
		IntPtr player, uint category_id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCategoryByName(
		IntPtr player, string category_name);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_UnsetCategory(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetCuePriority(
		IntPtr player, int cue_priority);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetPreDelayTime(
		IntPtr player, float predelay_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetEnvelopeAttackTime(
		IntPtr player, float attack_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetEnvelopeHoldTime(
		IntPtr player, float hold_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetEnvelopeDecayTime(
		IntPtr player, float decay_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetEnvelopeReleaseTime(
		IntPtr player, float release_time_ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetEnvelopeSustainLevel(
		IntPtr player, float susutain_level);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_AttachFader(
		IntPtr player, IntPtr config, IntPtr work, int work_size);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_AttachAisac(
		IntPtr player, string globalAisacName);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_DetachAisac(
		IntPtr player, string globalAisacName);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_DetachFader(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFadeOutTime(IntPtr player, int ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFadeInTime(IntPtr player, int ms );

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFadeInStartOffset(IntPtr player, int ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFadeOutEndDelay(IntPtr player, int ms);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayer_IsFading(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_ResetFaderParameters(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetGroupNumber(IntPtr player, int group_no);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayer_GetAttachedAisacInfo(
		IntPtr player, int aisac_attached_index, IntPtr aisac_info);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetFirstBlockIndex(IntPtr player, int index);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetSelectorLabel(IntPtr player, string selector, string label);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_UnsetSelectorLabel(IntPtr player, string selector);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_ClearSelectorLabels(IntPtr player);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetSoundRendererType(IntPtr player, CriAtomEx.SoundRendererType type);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetRandomSeed(IntPtr player, uint seed);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void CRIWARE8631C028(IntPtr player, bool sw);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetAsrRackId(IntPtr player, int asr_rack_id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayer_SetDspParameter(IntPtr player, int id, float value);

    [DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
    private static extern void criAtomExPlayer_AttachTween(IntPtr player, IntPtr tween);

    [DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
    private static extern void criAtomExPlayer_DetachTween(IntPtr player, IntPtr tween);

    [DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
    private static extern void criAtomExPlayer_DetachTweenAll(IntPtr player);
#else
	private Status _dummyStatus = Status.Stop;
	private bool _dummyPaused = false;
	private bool _dummyLoop = false;
	private static IntPtr criAtomExPlayer_Create(ref Config config, IntPtr work, int work_size) { return IntPtr.Zero; }
	private static void criAtomExPlayer_Destroy(IntPtr player) { }
	private static void criAtomExPlayer_SetCueId(IntPtr player, IntPtr acb_hn, int id) { }
	private static void criAtomExPlayer_SetCueName(IntPtr player, IntPtr acb_hn, string cue_name) { }
	private static void criAtomExPlayer_SetCueIndex(IntPtr player, IntPtr acb_hn, int index) { }
	private static void criAtomExPlayer_SetFile(IntPtr player, IntPtr binder, string path) { }
	private static void criAtomExPlayer_SetData(IntPtr player, byte[] buffer, int size) { }
	private static void criAtomExPlayer_SetData(IntPtr player, IntPtr buffer, int size) { }
	private static void criAtomExPlayer_SetContentId(IntPtr player, IntPtr binder, int id) { }
	private static void criAtomExPlayer_SetVoicePoolIdentifier(IntPtr player, uint identifier) { }
	private uint criAtomExPlayer_Start(IntPtr player) { _dummyStatus = Status.Prep; _dummyPaused = false; return 0u; }
	private uint criAtomExPlayer_Prepare(IntPtr player) { _dummyStatus = Status.Prep; return 0u; }
	private void criAtomExPlayer_Stop(IntPtr player) { _dummyStatus = Status.Stop; }
	private void criAtomExPlayer_StopWithoutReleaseTime(IntPtr player) { _dummyStatus = Status.Stop; }
	private void criAtomExPlayer_Pause(IntPtr player, bool sw) { _dummyPaused = false; }
	private static void criAtomExPlayer_Resume(IntPtr player, CriAtomEx.ResumeMode mode) { }
	private bool criAtomExPlayer_IsPaused(IntPtr player) { return _dummyPaused; }
	private Status criAtomExPlayer_GetStatus(IntPtr player) {
		Status ret = _dummyStatus;
		if (!_dummyPaused && _dummyStatus != Status.PlayEnd && _dummyStatus != Status.Stop) {
			_dummyStatus = _dummyStatus == Status.Prep ? Status.Playing :
							!(_dummyPaused || _dummyLoop) ? Status.PlayEnd : Status.Playing;
		}
		return ret;
	}
	private static long criAtomExPlayer_GetTime(IntPtr player) { return 0; }
	private static void criAtomExPlayer_SetFormat(IntPtr player, CriAtomEx.Format format) { }
	private static void criAtomExPlayer_SetNumChannels(IntPtr player, int num_channels) { }
	private static void criAtomExPlayer_SetSamplingRate(IntPtr player, int sampling_rate) { }
	private static IntPtr CRIWARE01E006CC(IntPtr player, int capacity, int max_path, bool stopOnEmpty) { return new IntPtr(1); }
	private static void CRIWAREC97D3429(IntPtr pool) { }
	private static int CRIWARE2DCE75C8(IntPtr pool) { return 0; }
	private static int CRIWAREB4B05A20(IntPtr pool) { return 0; }
	private static void CRIWAREE835C03B(IntPtr pool) { }
	private static bool CRIWAREC15C851D(IntPtr pool, IntPtr binder, string path, bool repeat, int max_path) { return true; }
	private static bool CRIWAREC08B2D9C(IntPtr pool,  IntPtr binder, int id, bool repeat) { return true; }
	private static bool CRIWAREE07795D8(IntPtr pool, byte[] buffer, int size, bool repeat) { return true; }
	private static bool CRIWAREE07795D8(IntPtr pool, IntPtr buffer, int size, bool repeat) { return true; }
	private static bool CRIWARE089F6DAB(IntPtr pool, IntPtr acbhn, string name, bool repeat) { return true; }
	private static void CRIWAREC9642C99(IntPtr pool) { }
	private static void criAtomExPlayer_SetStartTime(IntPtr player, long start_time_ms) { }
	private static void criAtomExPlayer_SetSequencePrepareTime(IntPtr player, uint seq_prep_time_ms) { }
	private void criAtomExPlayer_LimitLoopCount(IntPtr player, int count) { _dummyLoop = (count == -3);  }
	private static void criAtomExPlayer_Update(IntPtr player, uint id) { }
	private static void criAtomExPlayer_UpdateAll(IntPtr player) { }
	private static void criAtomExPlayer_ResetParameters(IntPtr player) { }
	private static float criAtomExPlayer_GetParameterFloat32(IntPtr player, CriAtomEx.Parameter id) { return 0.0f; }
	private static uint criAtomExPlayer_GetParameterUint32(IntPtr player, CriAtomEx.Parameter id) { return 0u; }
	private static int criAtomExPlayer_GetParameterSint32(IntPtr player, CriAtomEx.Parameter id) { return 0; }
	private static IntPtr criAtomExPlayer_GetPlayerParameter(IntPtr player) { return IntPtr.Zero; }
	private void criAtomExPlayerParameter_RemoveParameter(IntPtr player_parameter, System.UInt16 id) { _dummyLoop = false; }
	private static void criAtomExPlayer_SetVolume(IntPtr player, float volume) { }
	private static void criAtomExPlayer_SetPitch(IntPtr player, float pitch) { }
	private static void criAtomExPlayer_SetPlaybackRatio(IntPtr player, float playback_ratio) { }
	private static void criAtomExPlayer_SetPan3dAngle(IntPtr player, float pan3d_angle) { }
	private static void criAtomExPlayer_SetPan3dInteriorDistance(
		IntPtr player, float pan3d_interior_distance) { }
	private static void criAtomExPlayer_SetPan3dVolume(IntPtr player, float pan3d_volume) { }
	private static void criAtomExPlayer_SetPanType(IntPtr player, CriAtomEx.PanType panType) { }
	private static void criAtomExPlayer_SetSendLevel(IntPtr player, int channel, CriAtomEx.Speaker id, float level) { }
	private static void criAtomExPlayer_SetBusSendLevel(IntPtr player, int bus_id, float level) { }
	private static void criAtomExPlayer_SetBusSendLevelByName(IntPtr player, string bus_name, float level) { }
	private static bool criAtomExPlayer_GetBusSendLevelByName(IntPtr player, string bus_name, out float level) { level = 0.0f; return false; }
	private static void criAtomExPlayer_SetBusSendLevelOffset(IntPtr player, int bus_id, float level_offset) { }
	private static void criAtomExPlayer_SetBusSendLevelOffsetByName(IntPtr player, string bus_name, float level_offset) { }
	private static bool criAtomExPlayer_GetBusSendLevelOffsetByName(IntPtr player, string bus_name, out float level_offset) { level_offset = 0.0f; return false; }
	private static void criAtomExPlayer_SetBandpassFilterParameters(IntPtr player, float cof_low, float cof_high) { }
	private static void criAtomExPlayer_SetBiquadFilterParameters(
		IntPtr player, CriAtomEx.BiquadFilterType type, float frequency, float gain, float q) { }
	private static void criAtomExPlayer_SetVoicePriority(IntPtr player, int priority) { }
	private static void criAtomExPlayer_SetVoiceControlMethod(IntPtr player, CriAtomEx.VoiceControlMethod method) { }
	private static void criAtomExPlayer_AttachAisac(IntPtr player, string globalAisacName) { }
	private static void criAtomExPlayer_DetachAisac(IntPtr player, string globalAisacName) { }
	private static void criAtomExPlayer_SetAisacControlById(IntPtr player, ushort control_id, float control_value) { }
	private static void criAtomExPlayer_SetAisacControlByName(IntPtr player, string control_name, float control_value) { }
	private static void criAtomExPlayer_Set3dSourceHn(IntPtr player, IntPtr source) { }
	private static void criAtomExPlayer_Set3dListenerHn(IntPtr player, IntPtr listener) { }
	private static void criAtomExPlayer_SetCategoryById(IntPtr player, uint category_id) { }
	private static void criAtomExPlayer_SetCategoryByName(IntPtr player, string category_name) { }
	private static void criAtomExPlayer_UnsetCategory(IntPtr player) { }
	private static void criAtomExPlayer_SetCuePriority(IntPtr player, int cue_priority) { }
	private static void criAtomExPlayer_SetPreDelayTime(IntPtr player, float predelay_time_ms) { }
	private static void criAtomExPlayer_SetEnvelopeAttackTime(IntPtr player, float attack_time_ms) { }
	private static void criAtomExPlayer_SetEnvelopeHoldTime(IntPtr player, float hold_time_ms) { }
	private static void criAtomExPlayer_SetEnvelopeDecayTime(IntPtr player, float decay_time_ms) { }
	private static void criAtomExPlayer_SetEnvelopeReleaseTime(IntPtr player, float release_time_ms) { }
	private static void criAtomExPlayer_SetEnvelopeSustainLevel(IntPtr player, float susutain_level) { }
	private static void criAtomExPlayer_AttachFader(IntPtr player, IntPtr config, IntPtr work, int work_size) { }
	private static void criAtomExPlayer_DetachFader(IntPtr player) { }
	private static void criAtomExPlayer_SetFadeOutTime(IntPtr player, int ms) { }
	private static void criAtomExPlayer_SetFadeInTime(IntPtr player, int ms ) { }
	private static void criAtomExPlayer_SetFadeInStartOffset(IntPtr player, int ms) { }
	private static void criAtomExPlayer_SetFadeOutEndDelay(IntPtr player, int ms) { }
	private static bool criAtomExPlayer_IsFading(IntPtr player) { return false; }
	private static void criAtomExPlayer_ResetFaderParameters(IntPtr player) { }
	private static void criAtomExPlayer_SetGroupNumber(IntPtr player, int group_no) { }
	private static bool criAtomExPlayer_GetAttachedAisacInfo(
		IntPtr player, int aisac_attached_index, IntPtr aisac_info) { return false; }
	private static void criAtomExPlayer_SetFirstBlockIndex(IntPtr player, int index) { }
	private static void criAtomExPlayer_SetSelectorLabel(IntPtr player, string selector, string label) { }
	private static void criAtomExPlayer_UnsetSelectorLabel(IntPtr player, string selector) { }
	private static void criAtomExPlayer_ClearSelectorLabels(IntPtr player) { }
	private static void criAtomExPlayer_SetSoundRendererType(IntPtr player, CriAtomEx.SoundRendererType type) { }
	private static void criAtomExPlayer_SetRandomSeed(IntPtr player, uint seed) { }
	private void CRIWARE8631C028(IntPtr player, bool sw) { _dummyLoop = sw; }
	private static void criAtomExPlayer_SetAsrRackId(IntPtr player, int asr_rack_id) { }
	private static void criAtomExPlayer_SetDspParameter(IntPtr player, int id, float value) { }

    private static void criAtomExPlayer_AttachTween(IntPtr player, IntPtr tween){}
    private static void criAtomExPlayer_DetachTween(IntPtr player, IntPtr tween){}
    private static void criAtomExPlayer_DetachTweenAll(IntPtr player){}
#endif
    #endregion
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */
