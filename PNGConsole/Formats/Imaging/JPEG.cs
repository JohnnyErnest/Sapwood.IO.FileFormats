using Microsoft.VisualBasic.CompilerServices;
using Sapwood.IO.FileFormats.Collections;
using Sapwood.IO.FileFormats.Extensions;
using Sapwood.IO.FileFormats.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks.Sources;

namespace Sapwood.IO.FileFormats.Formats.Imaging
{
    public class JPEG
    {
        public class Header
        {
            public List<QuantizationTable> QuantizationTables { get; set; }
            public List<HuffmanTable> HuffmanTables { get; set; }
            public short RestartInterval { get; set; }
            public StartOfFrameInformation SOFInfo { get; set; }
            public StartOfImageScanInformation SOISInfo { get; set; }

            public Header()
            {
                QuantizationTables = new List<QuantizationTable>();
                HuffmanTables = new List<HuffmanTable>();
            }
        }

        public RGBImage<byte> Image { get; set; }

        public enum TableType
        {
            DC,
            AC
        }

        public enum JPEGCommandType
        {
            TEM = 0x01,
            StartOfFrameSOF0 = 0xc0,
            StartOfFrameSOF1 = 0xc1,
            StartOfFrameSOF2 = 0xc2,
            StartOfFrameSOF3 = 0xc3,
            HuffmanTable = 0xc4,
            StartOfFrameSOF5 = 0xc5,
            StartOfFrameSOF6 = 0xc6,
            StartOfFrameSOF7 = 0xc7,
            UndefinedReserved = 0xc8,
            StartOfFrameSOF9 = 0xc9,
            StartOfFrameSOF10 = 0xca,
            StartOfFrameSOF11 = 0xcb,
            DefineArithmeticTable = 0xcc,
            StartOfFrameSOF13 = 0xcd,
            StartOfFrameSOF14 = 0xce,
            StartOfFrameSOF15 = 0xcf,
            Rest0 = 0xd0,
            Rest1 = 0xd1,
            Rest2 = 0xd2,
            Rest3 = 0xd3,
            Rest4 = 0xd4,
            Rest5 = 0xd5,
            Rest6 = 0xd6,
            Rest7 = 0xd7,
            StartOfImage = 0xd8,
            EndOfImage = 0xd9,
            StartOfImageScan = 0xda,
            QuantizationTable = 0xdb,
            RestartInterval = 0xdd,
            DHP = 0xde,
            EXP = 0xdf,
            ApplicationExtension0 = 0xe0,
            ApplicationExtension1 = 0xe1,
            ApplicationExtension2 = 0xe2,
            ApplicationExtension3 = 0xe3,
            ApplicationExtension4 = 0xe4,
            ApplicationExtension5 = 0xe5,
            ApplicationExtension6 = 0xe6,
            ApplicationExtension7 = 0xe7,
            ApplicationExtension8 = 0xe8,
            ApplicationExtension9 = 0xe9,
            ApplicationExtension10 = 0xea,
            ApplicationExtension11 = 0xeb,
            ApplicationExtension12 = 0xec,
            ApplicationExtension13 = 0xed,
            ApplicationExtension14 = 0xee,
            ApplicationExtension15 = 0xef,
            Comment = 0xfe
        }

        public enum JPEGCompressionType
        {
            HuffmanCodingBaselineDCT = JPEGCommandType.StartOfFrameSOF0,
            HuffmanCodingExtendedSequentialDCT = JPEGCommandType.StartOfFrameSOF1,
            HuffmanCodingProgressiveDCT = JPEGCommandType.StartOfFrameSOF2,
            HuffmanCodingLossless = JPEGCommandType.StartOfFrameSOF3,
            HuffmanCodingDifferentialSequentialDCT = JPEGCommandType.StartOfFrameSOF5,
            HuffmanCodingDifferentialProgressiveDCT = JPEGCommandType.StartOfFrameSOF6,
            HuffmanCodingDifferentialLossless = JPEGCommandType.StartOfFrameSOF7,
            ArithmeticCodingExtendedSequentialDCT = JPEGCommandType.StartOfFrameSOF9,
            ArithmeticCodingProgressiveDCT = JPEGCommandType.StartOfFrameSOF10,
            ArithmeticCodingLossless = JPEGCommandType.StartOfFrameSOF11,
            ArithmeticCodingDifferentialSequentialDCT = JPEGCommandType.StartOfFrameSOF13,
            ArithmeticCodingDifferentialProgressiveDCT = JPEGCommandType.StartOfFrameSOF14,
            ArithmeticCodingDifferentialLossless = JPEGCommandType.StartOfFrameSOF15
        }

        void LogBytes(byte[] bytes)
        {
            //for (int z = 0; z < bytes.Length; z++)
            //    Console.Write("{0}:{1}, ", z, bytes[z]);
            //Console.WriteLine();
        }

        public class QuantizationTable
        {
            public int TableNumber { get; set; }

            public byte[] Table8Bit { get; set; }
            public short[] Table16Bit { get; set; }
            public byte[,] TableFormatted8Bit { get; set; }
            public short[,] TableFormatted16Bit { get; set; }

            public byte QTNumber { get; set; }
            public byte QTPrecision { get; set; }
        }

        List<QuantizationTable> ReadQuantizationTables(BinaryReader reader)
        {
            List<QuantizationTable> tables = new List<QuantizationTable>();
            // Quantization Table
            short length = (short)reader.ReadBytes(2).GetUIntBE(0);
            byte[] dataBytes = reader.ReadBytes(length - 2);
            //Console.WriteLine("Quantization Table - {0}", length - 2);
            int dataRead = 0;
            int tableNumber = 0;
            while (dataRead < dataBytes.Length)
            {
                byte qtInformation = dataBytes[dataRead];
                byte numberOfQT = (byte)(qtInformation & 0b1111);
                byte precisionOfQT = (byte)((qtInformation >> 4) & 0b1111);
                int n = 64 * (precisionOfQT + 1);
                dataRead += 1;
                //Console.WriteLine("- Reading Table #{0}, QT#:{1}, Precision:{2}", tableNumber, numberOfQT, precisionOfQT);
                byte[] table = dataBytes.GetSublength(dataRead, n);
                //LogBytes(table);
                short[] tableShort = new short[table.Length / 2];
                byte[,] tableFormatted = new byte[8, 8];
                short[,] tableShortFormatted = new short[8, 8];
                //int tableOffset = 0;
                if (precisionOfQT == 1)
                {
                    int idx1 = 0;
                    for (int idx = 0; idx < 128; idx += 2)
                    {
                        tableShort[idx1] = (short)table.GetUIntBE(idx);
                        idx1++;
                    }
                    InverseZigZag<short>(tableShort, 8, 8, out tableShortFormatted);
                    int z = 0;
                    for (int y = 0; y < 8; y++) {
                        for (int x = 0; x < 8; x++) {
                            tableShort[z] = tableShortFormatted[x, y];
                            z++;
                        }
                    }

                    //short[] tableout = new short[64];
                    //ZigZag<short>(tableShortFormatted, 8, 8, out tableout);
                    //Console.WriteLine("Formatted:");
                    //for (int z = 0; z < 64; z++)
                    //    Console.Write("{0}, ", tableout[z]);
                    //Console.WriteLine();
                    //Console.WriteLine("Original:");
                    //for (int z = 0; z < 64; z++)
                    //    Console.Write("{0}, ", tableShort[z]);
                    //Console.WriteLine();
                }
                else
                {
                    InverseZigZag<byte>(table, 8, 8, out tableFormatted);
                    int z = 0;
                    for (int y = 0; y < 8; y++)
                    {
                        for (int x = 0; x < 8; x++)
                        {
                            table[z] = tableFormatted[x, y];
                            z++;
                        }
                    }

                    //byte[] tableout = new byte[64];
                    //ZigZag<byte>(tableFormatted, 8, 8, out tableout);
                    //Console.WriteLine("Formatted:");
                    //LogBytes(tableout);
                    //Console.WriteLine("Original:");
                    //LogBytes(table);
                }
                tables.Add(new QuantizationTable() { QTNumber = numberOfQT, QTPrecision = precisionOfQT, TableNumber = tableNumber, Table8Bit = table, Table16Bit = tableShort, TableFormatted8Bit = tableFormatted, TableFormatted16Bit = tableShortFormatted });
                //for (int y = 0; y < 8; y++)
                //{
                //    for (int x = 0; x < 8; x++)
                //    {
                //        if (precisionOfQT == 0)
                //        {
                //            byte current = tableFormatted[x, y];
                //            Console.Write("{0}, ", current.ToString().PadLeft(3, ' '));
                //            tableOffset++;
                //        }
                //        else if (precisionOfQT == 1)
                //        {
                //            short current = tableShortFormatted[x, y];
                //            Console.Write("{0}, ", current.ToString().PadLeft(5, ' '));
                //            tableOffset += 2;
                //        }
                //    }
                //    Console.WriteLine();
                //}
                tableNumber++;
                dataRead += n;
            }
            //if (dataRead != dataBytes.Length)
            //    LogBytes(dataBytes);
            return tables;
        }

        public class HuffmanTable
        {
            public class SymbolObject
            {
                public int Bits { get; set; }
                public int Symbol { get; set; }
                public string CodeString { get; set; }
                public ulong Code { get; set; }
                public bool IsEOB { get; set; }
                public bool IsZRL { get; set; }

                public override string ToString()
                {
                    return $"[Symbol:[Bits:{Bits}, Symbol:{Symbol}, SymbolHex:{Symbol:X2}, Code:{Code}, CodeHex:{Code:X2}, CodeString:{CodeString}{((IsEOB == true) ? " (EOB)" : "")}{((IsZRL == true) ? " (ZRL)" : "")}]]";
                }
            }

            public int SymbolsTableNumber { get; set; }
            public byte HTNumber { get; set; }
            public TableType HTTableType { get; set; }
            public List<SymbolObject> Symbols { get; set; }
        }

        List<HuffmanTable> ReadHuffmanTables(BinaryReader reader)
        {
            // Huffman Table

            byte[] lengthBytes = reader.ReadBytes(2);
            short length = (short)lengthBytes.GetUIntBE(0);
            byte[] dataBytes = reader.ReadBytes(length - 2);

            byte[] buffer = new byte[lengthBytes.Length + dataBytes.Length];
            Array.Copy(lengthBytes, 0, buffer, 0, 2);
            Array.Copy(dataBytes, 0, buffer, 2, dataBytes.Length);

            //DecodeDHT(buffer, MaskLookup);

            int dataRead = 0;
            //Console.WriteLine("Huffman Table - {0}", length - 2);
            int symbolsTable = 0;

            List<HuffmanTable> tables = new List<HuffmanTable>();
            while (dataRead < dataBytes.Length)
            {
                HuffmanTable table = new HuffmanTable();
                byte htInformation = dataBytes[dataRead];
                dataRead++;
                byte numberOfHT = (byte)(htInformation & 0b1111);
                byte typeOfHT = (byte)((htInformation >> 4) & 0b1);
                byte notUsed = (byte)((htInformation >> 5) & 0b111);
                byte[] numberOfSymbols = dataBytes.GetSublength(dataRead, 16);
                dataRead += numberOfSymbols.Length;
                int n = 0;
                for (int z = 0; z < numberOfSymbols.Length; z++)
                    n += numberOfSymbols[z];
                //Console.WriteLine("Reading Symbols Table #:{0} - HT#:{1}, HT-Type:{2}, N:{3}", symbolsTable, numberOfHT, typeOfHT, n);
                //Console.WriteLine("Number of Symbols:");
                //LogBytes(numberOfSymbols);
                byte[] symbols = dataBytes.GetSublength(dataRead, n);
                dataRead += n;

                int symbols_idx = 0;
                uint code_value = 0;

                table.HTNumber = numberOfHT;
                table.HTTableType = (typeOfHT == 0) ? TableType.DC : TableType.AC;
                table.Symbols = new List<HuffmanTable.SymbolObject>();
                for (int z = 0; z < numberOfSymbols.Length; z++)
                {
                    int idx0 = 0;
                    int idx1 = numberOfSymbols[z];
                    uint bit_length = (uint)(z + 1);

                    if (idx1 > 0)
                    {
                        //Console.WriteLine("Symbols for {0} bits", (z + 1));
                        while (idx0 < idx1)
                        {
                            uint nDecVal = code_value;
                            uint nBinBit;
                            char[] acBinStr = ("").PadLeft(16, ' ').ToCharArray();
                            uint nBinStrLen = 0;

                            for (uint nBinInd = bit_length; nBinInd >= 1; nBinInd--)
                            {
                                nBinBit = (nDecVal >> (int)(nBinInd - 1)) & 1;
                                acBinStr[nBinStrLen++] = (nBinBit > 0) ? '1' : '0';
                            }
                            //acBinStr[nBinStrLen] = '\0';
                            string strFull = string.Format("{0}", new string(acBinStr).Trim());
                            ulong code = Convert.ToUInt64(strFull, 2);

                            HuffmanTable.SymbolObject symbolObject = new HuffmanTable.SymbolObject()
                            {
                                Bits = (byte)bit_length,
                                CodeString = strFull,
                                Code = code,
                                Symbol = symbols[symbols_idx],
                                IsEOB = ((symbols[symbols_idx] == 0 && table.HTTableType == TableType.AC) ? true : false),
                                IsZRL = ((symbols[symbols_idx] == 0xF0 && table.HTTableType == TableType.AC) ? true : false)
                            };

                            //string symbol_extra = "";
                            //if (symbols[symbols_idx] == 0)
                            //{
                            //    symbol_extra = " (EOB)";
                            //}
                            //else if (symbols[symbols_idx] == 0xF0)
                            //{
                            //    symbol_extra = " (ZRL)";
                            //}
                            //Console.WriteLine("{2} / {3} - {0:X2} ({0}){1}, ", symbols[symbols_idx], symbol_extra, strFull, code);

                            table.Symbols.Add(symbolObject);

                            symbols_idx++;
                            idx0++;
                            code_value++;
                        }
                        //Console.WriteLine();
                    }
                    code_value <<= 1;
                }
                table.SymbolsTableNumber = symbolsTable;
                tables.Add(table);
                symbolsTable++;
            }
            return tables;
        }


