using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBWizard
{
    /// <summary>
    /// Helper class to compute Crc32-values.
    /// </summary>
    internal class CCrc32
    {
        private static readonly UInt32[] _s_p_crc_table;

        static CCrc32()
        {
            _s_p_crc_table = new UInt32[256];
            for (Int32 i = 0; i < 256; ++i)
            {
                UInt32 val = (UInt32)i;
                for (Int32 j = 0; j < 8; ++j)
                {
                    if ((val & 1) != 0)
                    {
                        val = 0xEDB88320U ^ (val >> 1);
                    }
                    else
                    {
                        val >>= 1;
                    }
                }
                _s_p_crc_table[i] = val;
            }
        }

        /// <summary>
        /// Computes the Crc32-value for a given Byte array.
        /// </summary>
        /// <param name="p_buffer">The Byte array to compute the Crc32-value for.</param>
        /// <returns>The computed Crc32-value for the given Byte array.</returns>
        internal static Int32 GetCrc32(Byte[] p_buffer)
        {
            UInt32 crc_val = 0xFFFFFFFF;

            for (Int32 i = 0; i < p_buffer.Length; ++i)
            {
                crc_val = _s_p_crc_table[(crc_val ^ p_buffer[i]) & 0xFF] ^ (crc_val >> 8);
            }

            return (Int32)(crc_val ^ 0xFFFFFFFF);
        }

        /// <summary>
        /// Computes the Crc32-value for a given Byte list.
        /// </summary>
        /// <param name="p_buffer">The Byte list to compute the Crc32-value for.</param>
        /// <returns>The computed Crc32-value for the given Byte list.</returns>
        internal static Int32 GetCrc32(List<Byte> p_buffer)
        {
            UInt32 crc_val = 0xFFFFFFFF;

            for (Int32 i = 0; i < p_buffer.Count; ++i)
            {
                crc_val = _s_p_crc_table[(crc_val ^ p_buffer[i]) & 0xFF] ^ (crc_val >> 8);
            }

            return (Int32)(crc_val ^ 0xFFFFFFFF);
        }

        /// <summary>
        /// Computes the Crc32-value for a given message with UTF8-encoding.
        /// </summary>
        /// <param name="p_msg">The message to compute the Crc32-value for.</param>
        /// <returns>The computed Crc32-value for the given message.</returns>
        internal static Int32 GetCrc32(String p_msg)
        {
            return GetCrc32(Encoding.UTF8.GetBytes(p_msg));
        }

        /// <summary>
        /// Computes the Crc32-value for a given message with a given encoding.
        /// </summary>
        /// <param name="p_msg">The message to compute the Crc32-value for.</param>
        /// <param name="p_encoding">The encoding to use to convert the message into a Byte-array.</param>
        /// <returns>The computed Crc32-value for the given message.</returns>
        internal static Int32 GetCrc32(String p_msg, Encoding p_encoding)
        {
            return GetCrc32(p_encoding.GetBytes(p_msg));
        }
    }
}
