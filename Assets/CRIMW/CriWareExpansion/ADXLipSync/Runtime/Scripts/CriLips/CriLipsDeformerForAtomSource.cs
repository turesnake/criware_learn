/****************************************************************************
 *
 * Copyright (c) 2021 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/
#if UNITY_2019_3_OR_NEWER
using System;
using UnityEngine;

/**
 * \addtogroup CRILIPS_UNITY_COMPONENT
 * @{
 */

namespace CriWare {

	/**
	 * <summary>Component that operates ICriLipsMorph in conjunction with CriAtomSource.</summary>
	 * <remarks>
	 * <para header='Description'>A component to feed the mouth shape information obtained by analyzing voices played by ::CriAtomSource into ICriLipsMorph .</para>
	 * </remarks>
	 */
	[AddComponentMenu("CRIWARE/CriLipsDeformerForAtomSource")]
	public class CriLipsDeformerForAtomSource : CriLipsDeformer {
		#region Internal Variables

		[SerializeField]
		internal CriAtomSource source = null;

		/**
		 * <summary>CriLipsAtomAnalyzer used internally</summary>
		 * <remarks>
		 * <para header='Description'>If you want to control CriLipsAtomAnalyzer directly, get it from this property.</para>
		 * </remarks>
		 */
		public CriLipsAtomAnalyzer atomAnalyzer { get; protected set; }
		public float silenceThreshold = -40;
		public int samplingRate = 48000;
		#endregion

		#region Functions

		/**
		 * <summary>Attaches to the AtomSource to be analyzed</summary>
		 * <param name='source'>CriAtomSource</param>
		 * <returns>True if the data was set successfully, or False if failed</returns>
		 * <remarks>
		 * <para header='Description'>Attaches to ::CriAtomSource to be analyzed<br/>
		 * After calling this function, the sound played by ::CriAtomSource is analyzed
		 * and reflected in the blend shape.<br/>
		 * This function can be called only for stopped ::CriAtomSource .<br/>
		 * If you call the function on a ::CriAtomSource that is playing, it will fail
		 * with an error callback.<br/>
		 * If you pass null as an argument, it will detach from the ::CriAtomSource
		 * that is currently attached.<br/>
		 * Calling this function clears the internal state.</para>
		 * <para header='Note'>If it is already attached to ::CriAtomSource to be analyzed at the time of calling this function,
		 * it is detached internally.<br/>
		 * If the attached ::CriAtomSource is being played, this function fails
		 * because the detachment cannot be done .<br/>
		 * <br/>
		 * The lip-sync analysis uses the ADX2 filter callback inside the plug-in.<br/>
		 * Therefore, if there are any filter callbacks registered with CriAtomSource, they are unregistered.<br/>
		 * In addition, if a filter callback is registered with CriAtomSource while performing the lip-sync analysis,<br/>
		 * the lip-sync analysis stops.</para>
		 * </remarks>
		 * <seealso cref='::CriLipsAtomAnalyzer::DetachFromAtomExPlayer'/>
		 */
		public bool AttachToAtomSource(CriAtomSource source) {
			this.source = source;
			if (source != null) {
				return atomAnalyzer.AttachToAtomExPlayer(source.player);
			} else {
				return atomAnalyzer.DetachFromAtomExPlayer();
			}
		}

		private void Awake() {
			CriLipsAtomPlugin.InitializeLibrary(100u);

			atomAnalyzer = new CriLipsAtomAnalyzer();
			analyzeModule = atomAnalyzer;

			atomAnalyzer.SetSilenceThreshold(silenceThreshold);
			atomAnalyzer.SetSamplingRate(samplingRate);

			CriLipsMouth.Info info;
			atomAnalyzer.mouth.GetInfoAtSilence(out info);
			base.StartForMorphing(info);

			if (source != null) {
				atomAnalyzer.AttachToAtomExPlayer(source.player);
			}
		}

		// public override void CriInternalUpdate() {
		// 	if (source == null) {
		// 		return;
		// 	}
		//
		// 	atomAnalyzer.GetInfo(out info);
		// 	atomAnalyzer.GetMorphTargetBlendAmountAsJapanese(out blendAmount);
		// 	base.UpdateLipsParameter();
		// }
		
		public override void CriInternalLateUpdate()
		{
			if (source == null) {
				return;
			}

			if (atomAnalyzer != null)
			{
				atomAnalyzer.GetInfo(out info);
				atomAnalyzer.GetMorphTargetBlendAmountAsJapanese(out blendAmount);
			}
			base.UpdateLipsParameter();
		}

		private void OnDestroy() {
			try
			{
				if (source != null && source.player != null && atomAnalyzer != null)
				{
					source.player.StopWithoutReleaseTime();
					atomAnalyzer.DetachFromAtomExPlayer();
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}


			CriLipsAtomPlugin.FinalizeLibrary();
		}
		#endregion
	}

} //namespace CriWare
/**
 * @}
 */

#endif

/* end of file */