        public class StartOfFrameInformation
        {
            public byte DataPrecision { get; set; }
            public short ImageHeight { get; set; }
            public short ImageWidth { get; set; }
            public JPEGCompressionType CompressionType { get; set; }
            public bool ZeroBased { get; set; }
            public int MCUWidth { get; set; }
            public int MCUHeight { get; set; }
            public int MCUWidthReal { get; set; }
            public int MCUHeightReal { get; set; }
            public int HorizontalSamplingFactor { get; set; }
            public int VerticalSamplingFactor { get; set; }

            public class Component
            {
                public byte ComponentID { get; set; }
                public byte SubsamplingHorizontal { get; set; }
                public byte SubsamplingVertical { get; set; }
                public byte QuantizationTableDestination { get; set; }
            }

            public List<Component> Components { get; set; }
        }

        public class StartOfImageScanInformation
        {
            public byte NumberOfComponentsInScan { get; set; }
            public class Component
            {
                public byte ComponentID { get; set; }
                public byte DCTableDestination { get; set; }
                public byte ACTableDestination { get; set; }
            }

            public List<Component> Components { get; set; }
            public byte StartOfSpectralSelection { get; set; }
            public byte EndOfSpectralSelection { get; set; }
            public byte SuccessiveApproximationHigh { get; set; }
            public byte SuccessiveApproximationLow { get; set; }
        }

        public class HuffmanCodeCheckResult
        {
            public bool Success { get; set; }
            public HuffmanTable.SymbolObject CodeSymbol { get; set; }
        }

        public class MCU
        {
            public int[] Y = new int[64];
            public int[] Cb = new int[64];
            public int[] Cr = new int[64];

            public MCU Clone()
            {
                int[] y = new int[64];
                int[] cb = new int[64];
                int[] cr = new int[64];

                for(int z=0;z<64;z++)
                {
                    y[z] = Y[z];
                    cb[z] = Cb[z];
                    cr[z] = Cr[z];
                }
                return new MCU()
                {
                    Y = y,
                    Cb = cb,
                    Cr = cr
                };
            }

            public int[] this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return Y;
                        case 1: return Cb;
                        case 2: return Cr;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                set
                {
                    switch (index)
                    {
                        case 0: Y = value; break;
                        case 1: Cb = value; break;
                        case 2: Cr = value; break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public class MCUCollection
        {
            public int MCUWidth;
            public int MCUHeight;
            public int MCUWidthReal;
            public int MCUHeightReal;

            public MCUCollection()
            {

            }

            public MCUCollection(int mcuWidthReal, int mcuHeightReal, int mcuWidth, int mcuHeight)
            {
                MCUWidth = mcuWidth;
                MCUHeight = mcuHeight;
                MCUWidthReal = mcuWidthReal;
                MCUHeightReal = mcuHeightReal;
                MCUs = new MCU[MCUWidthReal * MCUHeightReal];
                for(int z=0;z<MCUs.Length;z++)
                {
                    MCUs[z] = new MCU();
                }
            }

            public MCU[] MCUs { get; set; }

            public int Length { get => MCUs.Length; }

            public MCU this[int index]
            {
                get
                {
                    return MCUs[index];
                }
                set
                {
                    MCUs[index] = value;
                }
            }

            public MCU this[long index]
            {
                get
                {
                    return MCUs[index];
                }
                set
                {
                    MCUs[index] = value;
                }
            }

            public MCU this[uint index]
            {
                get
                {
                    return MCUs[index];
                }
                set
                {
                    MCUs[index] = value;
                }
            }

            public MCU this[ulong index]
            {
                get
                {
                    return MCUs[index];
                }
                set
                {
                    MCUs[index] = value;
                }
            }


            public MCU this[int x, int y]
            {
                get
                {
                    return MCUs[MCUWidthReal * y + x];
                }
                set
                {
                    MCUs[MCUWidthReal * y + x] = value;
                }
            }

            public MCUCollection Clone()
            {
                MCUCollection collection = new MCUCollection();
                collection.MCUs = new MCU[this.MCUs.Length];
                for(int z=0;z<collection.MCUs.Length;z++)
                {
                    collection.MCUs[z] = this.MCUs[z].Clone();
                }
                collection.MCUHeight = this.MCUHeight;
                collection.MCUHeightReal = this.MCUHeightReal;
                collection.MCUWidth = this.MCUWidth;
                collection.MCUWidthReal = this.MCUWidthReal;
                return collection;
            }
        }

        HuffmanCodeCheckResult IsCodeInHuffmanTable(BitReader.Element[] elements, HuffmanTable table)
        {
            HuffmanCodeCheckResult result = new HuffmanCodeCheckResult();
            bool success = false;

            StringBuilder sb = new StringBuilder();
            for (int z = 0; z < elements.Length; z++)
                sb.Append(elements[z].ValueChar);
            string code = sb.ToString();
            for (int z = 0; z < table.Symbols.Count; z++)
            {
                if (code == table.Symbols[z].CodeString)
                {
                    success = true;
                    result.CodeSymbol = table.Symbols[z];
                    break;
                }
            }
            result.Success = success;
            return result;
        }

        List<HuffmanTable.SymbolObject> ProcessEncodedData(byte[] data, HuffmanTable table)
        {
            BitReader reader = new BitReader(data, true);
            string test = reader.BitsString.ToString();
            reader.Position = 0;
            List<BitReader.Element> elements = new List<BitReader.Element>();
            List<HuffmanTable.SymbolObject> symbols = new List<HuffmanTable.SymbolObject>();
            while (reader.Position != reader.BitsBool.Count)
            {
                elements.Add(reader.GetElement());
                BitReader.Element[] e = elements.ToArray();
                HuffmanCodeCheckResult result = IsCodeInHuffmanTable(e, table);
                if (result.Success)
                {
                    elements = new List<BitReader.Element>();
                    symbols.Add(result.CodeSymbol);
                    //Console.WriteLine("Table #:{4} - Type:{3} - Symbol:{0} - Position:{1} - Length:{2}", result.CodeSymbol, reader.Position, reader.BitsByte.Count, table.HTTableType, table.HTNumber);
                }
                else
                {
                    if (elements.Count >= 16)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int z = 0; z < elements.Count; z++)
                        {
                            sb.Append(elements[z].ValueChar);
                        }
                        //Console.WriteLine("***CANNOT FIND CODE**** {0} - {1}", sb.ToString(), sb.Length);
                    }
                }
            }
            return symbols;
        }

        // return the symbol from the Huffman table that corresponds to
        //   the next Huffman code read from the BitReader
        HuffmanTable.SymbolObject GetNextSymbol(BitReader b, HuffmanTable hTable) {
            b.LastPositionBeforeGetSymbol = b.Position;
            uint currentCode = 0;
            for (uint i = 0; i < 16; ++i) {
                //var c = b.ReadBit();
                int c = b.ReadBitInt();
                //if (c == null)
                if (c == -1)
                {
                    //return new HuffmanTable.SymbolObject()
                    //{
                    //    Symbol = (int)currentCode,
                    //    Code = 0,
                    //    CodeString = null
                    //};
                    return null;
                }
                //int bit = c.ValueByte;
                //if (bit == -1) {
                //    return null;
                //}
                int bit = c;
                currentCode = (uint)((currentCode << 1) | (uint)bit);

                //HuffmanTable.SymbolObject[] objects = hTable.Symbols.Where(x => x.Bits == i + 1).ToArray();
                //var objects = hTable.Symbols.Where(x => x.Bits == i + 1 && currentCode == x.Code);
                var result = hTable.Symbols.Where(x => x.Bits == i + 1 && currentCode == x.Code).FirstOrDefault();
                if (result != null)
                {
                    return result;
                }
                //int j = 0;
                //foreach (var obj in objects)
                //{
                //    return obj;
                //    //if (currentCode == obj.Code)
                //    //{
                //    //    return obj;
                //    //}
                //}
            }
            Console.WriteLine("*** COULD NOT FIND CODE *** {0}:{1}", currentCode, Convert.ToString(currentCode, 2));
            return null;
        }

        byte[] ZigZagMap = new byte[]{
            0,   1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63
        };


        // fill the coefficients of an MCU component based on Huffman codes
        //   read from the BitReader
        bool DecodeMCUComponent(Header header, BitReader b, ref int[] component, ref int previousDC, HuffmanTable dcTable, HuffmanTable acTable, bool useDC = true, bool useAC = true) {
            // get the DC value for this MCU component
            //Console.Write("DC: ");
            int coeff = 0;

            if (useDC)
            {
                var c = GetNextSymbol(b, dcTable);
                if (c == null)
                {
                    string last40 = b.BitsString.ToString().Substring(b.Position - 40, 40);
                    Console.WriteLine("Error - Invalid DC value - Last 20 Characters: {0}", last40);
                    return false;
                }
                else
                {
                    //Console.WriteLine(b.Position + " - " + c);
                }

                byte length = (byte)c.Symbol;

                if (length == 0xFF)
                {
                    Console.WriteLine("Error - Invalid DC value");
                    return false;
                }
                if (length > 11)
                {
                    Console.WriteLine("Error - DC coefficient length greater than 11");
                    return false;
                }

                coeff = (int)b.ReadBitsToCode(length);

                if (coeff == -1)
                {
                    Console.WriteLine("Error - Invalid DC value");
                    return false;
                }

                if (length != 0 && coeff < (1 << (length - 1)))
                {
                    coeff -= (1 << length) - 1;
                }
                component[0] = coeff + previousDC;
                previousDC = component[0];
            }

            if (useAC)
            {
                uint i = 1;
                uint i2 = 64;
                if (header.SOFInfo.CompressionType == JPEGCompressionType.HuffmanCodingProgressiveDCT)
                {
                    i = header.SOISInfo.StartOfSpectralSelection;
                    i2 = header.SOISInfo.EndOfSpectralSelection;
                }
                while (i < i2)
                {
                    //Console.Write("AC: ");
                    var c2 = GetNextSymbol(b, acTable);
                    if (c2 == null)
                    {
                        Console.WriteLine("Error - Invalid AC value");
                        return false;
                    }
                    else
                    {
                        //Console.WriteLine(c2);
                    }

                    byte symbol = (byte)c2.Symbol;

                    if (symbol == 0xFF)
                    {
                        Console.WriteLine("Error - Invalid AC value");
                        return false;
                    }

                    // symbol 0x00 means fill remainder of component with 0
                    if (symbol == 0x00)
                    {
                        for (; i < 64; ++i)
                        {
                            //component[i] = 0;
                            component[ZigZagMap[i]] = 0;
                        }
                        return true;
                    }

                    // otherwise, read next component coefficient
                    byte numZeroes = (byte)(symbol >> 4);
                    byte coeffLength = (byte)(symbol & 0x0F);
                    coeff = 0;

                    // symbol 0xF0 means skip 16 0's
                    if (symbol == 0xF0)
                    {
                        numZeroes = 16;
                    }

                    if (i + numZeroes >= 64)
                    {
                        Console.WriteLine("Error - Zero run-length exceeded MCU");
                        return false;
                    }
                    for (uint j = 0; j < numZeroes; ++j, ++i)
                    {
                        component[ZigZagMap[i]] = 0;
                        //component[i] = 0;
                    }

                    if (coeffLength > 10)
                    {
                        Console.WriteLine("Error - AC coefficient length greater than 10");
                        return false;
                    }
                    if (coeffLength != 0)
                    {
                        coeff = (int)b.ReadBitsToCode(coeffLength);
                        if (coeff == -1)
                        {
                            Console.WriteLine("Error - Invalid AC value");
                            return false;
                        }
                        if (coeff < (1 << (coeffLength - 1)))
                        {
                            coeff -= (1 << coeffLength) - 1;
                        }
                        component[ZigZagMap[i]] = coeff;
                        //component[i] = coeff;
                        i += 1;
                    }
                }
            }
            return true;
        }

        // dequantize an MCU component based on a quantization table
        void DequantizeMCUComponent(QuantizationTable qTable, ref int[] component) {
            for (uint i = 0; i < 64; ++i) {
                component[i] *= qTable.Table8Bit[i];
            }
        }

        // dequantize all MCUs
        void Dequantize(Header header, ref MCUCollection mcus)
        {
            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    for (uint i = 0; i < header.SOFInfo.Components.Count; ++i)
                    {
                        for (uint v = 0; v < header.SOFInfo.Components[(int)i].SubsamplingVertical; ++v)
                        {
                            for (uint h = 0; h < header.SOFInfo.Components[(int)i].SubsamplingHorizontal; ++h)
                            {
                                var mcu = mcus[((y + v) * header.SOFInfo.MCUWidthReal) + (x + h)][(int)i];
                                DequantizeMCUComponent(header.QuantizationTables[header.SOFInfo.Components[(int)i].QuantizationTableDestination], ref mcu);
                                mcus[((y + v) * header.SOFInfo.MCUWidthReal) + (x + h)][(int)i] = mcu;
                            }
                        }
                    }
                }
            }
        }

        // IDCT scaling factors
        float m0 = 2.0f * (float)Math.Cos(1.0f / 16.0f * 2.0f * Math.PI);
        float m1 = 2.0f * (float)Math.Cos(2.0f / 16.0f * 2.0f * Math.PI);
        float m3 = 2.0f * (float)Math.Cos(2.0f / 16.0f * 2.0f * Math.PI);
        float m5 = 2.0f * (float)Math.Cos(3.0f / 16.0f * 2.0f * Math.PI);
        float m2 = (2.0f * (float)Math.Cos(1.0f / 16.0f * 2.0f * Math.PI)) - (2.0f * (float)Math.Cos(3.0f / 16.0f * 2.0f * Math.PI));
        float m4 = (2.0f * (float)Math.Cos(1.0f / 16.0f * 2.0f * Math.PI)) + (2.0f * (float)Math.Cos(3.0f / 16.0f * 2.0f * Math.PI));

