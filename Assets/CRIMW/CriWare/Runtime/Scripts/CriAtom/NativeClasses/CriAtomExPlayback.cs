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
Playback sound object

 * On object returned when the function ::CriAtomExPlayer::Start() is called.
 * If you want to change parameters or get the status for each sound played back using the function ::CriAtomExPlayer::Start ,
 * not for each player, you need to use this object for controlling playback.

	调用 CriAtomExPlayer::Start() 返回的一个 playback 实例;
	可用这个 playback 实例来 修改参数, 获得状态;

 * <seealso cref='CriAtomExPlayer::Start'/>
 */
public struct CriAtomExPlayback
{
	/**
	 * Playback status
	
	 * Status of the sound that has been played on the AtomExPlayer.
	 * It can be obtained by using the ::CriAtomExPlayback::GetStatus() function.
	
	 * The playback status usually changes in the following order.<br/>
	 * -# Prep
	 * -# Playing
	 * -# Removed
	
	 * Status indicates the status of the sound that was played
	 * ( ::CriAtomExPlayer::Start function was called) by the AtomExPlayer
	 * instead of the status of the player.
	 --
	 不是 player 的状态, 而是被 player 播放的声音 的状态;
	
	 * The sound resource being played is discarded when the playback is stopped.
	 * Therefore, the status of the playback sound changes to Removed in the following cases.

	 * - When playback is complete.
	 * - When the sound being played is stopped using the Stop function.
	 * - When a Voice being played is stolen by a high priority playback request.
	 * - When an error occurred during playback.
	 ---
	 当 playback 停止, 声音资源将被 discarded; 因此，在以下情况下，播放声音的状态变为 removed:
	 -- 当 playback 完成时;
	 -- 当使用 stop() 将被播放的声音 停止时;
	 -- 当一个优先级更高的 playback 请求 将当前正在播放的声音 偷掉时;
	

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayback::GetStatus'/>
	 * <seealso cref='CriAtomExPlayback::Stop'/>
	 */
	public enum Status {
		Prep = 1,   /**< Preparing for playback */
		Playing,    /**< Playing */
		Removed     /**< Removed */
	}


	/**
	 	Playback Track information
	 * A structure to get the Track information of the Cue being played.

	 * <seealso cref='CriAtomExPlayback::GetTrackInfo'/>
	 */
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct TrackInfo {
		public uint id;                         /**< Playback ID */
		public CriAtomEx.CueType sequenceType;  /**< Parent Sequence type */
		public IntPtr playerHn;                 /**< Player handle */
		public ushort trackNo;                  /**< Track number */
		public ushort reserved;                 /**< Reserved area */
	}


	public CriAtomExPlayback(uint id)
		: this()
	{
		this.id = id;
	#if CRIWARE_ENABLE_HEADLESS_MODE
		this._dummyStatus = Status.Prep;
	#endif
	}


	/**
	 * Stops the playback sound

	 参数:
	 * <param name='ignoresReleaseTime'>Whether to ignore release time
	 * (False = perform release process, True = ignore release time and stop immediately)
	 ---
	 是否忽略释放时间: 
	 	-- false: perform release process
		-- true:  ignore release time and stop immediately
	
	 * Stops for each played sound.
	 * By using this function, it is possible to pause the sound played for each individual sound on the player.
	 ---
	通过使用此功能，可以暂停播放器上每个单独声音的播放。

	 * If you want to stop all the sounds played back by the AtomExPlayer,
	 * use the ::CriAtomExPlayer::Stop function instead of this function.
	* (The ::CriAtomExPlayer::Stop function stops all the sounds being played by the player.)
	---
	若要暂停一个 player 的所有正在播放的声音, 要改用 CriAtomExPlayer::Stop();

	
	 * When the playback sound is stopped using this function, the status of the sound being played changes to Removed.
	 * Since the Voice resource is also discarded when stopped, it will not be possible to acquire information
	 * from the playback object that changed to Removed state.
	 ---
	 当使用本函数 stop 一个声音, 这个声音的状态将被改为 removed;
	 它的声音资源会被 discard, 然后也不能通过本 playback 实例去获得它的 信息了;
	
	 * <seealso cref='CriAtomExPlayer::Stop'/>
	 * <seealso cref='CriAtomExPlayback::GetStatus'/>
	 */
	public void Stop(bool ignoresReleaseTime)
	{
		if (CriAtomPlugin.IsLibraryInitialized() == false) { return; }

		if (ignoresReleaseTime == false) {
			criAtomExPlayback_Stop(this.id);
		} else {
			criAtomExPlayback_StopWithoutReleaseTime(this.id);
		}
	}

