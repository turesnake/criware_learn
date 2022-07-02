/****************************************************************************
 *
 * Copyright (c) 2020 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
    #define CRIWARE_LIPS_TOOLUTILITY_SUPPORT
#endif

using System.Runtime.InteropServices;
using System.Security;

/*==========================================================================
 *      CRI Lips Tool Utility Native Methods
 *=========================================================================*/

namespace CriWare {

namespace CriLipsToolUtility
{
    public static partial class CriAdxlipParser
    {
        [SuppressUnmanagedCodeSecurity]
        internal static class SafeNativeMethods
        {
            #if !CRIWARE_ENABLE_HEADLESS_MODE && CRIWARE_LIPS_TOOLUTILITY_SUPPORT
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern FormatVersion criAdxlipParser_GetFormatVersion([In] System.Byte[] data);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Int32 criAdxlipParser_GetNumResults([In] System.Byte[] data);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_Parse([In] System.Byte[] data, [Out] FrameData[] result, System.Int32 num_result);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV1_ParseByFormat([In] System.Byte[] line, [Out] out FrameData result);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV1_GenerateEmptyHeaderLines([Out] System.Byte[] lines, System.Int32 lines_length);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV1_GenerateLine([Out] System.Byte[] lines, System.Int32 lines_length, [In] ref FrameData result);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV2_ParseByFormat([In] System.Byte[] line, [Out] out FrameData result);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV2_GenerateEmptyHeaderLines([Out] System.Byte[] lines, System.Int32 lines_length);
            [DllImport(CriLipsAtomPlugin.pluginName, CallingConvention = CriWare.Common.pluginCallingConvention)]
            public static extern System.Boolean criAdxlipParser_FormatV2_GenerateLine([Out] System.Byte[] lines, System.Int32 lines_length, [In] ref FrameData result);
            #else
            public static FormatVersion criAdxlipParser_GetFormatVersion([In] System.Byte[] data)
            {
                return default(FormatVersion);
            }
            public static System.Int32 criAdxlipParser_GetNumResults([In] System.Byte[] data)
            {
                return default(System.Int32);
            }
            public static System.Boolean criAdxlipParser_Parse([In] System.Byte[] data, [Out] FrameData[] result, System.Int32 num_result)
            {
                result = default(FrameData[]);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV1_ParseByFormat([In] System.Byte[] line, [Out] out FrameData result)
            {
                result = default(FrameData);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV1_GenerateEmptyHeaderLines([Out] System.Byte[] lines, System.Int32 lines_length)
            {
                lines = default(System.Byte[]);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV1_GenerateLine([Out] System.Byte[] lines, System.Int32 lines_length, [In] ref FrameData result)
            {
                lines = default(System.Byte[]);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV2_ParseByFormat([In] System.Byte[] line, [Out] out FrameData result)
            {
                result = default(FrameData);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV2_GenerateEmptyHeaderLines([Out] System.Byte[] lines, System.Int32 lines_length)
            {
                lines = default(System.Byte[]);
                return default(System.Boolean);
            }
            public static System.Boolean criAdxlipParser_FormatV2_GenerateLine([Out] System.Byte[] lines, System.Int32 lines_length, [In] ref FrameData result)
            {
                lines = default(System.Byte[]);
                return default(System.Boolean);
            }
            #endif
        }
    }
}

} //namespace CriWare
/* --- end of file --- */