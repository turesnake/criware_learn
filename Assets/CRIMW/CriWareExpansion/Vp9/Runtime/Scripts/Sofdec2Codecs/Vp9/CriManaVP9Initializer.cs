/****************************************************************************
 *
 * Copyright (c) 2020 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using UnityEngine;

/**
 * \addtogroup CriManaVp9
 * @{
 */

namespace CriWare {

/**
 * <summary>Component for CRI Sofdec2 Codec VP9 Expansion initializing</summary>
 * <remarks>
 * <para header='Description'>Component for initializing the VP9 expansion feature.<br/>
 * Please place it in the scene where CRIWARE is initialized.</para>
 * </remarks>
 */
public class CriManaVP9Initializer : MonoBehaviour
{
	void Awake() {
		CriManaVp9.SetupVp9Decoder();
	}

	/* Override OnEneable to ensure that Execution Order setting takes effect */
	void OnEnable() {
	}
}

} //namespace CriWare
/** @} */