	/**
	 * Pauses playback sound
	 * Pauses for each played sound.
	
	 * By using this function, it is possible to pause the sound played for each individual sound on the player.

	 * If you want to pause all the sounds played back by the player,
	 * use the ::CriAtomExPlayer::Pause function instead of this function.

	 * <seealso cref='CriAtomExPlayback::IsPaused'/>
	 * <seealso cref='CriAtomExPlayer::Pause'/>
	 * <seealso cref='CriAtomExPlayback::Resume'/>
	 */
	public void Pause()
	{
		criAtomExPlayback_Pause(this.id, true);
	}


	/**
	 * Unpauses the playback sound --  取消暂停

	 * <param name='mode'>Unpausing target</param> 
	
	 * When this function is called by specifying PausedPlayback in the argument (mode),
	 * the playback of the sound paused by the user using
	 * the ::CriAtomExPlayer::Pause function (or the ::CriAtomExPlayback::Pause function) is resumed.

	 * When this function is called by specifying PreparedPlayback in the argument (mode),
	 * the playback of the sound prepared by the user using the::CriAtomExPlayer::Prepare starts.

	 注意:
	 * If you prepare a playback using the ::CriAtomExPlayer::Prepare function for the player paused using the ::CriAtomExPlayer::Pause function,
	 * the sound is not played back until it is unpaused using PausedPlayback as well as unpaused with PreparedPlayback.
	
	 * If you want to always start playback regardless of whether
	 * the sound is paused using the ::CriAtomExPlayer::Pause or ::CriAtomExPlayer::Prepare function, 
	 call this function by specifying AllPlayback in the argument (mode).
	
	 * <seealso cref='CriAtomExPlayback::IsPaused'/>
	 * <seealso cref='CriAtomExPlayer::Resume'/>
	 * <seealso cref='CriAtomExPlayer::Pause'/>
	 */
	public void Resume(CriAtomEx.ResumeMode mode)
	{
		criAtomExPlayback_Resume(this.id, mode);
	}

	/**
	Gets pausing status of the playback sound

	Returns whether or not the sound being played is paused.

	 * <seealso cref='CriAtomExPlayback::Pause'/>
	 */
	public bool IsPaused()
	{
		return criAtomExPlayback_IsPaused(this.id);
	}


	/**
	 * Gets the playback sound format information

	 * <param name='info'>Format information</param>

	 返回:
	 * <returns> Whether the information can be acquired (True = could be acquired, False = could not be acquired)
	 返回 可否获得信息;

	 * Gets the format information of the sound played back using the ::CriAtomExPlayer::Start function.

	 * This function returns True if the format information can be obtained.
	 * This function returns False if the specified Voice was already deleted.

	 * When playing a Cue that contains multiple sound data, the information on the first sound data found is returned.
	 ---
	 若被播放的 cue 包含数个声音数据, 则返回找到的 第一个声音的数据;

	 * This function can get the format information only during sound playback.
	 * If the Voice is deleted by Voice control during playback preparation or after the playback,
	 * acquisition of format information fails.
	 ---
	 只有在 声音被播放期间, 才能得到的它的信息; 准备阶段 和 结束播放后, 都无法获得信息;
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::GetStatus'/>
	 */
	public bool GetFormatInfo(out CriAtomEx.FormatInfo info)
	{
		return criAtomExPlayback_GetFormatInfo(this.id, out info);
	}


	/**
	 * Gets the playback status

	 * <para header='Description'>Gets the status of the sound played back using the ::CriAtomExPlayer::Start function.<br/></para>
	 * <para header='Note'>This function gets the status of each sound already played while the
	 * ::CriAtomExPlayer::GetStatus function returns the status of the AtomExPlayer.<br/>

	 * The Voice resource of the sound being played is released in the following cases.
	 * - When playback is complete.
	 * - When the sound being played is stopped using the Stop function.
	 * - When a Voice being played is stolen by a high priority playback request.
	 * - When an error occurred during playback.
	
	 * Therefore, regardless of whether you explicitly stopped playback using the ::CriAtomExPlayback::Stop function
	 * or the playback is stopped due to some other factor,
	 * the status of the sound changes to Removed in all cases.
	 * (If you need to detect the occurrence of an error, it is necessary to check the status of the AtomExPlayer
	 * using the ::CriAtomExPlayer::GetStatus function instead of this function.)
	 ---
	 如果需要检测错误的发生，改用 player 版的 GetStatus() 函数;

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::GetStatus'/>
	 * <seealso cref='CriAtomExPlayback::Stop'/>
	 */
	public Status GetStatus()
	{
		return criAtomExPlayback_GetStatus(this.id);
	}