        float s0 = (float)Math.Cos(0.0f / 16.0f * MathF.PI) / MathF.Sqrt(8f);
        float s1 = (float)Math.Cos(1.0f / 16.0f * MathF.PI) / 2.0f;
        float s2 = (float)Math.Cos(2.0f / 16.0f * MathF.PI) / 2.0f;
        float s3 = (float)Math.Cos(3.0f / 16.0f * MathF.PI) / 2.0f;
        float s4 = (float)Math.Cos(4.0f / 16.0f * MathF.PI) / 2.0f;
        float s5 = (float)Math.Cos(5.0f / 16.0f * MathF.PI) / 2.0f;
        float s6 = (float)Math.Cos(6.0f / 16.0f * MathF.PI) / 2.0f;
        float s7 = (float)Math.Cos(7.0f / 16.0f * MathF.PI) / 2.0f;

        // perform 1-D IDCT on all columns and rows of an MCU component
        //   resulting in 2-D IDCT
        void InverseDCTComponent(ref int[] component)
        {
            for (uint i = 0; i < 8; ++i)
            {
                float g0 = component[0 * 8 + i] * s0;
                float g1 = component[4 * 8 + i] * s4;
                float g2 = component[2 * 8 + i] * s2;
                float g3 = component[6 * 8 + i] * s6;
                float g4 = component[5 * 8 + i] * s5;
                float g5 = component[1 * 8 + i] * s1;
                float g6 = component[7 * 8 + i] * s7;
                float g7 = component[3 * 8 + i] * s3;

                float f0 = g0;
                float f1 = g1;
                float f2 = g2;
                float f3 = g3;
                float f4 = g4 - g7;
                float f5 = g5 + g6;
                float f6 = g5 - g6;
                float f7 = g4 + g7;

                float e0 = f0;
                float e1 = f1;
                float e2 = f2 - f3;
                float e3 = f2 + f3;
                float e4 = f4;
                float e5 = f5 - f7;
                float e6 = f6;
                float e7 = f5 + f7;
                float e8 = f4 + f6;

                float d0 = e0;
                float d1 = e1;
                float d2 = e2 * m1;
                float d3 = e3;
                float d4 = e4 * m2;
                float d5 = e5 * m3;
                float d6 = e6 * m4;
                float d7 = e7;
                float d8 = e8 * m5;

                float c0 = d0 + d1;
                float c1 = d0 - d1;
                float c2 = d2 - d3;
                float c3 = d3;
                float c4 = d4 + d8;
                float c5 = d5 + d7;
                float c6 = d6 - d8;
                float c7 = d7;
                float c8 = c5 - c6;

                float b0 = c0 + c3;
                float b1 = c1 + c2;
                float b2 = c1 - c2;
                float b3 = c0 - c3;
                float b4 = c4 - c8;
                float b5 = c8;
                float b6 = c6 - c7;
                float b7 = c7;

                component[0 * 8 + i] = (int)(b0 + b7);
                component[1 * 8 + i] = (int)(b1 + b6);
                component[2 * 8 + i] = (int)(b2 + b5);
                component[3 * 8 + i] = (int)(b3 + b4);
                component[4 * 8 + i] = (int)(b3 - b4);
                component[5 * 8 + i] = (int)(b2 - b5);
                component[6 * 8 + i] = (int)(b1 - b6);
                component[7 * 8 + i] = (int)(b0 - b7);
            }
            for (uint i = 0; i < 8; ++i)
            {
                float g0 = component[i * 8 + 0] * s0;
                float g1 = component[i * 8 + 4] * s4;
                float g2 = component[i * 8 + 2] * s2;
                float g3 = component[i * 8 + 6] * s6;
                float g4 = component[i * 8 + 5] * s5;
                float g5 = component[i * 8 + 1] * s1;
                float g6 = component[i * 8 + 7] * s7;
                float g7 = component[i * 8 + 3] * s3;

                float f0 = g0;
                float f1 = g1;
                float f2 = g2;
                float f3 = g3;
                float f4 = g4 - g7;
                float f5 = g5 + g6;
                float f6 = g5 - g6;
                float f7 = g4 + g7;

                float e0 = f0;
                float e1 = f1;
                float e2 = f2 - f3;
                float e3 = f2 + f3;
                float e4 = f4;
                float e5 = f5 - f7;
                float e6 = f6;
                float e7 = f5 + f7;
                float e8 = f4 + f6;

                float d0 = e0;
                float d1 = e1;
                float d2 = e2 * m1;
                float d3 = e3;
                float d4 = e4 * m2;
                float d5 = e5 * m3;
                float d6 = e6 * m4;
                float d7 = e7;
                float d8 = e8 * m5;

                float c0 = d0 + d1;
                float c1 = d0 - d1;
                float c2 = d2 - d3;
                float c3 = d3;
                float c4 = d4 + d8;
                float c5 = d5 + d7;
                float c6 = d6 - d8;
                float c7 = d7;
                float c8 = c5 - c6;

                float b0 = c0 + c3;
                float b1 = c1 + c2;
                float b2 = c1 - c2;
                float b3 = c0 - c3;
                float b4 = c4 - c8;
                float b5 = c8;
                float b6 = c6 - c7;
                float b7 = c7;

                component[i * 8 + 0] = (int)(b0 + b7);
                component[i * 8 + 1] = (int)(b1 + b6);
                component[i * 8 + 2] = (int)(b2 + b5);
                component[i * 8 + 3] = (int)(b3 + b4);
                component[i * 8 + 4] = (int)(b3 - b4);
                component[i * 8 + 5] = (int)(b2 - b5);
                component[i * 8 + 6] = (int)(b1 - b6);
                component[i * 8 + 7] = (int)(b0 - b7);
            }
        }

