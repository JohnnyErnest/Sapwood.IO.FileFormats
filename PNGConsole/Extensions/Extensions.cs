using System;
using System.Collections.Generic;
using System.Text;

namespace Sapwood.IO.FileFormats.Extensions
{
    public static class Extensions
    {
        public static string Reverse(this string input)
        {
            StringBuilder sb = new StringBuilder();
            for(int z = input.Length - 1; z >= 0; z--)
            {
                sb.Append(input[z]);
            }
            return sb.ToString();
        }

        public static string GetHexadecimalString(this byte[] bytes, int countPerLine)
        {
            bool done = false;
            int idx = 0;
            StringBuilder sb = new StringBuilder();
            while (!done)
            {
                sb.Append(string.Format("{0:X2}, ", bytes[idx]));
                if (idx > 0 && idx % countPerLine == 0)
                    sb.AppendLine();
                idx++;
                if (idx == bytes.Length)
                    done = true;
            }
            return sb.ToString();
        }

        public static byte[] GetSublength(this byte[] bytes, int offset, int count)
        {
            byte[] outputBytes = new byte[count];
            Array.Copy(bytes, offset, outputBytes, 0, count);
            return outputBytes;
        }

        public static byte[] GetSublength(this byte[] bytes, int offset)
        {
            int count = bytes.Length - offset;
            byte[] outputBytes = new byte[count];
            Array.Copy(bytes, offset, outputBytes, 0, count);
            return outputBytes;
        }

        public static ulong GetULongLE(this byte[] bytes, int offset)
        {
            return (ulong)bytes[offset] | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24;
        }

        public static uint GetUIntLE(this byte[] bytes, int offset)
        {
            return (uint)bytes[offset] | (uint)bytes[offset + 1] << 8;
        }

        public static ulong GetULongBE(this byte[] bytes, int offset)
        {
            return (ulong)bytes[offset] << 24 | (ulong)bytes[offset + 1] << 16 | (ulong)bytes[offset + 2] << 8 | (ulong)bytes[offset + 3];
        }

        public static uint GetUIntBE(this byte[] bytes, int offset)
        {
            return (uint)bytes[offset] << 8 | (uint)bytes[offset + 1];
        }

        public static byte[] GetBytesBE(this uint data)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)((data & 0xFF000000) >> 24);
            bytes[1] = (byte)((data & 0x00FF0000) >> 16);
            bytes[2] = (byte)((data & 0x0000FF00) >> 8);
            bytes[3] = (byte)((data & 0x000000FF) >> 0);
            return bytes;
        }
        public static byte[] GetBytesBE(this ushort data)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)((data & 0xFF00) >> 8);
            bytes[1] = (byte)((data & 0x00FF) >> 0);
            return bytes;
        }

        public static byte[] GetBytesLE(this uint data)
        {
            byte[] bytes = new byte[4];
            bytes[3] = (byte)((data & 0xFF000000) >> 24);
            bytes[2] = (byte)((data & 0x00FF0000) >> 16);
            bytes[1] = (byte)((data & 0x0000FF00) >> 8);
            bytes[0] = (byte)((data & 0x000000FF) >> 0);
            return bytes;
        }
        public static byte[] GetBytesLE(this ushort data)
        {
            byte[] bytes = new byte[2];
            bytes[1] = (byte)((data & 0xFF00) >> 8);
            bytes[0] = (byte)((data & 0x00FF) >> 0);
            return bytes;
        }

    }
}