	/**
	 * Gets the playback time   ---  “播放开始后经过的时间”

	返回值:
	 * <returns>Playback time (in milliseconds)

	 * Gets the playback time of the sound played back using the ::CriAtomExPlayer::Start function.

	 * This function returns a value of 0 or greater if the playback time can be obtained.
	 * This function returns a negative value if the specified Voice was already deleted.
	---
	若目标声音 已被删除, 返回 负值;

	 * The playback time returned by this function is "the elapsed time from the start of playback". 播放开始后经过的时间”
	 * The time does not rewind depending on the playback position, even during loop playback or seamless linked playback.\
	 --
	 即使在 循环播放 或 无缝链接 播放期间，时间也不会根据播放位置倒回。

	 * When the playback is paused using the ::CriAtomExPlayer::Pause function,
	 * the playback time count-up also stops.<br/>
	 * (If you unpause the playback, the count-up resumes.)
	 ---
	 使用 player Pause() 函数可以同时 暂停 计时, unpause 之后 计时将恢复;

	 * The accuracy of the time that can be obtained by this function depends on the frequency of the server processing.<br/>
	 * (The time is updated for each server process.)<br/>
	 * If you need to get more accurate time, use the ::CriAtomExPlayback::GetNumPlayedSamples() function instead of this function
	 * to get the number of samples played.<br/></para>
	---
	计时精度;

	 * The return type is long, but currently there is no precision over 32bit.
	 * When performing control based on the playback time, it should be noted that the playback time becomes incorrect in about 24 days.
	 * (The playback time overflows and becomes a negative value when it exceeds 2147483647 milliseconds.)
	---
	24 天后溢出; 

	 * This function can get the time only during sound playback.
	 * (Unlike the ::CriAtomExPlayer::GetTime function, this function can get the time
	 * for each sound being played, but it cannot get the playback end time.)
	 * Getting the playback time fails after the playback ends or when the Voice is erased by the Voice control.<br/>
	 * (Negative value is returned.)
	
	 * If the sound data supply is temporarily interrupted due to device read retry processing, etc.,
	 * the count-up of the playback time is not interrupted.
	 ---
	 如果由于设备读取重试处理 而导致 声音数据供应暂时中断,  playback time 的计时 不会被中断;

	 * (The time progresses even if the playback is stopped due to the stop of data supply.)
	 * Therefore, when synchronizing sound with the source video based on the time acquired by this function,
	 * the synchronization may be greatly deviated each time a read retry occurs.
	 ---

	 * If it is necessary to strictly synchronize the waveform data and video,
	 * use the ::CriAtomExPlayback::GetNumPlayedSamples function instead of this function
	 * to synchronize with the number of played samples.<br/></para>

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::GetTime'/>
	 * <seealso cref='CriAtomExPlayback::GetNumPlayedSamples'/>
	 */
	public long GetTime()
	{
		return criAtomExPlayback_GetTime(this.id);
	}



