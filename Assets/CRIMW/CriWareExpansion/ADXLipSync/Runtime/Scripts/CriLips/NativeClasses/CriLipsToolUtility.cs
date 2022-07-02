/****************************************************************************
 *
 * Copyright (c) 2020 CRI Middleware Co., Ltd.
 *
 ****************************************************************************/

using System;

/*==========================================================================
 *      CRI Lips Tool Utility Native Wrapper
 *=========================================================================*/
/**
 * \addtogroup CRILIPSTOOLUTILITY_NATIVE_WRAPPER
 * @{
 */

namespace CriWare {

/**
 * <summary>LipsToolUtility namespace</summary>
 * <remarks>
 * <para header='Description'>A namespace for utilities related to input/output of the ADX LipSync tool.</para>
 * </remarks>
 */
namespace CriLipsToolUtility
{
    /**
     * <summary>adxlip file parser module</summary>
     * <remarks>
     * <para header='Description'>A module for reading/writing the adxlip files.</para>
     * </remarks>
     */
    public static partial class CriAdxlipParser
    {
        /**
         * <summary>adxlip data format version</summary>
         * <remarks>
         * <para header='Description'>The data format version of the adxlip file.</para>
         * </remarks>
         */
        public enum FormatVersion : Int32
        {
            V1_0 = 0,
            V2_0,
        }

        /**
         * <summary>Gets the adxlip data format version</summary>
         * <param name='data'>adxlip data</param>
         * <returns>FormatVersion adxlip data format version</returns>
         * <remarks>
         * <para header='Description'>Gets the data format version of the adxlip file.</para>
         * </remarks>
         */
        public static FormatVersion GetFormatVersion(byte[] data)
        {
            return SafeNativeMethods.criAdxlipParser_GetFormatVersion(data);
        }

        /**
         * <summary>Gets the number of adxlip data lines</summary>
         * <param name='data'>adxlip data</param>
         * <returns>Number of Int32 adxlip data lines</returns>
         * <remarks>
         * <para header='Description'>Gets the number of data lines in the adxlip file.</para>
         * </remarks>
         */
        public static Int32 GetNumResults(byte[] data)
        {
            return SafeNativeMethods.criAdxlipParser_GetNumResults(data);
        }

        /**
         * <summary>Parse of the adxlip data</summary>
         * <param name='data'>adxlip data</param>
         * <param name='result'>Array of frame data to be stored</param>
         * <param name='num_result'>The number of adxlip data lines</param>
         * <returns>Whether the bool function succeeded</returns>
         * <remarks>
         * <para header='Description'>Parses the data in the adxlip file.</para>
         * </remarks>
         */
        public static bool Parse(byte[] data, FrameData[] result, Int32 num_result)
        {
            return SafeNativeMethods.criAdxlipParser_Parse(data, result, num_result);
        }

        /**
         * <summary>adxlip data format version 1 related module</summary>
         * <remarks>
         * <para header='Description'>A module which contains functions related to the format version 1 of the adxlip file.</para>
         * </remarks>
         */
        public static class FormatV1
        {
            /**
             * <summary>Parses as format version 1</summary>
             * <param name='data'>adxlip line data</param>
             * <param name='result'>Frame data</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Parses a byte string as format version 1 data and write it to the frame data.</para>
             * </remarks>
             */
            public static bool ParseByFormat(byte[] data, out FrameData result)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV1_ParseByFormat(data, out result);
            }

            /**
             * <summary>Generates the format version 1 empty header</summary>
             * <param name='data'>adxlip header data output buffer</param>
             * <param name='data_length'>Length of the buffer (in bytes)</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Generates an empty adxlip file header that conforms to the format version 1 specification.</para>
             * </remarks>
             */
            public static bool GenerateEmptyHeaderLines(byte[] data, Int32 data_length)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV1_GenerateEmptyHeaderLines(data, data_length);
            }

            /**
             * <summary>Generates the format version 1 data</summary>
             * <param name='data'>adxlip data output buffer</param>
             * <param name='data_length'>Length of the buffer (in bytes)</param>
             * <param name='result'>Frame data</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Generates the format version 1 frame data.</para>
             * </remarks>
             */
            public static bool GenerateLine(byte[] data, Int32 data_length, ref FrameData result)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV1_GenerateLine(data, data_length, ref result);
            }
        }

        /**
         * <summary>adxlip data format version 2 related module</summary>
         * <remarks>
         * <para header='Description'>A module which contains functions related to the format version 2 of the adxlip file.</para>
         * </remarks>
         */
        public static class FormatV2
        {
            /**
             * <summary>Parses as format version 2</summary>
             * <param name='data'>adxlip line data</param>
             * <param name='result'>Frame data</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Parses a byte string as format version 2 data and write it to the frame data.</para>
             * </remarks>
             */
            public static bool ParseByFormat(byte[] data, out FrameData result)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV2_ParseByFormat(data, out result);
            }

            /**
             * <summary>Generates the format version 2 empty header</summary>
             * <param name='data'>adxlip header data output buffer</param>
             * <param name='data_length'>Length of the buffer (in bytes)</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Generates an empty adxlip file header that conforms to the format version 2 specification.</para>
             * </remarks>
             */
            public static bool GenerateEmptyHeaderLines(byte[] data, Int32 data_length)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV2_GenerateEmptyHeaderLines(data, data_length);
            }

            /**
             * <summary>Generates the format version 2 data</summary>
             * <param name='data'>adxlip data output buffer</param>
             * <param name='data_length'>Length of the buffer (in bytes)</param>
             * <param name='result'>Frame data</param>
             * <returns>Whether the bool function succeeded</returns>
             * <remarks>
             * <para header='Description'>Generates the format version 2 frame data.</para>
             * </remarks>
             */
            public static bool GenerateLine(byte[] data, Int32 data_length, ref FrameData result)
            {
                return SafeNativeMethods.criAdxlipParser_FormatV2_GenerateLine(data, data_length, ref result);
            }
        }
    }
}

} //namespace CriWare
/**
 * @}
 */

/* --- end of file --- */