        // perform IDCT on all MCUs
        void InverseDCT(Header header, ref MCUCollection mcus)
        {
            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    for (uint i = 0; i < header.SOFInfo.Components.Count; ++i)
                    {
                        for (uint v = 0; v < header.SOFInfo.Components[(int)i].SubsamplingVertical; ++v)
                        {
                            for (uint h = 0; h < header.SOFInfo.Components[(int)i].SubsamplingHorizontal; ++h)
                            {
                                var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][(int)i];
                                InverseDCTComponent(ref mcu);
                                mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][(int)i] = mcu;
                            }
                        }
                    }
                }
            }
        }

        // convert all pixels in an MCU from YCbCr color space to RGB
        void YCbCrToRGBMCU(Header header, ref MCU mcu, ref MCU cbcr, uint v, uint h)
        {
            for (uint y = 7; y < 8; --y)
            {
                for (uint x = 7; x < 8; --x)
                {
                    uint pixel = (uint)(y * 8 + x);
                    uint cbcrPixelRow = (uint)(y / header.SOFInfo.VerticalSamplingFactor + 4 * v);
                    uint cbcrPixelColumn = (uint)(x / header.SOFInfo.HorizontalSamplingFactor + 4 * h);
                    uint cbcrPixel = cbcrPixelRow * 8 + cbcrPixelColumn;
                    int r = (int)(mcu.Y[pixel] + 1.402f * cbcr.Cr[cbcrPixel] + 128);
                    int g = (int)(mcu.Y[pixel] - 0.344f * cbcr.Cb[cbcrPixel] - 0.714f * cbcr.Cr[cbcrPixel] + 128);
                    int b = (int)(mcu.Y[pixel] + 1.772f * cbcr.Cb[cbcrPixel] + 128);
                    if (r < 0) r = 0;
                    if (r > 255) r = 255;
                    if (g < 0) g = 0;
                    if (g > 255) g = 255;
                    if (b < 0) b = 0;
                    if (b > 255) b = 255;
                    mcu.Y[pixel] = r;
                    mcu.Cb[pixel] = g;
                    mcu.Cr[pixel] = b;
                }
            }
        }

        // convert all pixels from YCbCr color space to RGB
        void YCbCrToRGB(Header header, ref MCUCollection mcus)
        {
            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    var cbcr = mcus[(int)(y * header.SOFInfo.MCUWidthReal + x)];
                    for (uint v = (uint)header.SOFInfo.VerticalSamplingFactor - 1; v < header.SOFInfo.VerticalSamplingFactor; --v)
                    {
                        for (uint h = (uint)header.SOFInfo.HorizontalSamplingFactor - 1; h < header.SOFInfo.HorizontalSamplingFactor; --h)
                        {
                            var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))];
                            YCbCrToRGBMCU(header, ref mcu, ref cbcr, v, h);
                        }
                    }
                }
            }
        }

        void CompareBinaryData(string fileName, MCUCollection mcus)
        {
            byte[] data1 = File.ReadAllBytes(fileName);
            int filePointer = 0;
            for (int z = 0; z < mcus.Length; z++)
            {
                for (int i = 0; i < 64; i++)
                {
                    int r = data1[filePointer] + (data1[filePointer + 1] << 8) + (data1[filePointer + 2] << 16) + (data1[filePointer + 3] << 24);
                    //int r = data1[filePointer + 3] + (data1[filePointer + 2] << 8) + (data1[filePointer + 1] << 16) + (data1[filePointer] << 24);
                    filePointer += 4;
                    int g = data1[filePointer] + (data1[filePointer + 1] << 8) + (data1[filePointer + 2] << 16) + (data1[filePointer + 3] << 24);
                    //int g = data1[filePointer + 3] + (data1[filePointer + 2] << 8) + (data1[filePointer + 1] << 16) + (data1[filePointer] << 24);
                    filePointer += 4;
                    int b = data1[filePointer] + (data1[filePointer + 1] << 8) + (data1[filePointer + 2] << 16) + (data1[filePointer + 3] << 24);
                    //int b = data1[filePointer + 3] + (data1[filePointer + 2] << 8) + (data1[filePointer + 1] << 16) + (data1[filePointer] << 24);
                    filePointer += 4;

                    int r2 = mcus[z].Y[i];
                    int g2 = mcus[z].Cb[i];
                    int b2 = mcus[z].Cr[i];

                    if (r2 != r || g2 != g || b2 != b)
                    {
                        //Console.WriteLine($"Diff r:{r}, r2:{r2}, g:{g}, g2:{g2}, b:{b}, b2:{b2}");
                    }
                }
            }
        }

        void DecodeHuffmanData(byte[] data, Header header, ref MCUCollection mcus, bool useDC = true, bool useAC = true)
        {
            for (int z = 0; z < mcus.Length; z++)
                mcus[z] = new MCU();
            BitReader bitReader = new BitReader(data, true);
            int[] previousDCs = new int[3] { 0, 0, 0 };
            uint restartInterval = (uint)(header.RestartInterval * header.SOFInfo.HorizontalSamplingFactor * header.SOFInfo.VerticalSamplingFactor);
            int encodeCount = 0;

            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    if (restartInterval != 0 && (y * header.SOFInfo.MCUWidthReal + x) % restartInterval == 0)
                    {
                        previousDCs[0] = 0;
                        previousDCs[1] = 0;
                        previousDCs[2] = 0;
                        bitReader.AlignByte();
                    }

                    for (uint i = 0; i < header.SOISInfo.NumberOfComponentsInScan; ++i)
                    {
                        for (uint v = 0; v < header.SOFInfo.Components[(int)i].SubsamplingVertical; ++v)
                        {
                            for (uint h = 0; h < header.SOFInfo.Components[(int)i].SubsamplingHorizontal; ++h)
                            {
                                var component = header.SOISInfo.Components[(int)i];
                                //var component = scan.SOISInfo.Components[(int)i];
                                int sofComponent = 0;
                                for (int c = 0; c < header.SOFInfo.Components.Count; c++)
                                {
                                    if (header.SOFInfo.Components[c].ComponentID == component.ComponentID)
                                        sofComponent = c;
                                }
                                //var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];
                                var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];


                                //var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][(int)i];
                                var huffDC = header.HuffmanTables.Where(x => x.HTTableType == TableType.DC && x.HTNumber == component.DCTableDestination).FirstOrDefault();
                                var huffAC = header.HuffmanTables.Where(x => x.HTTableType == TableType.AC && x.HTNumber == component.ACTableDestination).FirstOrDefault();
                                if (!DecodeMCUComponent(
                                        header,
                                        bitReader,
                                        ref mcu,
                                        ref previousDCs[i],
                                        huffDC,
                                        huffAC,
                                        useDC, useAC))
                                {
                                    throw new Exception("Unable to decode image data from Jpeg.");
                                }
                                else
                                {
                                    mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][(int)i] = mcu;
                                    encodeCount++;
                                }
                            }
                        }
                    }
                }
            }
        }

        void CopyMCUsToImage(Header header, MCUCollection mcus)
        {
            for (uint y = (uint)(header.SOFInfo.ImageHeight - 1); y < header.SOFInfo.ImageHeight; --y)
            {
                uint mcuRow = y / 8;
                uint pixelRow = y % 8;
                for (uint x = 0; x < header.SOFInfo.ImageWidth; ++x)
                {
                    uint mcuColumn = x / 8;
                    uint pixelColumn = x % 8;
                    uint mcuIndex = (uint)(mcuRow * header.SOFInfo.MCUWidthReal + mcuColumn);
                    uint pixelIndex = pixelRow * 8 + pixelColumn;

                    byte r = (byte)mcus[(int)mcuIndex].Y[pixelIndex];
                    byte g = (byte)mcus[(int)mcuIndex].Cb[pixelIndex];
                    byte b = (byte)mcus[(int)mcuIndex].Cr[pixelIndex];

                    RGBAColor<byte> color = new RGBAColor<byte>(r, g, b, 255);
                    Image.Colors[x, y] = color;
                }
            }
        }

        void DecodeDCProgressiveFirst(Header header, ScanData scan, ref MCUCollection mcus)
        {
            byte al = scan.SOISInfo.SuccessiveApproximationLow;
            int[] previousDC = new int[scan.SOISInfo.Components.Count];

            //for (int z = 0; z < mcus.Length; z++)
            //    mcus[z] = new MCU();
            BitReader bitReader = new BitReader(scan.EntropyBytes.ToArray(), true);
            int[] previousDCs = new int[3] { 0, 0, 0 };
            uint restartInterval = (uint)(header.RestartInterval * header.SOFInfo.HorizontalSamplingFactor * header.SOFInfo.VerticalSamplingFactor);
            int encodeCount = 0;

            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    if (restartInterval != 0 && (y * header.SOFInfo.MCUWidthReal + x) % restartInterval == 0)
                    {
                        previousDCs[0] = 0;
                        previousDCs[1] = 0;
                        previousDCs[2] = 0;
                        bitReader.AlignByte();
                    }

                    for (uint i = 0; i < scan.SOISInfo.NumberOfComponentsInScan; ++i)
                    {
                        // Check v and h header component
                        for (uint v = 0; v < header.SOFInfo.Components[(int)i].SubsamplingVertical; ++v)
                        {
                            for (uint h = 0; h < header.SOFInfo.Components[(int)i].SubsamplingHorizontal; ++h)
                            {
                                var component = scan.SOISInfo.Components[(int)i];
                                int sofComponent = 0;
                                for(int c=0;c<header.SOFInfo.Components.Count;c++)
                                {
                                    if (header.SOFInfo.Components[c].ComponentID == component.ComponentID)
                                        sofComponent = c;
                                }
                                var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];
                                var huffDC = header.HuffmanTables.Where(x => x.HTTableType == TableType.DC && x.HTNumber == component.DCTableDestination).FirstOrDefault();
                                var huffAC = header.HuffmanTables.Where(x => x.HTTableType == TableType.AC && x.HTNumber == component.ACTableDestination).FirstOrDefault();
                                if (!DecodeDCProgressiveFirst(
                                        header,
                                        bitReader,
                                        ref mcu,
                                        ref previousDCs[i],
                                        huffDC, al))
                                {
                                    throw new Exception("Unable to decode image data from Jpeg.");
                                }
                                else
                                {
                                    mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent] = mcu;
                                    encodeCount++;
                                }
                                //if (logOutput)
                                //{
                                //    PrintOutputScan(outWriter, currentScanNumber, (int)(x + h), (int)(y + v), scan.SOISInfo.Components[(int)i].ComponentID, mcu);
                                //}
                            }
                        }
                    }
                }
            }
        }

        bool DecodeDCProgressiveFirst(Header header, BitReader reader, ref int[] mcu, ref int previousDC, HuffmanTable dcTable, byte al)
        {
            int coeff = 0;

            var c = GetNextSymbol(reader, dcTable);
            if (c == null)
            {
                string last40 = reader.BitsString.ToString().Substring(reader.Position - 40, 40);
                Console.WriteLine("Error - Invalid DC value - Last 20 Characters: {0}", last40);
                return false;
            }
            else
            {
                //Console.WriteLine(b.Position + " - " + c);
            }

            int length = c.Symbol;

            //if (length == 0xFF)
            //{
            //    Console.WriteLine("Error - Invalid DC value");
            //    return false;
            //}
            //if (length > 11)
            //{
            //    Console.WriteLine("Error - DC coefficient length greater than 11");
            //    return false;
            //}

            coeff = (int)reader.ReadBitsToCode(length);
            //Console.WriteLine("{0}:{1}", coeff, (ulong)coeff);

            //if (coeff == -1)
            //{
            //    Console.WriteLine("Error - Invalid DC value");
            //    return false;
            //}


            if (length != 0 && coeff < (1 << (length - 1)))
            {
                coeff -= ((1 << length) - 1);
            }
            mcu[0] = (coeff + previousDC);
            previousDC = mcu[0];
            mcu[0] <<= al;

            //int newDC = (coeff + previousDC);
            //mcu[0] = newDC << al;
            //previousDC = newDC;

            //mcu[0] = (coeff + previousDC) << al;
            //previousDC = mcu[0];
            //previousDC = (coeff + previousDC);
            //mcu[0] = previousDC << al;

            return true;
        }

        void PrintOutputScan(StreamWriter output, int scanNumber, int x, int y, int component, int[] mcu)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Scan:{scanNumber}, ({x}, {y}), Component:{component}, MCU:{{");
            for(int z=0;z<64;z++)
            {
                sb.Append($"{mcu[z]}, ");
            }
            sb.AppendLine("}}");
            output.WriteLine(sb.ToString());
        }

        //StreamWriter outWriter;
        //bool logOutput;
        int currentScanNumber = 0;

        void StartOutput(string fileName)
        {
            //outWriter = new StreamWriter(fileName);
        }

        void CloseOutput()
        {
            //outWriter.Close();
        }


        void DecodeDCProgressiveSubsequent(ScanData scan, Header header, ref MCUCollection mcus)
        {
            byte al = scan.SOISInfo.SuccessiveApproximationLow;
            int[] previousDC = new int[scan.SOISInfo.Components.Count];

            //for (int z = 0; z < mcus.Length; z++)
            //    mcus[z] = new MCU();
            BitReader bitReader = new BitReader(scan.EntropyBytes.ToArray(), true);
            int[] previousDCs = new int[3] { 0, 0, 0 };
            uint restartInterval = (uint)(header.RestartInterval * header.SOFInfo.HorizontalSamplingFactor * header.SOFInfo.VerticalSamplingFactor);
            int encodeCount = 0;

            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += (uint)header.SOFInfo.VerticalSamplingFactor)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += (uint)header.SOFInfo.HorizontalSamplingFactor)
                {
                    if (restartInterval != 0 && (y * header.SOFInfo.MCUWidthReal + x) % restartInterval == 0)
                    {
                        previousDCs[0] = 0;
                        previousDCs[1] = 0;
                        previousDCs[2] = 0;
                        bitReader.AlignByte();
                    }

                    for (uint i = 0; i < scan.SOISInfo.NumberOfComponentsInScan; ++i)
                    {
                        for (uint v = 0; v < header.SOFInfo.Components[(int)i].SubsamplingVertical; ++v)
                        {
                            for (uint h = 0; h < header.SOFInfo.Components[(int)i].SubsamplingHorizontal; ++h)
                            {
                                var component = scan.SOISInfo.Components[(int)i];
                                int sofComponent = 0;
                                for (int c = 0; c < header.SOFInfo.Components.Count; c++)
                                {
                                    if (header.SOFInfo.Components[c].ComponentID == component.ComponentID)
                                        sofComponent = c;
                                }
                                var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];
                                //var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][(int)i];
                                var huffDC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.DC && x.HTNumber == component.DCTableDestination).FirstOrDefault();
                                var huffAC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.AC && x.HTNumber == component.ACTableDestination).FirstOrDefault();
                                if (!DecodeDCProgressiveSubsequent(
                                        scan,
                                        header,
                                        bitReader,
                                        ref mcu,
                                        ref previousDCs[i],
                                        huffDC, al))
                                {
                                    throw new Exception("Unable to decode image data from Jpeg.");
                                }
                                else
                                {
                                    mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent] = mcu;
                                    encodeCount++;
                                }
                                //if (logOutput)
                                //{
                                //    PrintOutputScan(outWriter, currentScanNumber, (int)(x + h), (int)(y + v), scan.SOISInfo.Components[(int)i].ComponentID, mcu);
                                //}
                            }
                        }
                    }
                }
            }
        }

        bool DecodeDCProgressiveSubsequent(ScanData scan, Header header, BitReader reader, ref int[] mcu, ref int previousDC, HuffmanTable dcTable, byte al)
        {
            int Al = scan.SOISInfo.SuccessiveApproximationLow;

            if (reader.Position == reader.BitsBool.Count)
                return false;

            //var element = reader.ReadBit();
            var element = reader.ReadBitInt();
            if (element == -1)
                return false;
            if (element != 0)
            {
                mcu[0] |= (1 << Al);
                //Console.WriteLine($"{johnny_counter++}: DC Refine Setting Block (mcu[0] = {mcu[0]}) (Al = {al})");
                //if (johnny_counter > 0 && johnny_counter % 3000 == 0)
                //{
                //    Console.WriteLine("Waiting for ReadLine");
                //    Console.ReadLine();
                //}

            }

            return true;
        }

        void DecodeACProgressiveFirst(ScanData scan, Header header, ref MCUCollection mcus)
        {
            int lengthEOBRun = 0;

            byte al = header.SOISInfo.SuccessiveApproximationLow;
            int[] previousDC = new int[header.SOISInfo.Components.Count];

            //for (int z = 0; z < mcus.Length; z++)
            //    mcus[z] = new MCU();
            BitReader bitReader = scan.EntropyReader;
            int[] previousDCs = new int[3] { 0, 0, 0 };
            uint restartInterval = (uint)(header.RestartInterval * header.SOFInfo.HorizontalSamplingFactor * header.SOFInfo.VerticalSamplingFactor);
            int encodeCount = 0;

            var c_sois = scan.SOISInfo.Components[0];
            int c_id = c_sois.ComponentID;
            var component = header.SOFInfo.Components.Where(x => x.ComponentID == c_id).FirstOrDefault();

            uint y_inc = (uint)((component.SubsamplingVertical == 2) ? 1 : 2);
            uint x_inc = (uint)((component.SubsamplingHorizontal == 2) ? 1 : 2);

            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += y_inc)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += x_inc)
                {
                    if (restartInterval != 0 && (y * header.SOFInfo.MCUWidthReal + x) % restartInterval == 0)
                    {
                        previousDCs[0] = 0;
                        previousDCs[1] = 0;
                        previousDCs[2] = 0;
                        bitReader.AlignByte();
                    }

                    for (uint i = 0; i < scan.SOISInfo.NumberOfComponentsInScan; ++i)
                    {
                        //var component = scan.SOISInfo.Components[(int)i];
                        int sofComponent = 0;
                        for (int c = 0; c < header.SOFInfo.Components.Count; c++)
                        {
                            if (header.SOFInfo.Components[c].ComponentID == component.ComponentID)
                                sofComponent = c;
                        }
                        //var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];
                        var mcu = mcus[(int)((y) * header.SOFInfo.MCUWidthReal + (x))][sofComponent];

                        //int c_id_loop = scan.SOISInfo.Components[(int)i].ComponentID;
                        //var mcu = mcus[(y) * header.SOFInfo.MCUWidthReal + (x)][c_id_loop];
                        //var mcu = mcus[y * header.SOFInfo.MCUWidth + x][(int)i];
                        //var huffDC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.DC && x.HTNumber == c_sois.DCTableDestination).FirstOrDefault();
                        var huffAC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.AC && x.HTNumber == c_sois.ACTableDestination).FirstOrDefault();
                        //lengthEOBRun = (int)DecodeACProgressiveFirst(scan, lengthEOBRun, ref mcu);
                        if (!DecodeACProgressiveFirst(
                                scan,
                                header,
                                bitReader,
                                ref mcu,
                                ref previousDCs[i],
                                huffAC, al, ref lengthEOBRun))
                        {
                            //throw new Exception("Unable to decode image data from Jpeg.");
                            return;
                        }
                        else
                        {
                            mcus[(y) * header.SOFInfo.MCUWidthReal + (x)][sofComponent] = mcu;
                            if (x_inc == 2 && y_inc == 2)
                            {
                                mcus[(y+1) * header.SOFInfo.MCUWidthReal + (x+1)][sofComponent] = mcu;
                            }
                            if (x_inc == 2)
                            {
                                mcus[(y) * header.SOFInfo.MCUWidthReal + (x + 1)][sofComponent] = mcu;
                            }
                            if (y_inc == 2)
                            {
                                mcus[(y+1) * header.SOFInfo.MCUWidthReal + (x)][sofComponent] = mcu;
                            }
                            //mcus[y * header.SOFInfo.MCUWidth + x][(int)i] = mcu;

                            encodeCount++;
                        }
                        //if (logOutput)
                        //{
                        //    PrintOutputScan(outWriter, currentScanNumber, (int)(x), (int)(y), scan.SOISInfo.Components[(int)i].ComponentID, mcu);
                        //}
                    }
                }
            }
        }

        bool DecodeACProgressiveFirst(ScanData scan, Header header, BitReader reader, ref int[] mcu, ref int previousDC, HuffmanTable acTable, byte al, ref int lengthEOBRun)
        {
            if (reader.Position >= reader.BitsBool.Count)
                return false;

            if (lengthEOBRun > 0)
            {
                lengthEOBRun -= 1;
                return true;
            }

            int Al = scan.SOISInfo.SuccessiveApproximationLow;

            int Ss = scan.SOISInfo.StartOfSpectralSelection;
            int Se = scan.SOISInfo.EndOfSpectralSelection;

            for (int idx = Ss; idx <= Se; idx++)
            {
                if (reader.Position >= reader.BitsBool.Count)
                    return false;

                var c = GetNextSymbol(reader, acTable);
                int symbol = c.Symbol;
                int lobits = (int)(symbol & 0xF);
                int highbits = (int)((symbol & 0xF0) >> 4);

                int runLength = (symbol >> 4) & 0xF;
                int size = symbol & 0xF;

                if (lobits > 0)
                {
                    idx += highbits;
                    int s = lobits;
                    int r = (int)reader.ReadBitsToCode(s);
                    int s2 = s;
                    s = HUFF_EXTEND(r, s);
                    if (idx > Se)
                        return true;
                    mcu[ZigZagMap[idx]] = (s << Al);
                    //Console.WriteLine($"{johnny_counter++}: AC First Setting Block (k = {idx}) (s = {s}, hi = {r}, lo = {s2}) (s << Al = {s << Al})");
                    //if (johnny_counter > 0 && johnny_counter % 3000 == 0)
                    //{
                    //    Console.WriteLine("Waiting for ReadLine");
                    //    Console.ReadLine();
                    //}
                    //mcu[idx] = (s << Al);

                    //s = HUFF_EXTEND(r, s);
                    ///* Scale and output coefficient in natural (dezigzagged) order */
                    //(*block)[natural_order[k]] = (JCOEF)(s << Al);

                    //    If LOBITS<> 0 Then
                    //        Begin
                    //            EXTRABITS = ReadRawBits(LOBITS)
                    //            II = II + HIGHBITS
                    //            COEFFICIENTS[II] = Extend(EXTRABITS, LOBITS) LeftShift SUCCESSIVEAPPROXIMATION
                    //            II = II + 1
                    //        End
                    //    Else
                }
                else
                {
                    // if (r != 15)
                    // {   /* EOBr, run length is 2^r + appended bits */
                    //     if (r)
                    //     {       /* EOBr, r > 0 */
                    //         EOBRUN = 1 << r;
                    //         CHECK_BIT_BUFFER(br_state, r, return FALSE);
                    //         r = GET_BITS(r);
                    //         EOBRUN += r;
                    //         EOBRUN--;       /* this band is processed at this moment */
                    //     }
                    //  break;		/* force end-of-band */
                    //}
                    // k += 15;		

                    if (highbits != 15)
                    {
                        if (highbits > 0)
                        {
                            long tmp = Convert.ToInt64(reader.ReadBitsToCode(highbits));
                            //Console.WriteLine($"{johnny_counter++}: Get Bits (Lower) {tmp} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
                            long eobrun = (1 << highbits) + tmp - 1;
                            lengthEOBRun = (int)eobrun;
                            return true;
                            //return eobrun;
                        }
                        return true;
                    }
                    idx += 15;
                    //        If HIGHBITS = F16 Then
                    //            II = II + 16 // Run of 16 Zeros
                    //        Else If HIGHBITS = 0 Then
                    //            II = SSE + 1
                    //        Else
                    //            // We subtract one to account for ending the current block.
                    //            EOBRUN = (1 LeftShift HIGHBITS) +ReadRawBits(HIGHBITS) - 1
                    //            Return
                }

                //if (size == 0)
                //{
                //    //if (runLength == 15)
                //    //{
                //    //    idx += 16;
                //    //}
                //    //else
                //    //{
                //    //    int exponent = (int)Math.Pow(2, runLength);
                //    //    lengthEOBRun = (int)reader.ReadBitsToCode(runLength) + exponent - 1;
                //    //    return true;
                //    //}
                //    if (highbits != 15)
                //    {
                //        if (highbits > 0)
                //        {
                //            long tmp = Convert.ToInt64(reader.ReadBitsToCode(highbits));
                //            Console.WriteLine($"{johnny_counter++}: Get Bits (Lower) {tmp} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
                //            long eobrun = (1 << highbits) + tmp - 1;
                //            lengthEOBRun = (int)eobrun;
                //            return true;
                //            //return (int)eobrun;
                //        }
                //        break;
                //    }
                //    idx += 15;
                //}
                //else
                //{
                //    //idx += runLength;
                //    //int bits = (int)reader.ReadBitsToCode(size);
                //    //mcu[ZigZagMap[idx]] = bits << al;
                //    //idx++;

                //    idx += highbits;
                //    long extrabits = Convert.ToInt64(reader.ReadBitsToCode(lobits));
                //    Console.WriteLine($"{johnny_counter++}: Get Bits (Upper) {extrabits} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
                //    int s = HUFF_EXTEND(highbits, lobits);
                //    mcu[ZigZagMap[idx]] = (s << Al);
                //    //idx++;
                //}
            }
            return true;
        }

        void DecodeACProgressiveSubsequent(ScanData scan, Header header, ref MCUCollection mcus)
        {
            int lengthEOBRun = 0;

            byte al = header.SOISInfo.SuccessiveApproximationLow;
            int[] previousDC = new int[header.SOISInfo.Components.Count];

            //for (int z = 0; z < mcus.Length; z++)
            //    mcus[z] = new MCU();
            BitReader bitReader = scan.EntropyReader;
            int[] previousDCs = new int[3] { 0, 0, 0 };
            uint restartInterval = (uint)(header.RestartInterval * header.SOFInfo.HorizontalSamplingFactor * header.SOFInfo.VerticalSamplingFactor);
            int encodeCount = 0;

            var c_sois = scan.SOISInfo.Components[0];
            int c_id = c_sois.ComponentID;
            var component = header.SOFInfo.Components.Where(x => x.ComponentID == c_id).FirstOrDefault();

            uint y_inc = (uint)((component.SubsamplingVertical == 2) ? 1 : 2);
            uint x_inc = (uint)((component.SubsamplingHorizontal == 2) ? 1 : 2);

            for (uint y = 0; y < header.SOFInfo.MCUHeight; y += y_inc)
            {
                for (uint x = 0; x < header.SOFInfo.MCUWidth; x += x_inc)
                {
                    if (restartInterval != 0 && (y * header.SOFInfo.MCUWidthReal + x) % restartInterval == 0)
                    {
                        previousDCs[0] = 0;
                        previousDCs[1] = 0;
                        previousDCs[2] = 0;
                        bitReader.AlignByte();
                    }

                    for (uint i = 0; i < scan.SOISInfo.NumberOfComponentsInScan; ++i)
                    {
                        //var component = scan.SOISInfo.Components[(int)i];
                        int sofComponent = 0;
                        for (int c = 0; c < header.SOFInfo.Components.Count; c++)
                        {
                            if (header.SOFInfo.Components[c].ComponentID == component.ComponentID)
                                sofComponent = c;
                        }
                        //var mcu = mcus[(int)((y + v) * header.SOFInfo.MCUWidthReal + (x + h))][sofComponent];
                        var mcu = mcus[(int)((y) * header.SOFInfo.MCUWidthReal + (x))][sofComponent];
                        //int c_id_loop = scan.SOISInfo.Components[(int)i].ComponentID;
                        //var mcu = mcus[(y) * header.SOFInfo.MCUWidthReal + (x)][c_id_loop];
                        //var mcu = mcus[y * header.SOFInfo.MCUWidth + x][(int)i];
                        //var huffDC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.DC && x.HTNumber == c_sois.DCTableDestination).FirstOrDefault();
                        var huffAC = scan.HuffmanTables.Where(x => x.HTTableType == TableType.AC && x.HTNumber == c_sois.ACTableDestination).FirstOrDefault();
                        //lengthEOBRun = (int)DecodeACProgressiveFirst(scan, lengthEOBRun, ref mcu);
                        if (!DecodeACProgressiveSubsequent(
                                scan,
                                header,
                                bitReader,
                                ref mcu,
                                ref previousDCs[i],
                                huffAC, al, ref lengthEOBRun))
                        {
                            //throw new Exception("Unable to decode image data from Jpeg.");
                            return;
                        }
                        else
                        {
                            //mcus[(y) * header.SOFInfo.MCUWidthReal + (x)][(int)i] = mcu;
                            //mcus[y * header.SOFInfo.MCUWidth + x][(int)i] = mcu;
                            mcus[(y) * header.SOFInfo.MCUWidthReal + (x)][sofComponent] = mcu;
                            if (x_inc == 2 && y_inc == 2)
                            {
                                mcus[(y + 1) * header.SOFInfo.MCUWidthReal + (x + 1)][sofComponent] = mcu;
                            }
                            if (x_inc == 2)
                            {
                                mcus[(y) * header.SOFInfo.MCUWidthReal + (x + 1)][sofComponent] = mcu;
                            }
                            if (y_inc == 2)
                            {
                                mcus[(y + 1) * header.SOFInfo.MCUWidthReal + (x)][sofComponent] = mcu;
                            }

                            encodeCount++;
                        }
                        //if (logOutput)
                        //{
                        //    PrintOutputScan(outWriter, currentScanNumber, (int)(x), (int)(y), scan.SOISInfo.Components[(int)i].ComponentID, mcu);
                        //}
                    }
                }
            }
        }

        bool DecodeACProgressiveSubsequent(ScanData scan, Header header, BitReader reader, ref int[] mcu, ref int previousDC, HuffmanTable acTable, byte al, ref int lengthEOBRun)
        {
            //        METHODDEF(boolean)
            //decode_mcu_AC_refine (j_decompress_ptr cinfo, JBLOCKROW *MCU_data)
            //{
            //  huff_entropy_ptr entropy = (huff_entropy_ptr) cinfo->entropy;
            //  register int s, k, r;
            //  unsigned int EOBRUN;
            //  int Se;
            //  JCOEF p1, m1;
            //  const int * natural_order;
            //  JBLOCKROW block;
            //  JCOEFPTR thiscoef;
            //  BITREAD_STATE_VARS;
            //  d_derived_tbl * tbl;
            int s, k, r;
            int num_newnz;
            int [] newnz_pos = new int[64];
            int thiscoef;

            if (reader.Position >= reader.BitsBool.Count)
                return false;

            //if (lengthEOBRun > 0)
            //{
            //    lengthEOBRun -= 1;
            //    return true;
            //}

            int Al = scan.SOISInfo.SuccessiveApproximationLow;
            int idx = scan.SOISInfo.StartOfSpectralSelection;
            int idx2 = scan.SOISInfo.EndOfSpectralSelection;
            int Ss = scan.SOISInfo.StartOfSpectralSelection;
            int Se = scan.SOISInfo.EndOfSpectralSelection;
            int p1, m1;

            /* If we've run out of data, don't modify the MCU.
             */
            //if (!entropy->insufficient_data)
            if (!scan.EntropyReader.IsEOF)
            {

                //Se = cinfo->Se;
                p1 = 1 << Al;    /* 1 in the bit position being coded */
                m1 = -p1;           /* -1 in the bit position being coded */
                //natural_order = cinfo->natural_order;

                ///* Load up working state */
                //BITREAD_LOAD_STATE(cinfo, entropy->bitstate);
                //EOBRUN = entropy->saved.EOBRUN; /* only part of saved state we need */

                ///* There is always only one block per MCU */
                //block = MCU_data[0];
                //tbl = entropy->ac_derived_tbl;

                ///* If we are forced to suspend, we must undo the assignments to any newly
                // * nonzero coefficients in the block, because otherwise we'd get confused
                // * next time about which coefficients were already nonzero.
                // * But we need not undo addition of bits to already-nonzero coefficients;
                // * instead, we can test the current bit to see if we already did it.
                // */
                num_newnz = 0;

                /* initialize coefficient loop counter to start of band */
                k = Ss;

                if (lengthEOBRun == 0)
                {
                    do
                    {
                        //HUFF_DECODE(s, br_state, tbl, goto undoit, label3);
                        var c = GetNextSymbol(reader, acTable);
                        if (c == null)
                            return false;
                        s = c.Symbol;
                        r = s >> 4;
                        s &= 15;
                        //Console.WriteLine($"{johnny_counter++}: Data Read (LEOB0): S:{s}, R:{r}");
                        //if (johnny_counter > 0 && johnny_counter % 3000 == 0 || johnny_counter == 26)
                        //{
                        //    Console.WriteLine("Waiting for ReadLine...");
                        //    Console.ReadLine();
                        //}
                        if (s != 0)
                        {
                            if (s != 1)     /* size of new coef should always be 1 */
                                throw new Exception("Invalid Huffman Code");
                            //WARNMS(cinfo, JWRN_HUFF_BAD_CODE);
                            if (reader.IsEOF)
                                goto undoit;
                            //CHECK_BIT_BUFFER(br_state, 1, goto undoit);
                            //if (GET_BITS(1))
                            //if (reader.ReadBit().ValueBoolean)
                            var c2 = reader.ReadBitInt();
                            if (c2 != -1 && c2 != 0)
                                s = p1;     /* newly nonzero coef is positive */
                            else
                                s = m1;     /* newly nonzero coef is negative */
                        }
                        else
                        {
                            if (r != 15)
                            {
                                lengthEOBRun = 1 << r;    /* EOBr, run length is 2^r + appended bits */
                                if (r != 0)
                                {
                                    //CHECK_BIT_BUFFER(br_state, r, goto undoit);
                                    if (reader.IsEOF)
                                        goto undoit;
                                    //r = GET_BITS(r);
                                    r = (int)reader.ReadBitsToCode(r);
                                    //EOBRUN += r;
                                    lengthEOBRun += r;
                                }
                                break;      /* rest of block is handled by EOB logic */
                            }
                            /* note s = 0 for processing ZRL */
                        }
                        /* Advance over already-nonzero coefs and r still-zero coefs,
                         * appending correction bits to the nonzeroes.  A correction bit is 1
                         * if the absolute value of the coefficient must be increased.
                         */
                        do
                        {
                            //thiscoef = *block + natural_order[k];
                            thiscoef = mcu[ZigZagMap[k]];
                            if (thiscoef != 0)
                            {
                                //CHECK_BIT_BUFFER(br_state, 1, goto undoit);
                                //if (GET_BITS(1))
                                if (reader.IsEOF)
                                    goto undoit;

                                var c2 = reader.ReadBitInt();
                                if (c2 >= 1)
                                {
                                    if ((thiscoef & p1) == 0)
                                    { /* do nothing if already set it */
                                        if (thiscoef >= 0)
                                        {
                                            thiscoef += p1;
                                            mcu[ZigZagMap[k]] = thiscoef;
                                        }
                                        else
                                        {
                                            thiscoef += m1;
                                            mcu[ZigZagMap[k]] = thiscoef;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (--r < 0)
                                    break;      /* reached target zero coefficient */
                            }
                            k++;
                        } while (k <= Se);
                        if (s != 0)
                        {
                            // Code that I had to add in...
                            if (k == 64)
                                return false;

                            int pos = ZigZagMap[k];
                            /* Output newly nonzero coefficient */
                            //(*block)[pos] = (JCOEF)s;
                            mcu[pos] = s;
                            //Console.WriteLine("{0} - (s:{1}, r:{2}, k:{3})", johnny_counter++, s, r, k);
                            //if (johnny_counter > 0 && johnny_counter % 3000 == 0 || johnny_counter == 26)
                            //{
                            //    Console.WriteLine("Waiting for ReadLine...");
                            //    Console.ReadLine();
                            //}

                            /* Remember its position in case we have to suspend */
                            newnz_pos[num_newnz++] = pos;
                        }
                        k++;
                    } while (k <= Se);
                }

                if (lengthEOBRun > 0)
                {
                    do
                    {
                        //thiscoef = *block + natural_order[k];
                        thiscoef = mcu[ZigZagMap[k]];
                        if (thiscoef != 0)
                        {
                            //CHECK_BIT_BUFFER(br_state, 1, goto undoit);
                            if (reader.IsEOF)
                                goto undoit;
                            var c2 = reader.ReadBitInt();
                            if (c2 > 0)
                            {
                                if ((thiscoef & p1) == 0)
                                { /* do nothing if already changed it */
                                    if (thiscoef >= 0)
                                    {
                                        thiscoef += p1;
                                        mcu[ZigZagMap[k]] = thiscoef;
                                    }
                                    else
                                    {
                                        thiscoef += m1;
                                        mcu[ZigZagMap[k]] = thiscoef;
                                    }
                                }
                            }
                        }
                        k++;
                    } while (k <= Se);

                    lengthEOBRun--;
                }

                //if (EOBRUN)
                //{
                //    /* Scan any remaining coefficient positions after the end-of-band
                //     * (the last newly nonzero coefficient, if any).  Append a correction
                //     * bit to each already-nonzero coefficient.  A correction bit is 1
                //     * if the absolute value of the coefficient must be increased.
                //     */
                //    do
                //    {
                //        thiscoef = *block + natural_order[k];
                //        if (*thiscoef)
                //        {
                //            CHECK_BIT_BUFFER(br_state, 1, goto undoit);
                //            if (GET_BITS(1))
                //            {
                //                if ((*thiscoef & p1) == 0)
                //                { /* do nothing if already changed it */
                //                    if (*thiscoef >= 0)
                //                        *thiscoef += p1;
                //                    else
                //                        *thiscoef += m1;
                //                }
                //            }
                //        }
                //        k++;
                //    } while (k <= Se);
                //    /* Count one block completed in EOB run */
                //    EOBRUN--;
                //}

                ///* Completed MCU, so update state */
                //BITREAD_SAVE_STATE(cinfo, entropy->bitstate);
                //entropy->saved.EOBRUN = EOBRUN; /* only part of saved state we need */
                //}

                ///* Account for restart interval if using restarts */
                //if (header.RestartInterval)
                //    entropy->restarts_to_go--;
            }
            return true;

            undoit:
            /* Re-zero any output coefficients that we made newly nonzero */
            while (num_newnz > 0)
                newnz_pos[--num_newnz] = 0;

            return false;
        }

        MCUCollection mainMcus;

        void ProcessAllEncodedData(byte[] data, Header header, bool useDC = true, bool useAC = true, bool isFinal = false)
        {
            //byte[] data2 = File.ReadAllBytes(@"c:\users\johnr\documents\test1.bin");
            //for (int z = 0; z< data2.Length;z++)
            //{
            //    if (data2[z] != data[z])
            //    {
            //        Console.WriteLine("*** ERROR *** - Data:{0} - Data2:{1}", data[z], data2[z]);
            //    }
            //}

            //ResetMCUs(header);
            mainMcus = new MCUCollection(header.SOFInfo.MCUWidthReal, header.SOFInfo.MCUHeightReal, header.SOFInfo.MCUWidth, header.SOFInfo.MCUWidth);

            if (header.SOFInfo.CompressionType == JPEGCompressionType.HuffmanCodingProgressiveDCT)
            {
                for (int z = 0; z < Scans.Count; z++)
                {
                    currentScanNumber++;
                    if (Scans[z].ScanTypeInfo == ScanData.ScanType.FirstDC)
                    {
                        DecodeDCProgressiveFirst(header, Scans[z], ref mainMcus);
                    }
                    else if (Scans[z].ScanTypeInfo == ScanData.ScanType.FirstAC)
                    {
                        //PrintHexScanData(Scans[z], z);
                        DecodeACProgressiveFirst(Scans[z], header, ref mainMcus);
                    }
                    else if (Scans[z].ScanTypeInfo == ScanData.ScanType.RefineDC)
                    {
                        if (Scans[z].HuffmanTables.Count == 0)
                        {
                            Scans[z].HuffmanTables = Scans[0].HuffmanTables;
                        }
                        DecodeDCProgressiveSubsequent(Scans[z], header, ref mainMcus);
                    }
                    else if (Scans[z].ScanTypeInfo == ScanData.ScanType.RefineAC)
                    {
                        DecodeACProgressiveSubsequent(Scans[z], header, ref mainMcus);
                    }
                    //DecodeHuffmanData(data, header, ref mainMcus, useDC, useAC);
                }
                MCUCollection imageMcus = mainMcus.Clone();
                Dequantize(header, ref imageMcus);
                InverseDCT(header, ref imageMcus);
                YCbCrToRGB(header, ref imageMcus);
                CopyMCUsToImage(header, imageMcus);

                WriteBMP(header, imageMcus, $@"c:\users\johnr\Documents\test.bmp");
                //WriteBMPFromImage(header, $@"c:\users\johnr\Documents\test-{z}a.bmp");
            }
            else if (header.SOFInfo.CompressionType == JPEGCompressionType.HuffmanCodingBaselineDCT)
            {
                DecodeHuffmanData(data, header, ref mainMcus, useDC, useAC);
                MCUCollection imageMcus = mainMcus.Clone();
                Dequantize(header, ref imageMcus);
                InverseDCT(header, ref imageMcus);
                YCbCrToRGB(header, ref imageMcus);
                CopyMCUsToImage(header, imageMcus);

                WriteBMP(header, imageMcus, $@"c:\users\johnr\Documents\test.bmp");
                //WriteBMPFromImage(header, $@"c:\users\johnr\Documents\test2.bmp");
            }
            //}
        }

        // helper function to write a 4-byte integer in little-endian
        void PutInt(Stream outFile, uint v) 
        {
            outFile.WriteByte((byte)((v >>  0) & 0xFF));
            outFile.WriteByte((byte)((v >>  8) & 0xFF));
            outFile.WriteByte((byte)((v >> 16) & 0xFF));
            outFile.WriteByte((byte)((v >> 24) & 0xFF));
        }

        // helper function to write a 2-byte short integer in little-endian
        void PutShort(Stream outFile, uint v) 
        {
            outFile.WriteByte((byte)((v >>  0) & 0xFF));
            outFile.WriteByte((byte)((v >>  8) & 0xFF));
        }

        // write all the pixels in the MCUs to a BMP file
        void WriteBMPFromImage(Header header, string filename)
        {
            // open file
            //std::ofstream outFile = std::ofstream(filename, std::ios::out | std::ios::binary);
            Stream outFile = new FileStream(filename, FileMode.Create);
            if (outFile == null)
            {
                Console.WriteLine("Error - Error opening output file");
                return;
            }

            uint paddingSize = (uint)(header.SOFInfo.ImageWidth % 4);
            uint size = (uint)(14 + 12 + header.SOFInfo.ImageHeight * header.SOFInfo.ImageWidth * 3 + paddingSize * header.SOFInfo.ImageHeight);

            outFile.WriteByte((byte)'B');
            outFile.WriteByte((byte)'M');
            PutInt(outFile, size);
            PutInt(outFile, 0);
            PutInt(outFile, 0x1A);
            PutInt(outFile, 12);
            PutShort(outFile, (uint)header.SOFInfo.ImageWidth);
            PutShort(outFile, (uint)header.SOFInfo.ImageHeight);
            PutShort(outFile, 1);
            PutShort(outFile, 24);

            for (uint y = (uint)(header.SOFInfo.ImageHeight - 1); y < header.SOFInfo.ImageHeight; --y)
            {
                uint mcuRow = y / 8;
                uint pixelRow = y % 8;
                for (uint x = 0; x < header.SOFInfo.ImageWidth; ++x)
                {
                    uint mcuColumn = x / 8;
                    uint pixelColumn = x % 8;
                    uint mcuIndex = (uint)(mcuRow * header.SOFInfo.MCUWidthReal + mcuColumn);
                    uint pixelIndex = pixelRow * 8 + pixelColumn;

                    byte r = Image.Colors[x, y].R;
                    byte g = Image.Colors[x, y].G;
                    byte b = Image.Colors[x, y].B;

                    //outFile.WriteByte((byte)mcus[mcuIndex].Cr[pixelIndex]);
                    //outFile.WriteByte((byte)mcus[mcuIndex].Cb[pixelIndex]);
                    //outFile.WriteByte((byte)mcus[mcuIndex].Y[pixelIndex]);
                    outFile.WriteByte(b);
                    outFile.WriteByte(g);
                    outFile.WriteByte(r);
                }
                for (uint i = 0; i < paddingSize; ++i)
                {
                    outFile.WriteByte(0);
                }
            }

            outFile.Flush();
            outFile.Close();
        }

        // write all the pixels in the MCUs to a BMP file
        void WriteBMP(Header header, MCUCollection mcus, string filename) {
            // open file
            //std::ofstream outFile = std::ofstream(filename, std::ios::out | std::ios::binary);
            Stream outFile = new FileStream(filename, FileMode.Create);
            if (outFile == null) {
                Console.WriteLine("Error - Error opening output file");
                return;
            }

            uint paddingSize = (uint)(header.SOFInfo.ImageWidth % 4);
            uint size = (uint)(14 + 12 + header.SOFInfo.ImageHeight * header.SOFInfo.ImageWidth * 3 + paddingSize * header.SOFInfo.ImageHeight);

            outFile.WriteByte((byte)'B');
            outFile.WriteByte((byte)'M');
            PutInt(outFile, size);
            PutInt(outFile, 0);
            PutInt(outFile, 0x1A);
            PutInt(outFile, 12);
            PutShort(outFile, (uint)header.SOFInfo.ImageWidth);
            PutShort(outFile, (uint)header.SOFInfo.ImageHeight);
            PutShort(outFile, 1);
            PutShort(outFile, 24);

            for (uint y = (uint)(header.SOFInfo.ImageHeight - 1); y < header.SOFInfo.ImageHeight; --y) {
                uint mcuRow = y / 8;
                uint pixelRow = y % 8;
                for (uint x = 0; x < header.SOFInfo.ImageWidth; ++x) {
                    uint mcuColumn = x / 8;
                    uint pixelColumn = x % 8;
                    uint mcuIndex = (uint)(mcuRow * header.SOFInfo.MCUWidthReal + mcuColumn);
                    uint pixelIndex = pixelRow * 8 + pixelColumn;
                    outFile.WriteByte((byte)mcus[mcuIndex].Cr[pixelIndex]);
                    outFile.WriteByte((byte)mcus[mcuIndex].Cb[pixelIndex]);
                    outFile.WriteByte((byte)mcus[mcuIndex].Y[pixelIndex]);
                }
                for (uint i = 0; i < paddingSize; ++i) {
                    outFile.WriteByte(0);
                }
            }

            outFile.Flush();
            outFile.Close();
        }

        void ReadImageScanStart(Header header, BinaryReader reader)
        {
            short length = (short)reader.ReadBytes(2).GetUIntBE(0);
            int readCounter = 2;
            byte numberOfComponentsInScan = reader.ReadByte();
            readCounter++;
            //Console.WriteLine("# of Components in Scan: {0}", numberOfComponentsInScan);
            header.SOISInfo = new StartOfImageScanInformation()
            {
                NumberOfComponentsInScan = numberOfComponentsInScan,
                Components = new List<StartOfImageScanInformation.Component>()
            };
            for (int z = 0; z < numberOfComponentsInScan; z++)
            {
                byte componentId = reader.ReadByte();
                readCounter++;
                byte huffmanTableToUse = reader.ReadByte();
                readCounter++;
                byte huffmanHighDCTableDestination = (byte)((huffmanTableToUse >> 4) & 0xF);
                byte huffmanLowACTableDestination = (byte)(huffmanTableToUse & 0xF);
                //Console.WriteLine("Component #:{0} - ID:{1} - Huffman Table: AC Dest:{2} DC Dest:{3}", z, componentId, huffmanLowACTableDestination, huffmanHighDCTableDestination);
                header.SOISInfo.Components.Add(new StartOfImageScanInformation.Component()
                {
                    ComponentID = componentId,
                    DCTableDestination = huffmanHighDCTableDestination,
                    ACTableDestination = huffmanLowACTableDestination
                });
            }
            if (header.SOFInfo.ZeroBased)
            {
                for (int z = 0; z < numberOfComponentsInScan; z++)
                {
                    header.SOISInfo.Components[z].ComponentID++;
                }
            }

            byte startOfSpectralSelection = reader.ReadByte();
            byte endOfSpectralSelection = reader.ReadByte();
            byte successiveApproximation = reader.ReadByte();
            readCounter+=3;
            byte successiveApproximationHigh = (byte)((successiveApproximation >> 4) & 0xF);
            byte successiveApproximationLow = (byte)(successiveApproximation & 0xF);
            //Console.WriteLine("Spectral Selection Start: {0} - End: {1}, Successive Approximation High: {2} - Low: {3}", startOfSpectralSelection, endOfSpectralSelection, successiveApproximationHigh, successiveApproximationLow);
            header.SOISInfo.StartOfSpectralSelection = startOfSpectralSelection;
            header.SOISInfo.EndOfSpectralSelection = endOfSpectralSelection;
            header.SOISInfo.SuccessiveApproximationHigh = successiveApproximationHigh;
            header.SOISInfo.SuccessiveApproximationLow = successiveApproximationLow;

            //if (readCounter != length)
            //{
            //    Console.WriteLine("Expected {0} bytes but got {1}", length, readCounter);
            //}
        }

        void ResetMCUs(Header header)
        {
            mainMcus = new MCUCollection(header.SOFInfo.MCUWidthReal, header.SOFInfo.MCUHeightReal, header.SOFInfo.MCUWidth, header.SOFInfo.MCUHeight);
            for (int z = 0; z < mainMcus.Length; z++)
                mainMcus[z] = new MCU();
        }

        public class ScanData
        {
            public enum ScanType
            {
                FirstDC,
                RefineDC,
                FirstAC,
                RefineAC
            }

            public StartOfImageScanInformation SOISInfo { get; set; }
            public List<HuffmanTable> HuffmanTables { get; set; }
            public List<byte> EntropyBytes { get; set; }
            public BitReader EntropyReader { get; set; }
            public ScanType ScanTypeInfo { get {
                    if (SOISInfo.SuccessiveApproximationHigh == 0)
                    {
                        if (SOISInfo.StartOfSpectralSelection == 0)
                        {
                            return ScanType.FirstDC;
                        }
                        else
                        {
                            return ScanType.FirstAC;
                        }
                    }
                    else
                    {
                        if (SOISInfo.StartOfSpectralSelection == 0)
                        {
                            return ScanType.RefineDC;
                        }
                        else
                        {
                            return ScanType.RefineAC;
                        }
                    }
                }
            }
            public int ScanID { get; set; }

            public override string ToString()
            {
                return $"[ScanData:{ScanTypeInfo}, Ss:{SOISInfo.StartOfSpectralSelection}, Se:{SOISInfo.EndOfSpectralSelection}, Ah:{SOISInfo.SuccessiveApproximationHigh}, Al:{SOISInfo.SuccessiveApproximationLow}, EntropyBytes:{EntropyBytes.Count}, HuffmanTables:{HuffmanTables.Count}, Components:{SOISInfo.Components.Count}, Bits:{EntropyReader.BitsBool.Count}]";
            }
        }

        void PrintHexScanData(ScanData scan, int scanNumber)
        {
            bool done = false;
            int index = 0;
            StringBuilder sb = new StringBuilder();
            Console.WriteLine($"Scan #:{scanNumber}");
            Console.WriteLine(scan.ToString());
            while (!done)
            {
                for (int z = 0; z < 16; z++)
                {
                    byte data = scan.EntropyBytes[index];
                    sb.Append($"{data:X2} ");
                    index++;
                    if (index == scan.EntropyBytes.Count)
                    {
                        done = true;
                        break;
                    }
                }
                sb.AppendLine();
            }
            Console.Write(sb.ToString());
        }

        //Function Extend (ADDITIONAL, MAGNITUDE)
        //Begin
        //    vt = 1 LeftShift (MAGNITUDE - 1)
        //    If ADDITIONAL < vt Then
        //        return ADDITIONAL + (-1 LeftShift MAGNITUDE) + 1
        //    Else
        //        return ADDITIONAL
        //End
        //int johnny_counter = 0;
        int scanIdCounter = 1;

        /* bmask[n] is mask for n rightmost bits */
        int[] bmask = { 0, 0x0001, 0x0003, 0x0007, 0x000F, 0x001F, 0x003F, 0x007F, 0x00FF, 0x01FF, 0x03FF, 0x07FF, 0x0FFF, 0x1FFF, 0x3FFF, 0x7FFF };

        int HUFF_EXTEND(int x, int s)
        {
            x = (x <= bmask[s - 1]) ? (x - bmask[s]) : (x);
            return x;
        }

        //long DecodeACProgressiveFirst(ScanData scan, long length_EOB, ref int[] mcu)
        //{
        //    int Ss = scan.SOISInfo.StartOfSpectralSelection;
        //    int Se = scan.SOISInfo.EndOfSpectralSelection;
        //    int Ah = scan.SOISInfo.SuccessiveApproximationHigh;
        //    int Al = scan.SOISInfo.SuccessiveApproximationLow;
        //    HuffmanTable acTable = scan.HuffmanTables[0];
        //    BitReader b = scan.EntropyReader;

        //    if (length_EOB > 0)
        //    {
        //        Console.WriteLine($"{johnny_counter++}: EOB Decrement Run {length_EOB} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
        //        length_EOB--;
        //        return length_EOB;
        //    }

        //    for (int idx = Ss; idx <= Se; idx++)
        //    {
        //        var c = GetNextSymbol(b, acTable);
        //        int symbol = c.Symbol;
        //        int lobits = (int)(symbol & 0xF);
        //        int highbits = (int)((symbol & 0xF0) >> 4);
        //        Console.WriteLine($"{johnny_counter++}: Get Symbol R:{highbits}, S:{lobits} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");

        //        if (lobits > 0)
        //        {
        //            idx += highbits;
        //            long extrabits = Convert.ToInt64(b.ReadBitsToCode(lobits));
        //            Console.WriteLine($"{johnny_counter++}: Get Bits (Upper) {extrabits} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
        //            int s = HUFF_EXTEND(highbits, lobits);
        //            mcu[ZigZagMap[idx]] = (s << Al);
        //            //mcu[idx] = (s << Al);

        //            //s = HUFF_EXTEND(r, s);
        //            ///* Scale and output coefficient in natural (dezigzagged) order */
        //            //(*block)[natural_order[k]] = (JCOEF)(s << Al);

        //            //    If LOBITS<> 0 Then
        //            //        Begin
        //            //            EXTRABITS = ReadRawBits(LOBITS)
        //            //            II = II + HIGHBITS
        //            //            COEFFICIENTS[II] = Extend(EXTRABITS, LOBITS) LeftShift SUCCESSIVEAPPROXIMATION
        //            //            II = II + 1
        //            //        End
        //            //    Else
        //        }
        //        else
        //        {
        //            // if (r != 15)
        //            // {   /* EOBr, run length is 2^r + appended bits */
        //            //     if (r)
        //            //     {       /* EOBr, r > 0 */
        //            //         EOBRUN = 1 << r;
        //            //         CHECK_BIT_BUFFER(br_state, r, return FALSE);
        //            //         r = GET_BITS(r);
        //            //         EOBRUN += r;
        //            //         EOBRUN--;       /* this band is processed at this moment */
        //            //     }
        //            //  break;		/* force end-of-band */
        //            //}
        //            // k += 15;		

        //            if (highbits != 15)
        //            {
        //                if (highbits > 0)
        //                {
        //                    long tmp = Convert.ToInt64(b.ReadBitsToCode(highbits));
        //                    Console.WriteLine($"{johnny_counter++}: Get Bits (Lower) {tmp} {scan.EntropyReader.Position}/{scan.EntropyReader.Length}");
        //                    long eobrun = (1 << highbits) + tmp - 1;
        //                    return eobrun;
        //                }
        //                break;
        //            }
        //            idx += 15;
        //            //        If HIGHBITS = F16 Then
        //            //            II = II + 16 // Run of 16 Zeros
        //            //        Else If HIGHBITS = 0 Then
        //            //            II = SSE + 1
        //            //        Else
        //            //            // We subtract one to account for ending the current block.
        //            //            EOBRUN = (1 LeftShift HIGHBITS) +ReadRawBits(HIGHBITS) - 1
        //            //            Return
        //        }
        //    }
        //    return 0;
        //}

        //bool CheckACScan(Header header, ScanData scan, ref MCUCollection mcus)
        //{
        //    if (scan.ScanTypeInfo == ScanData.ScanType.FirstDC || scan.ScanTypeInfo == ScanData.ScanType.RefineDC)
        //        return false;

        //    int blockNumber = 0;
        //    long length_EOB = 0;
        //    johnny_counter = 0;
        //    int mcuNumber = 0;
        //    int mcuX = 0; 
        //    int mcuY = 0;
        //    int componentIndex = header.SOFInfo.Components.Where(x => x.ComponentID == scan.SOISInfo.Components[0].ComponentID).FirstOrDefault().ComponentID - 1;
        //    while(!scan.EntropyReader.IsEOF || length_EOB > 0)
        //    {
        //        var mcu = mcus[mcuX, mcuY][componentIndex];
        //        mcuX++;
        //        if (mcuX >= header.SOFInfo.MCUWidth)
        //        {
        //            mcuY++;
        //            mcuX = 0;
        //        }
        //        length_EOB = DecodeACProgressiveFirst(scan, length_EOB, ref mcu);
        //        mcuNumber++;
        //        blockNumber++;
        //    }
        //    return true;
        //}

        public Dictionary<int, int> ComponentIDMap = new Dictionary<int, int>();

        public List<ScanData> Scans { get; set; } = new List<ScanData>();

        public void DecodeJpeg(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(ms);
            Header header = new Header();
            JpegMain(ref header, ref reader);
            reader.Close();
            ms.Close();
        }

        public void DecodeJpeg(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream);
            Header header = new Header();
            JpegMain(ref header, ref reader);
            reader.Close();
            inputStream.Close();
        }

        public void DecodeJpeg(string fileName)
        {
            FileStream fileStream = File.OpenRead(fileName);
            BinaryReader reader = new BinaryReader(fileStream);
            Header header = new Header();
            JpegMain(ref header, ref reader);
            reader.Close();
            fileStream.Close();
        }

        public void JpegMain(ref Header header, ref BinaryReader reader)
        {
            bool isEOF = false;
            List<byte> entropyBytes = new List<byte>();
            while (!isEOF)
            {
                byte[] headerCheck = reader.ReadBytes(2);
                if (headerCheck.Length == 0)
                {
                    isEOF = true;
                    continue;
                }
                if (headerCheck[0] == 0xFF)
                {
                    switch ((JPEGCommandType)headerCheck[1])
                    {
                        case JPEGCommandType.StartOfImage:
                            {
                                //Console.WriteLine("Start of Image");
                                scanIdCounter = 1;
                            }
                            break;
                        case JPEGCommandType.EndOfImage:
                            {
                                //Console.WriteLine("End of Image");
                                isEOF = true;
                                ProcessAllEncodedData(entropyBytes.ToArray(), header, isFinal: true);
                            }
                            break;
                        case JPEGCommandType.StartOfImageScan:
                            {
                                // Start of Image Scan
                                //Console.WriteLine("Start of Image Scan");
                                ReadImageScanStart(header, reader);
                                ScanData imageScan = new ScanData();
                                imageScan.SOISInfo = header.SOISInfo;
                                imageScan.HuffmanTables = new List<HuffmanTable>();
                                imageScan.HuffmanTables.AddRange(header.HuffmanTables);
                                ResetMCUs(header);

                                bool isDone = false;
                                int readCount = 0;
                                //bool onNewScan = true;
                                while (!isDone)
                                {
                                    byte rbyte = reader.ReadByte();
                                    if (rbyte == 0xFF)
                                    {
                                        byte nbyte = reader.ReadByte();
                                        if (nbyte == 0)
                                        {
                                            // Add a 0xFF to the entropy data.
                                            entropyBytes.Add(0xFF);
                                            readCount++;
                                        }
                                        else
                                        {
                                            JPEGCommandType cmd = (JPEGCommandType)nbyte;
                                            //Console.WriteLine("Command in Entropy Coded Data: " + cmd.ToString());
                                            //ProcessEncodedData(entropyBytes.ToArray());
                                            if (cmd == JPEGCommandType.EndOfImage)
                                            {
                                                //Console.WriteLine("Read {0} bytes of scan data so far.", readCount);
                                                isDone = true;
                                                isEOF = true;
                                                if (Scans.Count == 0)
                                                {
                                                    // Baseline Sequential Scan
                                                    imageScan.EntropyBytes = entropyBytes;
                                                    //Console.WriteLine("Previous Entropy Bytes: {0}", imageScan.EntropyBytes.Count);
                                                    imageScan.EntropyReader = new BitReader(entropyBytes.ToArray(), true);
                                                    imageScan.ScanID = scanIdCounter;
                                                    Scans.Add(imageScan);
                                                    scanIdCounter++;
                                                }
                                                else if (entropyBytes.Count != 0)
                                                {
                                                    imageScan.EntropyBytes = entropyBytes.ToArray().ToList();
                                                    //Console.WriteLine("Previous Entropy Bytes: {0}", imageScan.EntropyBytes.Count);
                                                    BitReader b = new BitReader(entropyBytes.ToArray(), true);
                                                    imageScan.EntropyReader = b;
                                                    imageScan.ScanID = scanIdCounter;
                                                    Scans.Add(imageScan);
                                                    scanIdCounter++;
                                                    entropyBytes = new List<byte>();
                                                    imageScan = new ScanData();
                                                    imageScan.HuffmanTables = new List<HuffmanTable>();
                                                }
                                                ProcessAllEncodedData(entropyBytes.ToArray(), header, isFinal: true);
                                            }
                                            else if (cmd == JPEGCommandType.HuffmanTable)
                                            {
                                                //Console.WriteLine("Read {0} bytes of scan data so far.", readCount);
                                                if (entropyBytes.Count != 0)
                                                {
                                                    imageScan.EntropyBytes = entropyBytes.ToArray().ToList();
                                                    //Console.WriteLine("Previous Entropy Bytes: {0}", imageScan.EntropyBytes.Count);
                                                    BitReader b = new BitReader(entropyBytes.ToArray(), true);
                                                    //if (b == null)
                                                    //{
                                                    //    Console.WriteLine("Huh?");
                                                    //}
                                                    imageScan.EntropyReader = b;
                                                    imageScan.ScanID = scanIdCounter;
                                                    Scans.Add(imageScan);
                                                    scanIdCounter++;
                                                    entropyBytes = new List<byte>();
                                                    imageScan = new ScanData();
                                                    imageScan.HuffmanTables = new List<HuffmanTable>();
                                                }
                                                //if (onNewScan)
                                                //{
                                                //    ProcessAllEncodedData(entropyBytes.ToArray(), header);
                                                //    onNewScan = false;
                                                //    firstPass = false;
                                                //    //entropyBytes = new List<byte>();
                                                //}
                                                List<HuffmanTable> huffmanTables = ReadHuffmanTables(reader);
                                                imageScan.HuffmanTables.AddRange(huffmanTables);
                                                for (int z = 0; z < huffmanTables.Count; z++)
                                                {
                                                    bool found = false;
                                                    for (int i = 0; i < header.HuffmanTables.Count; i++)
                                                    {
                                                        if (header.HuffmanTables[i].HTNumber == huffmanTables[z].HTNumber && header.HuffmanTables[i].HTTableType == huffmanTables[z].HTTableType)
                                                        {
                                                            header.HuffmanTables[i] = huffmanTables[z];
                                                            found = true;
                                                        }
                                                    }
                                                    if (!found)
                                                    {
                                                        //Console.WriteLine("New Huffman Table");
                                                        header.HuffmanTables.Add(huffmanTables[z]);
                                                    }
                                                }
                                                //header.HuffmanTables.AddRange(huffmanTables);
                                            }
                                            else if (cmd == JPEGCommandType.QuantizationTable)
                                            {
                                                Console.WriteLine("Read {0} bytes of scan data so far.", readCount);
                                                List<QuantizationTable> quantizationTables = ReadQuantizationTables(reader);
                                                for (int z = 0; z < quantizationTables.Count; z++)
                                                {
                                                    bool found = false;
                                                    for (int i = 0; i < header.QuantizationTables.Count; i++)
                                                    {
                                                        if (header.QuantizationTables[i].QTNumber == quantizationTables[z].QTNumber)
                                                        {
                                                            header.QuantizationTables[i] = quantizationTables[z];
                                                            found = true;
                                                        }
                                                    }
                                                    if (!found)
                                                    {
                                                        Console.WriteLine("New Quantization Table");
                                                        header.QuantizationTables.Add(quantizationTables[z]);
                                                    }
                                                }
                                                //header.QuantizationTables.AddRange(quantizationTables);
                                            }
                                            else if (cmd == JPEGCommandType.StartOfImageScan)
                                            {
                                                //Console.WriteLine("Read {0} bytes of scan data so far.", readCount);
                                                if (entropyBytes.Count != 0)
                                                {
                                                    imageScan.EntropyBytes = entropyBytes.ToArray().ToList();
                                                    //Console.WriteLine("Previous Entropy Bytes: {0}", imageScan.EntropyBytes.Count);
                                                    BitReader b = new BitReader(entropyBytes.ToArray(), true);
                                                    imageScan.EntropyReader = b;
                                                    imageScan.ScanID = scanIdCounter;
                                                    Scans.Add(imageScan);
                                                    scanIdCounter++;
                                                    entropyBytes = new List<byte>();
                                                    imageScan = new ScanData();
                                                    imageScan.HuffmanTables = new List<HuffmanTable>();
                                                    imageScan.SOISInfo = new StartOfImageScanInformation();
                                                }
                                                else
                                                {
                                                    if (imageScan == null)
                                                    {
                                                        imageScan = new ScanData();
                                                        imageScan.HuffmanTables = new List<HuffmanTable>();
                                                        imageScan.SOISInfo = new StartOfImageScanInformation();
                                                    }
                                                    //entropyBytes = new List<byte>();
                                                }
                                                //Console.WriteLine("Read {0} bytes of scan data so far.", readCount);
                                                //ProcessAllEncodedData(entropyBytes.ToArray(), header);
                                                //WriteBMPFromImage(header, @"c:\users\johnr\documents\test-" + DateTime.Now.ToFileTime() + ".bmp");

                                                //ProcessAllEncodedData(entropyBytes.ToArray(), header);

                                                ReadImageScanStart(header, reader);
                                                imageScan.SOISInfo = header.SOISInfo;
                                                //onNewScan = true;
                                                entropyBytes = new List<byte>();
                                            }
                                            else
                                            {
                                                //Console.WriteLine("Unsupported Function");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        entropyBytes.Add(rbyte);
                                        readCount++;
                                    }
                                }
                                //Console.WriteLine("Read {0} bytes of image scan data.", readCount);
                            }
                            break;
                        case JPEGCommandType.StartOfFrameSOF0:
                        case JPEGCommandType.StartOfFrameSOF1:
                        case JPEGCommandType.StartOfFrameSOF2:
                        case JPEGCommandType.StartOfFrameSOF3:
                        //case JPEGCommandType.StartOfFrameSOF4:
                        case JPEGCommandType.StartOfFrameSOF5:
                        case JPEGCommandType.StartOfFrameSOF6:
                        case JPEGCommandType.StartOfFrameSOF7:
                        //case JPEGCommandType.StartOfFrameSOF8:
                        case JPEGCommandType.StartOfFrameSOF9:
                        case JPEGCommandType.StartOfFrameSOF10:
                        case JPEGCommandType.StartOfFrameSOF11:
                        //case JPEGCommandType.StartOfFrameSOF12:
                        case JPEGCommandType.StartOfFrameSOF13:
                        case JPEGCommandType.StartOfFrameSOF14:
                        case JPEGCommandType.StartOfFrameSOF15:
                            {
                                //Console.WriteLine("Start of Frame - Type: " + (JPEGCommandType)headerCheck[1]);
                                JPEGCompressionType compressionType = (JPEGCompressionType)((JPEGCommandType)headerCheck[1]);

                                int readCounter = 0;
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                readCounter += 2;
                                //Console.WriteLine("SOF Length: {0}", length);
                                byte dataPrecision = reader.ReadByte();
                                readCounter++;
                                short imageHeight = (short)reader.ReadBytes(2).GetUIntBE(0);
                                readCounter += 2;
                                short imageWidth = (short)reader.ReadBytes(2).GetUIntBE(0);
                                readCounter += 2;
                                byte numberOfComponents = reader.ReadByte();
                                readCounter++;
                                //Console.WriteLine("# of Components: {0} - Data Precision: {1} - Image: {2}x{3}", numberOfComponents, dataPrecision, imageWidth, imageHeight);

                                Image = new RGBImage<byte>((uint)imageWidth, (uint)imageHeight);

                                header.SOFInfo = new StartOfFrameInformation()
                                {
                                    CompressionType = compressionType,
                                    DataPrecision = dataPrecision,
                                    ImageWidth = imageWidth,
                                    ImageHeight = imageHeight,
                                    Components = new List<StartOfFrameInformation.Component>()
                                };

                                header.SOFInfo.MCUHeight = (header.SOFInfo.ImageHeight + 7) / 8;
                                header.SOFInfo.MCUWidth = (header.SOFInfo.ImageWidth + 7) / 8;
                                header.SOFInfo.MCUHeightReal = header.SOFInfo.MCUHeight;
                                header.SOFInfo.MCUWidthReal = header.SOFInfo.MCUWidth;

                                for (int z = 0; z < numberOfComponents; z++)
                                {
                                    byte componentId = reader.ReadByte();
                                    readCounter++;

                                    header.SOFInfo.ZeroBased = (componentId == 0) ? true : false;
                                    byte subsampling = reader.ReadByte();
                                    readCounter++;

                                    byte ssHigh = (byte)((subsampling >> 4) & 0xF);
                                    byte ssLow = (byte)(subsampling & 0xF);
                                    byte quantizationTableDestination = reader.ReadByte();
                                    readCounter++;

                                    //Console.WriteLine("SOF Component #: {5} - Component ID: {0} - Subsampling: {1}/{2}/{3} - Quantization Table Destination: {4}", componentId, subsampling, ssHigh, ssLow, quantizationTableDestination, z);
                                    header.SOFInfo.Components.Add(new StartOfFrameInformation.Component()
                                    {
                                        ComponentID = componentId,
                                        QuantizationTableDestination = quantizationTableDestination,
                                        SubsamplingHorizontal = ssHigh,
                                        SubsamplingVertical = ssLow
                                    });

                                    if (ssHigh == 2 && header.SOFInfo.MCUWidth % 2 == 1)
                                    {
                                        header.SOFInfo.MCUWidthReal += 1;
                                    }
                                    if (ssLow == 2 && header.SOFInfo.MCUHeight % 2 == 1)
                                    {
                                        header.SOFInfo.MCUHeightReal += 1;
                                    }
                                }
                                for (int z = 0; z < numberOfComponents; z++)
                                {
                                    //header.SOFInfo.Components[z].ComponentID++;
                                    ComponentIDMap.Add(z, header.SOFInfo.Components[z].ComponentID);
                                }
                                //if (header.SOFInfo.ZeroBased)
                                //{
                                //}
                                header.SOFInfo.HorizontalSamplingFactor = header.SOFInfo.Components.Max(x => x.SubsamplingHorizontal);
                                header.SOFInfo.VerticalSamplingFactor = header.SOFInfo.Components.Max(x => x.SubsamplingVertical);

                                //if (readCounter != length)
                                //{
                                //    Console.WriteLine("Expected {0} bytes but got {1}", length, readCounter);
                                //}
                            }
                            break;
                        case JPEGCommandType.QuantizationTable:
                            {
                                header.QuantizationTables.AddRange(ReadQuantizationTables(reader));
                            }
                            break;
                        case JPEGCommandType.HuffmanTable:
                            {
                                header.HuffmanTables.AddRange(ReadHuffmanTables(reader));
                            }
                            break;
                        case JPEGCommandType.RestartInterval:
                            {
                                //Console.WriteLine("RestartInterval");
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                short restartInterval = (short)reader.ReadBytes(2).GetUIntBE(0);
                            }
                            break;
                        case JPEGCommandType.ApplicationExtension0:
                            {
                                //Console.WriteLine("Mandatory App0 Extension");
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                string identifier = Encoding.Default.GetString(reader.ReadBytes(5)).Trim();
                                if (identifier.ToLower() == "jfif\0")
                                {
                                    byte jfifVersionMajor = reader.ReadByte();
                                    byte jfifVersionMinor = reader.ReadByte();
                                    byte densityUnits = reader.ReadByte();
                                    short xDensity = (short)reader.ReadBytes(2).GetUIntBE(0);
                                    short yDensity = (short)reader.ReadBytes(2).GetUIntBE(0);
                                    byte xThumbnail = reader.ReadByte();
                                    byte yThumbnail = reader.ReadByte();
                                    int n = (int)(xThumbnail * yThumbnail);
                                    byte[] thumbnailData = reader.ReadBytes(n * 3);
                                }
                            }
                            break;
                        case JPEGCommandType.ApplicationExtension1:
                        case JPEGCommandType.ApplicationExtension2:
                        case JPEGCommandType.ApplicationExtension3:
                        case JPEGCommandType.ApplicationExtension4:
                        case JPEGCommandType.ApplicationExtension5:
                        case JPEGCommandType.ApplicationExtension6:
                        case JPEGCommandType.ApplicationExtension7:
                        case JPEGCommandType.ApplicationExtension8:
                        case JPEGCommandType.ApplicationExtension9:
                        case JPEGCommandType.ApplicationExtension10:
                        case JPEGCommandType.ApplicationExtension11:
                        case JPEGCommandType.ApplicationExtension12:
                        case JPEGCommandType.ApplicationExtension13:
                        case JPEGCommandType.ApplicationExtension14:
                        case JPEGCommandType.ApplicationExtension15:
                            {
                                // Application Extension
                                //Console.WriteLine("Application Extension " + ((JPEGCommandType)headerCheck[1]).ToString());
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                byte[] dataBytes = reader.ReadBytes(length - 2);
                                int n = 0;
                                while (dataBytes[n] != 0)
                                    n++;
                                string identifier = Encoding.Default.GetString(dataBytes.GetSublength(0, n));
                                //Console.WriteLine("- Identifier: {0} - Length: {1}", identifier, length - 2);
                                //LogBytes(dataBytes);
                            }
                            break;
                        case JPEGCommandType.Comment:
                            {
                                //Console.WriteLine("Comment " + ((JPEGCommandType)headerCheck[1]).ToString());
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                string comment = Encoding.Default.GetString(reader.ReadBytes(length - 2));
                                //Console.WriteLine("- Comment: {0}", comment);
                            }
                            break;
                        default:
                            {
                                JPEGCommandType cmdType = (JPEGCommandType)headerCheck[1];
                                //Console.WriteLine("Unsupported Number: {0}, {1}", headerCheck[0], cmdType);
                                short length = (short)reader.ReadBytes(2).GetUIntBE(0);
                                if (length == -1)
                                {
                                    isEOF = true;
                                }
                                else
                                {
                                    byte[] bytes = reader.ReadBytes(length - 2);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    //Console.WriteLine("Unknown check header: {0}, {1}", headerCheck[0], headerCheck[1]);
                }
            }
        }

        public Header JpegHeader { get; set; }

        public JPEG(byte[] data)
        {
            currentScanNumber = 0;
            DecodeJpeg(data);
        }

        public JPEG(Stream inputStream)
        {
            currentScanNumber = 0;
            DecodeJpeg(inputStream);
        }

        public JPEG(string fileName)
        {
            currentScanNumber = 0;
            //logOutput = true;
            //StartOutput("c:\\users\\johnr\\documents\\scandata-pngconsole.txt");
            DecodeJpeg(fileName);
            //CloseOutput();
        }

        uint[] MaskLookup { get; set; } = new uint[32];

        void GenerateLookupHuffMask()
        {
            uint mask;
            for (uint len = 0; len < 32; len++)
            {
                mask = (uint)(((uint)1 << (int)(len)) - 1);
                mask <<= (int)(32 - len);
                MaskLookup[len] = mask;
            }
        }

        void ZigZag<T>(T[,] input, int num_rows, int num_cols, out T[] output)
        {
            output = new T[num_cols * num_rows];
            int cur_row = 1; 
            int cur_col = 1; 
            int cur_index = 0;
            //out= zeros(1, num_rows* num_cols);

            //% First element
            //%out(1)=in(1,1);
            bool done = false;
            while (!done)
            {
                if (cur_row == 1 && (cur_row + cur_col) % 2 == 0 && cur_col != num_cols)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_row == num_rows && (cur_row + cur_col) % 2 != 0 && cur_col != num_cols)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_col == 1 && (cur_row + cur_col) % 2 != 0 && cur_row != num_rows)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_row = cur_row + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_col == num_cols && (cur_row + cur_col) % 2 == 0 && cur_row != num_rows)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_row = cur_row + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_col != 1 && cur_row != num_rows && (cur_row + cur_col) % 2 != 0)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_row = cur_row + 1; cur_col = cur_col - 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_row != 1 && cur_col != num_cols && (cur_row + cur_col) % 2 == 0)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    cur_row = cur_row - 1; cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_row == num_rows && cur_col == num_cols)
                {
                    output[cur_index] = input[cur_col - 1, cur_row - 1];
                    done = true;
                }
            }
        }

        void InverseZigZag<T>(T[] input, int num_rows, int num_cols, out T[,] output)
        {
            int tot_elem=input.Length;
            if (tot_elem != num_rows * num_cols)
            {
                throw new ArgumentException("Input to inverse zig-zag ordering needs to be x*y == input.Length");
            }
            output = new T[num_cols, num_rows];
            output[0, 0] = input[0];
            int cur_index = 0;
            int cur_row = 1; 
            int cur_col = 1;
            bool done = false;
            while (!done)
            {
                if (cur_row == 1 && (cur_row + cur_col) % 2 == 0 && cur_col != num_cols)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_row== num_rows && (cur_row + cur_col) % 2 != 0 && cur_col != num_cols)
                { 
		            output[cur_col - 1, cur_row - 1] =input[cur_index];
                    cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_col== 1 && (cur_row + cur_col) % 2 != 0 && cur_row != num_rows)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    cur_row = cur_row + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_col== num_cols && (cur_row + cur_col) % 2 == 0 && cur_row != num_rows)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    cur_row = cur_row + 1;                          
                    cur_index = cur_index + 1;
                }
                else if (cur_col!= 1 && cur_row != num_rows && (cur_row + cur_col) % 2 != 0)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    cur_row = cur_row + 1; cur_col = cur_col - 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_row != 1 && cur_col != num_cols && (cur_row + cur_col) %2 == 0)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    cur_row = cur_row - 1; cur_col = cur_col + 1;
                    cur_index = cur_index + 1;
                }
                else if (cur_index == tot_elem - 1)
                {
                    output[cur_col - 1, cur_row - 1] = input[cur_index];
                    done = true;
                }
            }
        }
    }
}