	/**
	 * Gets the playback time synchronized with sound;

	返回:
	Playback time (in milliseconds)
	
	 * Gets the playback time of the sound played back using the ::CriAtomExPlayer::Start function.
	 
	 * This function returns a value of 0 or greater if the playback time can be obtained.<br/>
	 * This function returns a negative value if the specified Voice was already deleted.<br/></para>

	 * Unlike the "elapsed time from the start of playback" returned by the ::CriAtomExPlayback::GetTime function,
	 * it is possible to obtain the playback time synchronized with the sound being played using this function.
	---
	 使用该功能可以获得与正在播放的声音同步的播放时间。

	 * If the supply of sound data is interrupted due to device read retry, etc.
	 * and the playback is stopped, the update (increase) of the playback time will be temporarily stopped.<br/>
	 ---

	 * If you want to strictly synchronize with the sound played,
	 * use the playback time returned by this function.<br/>

	 * However, the time does not rewind depending on the playback position,
	 * even during loop playback or seamless concatenated playback.<br/>

	 * In addition, the playback time cannot be acquired
	 * if the Sequence Cue is not filled with waveforms without gaps, or if Blocks
	 * are used in which the waveform that is being played may change.<br/>
	 
	 * When the playback is paused using the ::CriAtomExPlayer::Pause function,
	 * the playback time will also stop increasing.<br/>
	 * (The updates will resume once the playback is unpaused.)<br/>
	
	 * In order to get the playback time using this function, you need to call the ::CriAtomExPlayer::CriAtomExPlayer(bool) function
	 * to create a player with the flag that enables the sound synchronization timer.
	
	 * <para header='Note'>The return type is long, but currently there is no precision over 32bit.
	 * When performing control based on the playback time, it should be noted that the playback time becomes incorrect in about 24 days.
	 * (The playback time overflows and becomes a negative value when it exceeds 2147483647 milliseconds.)
	
	 * This function can get the time only during sound playback;
	 * (Unlike the ::CriAtomExPlayer::GetTime function, this function can get the time
	 * for each sound being played, but it cannot get the playback end time.)
	 * Getting the playback time fails after the playback ends or when the Voice is erased by the Voice control.<br/>
	 * (Negative value is returned.)<br/>
	 
	 * This function calculates the time internally, so the processing load
	 * may be a problem depending on the platform. In addition, it returns the updated time with each call,
	 * even within the same frame of the application.<br/>
	 * Although it depends on how the playback time is used by the application,
	 * basically use this function to get the time only once per frame.<br/>
	
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayback::GetTime'/>
	 * <seealso cref='CriAtomExPlayback::GetNumPlayedSamples'/>
	 * <seealso cref='CriAtomExPlayer::CriAtomExPlayer(bool)'/>
	 */
	public long GetTimeSyncedWithAudio()
	{
		return criAtomExPlayback_GetTimeSyncedWithAudio(this.id);
	}

	/**
	 * Gets the number of playback samples

	 * <param name='numSamples'>   -- The number of played samples</param>
	 * <param name='samplingRate'> -- Sampling rate</param>

	返回:
	  Whether the sample count can be acquired (True = could be acquired, False = could not be acquired)
	
	 * Returns the number of playback samples and the sampling rate of the
	 * sound played back using the ::CriAtomExPlayer::Start function.<br/>

	 * This function returns True if the number of playback samples can be obtained.<br/>
	 * This function returns False if the specified Voice was already deleted.<br/>
	 * (When an error occurs, the values of numSamples and samplingRate are also negative.)<br/></para>
	 * <para header='Note'>The accuracy of the number of samples played depends on the
	 * sound library in the platform SDK.<br/>
	 * (The accuracy of the number of samples played depends on the platform.)<br/>

	 * When playing a Cue that contains multiple sound data, the information
	 * on the first sound data found is returned.<br/></para>
	 * <para header='Note'>If the sound data supply is interrupted due to device read retry processing, etc.,
	 * the count-up of the number of playback samples stops.<br/>
	 * (The count-up restarts when the data supply is resumed.)<br/>

	 * This function can get the number of playback samples only while the sound is being played.<br/>
	 * Getting the number of playback samples fails after the playback ends
	 * or when the Voice is erased by the Voice control.<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public bool GetNumPlayedSamples(out long numSamples, out int samplingRate)
	{
		return criAtomExPlayback_GetNumPlayedSamples(this.id, out numSamples, out samplingRate);
	}


	/**
	 * Gets the Sequence playback position

	 * <returns>Sequence playback position (in milliseconds)</returns>
	 
	 * Gets the Sequence playback position of the sound played back using the ::CriAtomExPlayer::Start function.
	
	 * This function returns a value of 0 or greater if the playback position can be obtained.<br/>
	 * This function returns a negative value if, for example, the specified Sequence was already deleted.<br/></para>
	 * <para header='Note'>The playback time returned by this function is the "playback position on the Sequence data".<br/>
	 * When a Sequence loop or Block transition was performed, the rewound value is returned.<br/>

	 * The Sequencer does not run if you performed non-Cue-specified playback. For playback other than Cue playback,
	 * this function returns a negative value.<br/>
	 ---
	 猜测: 只适用于 cue;

	 * When the playback is paused using the ::CriAtomExPlayer::Pause function, the playback position also stops being updated.<br/>
	 * (If you unpause, the update resumes.)

	 * The accuracy of the time that can be obtained by this function depends on the frequency of the server processing.<br/>
	 * (The time is updated for each server process.)<br/></para>
	 * <para header='Note'>The return type is long, but currently there is no precision over 32bit.<br/>
	 * When performing control based on the playback position, it should be noted that
	 * the playback position becomes incorrect in about 24 days for the data that has no settings such as Sequence loops.<br/>
	 * (The playback position overflows and becomes a negative value when it exceeds 2147483647 milliseconds.)<br/>

	 * This function can get the position only during sound playback.<br/>
	 * After the end of playback, or when the Sequence was erased by Voice control,
	 * acquisition of the playback position fails.<br/>
	 * (Negative value is returned.)<br/></para>
	 *
	 */
	public long GetSequencePosition()
	{
		return criAtomExPlayback_GetSequencePosition(this.id);
	}

