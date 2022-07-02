/****************************************************************************
 *
 * Copyright (c) 2020 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System.Runtime.InteropServices;

/*==========================================================================
 *      CRI Lips Tool Utility Native Structs
 *=========================================================================*/
/**
 * \addtogroup CRILIPSTOOLUTILITY_NATIVE_WRAPPER
 * @{
 */

namespace CriWare {

namespace CriLipsToolUtility
{
    public static partial class CriAdxlipParser
    {
        /**
         * <summary>Frame data</summary>
         * <remarks>
         * <para header='Description'>A structure for storing the data for each parsed frame.</para>
         * </remarks>
         */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct FrameData
        {
            public System.UInt32 frame_count;
            public System.UInt32 msec;
            public System.Single width;
            public System.Single height;
            public System.Single tongue;
            public System.Single a;
            public System.Single i;
            public System.Single u;
            public System.Single e;
            public System.Single o;
            public System.Single vol;
        }
    }
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */
