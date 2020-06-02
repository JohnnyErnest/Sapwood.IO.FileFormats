using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Sapwood.IO.FileFormats.Collections
{
    public class BitReader
    {
        public List<bool> BitsBool = new List<bool>();
        public List<byte> BitsByte = new List<byte>();
        public StringBuilder BitsString = new StringBuilder();

        public int LastPositionBeforeGetSymbol { get; set; }
        public int Length
        {
            get
            {
                return BitsBool.Count;
            }
        }

        public bool IsEOF
        {
            get
            {
                return (Position >= BitsBool.Count);
            }
        }

        public class Element
        {
            public bool ValueBoolean { get; set; }
            public byte ValueByte { get; set; }
            public char ValueChar { get; set; }
        }

        public int Position { get; set; }

        public BitReader()
        {
            BitsBool = new List<bool>();
            BitsByte = new List<byte>();
            BitsString = new StringBuilder();
        }

        public BitReader(byte[] data, bool msbFirst)
        {
            BitsBool = new List<bool>();
            BitsByte = new List<byte>();
            BitsString = new StringBuilder();
            if (msbFirst)
                SetBytesMSBFirst(data);
            else
                SetBytesMSBLast(data);
        }

        public Element GetElement()
        {
            if (Position == BitsBool.Count)
                return null;
            Element e = new Element()
            {
                ValueBoolean = BitsBool[Position],
                ValueByte = BitsByte[Position],
                ValueChar = BitsString[Position]
            };
            Position++;
            return e;
        }

        public Element ReadBit()
        {
            return GetElement();
        }

        public int ReadBitInt()
        {
            if (Position == Length)
                return -1;
            var c = BitsByte[Position];
            Position++;
            return c;
        }

        public byte ReadByte()
        {
            return (byte)ReadBitsToCode(8);
        }

        public byte[] ReadBytes(int count)
        {
            byte[] result = new byte[count];
            for(int z=0;z<count;z++)
            {
                result[z] = ReadByte();
            }
            return result;
        }

        public Element[] ReadBits(int count)
        {
            List<Element> elements = new List<Element>();
            for (int z = 0; z < count; z++)
            {
                elements.Add(ReadBit());
            }
            return elements.ToArray();
        }

        public ulong PeekBitsToCode(int count)
        {
            int pos = this.Position;
            if (count == 0)
            {
                return 0;
            }
            if (this.Position >= Length)
                return 0;
            //int shiftCounter = 0;
            ulong result = 0;
            for (int z = 0; z < count; z++)
            {
                ulong current = (ulong)ReadBitInt();
                result += (current << (count - z - 1));
                //shiftCounter++;
            }
            this.Position = pos;
            return result;
        }

        public ulong PeekBitsToCodeAtPosition(int count, int position)
        {
            int pos = this.Position;
            this.Position = position;
            if (count == 0)
            {
                return 0;
            }
            if (this.Position >= Length)
                return 0;
            //int shiftCounter = 0;
            ulong result = 0;
            for (int z = 0; z < count; z++)
            {
                ulong current = (ulong)ReadBitInt();
                result += (current << (count - z - 1));
                //shiftCounter++;
            }
            this.Position = pos;
            return result;
        }

        public ulong ReadBitsToCode(int count)
        {
            if (count == 0)
            {
                return 0;
            }
            if (this.Position >= Length)
                return 0;
            //int shiftCounter = 0;
            ulong result = 0;
            for(int z=0;z<count;z++)
            {
                ulong current = (ulong)ReadBitInt();
                result += (current << (count - z - 1));
                //shiftCounter++;
            }
            return result;
            //Element[] elements = ReadBits(count);
            //StringBuilder sb = new StringBuilder();
            //for (int z = 0; z < count; z++)
            //{
            //    Element e = elements[z];
            //    sb.Append(e.ValueChar);
            //}
            //return Convert.ToUInt64(sb.ToString(), 2);
        }

        public string ReadBitsToCodeString(int count)
        {
            if (count == 0)
            {
                return "";
            }
            if (this.Position >= Length)
                return "";
            string result = "";
            for (int z = 0; z < count; z++)
            {
                int current = ReadBitInt();
                result += (current == 1) ? "1" : "0";
            }
            return result;
        }

        public Element PeekElementAtIndex(int position)
        {
            Element e = new Element()
            {
                ValueBoolean = BitsBool[position],
                ValueByte = BitsByte[position],
                ValueChar = BitsString[position]
            };
            return e;
        }

        public void AlignByte()
        {
            while (this.Position % 8 != 0)
                this.Position++;
        }

        public void SetBytesMSBFirst(byte[] data)
        {
            for (int z = 0; z < data.Length; z++)
                SetBitsMSBFirst(data[z]);
        }

        public void SetBytesMSBLast(byte[] data)
        {
            for (int z = 0; z < data.Length; z++)
                SetBitsMSBLast(data[z]);
        }

        public void SetBitsMSBFirst(byte data)
        {
            BitsBool.Add((((data >> 7) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 6) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 5) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 4) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 3) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 2) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 1) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data) & 0x1) == 1) ? true : false);

            BitsByte.Add((byte)((((data >> 7) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 6) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 5) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 4) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 3) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 2) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 1) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data) & 0x1) == 1) ? 1 : 0));

            BitsString.Append((((data >> 7) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 6) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 5) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 4) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 3) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 2) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 1) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data) & 0x1) == 1) ? "1" : "0");
        }

        public void SetBitsMSBLast(byte data)
        {
            BitsBool.Add((((data) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 1) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 2) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 3) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 4) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 5) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 6) & 0x1) == 1) ? true : false);
            BitsBool.Add((((data >> 7) & 0x1) == 1) ? true : false);

            BitsByte.Add((byte)((((data) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 1) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 2) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 3) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 4) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 5) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 6) & 0x1) == 1) ? 1 : 0));
            BitsByte.Add((byte)((((data >> 7) & 0x1) == 1) ? 1 : 0));

            BitsString.Append((((data) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 1) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 2) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 3) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 4) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 5) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 6) & 0x1) == 1) ? "1" : "0");
            BitsString.Append((((data >> 7) & 0x1) == 1) ? "1" : "0");
        }
    }
}