	/**
	 * Gets the current Block index of the playback sound

	 * <returns>Current Block index</returns>
	
	 * <para header='Description'>Gets the current Block index of the Block sequence
	 * played back using the ::CriAtomExPlayer::Start function.<br/></para>

	 * <para header='Note'>Returns 0xFFFFFFFF if the data being played by the playback ID is not a Block Sequence.<br/></para>

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 * <seealso cref='CriAtomExPlayer::SetFirstBlockIndex'/>
	 * <seealso cref='CriAtomExPlayback::SetNextBlockIndex'/>
	 */
	public int GetCurrentBlockIndex()
	{
		return criAtomExPlayback_GetCurrentBlockIndex(this.id);
	}

	/**
	 * Gets the playback Track information

	 * <param name='info'>Playback Track information</param>
	 * <returns>Whether the acquisition succeeded</returns>

	 * <para header='Description'>Gets the Track information of the Cue played back using the ::CriAtomExPlayer::Start function.<br/>
	 * The Track information that can be obtained is only the information directly under the Cue; the information for sub-Sequence or Cue link cannot be obtained.<br/></para>
	 * <para header='Note'>An attempt to get the Track information fails if the following data is being played.<br/>
	 * - Data other than Cue is being played. (Since there is no Track information)<br/>
	 * - The Cue being played is a Polyphonic type or a selector-referenced switch type.
	 *   (Since there may be multiple Track information)<br/>
	 * - The Cue being played is a Track Transition type. (Since the playback Track changes by transition)<br/></para>

	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public bool GetTrackInfo(out TrackInfo info)
	{
		return criAtomExPlayback_GetPlaybackTrackInfo(this.id, out info);
	}

	/**
	 *Gets beat synchronization information

	 * <param name='info'>Beat synchronization information</param>
	 * <returns>Whether the acquisition succeeded</returns>
	 * <remarks>
	 * <para header='Description'>Gets the beat synchronization information of the Cue played back using the ::CriAtomExPlayer::Start function.<br/>
	 * You can get the current BPM, bar count, beat count, and beat progress rate (0.0 to 1.0).<br/>
	 * Beat synchronization information must be set for the Cue.<br/>
	 * Information for the Cues that are being played using Cue links or start actions cannot be acquired.<br/></para>
	 * <para header='Note'>An attempt to get the beat synchronization information fails if the following data is being played.<br/>
	 * - Data other than Cue is being played. (Since there is not beat synchronization information)<br/>
	 * - A Cue without beat synchronization information is being played.<br/>
	 * - A Cue with beat synchronization information is being played "indirectly".
	 *   (Being played with Cue link or start action)<br/></para>
	 * </remarks>
	 * <seealso cref='CriAtomExPlayer::Start'/>
	 */
	public bool GetBeatSyncInfo(out CriAtomExBeatSync.Info info)
	{
		return criAtomExPlayback_GetBeatSyncInfo(this.id, out info);
	}



	/**
		Block transition of playback sound;
		播放声音 的 块转换

		参数:  index -- Block index;
	 
	 * Block transition is performed for each played sound.
	 * When this function is executed, if the Voice with the specified ID is a block sequence, 
	   It transitions the block according to the specified block sequence setting. 
	 	---
	 	块转换 会作用于每一个 已经播放的声音;
		执行此功能时，如果指定ID的 语音 是 块序列，则会根据指定的块序列设置转换块。

	 * Use the      "::CriAtomExPlayer::SetFirstBlockIndex()"      function to specify the playback start Block,
	 * and use the  "::CriAtomExPlayback::GetCurrentBlockIndex()"  function to get the Block index during playback.

	 * <seealso cref='CriAtomExPlayer::SetFirstBlockIndex'/>
	 * <seealso cref='CriAtomExPlayback::GetCurrentBlockIndex'/>
	 */
	public void SetNextBlockIndex(int index)
	{
		criAtomExPlayback_SetNextBlockIndex(this.id, index);
	}



	public uint id
	{
		get;
		private set;
	}

	public Status status
	{
		get {
			return this.GetStatus();
		}
	}

	public long time
	{
		get {
			return this.GetTime();
		}
	}

	public long timeSyncedWithAudio
	{
		get {
			return this.GetTimeSyncedWithAudio();
		}
	}

	public const uint invalidId = 0xFFFFFFFF;



	// -----------------------------------------------  下面的都不用看 --------------------------------------------------------------------- #
	/* Old APIs */
	public void Stop() {
		if (CriAtomPlugin.IsLibraryInitialized() == false) { return; }
		criAtomExPlayback_Stop(this.id);
	}
	public void StopWithoutReleaseTime() {
		if (CriAtomPlugin.IsLibraryInitialized() == false) { return; }
		criAtomExPlayback_StopWithoutReleaseTime(this.id);
	}
	public void Pause(bool sw) { criAtomExPlayback_Pause(this.id, sw); }




	#region DLL Import
	#if !CRIWARE_ENABLE_HEADLESS_MODE
	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayback_Stop(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayback_StopWithoutReleaseTime(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayback_Pause(uint id, bool sw);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayback_Resume(uint id, CriAtomEx.ResumeMode mode);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayback_IsPaused(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern Status criAtomExPlayback_GetStatus(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayback_GetFormatInfo(
		uint id, out CriAtomEx.FormatInfo info);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern long criAtomExPlayback_GetTime(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern long criAtomExPlayback_GetTimeSyncedWithAudio(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayback_GetNumPlayedSamples(
		uint id, out long num_samples, out int sampling_rate);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern long criAtomExPlayback_GetSequencePosition(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern void criAtomExPlayback_SetNextBlockIndex(uint id, int index);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern int criAtomExPlayback_GetCurrentBlockIndex(uint id);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayback_GetPlaybackTrackInfo(uint id, out TrackInfo info);

	[DllImport(CriWare.Common.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
	private static extern bool criAtomExPlayback_GetBeatSyncInfo(uint id, out CriAtomExBeatSync.Info info);
	#else
	private Status _dummyStatus;
	private bool _dummyPaused;
	private void criAtomExPlayback_Stop(uint id) { _dummyStatus = Status.Removed; }
	private void criAtomExPlayback_StopWithoutReleaseTime(uint id) { _dummyStatus = Status.Removed; }
	private void criAtomExPlayback_Pause(uint id, bool sw) { _dummyPaused = sw; }
	private static void criAtomExPlayback_Resume(uint id, CriAtomEx.ResumeMode mode) { }
	private bool criAtomExPlayback_IsPaused(uint id) { return _dummyPaused; }
	private Status criAtomExPlayback_GetStatus(uint id)
	{
		if (_dummyStatus != Status.Removed) {
			_dummyStatus = _dummyStatus + 1;
		}
		return _dummyStatus;
	}
	private static bool criAtomExPlayback_GetFormatInfo(
		uint id, out CriAtomEx.FormatInfo info) { info = new CriAtomEx.FormatInfo(); return false; }
	private static long criAtomExPlayback_GetTime(uint id) { return 0; }
	private static long criAtomExPlayback_GetTimeSyncedWithAudio(uint id) { return 0; }
	private static bool criAtomExPlayback_GetNumPlayedSamples(
		uint id, out long num_samples, out int sampling_rate) { num_samples = sampling_rate = 0; return false; }
	private static long criAtomExPlayback_GetSequencePosition(uint id) { return 0; }
	private static void criAtomExPlayback_SetNextBlockIndex(uint id, int index) { }
	private static int criAtomExPlayback_GetCurrentBlockIndex(uint id) { return -1; }
	private static bool criAtomExPlayback_GetPlaybackTrackInfo(uint id, out TrackInfo info) { info = new TrackInfo(); return false; }
	private static bool criAtomExPlayback_GetBeatSyncInfo(uint id, out CriAtomExBeatSync.Info info) { info = new CriAtomExBeatSync.Info(); return false; }
	#endif
	#endregion
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */
