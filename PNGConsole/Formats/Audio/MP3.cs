using Sapwood.IO.FileFormats.Algorithms;
using Sapwood.IO.FileFormats.Collections;
using Sapwood.IO.FileFormats.Extensions;
using Sapwood.IO.FileFormats.Formats.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sapwood.IO.FileFormats.Formats.Audio
{
    public class MP3
    {
        public MP3()
        {

        }

        public class MP3FrameHeader
        {
            public uint CodeSync { get; set; }
            public enum MPEGAudioVersionType
            {
                MPEGVersion2_5 = 00,
                Reserved = 1,
                MPEGVersion2 = 2,
                MPEGVersion1 = 3
            }

            public MPEGAudioVersionType MPEGAudioVersion { get; set; }

            public enum LayerDescriptionType
            {
                Reserved = 0,
                Layer3 = 1,
                Layer2 = 2,
                Layer1 = 3
            }

            public LayerDescriptionType LayerDescription { get; set; }

            public int BitRate
            {
                get
                {
                    if (MPEGAudioVersion == MPEGAudioVersionType.Reserved)
                        return -1;
                    if (LayerDescription == LayerDescriptionType.Reserved)
                        return -1;

                    int V = 1;
                    if (MPEGAudioVersion == MPEGAudioVersionType.MPEGVersion2 || MPEGAudioVersion == MPEGAudioVersionType.MPEGVersion2_5)
                        V = 2;

                    int L = 1;
                    if (LayerDescription == LayerDescriptionType.Layer2)
                        L = 2;
                    else if (LayerDescription == LayerDescriptionType.Layer3)
                        L = 3;

                    int idx1 = 0;
                    if (V == 1 && L == 2)
                        idx1 = 1;
                    else if (V == 1 && L == 3)
                        idx1 = 2;
                    else if (V == 2 && L == 1)
                        idx1 = 3;
                    else if ((V == 2 && L == 2) || (V == 2 && L == 3))
                        idx1 = 4;

                    return BitrateIndexTable[idx1][BitRateLookup];
                }
            }

            private int[][] BitrateIndexTable 
            {
                get
                {
                    return new int[][]
                    {
                        new int[] { 0,32,64,96,128,160,192,224,256,288,320,352,384,416,448,-1 },
                        new int[] { 0,32,48,56,64,80,96,112,128,160,192,224,256,320,384,-1},
                        new int[] { 0,32,40,48,56,64,80,96,112,128,160,192,224,256,320,-1 },
                        new int[] { 0,32,48,56,64,80,96,112,128,144,160,176,192,224,256,-1 },
                        new int[] { 0,8,16,24,32,40,48,56,64,80,96,112,128,144,160,-1 }
                    };
                }
            }

            public int BitRateLookup { get; set; }

            public bool ProtectionBit { get; set; }

            public int SamplingRateFrequency
            {
                get
                {
                    if (MPEGAudioVersion == MPEGAudioVersionType.Reserved)
                        return -1;

                    int idx = 0;
                    if (MPEGAudioVersion == MPEGAudioVersionType.MPEGVersion1)
                        idx = 0;
                    else if (MPEGAudioVersion == MPEGAudioVersionType.MPEGVersion2)
                        idx = 1;
                    else if (MPEGAudioVersion == MPEGAudioVersionType.MPEGVersion2_5)
                        idx = 2;

                    return SamplingRateFrequencyIndexTable[idx][SamplingRateLookupIndex];
                }
            }

            public int SamplingRateLookupIndex { get; set; }

            private int[][] SamplingRateFrequencyIndexTable 
            { 
                get
                {
                    return new int[][] {
                        new int[] { 44100, 48000, 32000, -1 },
                        new int[] { 22050, 24000, 16000, -1 },
                        new int[] { 11025, 12000, 8000, -1 }
                    };
                }
            }

            public bool PaddingBit { get; set; }
            public bool PrivateBit { get; set; }

            public enum ChannelModeType
            {
                Stereo = 0,
                JointStereo = 1,
                DualChannelStereo = 2,
                SingleChannelMono = 3
            }

            public int NumberOfChannels
            {
                get
                {
                    if (ChannelMode == ChannelModeType.SingleChannelMono)
                        return 1;
                    else
                        return 2;
                }
            }

            public enum StereoType
            {
                NotApplicable,
                IntensityStereoOff,
                IntensityStereoOn,
                MSStereoOff,
                MSStereoOn,
                Bands4to31,
                Bands8to31,
                Bands12to31,
                Bands16to31
            }

            public StereoType ModeExtension
            {
                get
                {
                    if (ChannelMode != ChannelModeType.JointStereo)
                        return StereoType.NotApplicable;

                    if (LayerDescription == LayerDescriptionType.Reserved)
                        return StereoType.NotApplicable;

                    if (LayerDescription == LayerDescriptionType.Layer1 || LayerDescription == LayerDescriptionType.Layer2)
                    {
                        switch(ModeExtensionLookup)
                        {
                            case 0: return StereoType.Bands4to31;
                            case 1: return StereoType.Bands8to31;
                            case 2: return StereoType.Bands12to31;
                            case 3: return StereoType.Bands16to31;
                            default: return StereoType.NotApplicable;
                        }
                    }
                    else if (LayerDescription == LayerDescriptionType.Layer2)
                    {
                        switch (ModeExtensionLookup)
                        {
                            case 0: return StereoType.IntensityStereoOff;
                            case 1: return StereoType.IntensityStereoOn;
                            case 2: return StereoType.IntensityStereoOff;
                            case 3: return StereoType.IntensityStereoOn;
                            default: return StereoType.NotApplicable;
                        }
                    }
                    else if (LayerDescription == LayerDescriptionType.Layer2)
                    {
                        switch (ModeExtensionLookup)
                        {
                            case 0: return StereoType.MSStereoOff;
                            case 1: return StereoType.MSStereoOff;
                            case 2: return StereoType.MSStereoOn;
                            case 3: return StereoType.MSStereoOn;
                            default: return StereoType.NotApplicable;
                        }
                    }
                    return StereoType.NotApplicable;
                }
            }

            public int ModeExtensionLookup { get; set; }

            public ChannelModeType ChannelMode { get; set; }

            public bool Copyright { get; set; }
            public bool Original { get; set; }

            public enum EmphasisType
            {
                None = 0,
                Emphasis50_15ms = 1,
                Reserved = 2,
                CCITJ_17 = 3
            }

            public EmphasisType Emphasis { get; set; }

            public ulong CRC { get; set; }

            public bool IsValid { get; set; }
            public enum BitrateCodingType
            {
                CBR,
                VBR,
                ABR,
                NotApplicable
            }
            public BitrateCodingType BitrateCoding { get; set; }

            public int FrameLength { get; set; }

            public byte[] FrameData { get; set; }

            public SideInformation FrameSideInformation { get; set; }

            public MainData FrameMainData { get; set; }

            public uint HSynthInit { get; set; }
            public uint SynthInit { get; set; }

            public override string ToString()
            {
                return $@"MP3FrameHeader:[Type:{BitrateCoding}, BitRate:{BitRate*1000}, Hz:{SamplingRateFrequency}, FrameLength:{FrameLength}, AudioType:{MPEGAudioVersion}, Layer:{LayerDescription}]";
            }
        }

        public class SideInformation
        {
            public ulong MainData { get; set; }
            public ulong PrivateBits { get; set; }
            public bool[][] ScaleFactorSelectionInformation { get; set; }
            public Dictionary<int, int[]> ScaleFactorBands
            {
                get
                {
                    return new Dictionary<int, int[]>()
                    {
                        { 0, new int[] { 0, 1, 2, 3, 4, 5 } },
                        { 1, new int[] { 6, 7, 8, 9, 10 } },
                        { 2, new int[] { 11, 12, 13, 14, 15 } },
                        { 3, new int[] { 16, 17, 18, 19, 20 } }
                    };
                }
            }
            public Granule[] Granules { get; set; }
            public class Granule
            {
                public class Channel
                {
                    public ulong Part2_3_Length { get; set; }
                    public ulong BigValues { get; set; }
                    public ulong GlobalGain { get; set; }
                    public ulong ScaleFactor_Compress { get; set; }
                    public ulong WindowsSwitchingFlag { get; set; }
                    public ulong BlockType { get; set; }
                    public ulong MixedBlockFlag { get; set; }
                    public ulong[] TableSelect { get; set; }
                    public ulong[] SubBlockGain { get; set; }
                    public ulong Region0Count { get; set; }
                    public ulong Region1Count { get; set; }
                    public ulong Preflag { get; set; }
                    public ulong ScaleFactorScale { get; set; }
                    public ulong Count1TableSelect { get; set; }

                    public ulong Count1 { get; set; }

                    public int SLen1
                    {
                        get
                        {
                            int[] slen1 = { 0, 0, 0, 0, 3, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4 };
                            return slen1[(int)ScaleFactor_Compress];
                        }
                    }

                    public int SLen2
                    {
                        get
                        {
                            int[] slen2 = { 0, 1, 2, 3, 0, 1, 2, 3, 1, 2, 3, 1, 2, 3, 2, 3 };
                            return slen2[(int)ScaleFactor_Compress];
                        }
                    }

                    public int Part2_Length
                    {
                        get
                        {
                            int[] blockTypes = { 0, 1, 3 };
                            if (blockTypes.Contains((int)BlockType))
                            {
                                return 11 * SLen1 + 10 * SLen2;
                            }
                            else
                            {
                                if (BlockType == 2 && MixedBlockFlag == 0)
                                {
                                    return 18 * SLen1 + 18 * SLen2;
                                }
                                else if (BlockType == 2 && MixedBlockFlag == 1)
                                {
                                    return 17 * SLen1 + 18 * SLen2;
                                }
                            }
                            return -1;
                        }
                    }

                    //public int Region2Count
                    //{
                    //    get
                    //    {
                    //        return (int)BigValues - (int)(Region0Count + Region1Count);
                    //    }
                    //}

                    public int Part3_Length
                    {
                        get
                        {
                            return (int)Part2_3_Length - Part2_Length;
                        }
                    }

                    public override string ToString()
                    {
                        return $"[Channel:Window:{WindowsSwitchingFlag}, P23:{Part2_3_Length}, P2:{Part2_Length}, P3:{(int)Part2_3_Length - Part2_Length}, BigValues:{BigValues}, SLen1:{SLen1}, SLen2:{SLen2}]";
                    }
                }

                public Channel[] Channels { get; set; }

                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[Granule:");
                    for (int z = 0; z < Channels.Length; z++)
                    {
                        sb.Append(Channels[z].ToString() + " ");
                    }
                    sb.Append("]");
                    return sb.ToString();
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"[SideInfo: MainDataBegin:{MainData}, Private:{PrivateBits}, ScaleFactorBands:");
                for (int z = 0; z < ScaleFactorSelectionInformation.Length; z++)
                {
                    for (int y = 0; y < ScaleFactorSelectionInformation[z].Length; y++)
                        sb.Append($"({z},{y}:{ScaleFactorSelectionInformation[z][y]}), ");
                }
                for (int z = 0; z < Granules.Length; z++)
                {
                    sb.Append(Granules[z].ToString() + " ");
                }
                sb.Append("]");
                return sb.ToString();
            }
        }

        public class MainData
        {
            //typedef struct { /* MPEG1 Layer 3 Main Data */
            //  unsigned scalefac_l[2][2] [21];    /* 0-4 bits */
            //  unsigned scalefac_s[2][2] [12] [3]; /* 0-4 bits */
            //  float is[2] [2] [576];               /* Huffman coded freq. lines */
            //}
            public byte[][][] ScaleFac_Long { get; set; }
            public byte[][][][] ScaleFac_Short { get; set; }
            public float[][][] IS { get; set; }

            public class Granule
            {
                public uint[] OutputData { get; set; }
            }

            public Granule[] Granules { get; set; }

            public void Init(int channels)
            {
                ScaleFac_Long = new byte[2][][];
                ScaleFac_Short = new byte[2][][][];
                IS = new float[2][][];
                Granules = new Granule[2];

                for(int gr=0;gr<2;gr++)
                {
                    Granules[gr] = new Granule();
                    Granules[gr].OutputData = new uint[576];
                    //Granules[gr].Channels = new Granule.Channel[channels];

                    ScaleFac_Long[gr] = new byte[2][];
                    ScaleFac_Short[gr] = new byte[2][][];
                    IS[gr] = new float[2][];

                    for(int ch=0;ch<2;ch++)
                    {
                        //Granules[gr].Channels[ch] = new Granule.Channel();
                        ScaleFac_Long[gr][ch] = new byte[21];
                        ScaleFac_Short[gr][ch] = new byte[12][];
                        for(int w=0;w<12;w++)
                        {
                            ScaleFac_Short[gr][ch][w] = new byte[3];
                        }
                        IS[gr][ch] = new float[576];
                    }
                }
            }

            public MainData()
            {
                Init(2);
            }

            public MainData(int channels)
            {
                Init(channels);
            }

            //        t_mpeg1_main_data;
            //typedef struct hufftables
            //        {
            //            const unsigned short* hufftable;
            //            uint16_t treelen;
            //            uint8_t linbits;
            //        }
            //        hufftables;
            //typedef struct { /* Scale factor band indices,for long and short windows */
            //  unsigned l[23];
            //        unsigned s[14];
            //    }
            //    t_sf_band_indices;
        }

        public class ScaleFactorBandIndices
        {
            //typedef struct { /* Scale factor band indices,for long and short windows */
            //    unsigned l[23];
            //    unsigned s[14];
            //}
            //t_sf_band_indices;
            public uint[] L { get; set; }
            public uint[] S { get; set; }

            public ScaleFactorBandIndices()
            {
                L = new uint[23];
                S = new uint[14];
            }

            //  /* Scale factor band indices
            //   *
            //   * One table per sample rate. Each table contains the frequency indices
            //   * for the 12 short and 21 long scalefactor bands. The short indices
            //   * must be multiplied by 3 to get the actual index.
            //   */
            public static ScaleFactorBandIndices[] AllBandIndices
            {
                get
                {
                    return new ScaleFactorBandIndices[3]
                    {
                        new ScaleFactorBandIndices()
                        {
                            L = new uint[23]{ 0,4,8,12,16,20,24,30,36,44,52,62,74,90,110,134,162,196,238,288,342,418,576 },
                            S = new uint[14]{ 0, 4, 8, 12, 16, 22, 30, 40, 52, 66, 84, 106, 136, 192 }
                        },
                        new ScaleFactorBandIndices()
                        {
                            L = new uint[23]{ 0,4,8,12,16,20,24,30,36,42,50,60,72,88,106,128,156,190,230,276,330,384,576 },
                            S = new uint[14]{ 0, 4, 8, 12, 16, 22, 28, 38, 50, 64, 80, 100, 126, 192 }
                        },
                        new ScaleFactorBandIndices()
                        {
                            L = new uint[23]{ 0,4,8,12,16,20,24,30,36,44,54,66,82,102,126,156,194,240,296,364,448,550,576 },
                            S = new uint[14]{ 0, 4, 8, 12, 16, 22, 30, 42, 58, 78, 104, 138, 180, 192 }
                        }
                    };
                }
            }
        }

        public class HuffmanTables
        {
            ushort[] huffman_table = {
            //g_huffman_table_1[7] = {
                0x0201,0x0000,0x0201,0x0010,0x0201,0x0001,0x0011,
            //},g_huffman_table_2[17] = {
                0x0201,0x0000,0x0401,0x0201,0x0010,0x0001,0x0201,0x0011,0x0401,0x0201,0x0020,
                0x0021,0x0201,0x0012,0x0201,0x0002,0x0022,
            //},g_huffman_table_3[17] = {
                0x0401,0x0201,0x0000,0x0001,0x0201,0x0011,0x0201,0x0010,0x0401,0x0201,0x0020,
                0x0021,0x0201,0x0012,0x0201,0x0002,0x0022,
            //},g_huffman_table_5[31] = {
                0x0201,0x0000,0x0401,0x0201,0x0010,0x0001,0x0201,0x0011,0x0801,0x0401,0x0201,
                0x0020,0x0002,0x0201,0x0021,0x0012,0x0801,0x0401,0x0201,0x0022,0x0030,0x0201,
                0x0003,0x0013,0x0201,0x0031,0x0201,0x0032,0x0201,0x0023,0x0033,
            //},g_huffman_table_6[31] = {
                0x0601,0x0401,0x0201,0x0000,0x0010,0x0011,0x0601,0x0201,0x0001,0x0201,0x0020,
                0x0021,0x0601,0x0201,0x0012,0x0201,0x0002,0x0022,0x0401,0x0201,0x0031,0x0013,
                0x0401,0x0201,0x0030,0x0032,0x0201,0x0023,0x0201,0x0003,0x0033,
            //},g_huffman_table_7[71] = {
                0x0201,0x0000,0x0401,0x0201,0x0010,0x0001,0x0801,0x0201,0x0011,0x0401,0x0201,
                0x0020,0x0002,0x0021,0x1201,0x0601,0x0201,0x0012,0x0201,0x0022,0x0030,0x0401,
                0x0201,0x0031,0x0013,0x0401,0x0201,0x0003,0x0032,0x0201,0x0023,0x0004,0x0a01,
                0x0401,0x0201,0x0040,0x0041,0x0201,0x0014,0x0201,0x0042,0x0024,0x0c01,0x0601,
                0x0401,0x0201,0x0033,0x0043,0x0050,0x0401,0x0201,0x0034,0x0005,0x0051,0x0601,
                0x0201,0x0015,0x0201,0x0052,0x0025,0x0401,0x0201,0x0044,0x0035,0x0401,0x0201,
                0x0053,0x0054,0x0201,0x0045,0x0055,
            //},g_huffman_table_8[71] = {
                0x0601,0x0201,0x0000,0x0201,0x0010,0x0001,0x0201,0x0011,0x0401,0x0201,0x0021,
                0x0012,0x0e01,0x0401,0x0201,0x0020,0x0002,0x0201,0x0022,0x0401,0x0201,0x0030,
                0x0003,0x0201,0x0031,0x0013,0x0e01,0x0801,0x0401,0x0201,0x0032,0x0023,0x0201,
                0x0040,0x0004,0x0201,0x0041,0x0201,0x0014,0x0042,0x0c01,0x0601,0x0201,0x0024,
                0x0201,0x0033,0x0050,0x0401,0x0201,0x0043,0x0034,0x0051,0x0601,0x0201,0x0015,
                0x0201,0x0005,0x0052,0x0601,0x0201,0x0025,0x0201,0x0044,0x0035,0x0201,0x0053,
                0x0201,0x0045,0x0201,0x0054,0x0055,
            //},g_huffman_table_9[71] = {
                0x0801,0x0401,0x0201,0x0000,0x0010,0x0201,0x0001,0x0011,0x0a01,0x0401,0x0201,
                0x0020,0x0021,0x0201,0x0012,0x0201,0x0002,0x0022,0x0c01,0x0601,0x0401,0x0201,
                0x0030,0x0003,0x0031,0x0201,0x0013,0x0201,0x0032,0x0023,0x0c01,0x0401,0x0201,
                0x0041,0x0014,0x0401,0x0201,0x0040,0x0033,0x0201,0x0042,0x0024,0x0a01,0x0601,
                0x0401,0x0201,0x0004,0x0050,0x0043,0x0201,0x0034,0x0051,0x0801,0x0401,0x0201,
                0x0015,0x0052,0x0201,0x0025,0x0044,0x0601,0x0401,0x0201,0x0005,0x0054,0x0053,
                0x0201,0x0035,0x0201,0x0045,0x0055,
            //},g_huffman_table_10[127] = {
                0x0201,0x0000,0x0401,0x0201,0x0010,0x0001,0x0a01,0x0201,0x0011,0x0401,0x0201,
                0x0020,0x0002,0x0201,0x0021,0x0012,0x1c01,0x0801,0x0401,0x0201,0x0022,0x0030,
                0x0201,0x0031,0x0013,0x0801,0x0401,0x0201,0x0003,0x0032,0x0201,0x0023,0x0040,
                0x0401,0x0201,0x0041,0x0014,0x0401,0x0201,0x0004,0x0033,0x0201,0x0042,0x0024,
                0x1c01,0x0a01,0x0601,0x0401,0x0201,0x0050,0x0005,0x0060,0x0201,0x0061,0x0016,
                0x0c01,0x0601,0x0401,0x0201,0x0043,0x0034,0x0051,0x0201,0x0015,0x0201,0x0052,
                0x0025,0x0401,0x0201,0x0026,0x0036,0x0071,0x1401,0x0801,0x0201,0x0017,0x0401,
                0x0201,0x0044,0x0053,0x0006,0x0601,0x0401,0x0201,0x0035,0x0045,0x0062,0x0201,
                0x0070,0x0201,0x0007,0x0064,0x0e01,0x0401,0x0201,0x0072,0x0027,0x0601,0x0201,
                0x0063,0x0201,0x0054,0x0055,0x0201,0x0046,0x0073,0x0801,0x0401,0x0201,0x0037,
                0x0065,0x0201,0x0056,0x0074,0x0601,0x0201,0x0047,0x0201,0x0066,0x0075,0x0401,
                0x0201,0x0057,0x0076,0x0201,0x0067,0x0077,
            //},g_huffman_table_11[127] = {
                0x0601,0x0201,0x0000,0x0201,0x0010,0x0001,0x0801,0x0201,0x0011,0x0401,0x0201,
                0x0020,0x0002,0x0012,0x1801,0x0801,0x0201,0x0021,0x0201,0x0022,0x0201,0x0030,
                0x0003,0x0401,0x0201,0x0031,0x0013,0x0401,0x0201,0x0032,0x0023,0x0401,0x0201,
                0x0040,0x0004,0x0201,0x0041,0x0014,0x1e01,0x1001,0x0a01,0x0401,0x0201,0x0042,
                0x0024,0x0401,0x0201,0x0033,0x0043,0x0050,0x0401,0x0201,0x0034,0x0051,0x0061,
                0x0601,0x0201,0x0016,0x0201,0x0006,0x0026,0x0201,0x0062,0x0201,0x0015,0x0201,
                0x0005,0x0052,0x1001,0x0a01,0x0601,0x0401,0x0201,0x0025,0x0044,0x0060,0x0201,
                0x0063,0x0036,0x0401,0x0201,0x0070,0x0017,0x0071,0x1001,0x0601,0x0401,0x0201,
                0x0007,0x0064,0x0072,0x0201,0x0027,0x0401,0x0201,0x0053,0x0035,0x0201,0x0054,
                0x0045,0x0a01,0x0401,0x0201,0x0046,0x0073,0x0201,0x0037,0x0201,0x0065,0x0056,
                0x0a01,0x0601,0x0401,0x0201,0x0055,0x0057,0x0074,0x0201,0x0047,0x0066,0x0401,
                0x0201,0x0075,0x0076,0x0201,0x0067,0x0077,
            //},g_huffman_table_12[127] = {
                0x0c01,0x0401,0x0201,0x0010,0x0001,0x0201,0x0011,0x0201,0x0000,0x0201,0x0020,
                0x0002,0x1001,0x0401,0x0201,0x0021,0x0012,0x0401,0x0201,0x0022,0x0031,0x0201,
                0x0013,0x0201,0x0030,0x0201,0x0003,0x0040,0x1a01,0x0801,0x0401,0x0201,0x0032,
                0x0023,0x0201,0x0041,0x0033,0x0a01,0x0401,0x0201,0x0014,0x0042,0x0201,0x0024,
                0x0201,0x0004,0x0050,0x0401,0x0201,0x0043,0x0034,0x0201,0x0051,0x0015,0x1c01,
                0x0e01,0x0801,0x0401,0x0201,0x0052,0x0025,0x0201,0x0053,0x0035,0x0401,0x0201,
                0x0060,0x0016,0x0061,0x0401,0x0201,0x0062,0x0026,0x0601,0x0401,0x0201,0x0005,
                0x0006,0x0044,0x0201,0x0054,0x0045,0x1201,0x0a01,0x0401,0x0201,0x0063,0x0036,
                0x0401,0x0201,0x0070,0x0007,0x0071,0x0401,0x0201,0x0017,0x0064,0x0201,0x0046,
                0x0072,0x0a01,0x0601,0x0201,0x0027,0x0201,0x0055,0x0073,0x0201,0x0037,0x0056,
                0x0801,0x0401,0x0201,0x0065,0x0074,0x0201,0x0047,0x0066,0x0401,0x0201,0x0075,
                0x0057,0x0201,0x0076,0x0201,0x0067,0x0077,
            //},g_huffman_table_13[511] = {
                0x0201,0x0000,0x0601,0x0201,0x0010,0x0201,0x0001,0x0011,0x1c01,0x0801,0x0401,
                0x0201,0x0020,0x0002,0x0201,0x0021,0x0012,0x0801,0x0401,0x0201,0x0022,0x0030,
                0x0201,0x0003,0x0031,0x0601,0x0201,0x0013,0x0201,0x0032,0x0023,0x0401,0x0201,
                0x0040,0x0004,0x0041,0x4601,0x1c01,0x0e01,0x0601,0x0201,0x0014,0x0201,0x0033,
                0x0042,0x0401,0x0201,0x0024,0x0050,0x0201,0x0043,0x0034,0x0401,0x0201,0x0051,
                0x0015,0x0401,0x0201,0x0005,0x0052,0x0201,0x0025,0x0201,0x0044,0x0053,0x0e01,
                0x0801,0x0401,0x0201,0x0060,0x0006,0x0201,0x0061,0x0016,0x0401,0x0201,0x0080,
                0x0008,0x0081,0x1001,0x0801,0x0401,0x0201,0x0035,0x0062,0x0201,0x0026,0x0054,
                0x0401,0x0201,0x0045,0x0063,0x0201,0x0036,0x0070,0x0601,0x0401,0x0201,0x0007,
                0x0055,0x0071,0x0201,0x0017,0x0201,0x0027,0x0037,0x4801,0x1801,0x0c01,0x0401,
                0x0201,0x0018,0x0082,0x0201,0x0028,0x0401,0x0201,0x0064,0x0046,0x0072,0x0801,
                0x0401,0x0201,0x0084,0x0048,0x0201,0x0090,0x0009,0x0201,0x0091,0x0019,0x1801,
                0x0e01,0x0801,0x0401,0x0201,0x0073,0x0065,0x0201,0x0056,0x0074,0x0401,0x0201,
                0x0047,0x0066,0x0083,0x0601,0x0201,0x0038,0x0201,0x0075,0x0057,0x0201,0x0092,
                0x0029,0x0e01,0x0801,0x0401,0x0201,0x0067,0x0085,0x0201,0x0058,0x0039,0x0201,
                0x0093,0x0201,0x0049,0x0086,0x0601,0x0201,0x00a0,0x0201,0x0068,0x000a,0x0201,
                0x00a1,0x001a,0x4401,0x1801,0x0c01,0x0401,0x0201,0x00a2,0x002a,0x0401,0x0201,
                0x0095,0x0059,0x0201,0x00a3,0x003a,0x0801,0x0401,0x0201,0x004a,0x0096,0x0201,
                0x00b0,0x000b,0x0201,0x00b1,0x001b,0x1401,0x0801,0x0201,0x00b2,0x0401,0x0201,
                0x0076,0x0077,0x0094,0x0601,0x0401,0x0201,0x0087,0x0078,0x00a4,0x0401,0x0201,
                0x0069,0x00a5,0x002b,0x0c01,0x0601,0x0401,0x0201,0x005a,0x0088,0x00b3,0x0201,
                0x003b,0x0201,0x0079,0x00a6,0x0601,0x0401,0x0201,0x006a,0x00b4,0x00c0,0x0401,
                0x0201,0x000c,0x0098,0x00c1,0x3c01,0x1601,0x0a01,0x0601,0x0201,0x001c,0x0201,
                0x0089,0x00b5,0x0201,0x005b,0x00c2,0x0401,0x0201,0x002c,0x003c,0x0401,0x0201,
                0x00b6,0x006b,0x0201,0x00c4,0x004c,0x1001,0x0801,0x0401,0x0201,0x00a8,0x008a,
                0x0201,0x00d0,0x000d,0x0201,0x00d1,0x0201,0x004b,0x0201,0x0097,0x00a7,0x0c01,
                0x0601,0x0201,0x00c3,0x0201,0x007a,0x0099,0x0401,0x0201,0x00c5,0x005c,0x00b7,
                0x0401,0x0201,0x001d,0x00d2,0x0201,0x002d,0x0201,0x007b,0x00d3,0x3401,0x1c01,
                0x0c01,0x0401,0x0201,0x003d,0x00c6,0x0401,0x0201,0x006c,0x00a9,0x0201,0x009a,
                0x00d4,0x0801,0x0401,0x0201,0x00b8,0x008b,0x0201,0x004d,0x00c7,0x0401,0x0201,
                0x007c,0x00d5,0x0201,0x005d,0x00e0,0x0a01,0x0401,0x0201,0x00e1,0x001e,0x0401,
                0x0201,0x000e,0x002e,0x00e2,0x0801,0x0401,0x0201,0x00e3,0x006d,0x0201,0x008c,
                0x00e4,0x0401,0x0201,0x00e5,0x00ba,0x00f0,0x2601,0x1001,0x0401,0x0201,0x00f1,
                0x001f,0x0601,0x0401,0x0201,0x00aa,0x009b,0x00b9,0x0201,0x003e,0x0201,0x00d6,
                0x00c8,0x0c01,0x0601,0x0201,0x004e,0x0201,0x00d7,0x007d,0x0201,0x00ab,0x0201,
                0x005e,0x00c9,0x0601,0x0201,0x000f,0x0201,0x009c,0x006e,0x0201,0x00f2,0x002f,
                0x2001,0x1001,0x0601,0x0401,0x0201,0x00d8,0x008d,0x003f,0x0601,0x0201,0x00f3,
                0x0201,0x00e6,0x00ca,0x0201,0x00f4,0x004f,0x0801,0x0401,0x0201,0x00bb,0x00ac,
                0x0201,0x00e7,0x00f5,0x0401,0x0201,0x00d9,0x009d,0x0201,0x005f,0x00e8,0x1e01,
                0x0c01,0x0601,0x0201,0x006f,0x0201,0x00f6,0x00cb,0x0401,0x0201,0x00bc,0x00ad,
                0x00da,0x0801,0x0201,0x00f7,0x0401,0x0201,0x007e,0x007f,0x008e,0x0601,0x0401,
                0x0201,0x009e,0x00ae,0x00cc,0x0201,0x00f8,0x008f,0x1201,0x0801,0x0401,0x0201,
                0x00db,0x00bd,0x0201,0x00ea,0x00f9,0x0401,0x0201,0x009f,0x00eb,0x0201,0x00be,
                0x0201,0x00cd,0x00fa,0x0e01,0x0401,0x0201,0x00dd,0x00ec,0x0601,0x0401,0x0201,
                0x00e9,0x00af,0x00dc,0x0201,0x00ce,0x00fb,0x0801,0x0401,0x0201,0x00bf,0x00de,
                0x0201,0x00cf,0x00ee,0x0401,0x0201,0x00df,0x00ef,0x0201,0x00ff,0x0201,0x00ed,
                0x0201,0x00fd,0x0201,0x00fc,0x00fe,
            //},g_huffman_table_15[511] = {
                0x1001,0x0601,0x0201,0x0000,0x0201,0x0010,0x0001,0x0201,0x0011,0x0401,0x0201,
                0x0020,0x0002,0x0201,0x0021,0x0012,0x3201,0x1001,0x0601,0x0201,0x0022,0x0201,
                0x0030,0x0031,0x0601,0x0201,0x0013,0x0201,0x0003,0x0040,0x0201,0x0032,0x0023,
                0x0e01,0x0601,0x0401,0x0201,0x0004,0x0014,0x0041,0x0401,0x0201,0x0033,0x0042,
                0x0201,0x0024,0x0043,0x0a01,0x0601,0x0201,0x0034,0x0201,0x0050,0x0005,0x0201,
                0x0051,0x0015,0x0401,0x0201,0x0052,0x0025,0x0401,0x0201,0x0044,0x0053,0x0061,
                0x5a01,0x2401,0x1201,0x0a01,0x0601,0x0201,0x0035,0x0201,0x0060,0x0006,0x0201,
                0x0016,0x0062,0x0401,0x0201,0x0026,0x0054,0x0201,0x0045,0x0063,0x0a01,0x0601,
                0x0201,0x0036,0x0201,0x0070,0x0007,0x0201,0x0071,0x0055,0x0401,0x0201,0x0017,
                0x0064,0x0201,0x0072,0x0027,0x1801,0x1001,0x0801,0x0401,0x0201,0x0046,0x0073,
                0x0201,0x0037,0x0065,0x0401,0x0201,0x0056,0x0080,0x0201,0x0008,0x0074,0x0401,
                0x0201,0x0081,0x0018,0x0201,0x0082,0x0028,0x1001,0x0801,0x0401,0x0201,0x0047,
                0x0066,0x0201,0x0083,0x0038,0x0401,0x0201,0x0075,0x0057,0x0201,0x0084,0x0048,
                0x0601,0x0401,0x0201,0x0090,0x0019,0x0091,0x0401,0x0201,0x0092,0x0076,0x0201,
                0x0067,0x0029,0x5c01,0x2401,0x1201,0x0a01,0x0401,0x0201,0x0085,0x0058,0x0401,
                0x0201,0x0009,0x0077,0x0093,0x0401,0x0201,0x0039,0x0094,0x0201,0x0049,0x0086,
                0x0a01,0x0601,0x0201,0x0068,0x0201,0x00a0,0x000a,0x0201,0x00a1,0x001a,0x0401,
                0x0201,0x00a2,0x002a,0x0201,0x0095,0x0059,0x1a01,0x0e01,0x0601,0x0201,0x00a3,
                0x0201,0x003a,0x0087,0x0401,0x0201,0x0078,0x00a4,0x0201,0x004a,0x0096,0x0601,
                0x0401,0x0201,0x0069,0x00b0,0x00b1,0x0401,0x0201,0x001b,0x00a5,0x00b2,0x0e01,
                0x0801,0x0401,0x0201,0x005a,0x002b,0x0201,0x0088,0x0097,0x0201,0x00b3,0x0201,
                0x0079,0x003b,0x0801,0x0401,0x0201,0x006a,0x00b4,0x0201,0x004b,0x00c1,0x0401,
                0x0201,0x0098,0x0089,0x0201,0x001c,0x00b5,0x5001,0x2201,0x1001,0x0601,0x0401,
                0x0201,0x005b,0x002c,0x00c2,0x0601,0x0401,0x0201,0x000b,0x00c0,0x00a6,0x0201,
                0x00a7,0x007a,0x0a01,0x0401,0x0201,0x00c3,0x003c,0x0401,0x0201,0x000c,0x0099,
                0x00b6,0x0401,0x0201,0x006b,0x00c4,0x0201,0x004c,0x00a8,0x1401,0x0a01,0x0401,
                0x0201,0x008a,0x00c5,0x0401,0x0201,0x00d0,0x005c,0x00d1,0x0401,0x0201,0x00b7,
                0x007b,0x0201,0x001d,0x0201,0x000d,0x002d,0x0c01,0x0401,0x0201,0x00d2,0x00d3,
                0x0401,0x0201,0x003d,0x00c6,0x0201,0x006c,0x00a9,0x0601,0x0401,0x0201,0x009a,
                0x00b8,0x00d4,0x0401,0x0201,0x008b,0x004d,0x0201,0x00c7,0x007c,0x4401,0x2201,
                0x1201,0x0a01,0x0401,0x0201,0x00d5,0x005d,0x0401,0x0201,0x00e0,0x000e,0x00e1,
                0x0401,0x0201,0x001e,0x00e2,0x0201,0x00aa,0x002e,0x0801,0x0401,0x0201,0x00b9,
                0x009b,0x0201,0x00e3,0x00d6,0x0401,0x0201,0x006d,0x003e,0x0201,0x00c8,0x008c,
                0x1001,0x0801,0x0401,0x0201,0x00e4,0x004e,0x0201,0x00d7,0x007d,0x0401,0x0201,
                0x00e5,0x00ba,0x0201,0x00ab,0x005e,0x0801,0x0401,0x0201,0x00c9,0x009c,0x0201,
                0x00f1,0x001f,0x0601,0x0401,0x0201,0x00f0,0x006e,0x00f2,0x0201,0x002f,0x00e6,
                0x2601,0x1201,0x0801,0x0401,0x0201,0x00d8,0x00f3,0x0201,0x003f,0x00f4,0x0601,
                0x0201,0x004f,0x0201,0x008d,0x00d9,0x0201,0x00bb,0x00ca,0x0801,0x0401,0x0201,
                0x00ac,0x00e7,0x0201,0x007e,0x00f5,0x0801,0x0401,0x0201,0x009d,0x005f,0x0201,
                0x00e8,0x008e,0x0201,0x00f6,0x00cb,0x2201,0x1201,0x0a01,0x0601,0x0401,0x0201,
                0x000f,0x00ae,0x006f,0x0201,0x00bc,0x00da,0x0401,0x0201,0x00ad,0x00f7,0x0201,
                0x007f,0x00e9,0x0801,0x0401,0x0201,0x009e,0x00cc,0x0201,0x00f8,0x008f,0x0401,
                0x0201,0x00db,0x00bd,0x0201,0x00ea,0x00f9,0x1001,0x0801,0x0401,0x0201,0x009f,
                0x00dc,0x0201,0x00cd,0x00eb,0x0401,0x0201,0x00be,0x00fa,0x0201,0x00af,0x00dd,
                0x0e01,0x0601,0x0401,0x0201,0x00ec,0x00ce,0x00fb,0x0401,0x0201,0x00bf,0x00ed,
                0x0201,0x00de,0x00fc,0x0601,0x0401,0x0201,0x00cf,0x00fd,0x00ee,0x0401,0x0201,
                0x00df,0x00fe,0x0201,0x00ef,0x00ff,
            //},g_huffman_table_16[511] = {
                0x0201,0x0000,0x0601,0x0201,0x0010,0x0201,0x0001,0x0011,0x2a01,0x0801,0x0401,
                0x0201,0x0020,0x0002,0x0201,0x0021,0x0012,0x0a01,0x0601,0x0201,0x0022,0x0201,
                0x0030,0x0003,0x0201,0x0031,0x0013,0x0a01,0x0401,0x0201,0x0032,0x0023,0x0401,
                0x0201,0x0040,0x0004,0x0041,0x0601,0x0201,0x0014,0x0201,0x0033,0x0042,0x0401,
                0x0201,0x0024,0x0050,0x0201,0x0043,0x0034,0x8a01,0x2801,0x1001,0x0601,0x0401,
                0x0201,0x0005,0x0015,0x0051,0x0401,0x0201,0x0052,0x0025,0x0401,0x0201,0x0044,
                0x0035,0x0053,0x0a01,0x0601,0x0401,0x0201,0x0060,0x0006,0x0061,0x0201,0x0016,
                0x0062,0x0801,0x0401,0x0201,0x0026,0x0054,0x0201,0x0045,0x0063,0x0401,0x0201,
                0x0036,0x0070,0x0071,0x2801,0x1201,0x0801,0x0201,0x0017,0x0201,0x0007,0x0201,
                0x0055,0x0064,0x0401,0x0201,0x0072,0x0027,0x0401,0x0201,0x0046,0x0065,0x0073,
                0x0a01,0x0601,0x0201,0x0037,0x0201,0x0056,0x0008,0x0201,0x0080,0x0081,0x0601,
                0x0201,0x0018,0x0201,0x0074,0x0047,0x0201,0x0082,0x0201,0x0028,0x0066,0x1801,
                0x0e01,0x0801,0x0401,0x0201,0x0083,0x0038,0x0201,0x0075,0x0084,0x0401,0x0201,
                0x0048,0x0090,0x0091,0x0601,0x0201,0x0019,0x0201,0x0009,0x0076,0x0201,0x0092,
                0x0029,0x0e01,0x0801,0x0401,0x0201,0x0085,0x0058,0x0201,0x0093,0x0039,0x0401,
                0x0201,0x00a0,0x000a,0x001a,0x0801,0x0201,0x00a2,0x0201,0x0067,0x0201,0x0057,
                0x0049,0x0601,0x0201,0x0094,0x0201,0x0077,0x0086,0x0201,0x00a1,0x0201,0x0068,
                0x0095,0xdc01,0x7e01,0x3201,0x1a01,0x0c01,0x0601,0x0201,0x002a,0x0201,0x0059,
                0x003a,0x0201,0x00a3,0x0201,0x0087,0x0078,0x0801,0x0401,0x0201,0x00a4,0x004a,
                0x0201,0x0096,0x0069,0x0401,0x0201,0x00b0,0x000b,0x00b1,0x0a01,0x0401,0x0201,
                0x001b,0x00b2,0x0201,0x002b,0x0201,0x00a5,0x005a,0x0601,0x0201,0x00b3,0x0201,
                0x00a6,0x006a,0x0401,0x0201,0x00b4,0x004b,0x0201,0x000c,0x00c1,0x1e01,0x0e01,
                0x0601,0x0401,0x0201,0x00b5,0x00c2,0x002c,0x0401,0x0201,0x00a7,0x00c3,0x0201,
                0x006b,0x00c4,0x0801,0x0201,0x001d,0x0401,0x0201,0x0088,0x0097,0x003b,0x0401,
                0x0201,0x00d1,0x00d2,0x0201,0x002d,0x00d3,0x1201,0x0601,0x0401,0x0201,0x001e,
                0x002e,0x00e2,0x0601,0x0401,0x0201,0x0079,0x0098,0x00c0,0x0201,0x001c,0x0201,
                0x0089,0x005b,0x0e01,0x0601,0x0201,0x003c,0x0201,0x007a,0x00b6,0x0401,0x0201,
                0x004c,0x0099,0x0201,0x00a8,0x008a,0x0601,0x0201,0x000d,0x0201,0x00c5,0x005c,
                0x0401,0x0201,0x003d,0x00c6,0x0201,0x006c,0x009a,0x5801,0x5601,0x2401,0x1001,
                0x0801,0x0401,0x0201,0x008b,0x004d,0x0201,0x00c7,0x007c,0x0401,0x0201,0x00d5,
                0x005d,0x0201,0x00e0,0x000e,0x0801,0x0201,0x00e3,0x0401,0x0201,0x00d0,0x00b7,
                0x007b,0x0601,0x0401,0x0201,0x00a9,0x00b8,0x00d4,0x0201,0x00e1,0x0201,0x00aa,
                0x00b9,0x1801,0x0a01,0x0601,0x0401,0x0201,0x009b,0x00d6,0x006d,0x0201,0x003e,
                0x00c8,0x0601,0x0401,0x0201,0x008c,0x00e4,0x004e,0x0401,0x0201,0x00d7,0x00e5,
                0x0201,0x00ba,0x00ab,0x0c01,0x0401,0x0201,0x009c,0x00e6,0x0401,0x0201,0x006e,
                0x00d8,0x0201,0x008d,0x00bb,0x0801,0x0401,0x0201,0x00e7,0x009d,0x0201,0x00e8,
                0x008e,0x0401,0x0201,0x00cb,0x00bc,0x009e,0x00f1,0x0201,0x001f,0x0201,0x000f,
                0x002f,0x4201,0x3801,0x0201,0x00f2,0x3401,0x3201,0x1401,0x0801,0x0201,0x00bd,
                0x0201,0x005e,0x0201,0x007d,0x00c9,0x0601,0x0201,0x00ca,0x0201,0x00ac,0x007e,
                0x0401,0x0201,0x00da,0x00ad,0x00cc,0x0a01,0x0601,0x0201,0x00ae,0x0201,0x00db,
                0x00dc,0x0201,0x00cd,0x00be,0x0601,0x0401,0x0201,0x00eb,0x00ed,0x00ee,0x0601,
                0x0401,0x0201,0x00d9,0x00ea,0x00e9,0x0201,0x00de,0x0401,0x0201,0x00dd,0x00ec,
                0x00ce,0x003f,0x00f0,0x0401,0x0201,0x00f3,0x00f4,0x0201,0x004f,0x0201,0x00f5,
                0x005f,0x0a01,0x0201,0x00ff,0x0401,0x0201,0x00f6,0x006f,0x0201,0x00f7,0x007f,
                0x0c01,0x0601,0x0201,0x008f,0x0201,0x00f8,0x00f9,0x0401,0x0201,0x009f,0x00fa,
                0x00af,0x0801,0x0401,0x0201,0x00fb,0x00bf,0x0201,0x00fc,0x00cf,0x0401,0x0201,
                0x00fd,0x00df,0x0201,0x00fe,0x00ef,
            //},g_huffman_table_24[512] = {
                0x3c01,0x0801,0x0401,0x0201,0x0000,0x0010,0x0201,0x0001,0x0011,0x0e01,0x0601,
                0x0401,0x0201,0x0020,0x0002,0x0021,0x0201,0x0012,0x0201,0x0022,0x0201,0x0030,
                0x0003,0x0e01,0x0401,0x0201,0x0031,0x0013,0x0401,0x0201,0x0032,0x0023,0x0401,
                0x0201,0x0040,0x0004,0x0041,0x0801,0x0401,0x0201,0x0014,0x0033,0x0201,0x0042,
                0x0024,0x0601,0x0401,0x0201,0x0043,0x0034,0x0051,0x0601,0x0401,0x0201,0x0050,
                0x0005,0x0015,0x0201,0x0052,0x0025,0xfa01,0x6201,0x2201,0x1201,0x0a01,0x0401,
                0x0201,0x0044,0x0053,0x0201,0x0035,0x0201,0x0060,0x0006,0x0401,0x0201,0x0061,
                0x0016,0x0201,0x0062,0x0026,0x0801,0x0401,0x0201,0x0054,0x0045,0x0201,0x0063,
                0x0036,0x0401,0x0201,0x0071,0x0055,0x0201,0x0064,0x0046,0x2001,0x0e01,0x0601,
                0x0201,0x0072,0x0201,0x0027,0x0037,0x0201,0x0073,0x0401,0x0201,0x0070,0x0007,
                0x0017,0x0a01,0x0401,0x0201,0x0065,0x0056,0x0401,0x0201,0x0080,0x0008,0x0081,
                0x0401,0x0201,0x0074,0x0047,0x0201,0x0018,0x0082,0x1001,0x0801,0x0401,0x0201,
                0x0028,0x0066,0x0201,0x0083,0x0038,0x0401,0x0201,0x0075,0x0057,0x0201,0x0084,
                0x0048,0x0801,0x0401,0x0201,0x0091,0x0019,0x0201,0x0092,0x0076,0x0401,0x0201,
                0x0067,0x0029,0x0201,0x0085,0x0058,0x5c01,0x2201,0x1001,0x0801,0x0401,0x0201,
                0x0093,0x0039,0x0201,0x0094,0x0049,0x0401,0x0201,0x0077,0x0086,0x0201,0x0068,
                0x00a1,0x0801,0x0401,0x0201,0x00a2,0x002a,0x0201,0x0095,0x0059,0x0401,0x0201,
                0x00a3,0x003a,0x0201,0x0087,0x0201,0x0078,0x004a,0x1601,0x0c01,0x0401,0x0201,
                0x00a4,0x0096,0x0401,0x0201,0x0069,0x00b1,0x0201,0x001b,0x00a5,0x0601,0x0201,
                0x00b2,0x0201,0x005a,0x002b,0x0201,0x0088,0x00b3,0x1001,0x0a01,0x0601,0x0201,
                0x0090,0x0201,0x0009,0x00a0,0x0201,0x0097,0x0079,0x0401,0x0201,0x00a6,0x006a,
                0x00b4,0x0c01,0x0601,0x0201,0x001a,0x0201,0x000a,0x00b0,0x0201,0x003b,0x0201,
                0x000b,0x00c0,0x0401,0x0201,0x004b,0x00c1,0x0201,0x0098,0x0089,0x4301,0x2201,
                0x1001,0x0801,0x0401,0x0201,0x001c,0x00b5,0x0201,0x005b,0x00c2,0x0401,0x0201,
                0x002c,0x00a7,0x0201,0x007a,0x00c3,0x0a01,0x0601,0x0201,0x003c,0x0201,0x000c,
                0x00d0,0x0201,0x00b6,0x006b,0x0401,0x0201,0x00c4,0x004c,0x0201,0x0099,0x00a8,
                0x1001,0x0801,0x0401,0x0201,0x008a,0x00c5,0x0201,0x005c,0x00d1,0x0401,0x0201,
                0x00b7,0x007b,0x0201,0x001d,0x00d2,0x0901,0x0401,0x0201,0x002d,0x00d3,0x0201,
                0x003d,0x00c6,0x55fa,0x0401,0x0201,0x006c,0x00a9,0x0201,0x009a,0x00d4,0x2001,
                0x1001,0x0801,0x0401,0x0201,0x00b8,0x008b,0x0201,0x004d,0x00c7,0x0401,0x0201,
                0x007c,0x00d5,0x0201,0x005d,0x00e1,0x0801,0x0401,0x0201,0x001e,0x00e2,0x0201,
                0x00aa,0x00b9,0x0401,0x0201,0x009b,0x00e3,0x0201,0x00d6,0x006d,0x1401,0x0a01,
                0x0601,0x0201,0x003e,0x0201,0x002e,0x004e,0x0201,0x00c8,0x008c,0x0401,0x0201,
                0x00e4,0x00d7,0x0401,0x0201,0x007d,0x00ab,0x00e5,0x0a01,0x0401,0x0201,0x00ba,
                0x005e,0x0201,0x00c9,0x0201,0x009c,0x006e,0x0801,0x0201,0x00e6,0x0201,0x000d,
                0x0201,0x00e0,0x000e,0x0401,0x0201,0x00d8,0x008d,0x0201,0x00bb,0x00ca,0x4a01,
                0x0201,0x00ff,0x4001,0x3a01,0x2001,0x1001,0x0801,0x0401,0x0201,0x00ac,0x00e7,
                0x0201,0x007e,0x00d9,0x0401,0x0201,0x009d,0x00e8,0x0201,0x008e,0x00cb,0x0801,
                0x0401,0x0201,0x00bc,0x00da,0x0201,0x00ad,0x00e9,0x0401,0x0201,0x009e,0x00cc,
                0x0201,0x00db,0x00bd,0x1001,0x0801,0x0401,0x0201,0x00ea,0x00ae,0x0201,0x00dc,
                0x00cd,0x0401,0x0201,0x00eb,0x00be,0x0201,0x00dd,0x00ec,0x0801,0x0401,0x0201,
                0x00ce,0x00ed,0x0201,0x00de,0x00ee,0x000f,0x0401,0x0201,0x00f0,0x001f,0x00f1,
                0x0401,0x0201,0x00f2,0x002f,0x0201,0x00f3,0x003f,0x1201,0x0801,0x0401,0x0201,
                0x00f4,0x004f,0x0201,0x00f5,0x005f,0x0401,0x0201,0x00f6,0x006f,0x0201,0x00f7,
                0x0201,0x007f,0x008f,0x0a01,0x0401,0x0201,0x00f8,0x00f9,0x0401,0x0201,0x009f,
                0x00af,0x00fa,0x0801,0x0401,0x0201,0x00fb,0x00bf,0x0201,0x00fc,0x00cf,0x0401,
                0x0201,0x00fd,0x00df,0x0201,0x00fe,0x00ef,
            //},g_huffman_table_32[31] = {
                0x0201,0x0000,0x0801,0x0401,0x0201,0x0008,0x0004,0x0201,0x0001,0x0002,0x0801,
                0x0401,0x0201,0x000c,0x000a,0x0201,0x0003,0x0006,0x0601,0x0201,0x0009,0x0201,
                0x0005,0x0007,0x0401,0x0201,0x000e,0x000d,0x0201,0x000f,0x000b,
            //},g_huffman_table_33[31] = {
                0x1001,0x0801,0x0401,0x0201,0x0000,0x0001,0x0201,0x0002,0x0003,0x0401,0x0201,
                0x0004,0x0005,0x0201,0x0006,0x0007,0x0801,0x0401,0x0201,0x0008,0x0009,0x0201,
                0x000a,0x000b,0x0401,0x0201,0x000c,0x000d,0x0201,0x000e,0x000f,
            };

            //typedef struct hufftables
            //{
            //    const unsigned short* hufftable;
            //    uint16_t treelen;
            //    uint8_t linbits;
            //}
            //hufftables;
            public class HuffmanTable
            {
                public ushort[] Table { get; set; }
                public ushort TreeLen { get; set; }
                public byte LinBits { get; set; }
            }
            
            HuffmanTable GetHuffmanTable(uint offset, uint count, byte linBits)
            {
                ushort[] table = new ushort[count];
                for(uint z=0 ; z < count; z++)
                {
                    table[z] = huffman_table[offset + z];
                }
                return new HuffmanTable()
                {
                    Table = table, TreeLen = (ushort)count, LinBits = linBits
                };
            }

            public HuffmanTables()
            {
                Init();
            }

            public HuffmanTable[] AllTables;

            public void Init()
            {
                AllTables = new HuffmanTable[]
                {
                    GetHuffmanTable(0, 0, 0 ),  /* Table  0 */
                    GetHuffmanTable(0, 7, 0 ),  /* Table  1 */
                    GetHuffmanTable( +7    , 17, 0 ),  /* Table  2 */
                    GetHuffmanTable( +24   , 17, 0 ),  /* Table  3 */
                    GetHuffmanTable(0,  0, 0 ),  /* Table  4 */
                    GetHuffmanTable( +41   , 31, 0 ),  /* Table  5 */
                    GetHuffmanTable( +72   , 31, 0 ),  /* Table  6 */
                    GetHuffmanTable( +103  , 71, 0 ),  /* Table  7 */
                    GetHuffmanTable( +174  , 71, 0 ),  /* Table  8 */
                    GetHuffmanTable( +245  , 71, 0 ),  /* Table  9 */
                    GetHuffmanTable( +316  ,127, 0 ),  /* Table 10 */
                    GetHuffmanTable( +443  ,127, 0 ),  /* Table 11 */
                    GetHuffmanTable( +570  ,127, 0 ),  /* Table 12 */
                    GetHuffmanTable( +697  ,511, 0 ),  /* Table 13 */
                    GetHuffmanTable(0,  0, 0 ),  /* Table 14 */
                    GetHuffmanTable( +1208 ,511, 0 ),  /* Table 15 */
                    GetHuffmanTable( +1719 ,511, 1 ),  /* Table 16 */
                    GetHuffmanTable( +1719 ,511, 2 ),  /* Table 17 */
                    GetHuffmanTable( +1719 ,511, 3 ),  /* Table 18 */
                    GetHuffmanTable( +1719 ,511, 4 ),  /* Table 19 */
                    GetHuffmanTable( +1719 ,511, 6 ),  /* Table 20 */
                    GetHuffmanTable( +1719 ,511, 8 ),  /* Table 21 */
                    GetHuffmanTable( +1719 ,511,10 ),  /* Table 22 */
                    GetHuffmanTable( +1719 ,511,13 ),  /* Table 23 */
                    GetHuffmanTable( +2230 ,512, 4 ),  /* Table 24 */
                    GetHuffmanTable( +2230 ,512, 5 ),  /* Table 25 */
                    GetHuffmanTable( +2230 ,512, 6 ),  /* Table 26 */
                    GetHuffmanTable( +2230 ,512, 7 ),  /* Table 27 */
                    GetHuffmanTable( +2230 ,512, 8 ),  /* Table 28 */
                    GetHuffmanTable( +2230 ,512, 9 ),  /* Table 29 */
                    GetHuffmanTable( +2230 ,512,11 ),  /* Table 30 */
                    GetHuffmanTable( +2230 ,512,13 ),  /* Table 31 */
                    GetHuffmanTable( +2742 , 31, 0 ),  /* Table 32 */
                    GetHuffmanTable( +2261 , 31, 0 )  /* Table 33 */
                };
            }
        }

        MP3FrameHeader ReadHeader(BitReader reader)
        {
            uint codeSync = (uint)reader.ReadBitsToCode(11);
            uint mpegAudioVersion = (uint)reader.ReadBitsToCode(2);
            uint layerVersion = (uint)reader.ReadBitsToCode(2);
            uint protectionBit = (uint)reader.ReadBitsToCode(1);
            uint bitRateIndex = (uint)reader.ReadBitsToCode(4);
            uint samplingRate = (uint)reader.ReadBitsToCode(2);
            uint paddingBit = (uint)reader.ReadBitsToCode(1);
            uint privateBit = (uint)reader.ReadBitsToCode(1);
            uint channelMode = (uint)reader.ReadBitsToCode(2);
            uint modeExtensions = (uint)reader.ReadBitsToCode(2);
            uint copyright = (uint)reader.ReadBitsToCode(1);
            uint original = (uint)reader.ReadBitsToCode(1);
            uint emphasis = (uint)reader.ReadBitsToCode(2);
            ulong crc = 0;

            if (protectionBit == 0)
            {
                crc = reader.ReadBitsToCode(16);
            }

            var result = new MP3FrameHeader()
            {
                BitRateLookup = (int)bitRateIndex,
                ChannelMode = (MP3FrameHeader.ChannelModeType)channelMode,
                Copyright = (copyright == 1) ? true : false,
                CRC = crc,
                Emphasis = (MP3FrameHeader.EmphasisType)emphasis,
                LayerDescription = (MP3FrameHeader.LayerDescriptionType)layerVersion,
                MPEGAudioVersion = (MP3FrameHeader.MPEGAudioVersionType)mpegAudioVersion,
                Original = (original == 1) ? true : false,
                ModeExtensionLookup = (int)modeExtensions,
                PaddingBit = (paddingBit == 1) ? true : false,
                PrivateBit = (privateBit == 1) ? true : false,
                ProtectionBit = (protectionBit == 1) ? true : false,
                SamplingRateLookupIndex = (int)samplingRate,
                CodeSync = codeSync
            };

            if (result.BitRate == -1 || result.SamplingRateFrequency == -1 || result.MPEGAudioVersion == MP3FrameHeader.MPEGAudioVersionType.Reserved || result.LayerDescription == MP3FrameHeader.LayerDescriptionType.Reserved)
            {
                result.IsValid = false;
                result.BitrateCoding = MP3FrameHeader.BitrateCodingType.NotApplicable;
            }
            else
            {
                result.IsValid = true;
                if (result.BitRate == 0)
                    result.BitrateCoding = MP3FrameHeader.BitrateCodingType.VBR;
                else
                    result.BitrateCoding = MP3FrameHeader.BitrateCodingType.CBR;
            }

            if (result.BitrateCoding == MP3FrameHeader.BitrateCodingType.CBR)
            {
                if (result.LayerDescription == MP3FrameHeader.LayerDescriptionType.Layer1)
                {
                    result.FrameLength = (12 * (result.BitRate * 1000) / result.SamplingRateFrequency + (result.PaddingBit == true ? 1 : 0)) * 4;
                    result.FrameData = reader.ReadBytes(result.FrameLength - 4);
                }
                else if (result.LayerDescription == MP3FrameHeader.LayerDescriptionType.Layer2 || result.LayerDescription == MP3FrameHeader.LayerDescriptionType.Layer3)
                {
                    result.FrameLength = 144 * (result.BitRate * 1000) / result.SamplingRateFrequency + (result.PaddingBit == true ? 1 : 0);
                    result.FrameData = reader.ReadBytes(result.FrameLength - 4);
                }
            }

            return result;
        }

        void ReadID3v2Tag(BitReader reader)
        {
            string tag = Encoding.Default.GetString(reader.ReadBytes(3));
            if (tag.ToUpper() != "ID3" && tag != "TAG")
            {
                reader.Position -= 3 * 8;
                return;
            }

            byte majorVersion = reader.ReadByte();
            byte minorVersion = reader.ReadByte();

            byte unsynchronization = (byte)reader.ReadBitInt();
            byte extendedHeader = (byte)reader.ReadBitInt();
            byte experimentalIndicator = (byte)reader.ReadBitInt();
            byte extraBits = (byte)reader.ReadBitsToCode(5);

            byte[] sizeBytes = reader.ReadBytes(4);

            ulong iSize = (ulong)sizeBytes[0] << 21 | (ulong)sizeBytes[1] << 14 | (ulong)sizeBytes[2] << 7 | (ulong)sizeBytes[3];
            Console.WriteLine("Frame Data Size : " + iSize.ToString());

            ulong dataSize = (ulong)iSize;
            ulong dataCounter = dataSize;

            while (dataCounter > 0)
            {
                Console.WriteLine(dataSize - dataCounter);
                string frameId = Encoding.Default.GetString(reader.ReadBytes(4));

                if (frameId == "\0\0\0\0")
                {
                    dataCounter -= 4;
                    byte[] extraData = reader.ReadBytes((int)dataCounter);
                    dataCounter = 0;
                }
                else
                {
                    uint frameSize = (uint)reader.ReadBitsToCode(32);
                    reader.Position -= 32;
                    byte[] frameSizeBytes = reader.ReadBytes(4);
                    ulong frameSize2 = (ulong)frameSizeBytes[0] << 21 | (ulong)frameSizeBytes[1] << 14 | (ulong)frameSizeBytes[2] << 7 | (ulong)frameSizeBytes[3];

                    uint test1 = (uint)frameSizeBytes.GetULongBE(0);
                    uint test2 = (uint)frameSizeBytes.GetULongLE(0);

                    Console.WriteLine($"{frameSize} - {frameSize2}");

                    byte tagAlterPreservation = (byte)reader.ReadBitInt();
                    byte fileAlterPreservation = (byte)reader.ReadBitInt();
                    byte frameReadOnly = (byte)reader.ReadBitInt();
                    byte extraBitsFrameFlag1 = (byte)reader.ReadBitsToCode(5);

                    byte compression = (byte)reader.ReadBitInt();
                    byte encryption = (byte)reader.ReadBitInt();
                    byte groupingIdentity = (byte)reader.ReadBitInt();
                    byte extraBitsFrameFlag2 = (byte)reader.ReadBitsToCode(5);

                    byte[] frameData = reader.ReadBytes((int)frameSize);
                    dataCounter -= (frameSize + 10);
                    Console.WriteLine(frameId);
                    if (frameData.Length != 0)
                    {
                        if (frameId == "APIC")
                        {
                            BitReader reader2 = new BitReader(frameData, true);
                            byte encoding = reader2.ReadByte();
                            Encoding encoding1 = Encoding.Default;
                            if (encoding == 1)
                            {
                                encoding1 = Encoding.Unicode;
                            }
                            bool done = false;
                            List<byte> bytes = new List<byte>();
                            while(!done)
                            {
                                byte b = reader2.ReadByte();
                                if (b != 0)
                                    bytes.Add(b);
                                else
                                    done = true;
                            }
                            string imageType = encoding1.GetString(bytes.ToArray());
                            byte pictureType = reader2.ReadByte();
                            done = false;
                            bytes = new List<byte>();
                            while (!done)
                            {
                                byte b = reader2.ReadByte();
                                if (b != 0)
                                    bytes.Add(b);
                                else
                                    done = true;
                            }
                            string description = encoding1.GetString(bytes.ToArray());
                            byte[] pictureData = reader2.ReadBytes(reader2.Length / 8 - reader2.Position / 8);
                            JPEG jpeg = new JPEG(pictureData);
                        }
                        else if (frameData[0] == 1)
                        {
                            if (frameId == "COMM")
                            {
                                string language = Encoding.Default.GetString(frameData.GetSublength(1, 3));
                                bool done = false;
                                int idx = 4;
                                int idxStart = idx;
                                while (!done)
                                {
                                    byte[] dataComment = frameData.GetSublength(idx, 2);
                                    idx += 2;
                                    if (dataComment[0] == 0x00 && dataComment[1] == 0x00)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                string strDesc = Encoding.Unicode.GetString(frameData.GetSublength(idxStart, idx - idxStart));
                                done = false;
                                idxStart = idx;
                                while (!done)
                                {
                                    byte[] dataComment = frameData.GetSublength(idx, 2);
                                    idx += 2;
                                    if (dataComment[0] == 0x00 && dataComment[1] == 0x00)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                string strText = Encoding.Unicode.GetString(frameData.GetSublength(idxStart, idx - idxStart));

                                Console.WriteLine($"Comment: Desc:{strDesc.Substring(1)}, Text:{strText.Substring(1)}");
                            }
                            else
                            {
                                string strFrameData = Encoding.Unicode.GetString(frameData.GetSublength(1, frameData.Length - 1));
                                Console.OutputEncoding = Encoding.Unicode;
                                Console.WriteLine("String Data:" + strFrameData.Substring(1));
                            }
                        }
                        else
                        {
                            Console.OutputEncoding = Encoding.Default;
                            Console.WriteLine("Check the data here");
                        }
                    }
                }                
            }
        }

        void ReadID3Tag(BitReader reader, bool skipTag = true, bool readFromEnd = false)
        {
            if (readFromEnd)
            {
                reader.Position = reader.Length - 128*8;
            }

            string tag = string.Empty;
            if (!skipTag)
            {
                tag = Encoding.Default.GetString(reader.ReadBytes(3));
            }

            string title = Encoding.Default.GetString(reader.ReadBytes(30));
            string artist = Encoding.Default.GetString(reader.ReadBytes(30));
            string album = Encoding.Default.GetString(reader.ReadBytes(30));
            string year = Encoding.Default.GetString(reader.ReadBytes(4));
            string comment = Encoding.Default.GetString(reader.ReadBytes(30));
            byte genre = reader.ReadByte();
        }

        void ReadInfoHeader(MP3FrameHeader header)
        {
            var firstFrame = header;
            BitReader b2 = new BitReader(firstFrame.FrameData, true);
            var mono = (firstFrame.ChannelMode == MP3FrameHeader.ChannelModeType.SingleChannelMono);
            var sideInformation = ReadSideInformation(header, ref b2);
            string xingTag = Encoding.Default.GetString(b2.ReadBytes(4));

            int headerFlags = (int)b2.ReadBitsToCode(32);
            ulong frameSize, streamSize;
            int vbrScale;
            byte[] numTocEntries;
            if ((headerFlags & 0x0001) != 0)
                frameSize = b2.ReadBytes(4).GetUIntBE(0);
            if ((headerFlags & 0x0002) != 0)
                streamSize = b2.ReadBytes(4).GetUIntBE(0);
            if ((headerFlags & 0x0004) != 0)
                numTocEntries = b2.ReadBytes(100);
            if ((headerFlags & 0x0008) != 0)
                vbrScale = (int)b2.ReadBytes(4).GetUIntBE(0);
            string encoder = Encoding.Default.GetString(b2.ReadBytes(9));
            ulong infoTagVersion = b2.ReadBitsToCode(4);
            ulong vbrMethod = b2.ReadBitsToCode(4);
            byte lowPassFilterValue = b2.ReadByte();
            ulong peakSignalAmplitude = b2.ReadBitsToCode(32);
            ulong radioReplayGain = b2.ReadBitsToCode(16);
            ulong audiophileReplayGain = b2.ReadBitsToCode(16);
            ulong encodingFlags = b2.ReadBitsToCode(4);
            ulong lameAthType = b2.ReadBitsToCode(4);
            ulong abrSpecifiedBiteRateElseMinimalBitrate = b2.ReadByte();
            ulong encoderDelaysX = b2.ReadBitsToCode(12);
            ulong encoderDelaysY = b2.ReadBitsToCode(12);
            byte misc = b2.ReadByte();
            byte mp3Gain = b2.ReadByte();
            ulong unused = b2.ReadBitsToCode(2);
            ulong surroundInfo = b2.ReadBitsToCode(3);
            ulong presetUsed = b2.ReadBitsToCode(11);
            byte[] musicLength = b2.ReadBytes(4);
            ulong totalMusicLength = musicLength.GetULongBE(0);
            byte[] musicCRC = b2.ReadBytes(2);
            byte[] crcInfoTag = b2.ReadBytes(2);

            byte[] bytesLeft = b2.ReadBytes(b2.Length / 8 - b2.Position / 8 - 1);
        }

        int SeekPoint(byte[] TOC, int file_bytes, float percent)
        {
            // interpolate in TOC to get file seek point in bytes
            int a, seekpoint;
            float fa, fb, fx;

            if (percent < 0.0f) percent = 0.0f;
            if (percent > 100.0f) percent = 100.0f;

            a = (int)percent;
            if (a > 99) a = 99;
            fa = TOC[a];
            if (a < 99)
            {
                fb = TOC[a + 1];
            }
            else
            {
                fb = 256.0f;
            }

            fx = fa + (fb - fa) * (percent - a);
            seekpoint = (int)((1.0f / 256.0f) * fx * file_bytes);
            return seekpoint;
        }

        SideInformation ReadSideInformation(MP3FrameHeader header, ref BitReader reader)
        {
            SideInformation side = new SideInformation();

            side.MainData = reader.ReadBitsToCode(9);

            int granules = 2;
            int channels = 1;

            if (header.ChannelMode == MP3FrameHeader.ChannelModeType.SingleChannelMono)
            {
                channels = 1;
                side.PrivateBits = reader.ReadBitsToCode(5);
                var elements = reader.ReadBits(4);
                side.ScaleFactorSelectionInformation = new bool[1][];
                side.ScaleFactorSelectionInformation[0] = new bool[4];
                for(int z=0;z<4;z++)
                {
                    side.ScaleFactorSelectionInformation[0][z] = elements[z].ValueBoolean;
                }
            }
            else
            {
                channels = 2;
                side.PrivateBits = reader.ReadBitsToCode(3);
                var elements = reader.ReadBits(4);
                var elements2 = reader.ReadBits(4);
                side.ScaleFactorSelectionInformation = new bool[2][];
                side.ScaleFactorSelectionInformation[0] = new bool[4];
                side.ScaleFactorSelectionInformation[1] = new bool[4];
                for (int z = 0; z < 4; z++)
                {
                    side.ScaleFactorSelectionInformation[0][z] = elements[z].ValueBoolean;
                    side.ScaleFactorSelectionInformation[1][z] = elements2[z].ValueBoolean;
                }
            }

            side.Granules = new SideInformation.Granule[granules];
            for(int gr=0; gr<granules; gr++)
            {
                side.Granules[gr] = new SideInformation.Granule();
                side.Granules[gr].Channels = new SideInformation.Granule.Channel[channels];
                for(int ch=0; ch<channels; ch++)
                {
                    side.Granules[gr].Channels[ch] = new SideInformation.Granule.Channel();
                    side.Granules[gr].Channels[ch].Part2_3_Length = reader.ReadBitsToCode(12);
                    side.Granules[gr].Channels[ch].BigValues = reader.ReadBitsToCode(9);
                    side.Granules[gr].Channels[ch].GlobalGain = reader.ReadBitsToCode(8);
                    side.Granules[gr].Channels[ch].ScaleFactor_Compress = reader.ReadBitsToCode(4);
                    side.Granules[gr].Channels[ch].WindowsSwitchingFlag = reader.ReadBitsToCode(1);
                    if (side.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1)
                    {
                        side.Granules[gr].Channels[ch].BlockType = reader.ReadBitsToCode(2);
                        side.Granules[gr].Channels[ch].MixedBlockFlag = reader.ReadBitsToCode(1);
                        side.Granules[gr].Channels[ch].TableSelect = new ulong[2];
                        for (int region = 0; region < 2; region++) {
                            side.Granules[gr].Channels[ch].TableSelect[region] = reader.ReadBitsToCode(5);
                        }
                        side.Granules[gr].Channels[ch].SubBlockGain = new ulong[3];
                        for (int window = 0; window < 3; window++) {
                            side.Granules[gr].Channels[ch].SubBlockGain[window] = reader.ReadBitsToCode(3);
                        }

                        if (side.Granules[gr].Channels[ch].BlockType == 2 && side.Granules[gr].Channels[ch].MixedBlockFlag == 0) {
                            side.Granules[gr].Channels[ch].Region0Count = 8;
                        }
                        else
                        {
                            side.Granules[gr].Channels[ch].Region0Count = 7;
                        }
                        side.Granules[gr].Channels[ch].Region1Count = 20 - side.Granules[gr].Channels[ch].Region0Count;
                    }
                    else
                    {
                        side.Granules[gr].Channels[ch].TableSelect = new ulong[3];
                        for (int region = 0; region < 3; region++) {
                            side.Granules[gr].Channels[ch].TableSelect[region] = reader.ReadBitsToCode(5);
                        }
                        side.Granules[gr].Channels[ch].Region0Count = reader.ReadBitsToCode(4);
                        side.Granules[gr].Channels[ch].Region1Count = reader.ReadBitsToCode(3);
                        side.Granules[gr].Channels[ch].BlockType = 0;
                    }
                    side.Granules[gr].Channels[ch].Preflag = reader.ReadBitsToCode(1);
                    side.Granules[gr].Channels[ch].ScaleFactorScale = reader.ReadBitsToCode(1);
                    side.Granules[gr].Channels[ch].Count1TableSelect = reader.ReadBitsToCode(1);
                }
            }

            return side;
        }

        int GetMainPos(ref BitReader reader)
        {
            return reader.Position;
        }

        void SetMainPos(ref BitReader reader, uint position)
        {
            reader.Position = (int)position;
        }

        float C_PI = 3.14159265358979323846f;
        float C_INV_SQRT_2 = 0.70710678118654752440f;
        // ci[8]={-0.6,-0.535,-0.33,-0.185,-0.095,-0.041,-0.0142,-0.0037},
        float[] cs = { 0.857493f, 0.881742f, 0.949629f, 0.983315f, 0.995518f, 0.999161f, 0.999899f, 0.999993f };
        float[] ca = { -0.514496f, -0.471732f, -0.313377f, -0.181913f, -0.094574f, -0.040966f, -0.014199f, -0.003700f };
        float[] is_ratios = { 0.000000f, 0.267949f, 0.577350f, 1.000000f, 1.732051f, 3.732051f };
        double [] g_synth_dtbl = {
           0.000000000,-0.000015259,-0.000015259,-0.000015259,
          -0.000015259,-0.000015259,-0.000015259,-0.000030518,
          -0.000030518,-0.000030518,-0.000030518,-0.000045776,
          -0.000045776,-0.000061035,-0.000061035,-0.000076294,
          -0.000076294,-0.000091553,-0.000106812,-0.000106812,
          -0.000122070,-0.000137329,-0.000152588,-0.000167847,
          -0.000198364,-0.000213623,-0.000244141,-0.000259399,
          -0.000289917,-0.000320435,-0.000366211,-0.000396729,
          -0.000442505,-0.000473022,-0.000534058,-0.000579834,
          -0.000625610,-0.000686646,-0.000747681,-0.000808716,
          -0.000885010,-0.000961304,-0.001037598,-0.001113892,
          -0.001205444,-0.001296997,-0.001388550,-0.001480103,
          -0.001586914,-0.001693726,-0.001785278,-0.001907349,
          -0.002014160,-0.002120972,-0.002243042,-0.002349854,
          -0.002456665,-0.002578735,-0.002685547,-0.002792358,
          -0.002899170,-0.002990723,-0.003082275,-0.003173828,
           0.003250122, 0.003326416, 0.003387451, 0.003433228,
           0.003463745, 0.003479004, 0.003479004, 0.003463745,
           0.003417969, 0.003372192, 0.003280640, 0.003173828,
           0.003051758, 0.002883911, 0.002700806, 0.002487183,
           0.002227783, 0.001937866, 0.001617432, 0.001266479,
           0.000869751, 0.000442505,-0.000030518,-0.000549316,
          -0.001098633,-0.001693726,-0.002334595,-0.003005981,
          -0.003723145,-0.004486084,-0.005294800,-0.006118774,
          -0.007003784,-0.007919312,-0.008865356,-0.009841919,
          -0.010848999,-0.011886597,-0.012939453,-0.014022827,
          -0.015121460,-0.016235352,-0.017349243,-0.018463135,
          -0.019577026,-0.020690918,-0.021789551,-0.022857666,
          -0.023910522,-0.024932861,-0.025909424,-0.026840210,
          -0.027725220,-0.028533936,-0.029281616,-0.029937744,
          -0.030532837,-0.031005859,-0.031387329,-0.031661987,
          -0.031814575,-0.031845093,-0.031738281,-0.031478882,
           0.031082153, 0.030517578, 0.029785156, 0.028884888,
           0.027801514, 0.026535034, 0.025085449, 0.023422241,
           0.021575928, 0.019531250, 0.017257690, 0.014801025,
           0.012115479, 0.009231567, 0.006134033, 0.002822876,
          -0.000686646,-0.004394531,-0.008316040,-0.012420654,
          -0.016708374,-0.021179199,-0.025817871,-0.030609131,
          -0.035552979,-0.040634155,-0.045837402,-0.051132202,
          -0.056533813,-0.061996460,-0.067520142,-0.073059082,
          -0.078628540,-0.084182739,-0.089706421,-0.095169067,
          -0.100540161,-0.105819702,-0.110946655,-0.115921021,
          -0.120697021,-0.125259399,-0.129562378,-0.133590698,
          -0.137298584,-0.140670776,-0.143676758,-0.146255493,
          -0.148422241,-0.150115967,-0.151306152,-0.151962280,
          -0.152069092,-0.151596069,-0.150497437,-0.148773193,
          -0.146362305,-0.143264771,-0.139450073,-0.134887695,
          -0.129577637,-0.123474121,-0.116577148,-0.108856201,
           0.100311279, 0.090927124, 0.080688477, 0.069595337,
           0.057617188, 0.044784546, 0.031082153, 0.016510010,
           0.001068115,-0.015228271,-0.032379150,-0.050354004,
          -0.069168091,-0.088775635,-0.109161377,-0.130310059,
          -0.152206421,-0.174789429,-0.198059082,-0.221984863,
          -0.246505737,-0.271591187,-0.297210693,-0.323318481,
          -0.349868774,-0.376800537,-0.404083252,-0.431655884,
          -0.459472656,-0.487472534,-0.515609741,-0.543823242,
          -0.572036743,-0.600219727,-0.628295898,-0.656219482,
          -0.683914185,-0.711318970,-0.738372803,-0.765029907,
          -0.791213989,-0.816864014,-0.841949463,-0.866363525,
          -0.890090942,-0.913055420,-0.935195923,-0.956481934,
          -0.976852417,-0.996246338,-1.014617920,-1.031936646,
          -1.048156738,-1.063217163,-1.077117920,-1.089782715,
          -1.101211548,-1.111373901,-1.120223999,-1.127746582,
          -1.133926392,-1.138763428,-1.142211914,-1.144287109,
           1.144989014, 1.144287109, 1.142211914, 1.138763428,
           1.133926392, 1.127746582, 1.120223999, 1.111373901,
           1.101211548, 1.089782715, 1.077117920, 1.063217163,
           1.048156738, 1.031936646, 1.014617920, 0.996246338,
           0.976852417, 0.956481934, 0.935195923, 0.913055420,
           0.890090942, 0.866363525, 0.841949463, 0.816864014,
           0.791213989, 0.765029907, 0.738372803, 0.711318970,
           0.683914185, 0.656219482, 0.628295898, 0.600219727,
           0.572036743, 0.543823242, 0.515609741, 0.487472534,
           0.459472656, 0.431655884, 0.404083252, 0.376800537,
           0.349868774, 0.323318481, 0.297210693, 0.271591187,
           0.246505737, 0.221984863, 0.198059082, 0.174789429,
           0.152206421, 0.130310059, 0.109161377, 0.088775635,
           0.069168091, 0.050354004, 0.032379150, 0.015228271,
          -0.001068115,-0.016510010,-0.031082153,-0.044784546,
          -0.057617188,-0.069595337,-0.080688477,-0.090927124,
           0.100311279, 0.108856201, 0.116577148, 0.123474121,
           0.129577637, 0.134887695, 0.139450073, 0.143264771,
           0.146362305, 0.148773193, 0.150497437, 0.151596069,
           0.152069092, 0.151962280, 0.151306152, 0.150115967,
           0.148422241, 0.146255493, 0.143676758, 0.140670776,
           0.137298584, 0.133590698, 0.129562378, 0.125259399,
           0.120697021, 0.115921021, 0.110946655, 0.105819702,
           0.100540161, 0.095169067, 0.089706421, 0.084182739,
           0.078628540, 0.073059082, 0.067520142, 0.061996460,
           0.056533813, 0.051132202, 0.045837402, 0.040634155,
           0.035552979, 0.030609131, 0.025817871, 0.021179199,
           0.016708374, 0.012420654, 0.008316040, 0.004394531,
           0.000686646,-0.002822876,-0.006134033,-0.009231567,
          -0.012115479,-0.014801025,-0.017257690,-0.019531250,
          -0.021575928,-0.023422241,-0.025085449,-0.026535034,
          -0.027801514,-0.028884888,-0.029785156,-0.030517578,
           0.031082153, 0.031478882, 0.031738281, 0.031845093,
           0.031814575, 0.031661987, 0.031387329, 0.031005859,
           0.030532837, 0.029937744, 0.029281616, 0.028533936,
           0.027725220, 0.026840210, 0.025909424, 0.024932861,
           0.023910522, 0.022857666, 0.021789551, 0.020690918,
           0.019577026, 0.018463135, 0.017349243, 0.016235352,
           0.015121460, 0.014022827, 0.012939453, 0.011886597,
           0.010848999, 0.009841919, 0.008865356, 0.007919312,
           0.007003784, 0.006118774, 0.005294800, 0.004486084,
           0.003723145, 0.003005981, 0.002334595, 0.001693726,
           0.001098633, 0.000549316, 0.000030518,-0.000442505,
          -0.000869751,-0.001266479,-0.001617432,-0.001937866,
          -0.002227783,-0.002487183,-0.002700806,-0.002883911,
          -0.003051758,-0.003173828,-0.003280640,-0.003372192,
          -0.003417969,-0.003463745,-0.003479004,-0.003479004,
          -0.003463745,-0.003433228,-0.003387451,-0.003326416,
           0.003250122, 0.003173828, 0.003082275, 0.002990723,
           0.002899170, 0.002792358, 0.002685547, 0.002578735,
           0.002456665, 0.002349854, 0.002243042, 0.002120972,
           0.002014160, 0.001907349, 0.001785278, 0.001693726,
           0.001586914, 0.001480103, 0.001388550, 0.001296997,
           0.001205444, 0.001113892, 0.001037598, 0.000961304,
           0.000885010, 0.000808716, 0.000747681, 0.000686646,
           0.000625610, 0.000579834, 0.000534058, 0.000473022,
           0.000442505, 0.000396729, 0.000366211, 0.000320435,
           0.000289917, 0.000259399, 0.000244141, 0.000213623,
           0.000198364, 0.000167847, 0.000152588, 0.000137329,
           0.000122070, 0.000106812, 0.000106812, 0.000091553,
           0.000076294, 0.000076294, 0.000061035, 0.000061035,
           0.000045776, 0.000045776, 0.000030518, 0.000030518,
           0.000030518, 0.000030518, 0.000015259, 0.000015259,
           0.000015259, 0.000015259, 0.000015259, 0.000015259,
        //},g_synth_n_win[64][32]={
        };


        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Antialias(ref MP3FrameHeader header, uint gr, uint ch)
        {
            uint sb /* subband of 18 samples */, i, sblim, ui, li;
            float ub, lb;

            /* No antialiasing is done for short blocks */
            if ((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) &&
               (header.FrameSideInformation.Granules[gr].Channels[ch].BlockType == 2) &&
               (header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag) == 0)
            {
                return; /* Done */
            }
            /* Setup the limit for how many subbands to transform */
            sblim = (uint)(((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) &&
              (header.FrameSideInformation.Granules[gr].Channels[ch].BlockType == 2) &&
              (header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag == 1)) ? 2 : 32);
            /* Do the actual antialiasing */
            for (sb = 1; sb < sblim; sb++)
            {
                for (i = 0; i < 8; i++)
                {
                    li = 18 * sb - 1 - i;
                    ui = 18 * sb + i;
                    lb = header.FrameMainData.IS[gr][ch][li] * cs[i] - header.FrameMainData.IS[gr][ch][ui] * ca[i];
                    ub = header.FrameMainData.IS[gr][ch][ui] * cs[i] + header.FrameMainData.IS[gr][ch][li] * ca[i];
                    header.FrameMainData.IS[gr][ch][li] = lb;
                    header.FrameMainData.IS[gr][ch][ui] = ub;
                }
            }
            return; /* Done */
        }

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Frequency_Inversion(ref MP3FrameHeader header, uint gr, uint ch)
        {
            uint sb, i;

            for (sb = 1; sb < 32; sb += 2)
            { //OPT? : for(sb = 18; sb < 576; sb += 36)
                for (i = 1; i < 18; i += 2)
                    header.FrameMainData.IS[gr][ch][sb * 18 + i] = -header.FrameMainData.IS[gr][ch][sb * 18 + i];
            }
            return; /* Done */
        }

        float[][] g_imdct_win = new float[4][]; //new float[4][][36];
        uint initIMDCT_Win = 1;
        bool IMDCT_FirstPass = true;

        /**Description: Does inverse modified DCT and windowing.
        * Parameters: TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void IMDCT_Win(ref float [] input, ref float [] output, uint block_type)
        {
            //float [] input [18], float [] output [36]
            uint i, m, N, p;
            float sum; 
            float [] tin = new float[18];
            //# ifndef IMDCT_TABLES
            if (IMDCT_FirstPass)
            {
                for (int a = 0; a < 4; a++)
                    g_imdct_win[a] = new float[36];
                IMDCT_FirstPass = false;
            }
            //TODO : move to separate init function
            if(initIMDCT_Win != 0) { /* Setup the four(one for each block type) window vectors */
                for(i = 0; i< 36; i++)  g_imdct_win[0][i] = (float)Math.Sin(C_PI/36 *(i + 0.5)); //0
                for(i = 0; i< 18; i++)  g_imdct_win[1][i] = (float)Math.Sin(C_PI/36 *(i + 0.5)); //1
                for(i = 18; i< 24; i++) g_imdct_win[1][i] = 1.0f;
                for(i = 24; i< 30; i++) g_imdct_win[1][i] = (float)Math.Sin(C_PI/12 *(i + 0.5 - 18.0));
                for(i = 30; i< 36; i++) g_imdct_win[1][i] = 0.0f;
                for(i = 0; i< 12; i++)  g_imdct_win[2][i] = (float)Math.Sin(C_PI/12 *(i + 0.5)); //2
                for(i = 12; i< 36; i++) g_imdct_win[2][i] = 0.0f;
                for(i = 0; i< 6; i++)   g_imdct_win[3][i] = 0.0f; //3
                for(i = 6; i< 12; i++)  g_imdct_win[3][i] = (float)Math.Sin(C_PI/12 *(i + 0.5 - 6.0));
                for(i = 12; i< 18; i++) g_imdct_win[3][i] = 1.0f;
                for(i = 18; i< 36; i++) g_imdct_win[3][i] = (float)Math.Sin(C_PI/36 *(i + 0.5));
                initIMDCT_Win = 0;
            } /* end of init */
        //#endif
            for(i = 0; i< 36; i++) output[i] = 0.0f;
            for(i = 0; i< 18; i++) tin[i] = input[i];
            if(block_type == 2) { /* 3 short blocks */
            N = 12;
            for(i = 0; i< 3; i++) {
                for(p = 0; p<N; p++) {
                sum = 0.0f;
                for(m = 0;m<N/2; m++)
        //#ifdef IMDCT_NTABLES
        //            sum += tin[i + 3 * m] * cos_N12[m][p];
        //#else
                    sum += tin[i + 3 * m] * (float)Math.Cos(C_PI/(2*N)* (2* p+1+N/2)* (2* m+1));
        //#endif
                    output[6*i+p+6] += sum* g_imdct_win[block_type][p]; //TODO FIXME +=?
                }
            } /* end for(i... */
            }else{ /* block_type != 2 */
                N = 36;
                for(p = 0; p<N; p++){
                    sum = 0.0f;
                    for(m = 0; m<N/2; m++)
                        sum += input[m] * (float)Math.Cos(C_PI / (2 * N) * (2 * p + 1 + N / 2) * (2 * m + 1));
                    //#ifdef IMDCT_NTABLES
                    //        sum += in[m] * cos_N36[m][p];
                    //#else
                    //#endif
                    output[p] = sum* g_imdct_win[block_type][p];
                }
            }
        }

        /**Description: calculates y=x^(4/3) when requantizing samples.
        * Parameters: TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        //float Requantize_Pow_43(uint is_pos)
        float Requantize_Pow_43(float is_pos)
        {
            ////# ifdef POW34_TABLE
            //            float[] powtab34 = new float[8207];
            //            int init = 0;
            //            int i;

            //            if (init == 0)
            //            {   /* First time initialization */
            //                for (i = 0; i < 8207; i++)
            //                    powtab34[i] = pow((float)i, 4.0 / 3.0);
            //                init = 1;
            //            }
            ////# ifdef DEBUG
            ////            if (is_pos > 8206)
            ////            {
            ////                ERR("is_pos = %d larger than 8206!", is_pos);
            ////                is_pos = 8206;
            ////            }
            ////#endif /* DEBUG */
            //            return (powtab34[is_pos]);  /* Done */
            //#elif defined POW34_ITERATE
            //            float a4, a2, x, x2, x3, x_next, is_f1, is_f2, is_f3;
            //            uint i;
            //            //static unsigned init = 0;
            //            //static float powtab34[32];
            //            float [] coeff = new float[]{ -1.030797119e+02, 6.319399834e+00, 2.395095071e-03 };
            //            //if(init == 0) { /* First time initialization */
            //            //  for(i = 0; i < 32; i++) powtab34[i] = pow((float) i,4.0 / 3.0);
            //            //  init = 1;
            //            //}
            //            /* We use a table for 0<is_pos<32 since they are so common */
            //            if (is_pos < 32) return (powtab34[is_pos]);
            //            a2 = is_pos * is_pos;
            //            a4 = a2 * a2;
            //            is_f1 = (float)is_pos;
            //            is_f2 = is_f1 * is_f1;
            //            is_f3 = is_f1 * is_f2;
            //            /*  x = coeff[0] + coeff[1]*is_f1 + coeff[2]*is_f2 + coeff[3]*is_f3; */
            //            x = coeff[0] + coeff[1] * is_f1 + coeff[2] * is_f2;
            //            for (i = 0; i < 3; i++)
            //            {
            //                x2 = x * x;
            //                x3 = x * x2;
            //                x_next = (2 * x3 + a4) / (3 * x2);
            //                x = x_next;
            //            }
            //            return (x);
            //#else /* no optimization */
            //return powf((float)is_pos, 4.0f / 3.0f);
            return (float)Math.Pow((float)is_pos, 4.0f / 3.0f);
            //#endif /* POW34_TABLE || POW34_ITERATE */
        }

        float[] pretab = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f, 2f, 2f, 3f, 3f, 3f, 2f };
        /**Description: requantize sample in subband that uses long blocks.
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void Requantize_Process_Long(ref MP3FrameHeader header, uint gr, uint ch, uint is_pos, uint sfb)
        {
            float tmp1, tmp2, tmp3, sf_mult, pf_x_pt;            

            sf_mult = (float)(header.FrameSideInformation.Granules[gr].Channels[ch].ScaleFactorScale != 0 ? 1.0 : 0.5);
            if (sfb < 21)
            {
                pf_x_pt = header.FrameSideInformation.Granules[gr].Channels[ch].Preflag * pretab[sfb];
                tmp1 = (float)Math.Pow(2.0, -(sf_mult * (header.FrameMainData.ScaleFac_Long[gr][ch][sfb] + pf_x_pt)));
            }
            else
            {
                pf_x_pt = 0;
                tmp1 = (float)Math.Pow(2.0, 0.0);
            }
            
            tmp2 = (float)Math.Pow(2.0, 0.25 * ((int)header.FrameSideInformation.Granules[gr].Channels[ch].GlobalGain - 210));
            if (header.FrameMainData.IS[gr][ch][is_pos] < 0.0)
                tmp3 = -Requantize_Pow_43((-header.FrameMainData.IS[gr][ch][is_pos]));
            else tmp3 = Requantize_Pow_43(header.FrameMainData.IS[gr][ch][is_pos]);
            header.FrameMainData.IS[gr][ch][is_pos] = tmp1 * tmp2 * tmp3;
            return; /* Done */
        }

        /**Description: requantize sample in subband that uses short blocks.
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void Requantize_Process_Short(ref MP3FrameHeader header, uint gr, uint ch, uint is_pos, uint sfb, uint win)
        {
            float tmp1, tmp2, tmp3, sf_mult;

            sf_mult = (float)(header.FrameSideInformation.Granules[gr].Channels[ch].ScaleFactorScale != 0 ? 1.0f : 0.5f);
            if (sfb < 12)
            {
                tmp1 = (float)Math.Pow(2.0f, -(sf_mult * header.FrameMainData.ScaleFac_Short[gr][ch][sfb][win]));
            }
            else
            {
                tmp1 = (float)Math.Pow(2.0f, 0.0f);
            }
            tmp2 = (float)Math.Pow(2.0f, 0.25f * ((float)header.FrameSideInformation.Granules[gr].Channels[ch].GlobalGain - 210.0f -
                        8.0f * (float)header.FrameSideInformation.Granules[gr].Channels[ch].SubBlockGain[win]));
            tmp3 = (header.FrameMainData.IS[gr][ch][is_pos] < 0.0)
              ? -Requantize_Pow_43(-header.FrameMainData.IS[gr][ch][is_pos])
              : Requantize_Pow_43(header.FrameMainData.IS[gr][ch][is_pos]);
            header.FrameMainData.IS[gr][ch][is_pos] = tmp1 * tmp2 * tmp3;
            return; /* Done */
        }

        /**Description: intensity stereo processing for entire subband with long blocks.
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void Stereo_Process_Intensity_Long(ref MP3FrameHeader header, uint gr, uint sfb)
        {
            uint i, sfreq, sfb_start, sfb_stop, is_pos;
            float is_ratio_l, is_ratio_r, left, right;

            /* Check that((is_pos[sfb]=scalefac) != 7) => no intensity stereo */
            if ((is_pos = header.FrameMainData.ScaleFac_Long[gr][0][sfb]) != 7)
            {
                sfreq = (uint)header.SamplingRateLookupIndex; /* Setup sampling freq index */
                sfb_start = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb];
                sfb_stop = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb + 1];
                if (is_pos == 6)
                { /* tan((6*PI)/12 = PI/2) needs special treatment! */
                    is_ratio_l = 1.0f;
                    is_ratio_r = 0.0f;
                }
                else
                {
                    is_ratio_l = (is_ratios[is_pos] / (1.0f + is_ratios[is_pos]));
                    is_ratio_r = (1.0f / (1.0f + is_ratios[is_pos]));
                }
                /* Now decode all samples in this scale factor band */
                for (i = sfb_start; i < sfb_stop; i++)
                {
                    left = is_ratio_l * header.FrameMainData.IS[gr][0][i];
                    right = is_ratio_r * header.FrameMainData.IS[gr][0][i];
                    header.FrameMainData.IS[gr][0][i] = left;
                    header.FrameMainData.IS[gr][1][i] = right;
                }
            }
            return; /* Done */
        } /* end Stereo_Process_Intensity_Long() */

        /**Description: This function is used to perform intensity stereo processing
        *              for an entire subband that uses short blocks.
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void Stereo_Process_Intensity_Short(ref MP3FrameHeader header, uint gr, uint sfb)
        {
            uint sfb_start, sfb_stop, is_pos, is_ratio_l, is_ratio_r, i, sfreq, win, win_len;
            float left, right;

            sfreq = (uint)header.SamplingRateLookupIndex;   /* Setup sampling freq index */
            /* The window length */
            win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] - ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
            /* The three windows within the band has different scalefactors */
            for (win = 0; win < 3; win++)
            {
                /* Check that((is_pos[sfb]=scalefac) != 7) => no intensity stereo */
                if ((is_pos = header.FrameMainData.ScaleFac_Short[gr][0][sfb][win]) != 7)
                {
                    sfb_start = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb] * 3 + win_len * win;
                    sfb_stop = sfb_start + win_len;
                    if (is_pos == 6)
                    { /* tan((6*PI)/12 = PI/2) needs special treatment! */
                        is_ratio_l = (uint)1.0;
                        is_ratio_r = (uint)0.0;
                    }
                    else
                    {
                        is_ratio_l = (uint)(is_ratios[is_pos] / (1.0f + is_ratios[is_pos]));
                        is_ratio_r = (uint)(1.0f / (1.0f + is_ratios[is_pos]));
                    }
                    /* Now decode all samples in this scale factor band */
                    for (i = sfb_start; i < sfb_stop; i++)
                    {
                        left = is_ratio_l = (uint)header.FrameMainData.IS[gr][0][i];
                        right = is_ratio_r = (uint)header.FrameMainData.IS[gr][0][i];
                        header.FrameMainData.IS[gr][0][i] = left;
                        header.FrameMainData.IS[gr][1][i] = right;
                    }
                } /* end if(not illegal is_pos) */
            } /* end for(win... */
            return; /* Done */
        } /* end Stereo_Process_Intensity_Short() */

        /// <summary>
        /// Static entity in Hybrid Synthesis
        /// </summary>
        float[][][] store = new float[2][][];

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Hybrid_Synthesis(ref MP3FrameHeader header, uint gr, uint ch)
        {
            uint sb, i, j, bt;
            float [] rawout = new float[36];

            if(header.HSynthInit != 0) { /* Clear stored samples vector. OPT? use memset */
                for (int a = 0; a < 2; a++)
                {
                    store[a] = new float[32][];
                    for (int b = 0; b < 32; b++)
                    {
                        store[a][b] = new float[18];
                    }
                }

                for (j = 0; j< 2; j++) {
                    for(sb = 0; sb< 32; sb++) {
                        for(i = 0; i< 18; i++) {
                            store[j][sb][i] = 0.0f;
                        }
                    }
                }
                header.HSynthInit = 0;
            } /* end if(hsynth_init) */
            for(sb = 0; sb< 32; sb++) { /* Loop through all 32 subbands */
                /* Determine blocktype for this subband */
                bt =(uint)(((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) &&
                    (header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag == 1) && (sb< 2))
                    ? 0 : header.FrameSideInformation.Granules[gr].Channels[ch].BlockType);
                /* Do the inverse modified DCT and windowing */

                float[] input = new float[576];
                int idx = 0;
                for(int z=(int)sb*18;z<576;z++)
                {
                    input[idx++] = header.FrameMainData.IS[gr][ch][z];
                }
                IMDCT_Win(ref input,ref rawout,bt);
                idx = 0;
                //for(int z= (int)sb * 18; z < 576; z++)
                //{
                //    header.FrameMainData.IS[gr][ch][z] = input[idx++];
                //}

                for(i = 0; i< 18; i++) { /* Overlapp add with stored vector into main_data vector */
                    header.FrameMainData.IS[gr][ch][sb*18 + i] = rawout[i] + store[ch][sb][i];
                    store[ch][sb][i] = rawout[i + 18];
                } /* end for(i... */
            } /* end for(sb... */
            return; /* Done */
        }

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Reorder(ref MP3FrameHeader header, uint gr, uint ch)
        {
            uint sfreq, i, j, next_sfb, sfb, win_len, win;
            float[] re = new float[576];

            sfreq = (uint)header.SamplingRateLookupIndex; /* Setup sampling freq index */
            /* Only reorder short blocks */
            if ((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) &&
               (header.FrameSideInformation.Granules[gr].Channels[ch].BlockType == 2))
            { /* Short blocks */
                /* Check if the first two subbands
                 *(=2*18 samples = 8 long or 3 short sfb's) uses long blocks */
                sfb = (uint)((header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag != 0) ? 3 : 0); /* 2 longbl. sb  first */
                
                next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] - ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
                for (i = (uint)((sfb == 0) ? 0 : 36); i < 576; /* i++ done below! */)
                {
                    /* Check if we're into the next scalefac band */
                    if (i == next_sfb)
                    {        /* Yes */
                        /* Copy reordered data back to the original vector */
                        for (j = 0; j < 3 * win_len; j++)
                            header.FrameMainData.IS[gr][ch][3 * ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb] + j] = re[j];
                        /* Check if this band is above the rzero region,if so we're done */
                        if (i >= header.FrameSideInformation.Granules[gr].Channels[ch].Count1) return; /* Done */
                        sfb++;
                        next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                        win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] - ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
                    } /* end if(next_sfb) */
                    for (win = 0; win < 3; win++)
                    { /* Do the actual reordering */
                        for (j = 0; j < win_len; j++)
                        {
                            re[j * 3 + win] = header.FrameMainData.IS[gr][ch][i];
                            i++;
                        } /* end for(j... */
                    } /* end for(win... */
                } /* end for(i... */
                /* Copy reordered data of last band back to original vector */
                for (j = 0; j < 3 * win_len; j++)
                    header.FrameMainData.IS[gr][ch][3 * ScaleFactorBandIndices.AllBandIndices[sfreq].S[12] + j] = re[j];
            } /* end else(only long blocks) */
            return; /* Done */
        }

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Requantize(ref MP3FrameHeader header, uint gr, uint ch)
        {
            uint sfb /* scalefac band index */, next_sfb /* frequency of next sfb */,
              sfreq, i, j, win, win_len;

            /* Setup sampling frequency index */
            sfreq = (uint)header.SamplingRateLookupIndex;
            /* Determine type of block to process */
            if ((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) && (header.FrameSideInformation.Granules[gr].Channels[ch].BlockType == 2))
            { /* Short blocks */
                /* Check if the first two subbands
                 *(=2*18 samples = 8 long or 3 short sfb's) uses long blocks */
                if (header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag != 0)
                { /* 2 longbl. sb  first */
                    /* First process the 2 long block subbands at the start */
                    sfb = 0;
                    next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb + 1];
                    for (i = 0; i < 36; i++)
                    {
                        if (i == next_sfb)
                        {
                            sfb++;
                            next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb + 1];
                        } /* end if */
                        Requantize_Process_Long(ref header, gr, ch, i, sfb);
                    }
                    /* And next the remaining,non-zero,bands which uses short blocks */
                    sfb = 3;
                    next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                    win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] -
                      ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];

                    for (i = 36; i < header.FrameSideInformation.Granules[gr].Channels[ch].Count1; /* i++ done below! */)
                    {
                        /* Check if we're into the next scalefac band */
                        if (i == next_sfb)
                        {        /* Yes */
                            sfb++;
                            next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                            win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] -
                              ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
                        } /* end if(next_sfb) */
                        for (win = 0; win < 3; win++)
                        {
                            for (j = 0; j < win_len; j++)
                            {
                                Requantize_Process_Short(ref header, gr, ch, i, sfb, win);
                                i++;
                            } /* end for(j... */
                        } /* end for(win... */

                    } /* end for(i... */
                }
                else
                { /* Only short blocks */
                    sfb = 0;
                    next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                    win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] -
                      ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
                    for (i = 0; i < header.FrameSideInformation.Granules[gr].Channels[ch].Count1; /* i++ done below! */)
                    {
                        /* Check if we're into the next scalefac band */
                        if (i == next_sfb)
                        {        /* Yes */
                            sfb++;
                            next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] * 3;
                            win_len = ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb + 1] -
                              ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb];
                        } /* end if(next_sfb) */
                        for (win = 0; win < 3; win++)
                        {
                            for (j = 0; j < win_len; j++)
                            {
                                Requantize_Process_Short(ref header, gr, ch, i, sfb, win);
                                i++;
                            } /* end for(j... */
                        } /* end for(win... */
                    } /* end for(i... */
                } /* end else(only short blocks) */
            }
            else
            { /* Only long blocks */
                sfb = 0;
                next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb + 1];
                for (i = 0; i < header.FrameSideInformation.Granules[gr].Channels[ch].Count1; i++)
                {
                    if (i == next_sfb)
                    {
                        sfb++;
                        next_sfb = ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb + 1];
                    } /* end if */
                    Requantize_Process_Long(ref header, gr, ch, i, sfb);
                }
            } /* end else(only long blocks) */
            return; /* Done */
        }

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Stereo(ref MP3FrameHeader header, uint gr)
        {
            uint max_pos, i, sfreq, sfb /* scalefac band index */;
            float left, right;

            /* Do nothing if joint stereo is not enabled */
            if ((header.ChannelMode != MP3FrameHeader.ChannelModeType.JointStereo) || ( header.ModeExtensionLookup == 0)) return;
            /* Do Middle/Side("normal") stereo processing */
            if ((header.ModeExtensionLookup & 0x2) != 0)
            {
                /* Determine how many frequency lines to transform */
                //max_pos = id->g_side_info.count1[gr][!!(id->g_side_info.count1[gr][0] > id->g_side_info.count1[gr][1])];
                bool ch_check = !!(header.FrameSideInformation.Granules[gr].Channels[0].Count1 > header.FrameSideInformation.Granules[gr].Channels[1].Count1);
                int ch2 = (int)((ch_check) ? 1 : 0);
                
                max_pos = (uint)header.FrameSideInformation.Granules[gr].Channels[ch2].Count1;
                //max_pos = .count1[gr][!!(id->g_side_info.count1[gr][0] > id->g_side_info.count1[gr][1])];
                /* Do the actual processing */
                for (i = 0; i < max_pos; i++)
                {
                    left = (header.FrameMainData.IS[gr][0][i] + header.FrameMainData.IS[gr][1][i]) * (C_INV_SQRT_2);
                    right = (header.FrameMainData.IS[gr][0][i] - header.FrameMainData.IS[gr][1][i]) * (C_INV_SQRT_2);
                    header.FrameMainData.IS[gr][0][i] = left;
                    header.FrameMainData.IS[gr][1][i] = right;
                } /* end for(i... */
            } /* end if(ms_stereo... */
            /* Do intensity stereo processing */
            if ((header.ModeExtensionLookup & 0x1) != 0)
            {
                /* Setup sampling frequency index */
                sfreq = (uint)header.SamplingRateLookupIndex;
                /* First band that is intensity stereo encoded is first band scale factor
                 * band on or above count1 frequency line. N.B.: Intensity stereo coding is
                 * only done for higher subbands, but logic is here for lower subbands. */
                /* Determine type of block to process */
                if ((header.FrameSideInformation.Granules[gr].Channels[0].WindowsSwitchingFlag == 1) &&
                   (header.FrameSideInformation.Granules[gr].Channels[0].BlockType == 2))
                { /* Short blocks */
                    /* Check if the first two subbands
                     *(=2*18 samples = 8 long or 3 short sfb's) uses long blocks */
                    if (header.FrameSideInformation.Granules[gr].Channels[0].MixedBlockFlag != 0)
                    { /* 2 longbl. sb  first */
                        for (sfb = 0; sfb < 8; sfb++)
                        {/* First process 8 sfb's at start */
                            /* Is this scale factor band above count1 for the right channel? */
                            if (ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb] >= header.FrameSideInformation.Granules[gr].Channels[1].Count1)
                                Stereo_Process_Intensity_Long(ref header, gr, sfb);
                        } /* end if(sfb... */
                        /* And next the remaining bands which uses short blocks */
                        for (sfb = 3; sfb < 12; sfb++)
                        {
                            /* Is this scale factor band above count1 for the right channel? */
                            if (ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb] * 3 >= header.FrameSideInformation.Granules[gr].Channels[1].Count1)
                                Stereo_Process_Intensity_Short(ref header, gr, sfb); /* intensity stereo processing */
                        }
                    }
                    else
                    { /* Only short blocks */
                        for (sfb = 0; sfb < 12; sfb++)
                        {
                            /* Is this scale factor band above count1 for the right channel? */
                            if (ScaleFactorBandIndices.AllBandIndices[sfreq].S[sfb] * 3 >= header.FrameSideInformation.Granules[gr].Channels[1].Count1)
                                Stereo_Process_Intensity_Short(ref header, gr, sfb); /* intensity stereo processing */
                        }
                    } /* end else(only short blocks) */
                }
                else
                {                        /* Only long blocks */
                    for (sfb = 0; sfb < 21; sfb++)
                    {
                        /* Is this scale factor band above count1 for the right channel? */
                        if (ScaleFactorBandIndices.AllBandIndices[sfreq].L[sfb] >= header.FrameSideInformation.Granules[gr].Channels[1].Count1)
                        {
                            /* Perform the intensity stereo processing */
                            Stereo_Process_Intensity_Long(ref header, gr, sfb);
                        }
                    }
                } /* end else(only long blocks) */
            } /* end if(intensity_stereo processing) */
        }

        /// <summary>
        /// Static entities used in Subband Synthesis
        /// </summary>
        float[][] g_synth_n_win = new float[64][];
        float[][] v_vec = new float[2][];
        uint initSubbandSynthesis = 1;
        bool firstInitSubband = true;

        /**Description: TBD
        * Parameters: Stream handle,TBD
        * Return value: TBD
        * Author: Krister Lagerström(krister@kmlager.com) **/
        void L3_Subband_Synthesis(ref MP3FrameHeader header, uint gr, uint ch, ref uint[] outdata)
        {
            //uint[] outdata = new uint[576];

            float[] u_vec = new float[512];
            float[] s_vec = new float[32]; 
            float sum; /* u_vec can be used insted of s_vec */
            int samp;
            uint i, j, ss, nch;

            if (firstInitSubband)
            {
                for (int a = 0; a < 64; a++)
                {
                    g_synth_n_win[a] = new float[32];
                }
                for (int a = 0; a < 2; a++)
                {
                    v_vec[a] = new float[1024];
                }
                firstInitSubband = false;
            }

            /* Number of channels(1 for mono and 2 for stereo) */
            nch = (uint)((header.ChannelMode == MP3FrameHeader.ChannelModeType.SingleChannelMono) ? 1 : 2 );
            /* Setup the n_win windowing vector and the v_vec intermediate vector */

            if (initSubbandSynthesis != 0) {
                for(i = 0; i< 64; i++) {
                    for(j = 0; j< 32; j++) /*TODO: put in lookup table*/
                        g_synth_n_win[i][j] = (float)Math.Cos(((float)(16+i)* (2* j+1)) * (C_PI/64.0));
                }
                for(i = 0; i< 2; i++) /* Setup the v_vec intermediate vector */
                    for(j = 0; j< 1024; j++) 
                        v_vec[i][j] = 0.0f; /*TODO: memset */
                initSubbandSynthesis = 0;
            } /* end if(init) */

            if(header.SynthInit != 0) {
                for(i = 0; i< 2; i++) /* Setup the v_vec intermediate vector */
                    for(j = 0; j< 1024; j++) 
                        v_vec[i][j] = 0.0f; /*TODO: memset*/
                header.SynthInit = 0;
            } /* end if(synth_init) */

            for(ss = 0; ss< 18; ss++) { /* Loop through 18 samples in 32 subbands */
                for(i = 1023; i > 63; i--)  /* Shift up the V vector */
                    v_vec[ch][i] = v_vec[ch][i - 64];
                for(i = 0; i< 32; i++) /* Copy next 32 time samples to a temp vector */
                    s_vec[i] =((float) header.FrameMainData.IS[gr] [ch] [i*18 + ss]);
                for(i = 0; i< 64; i++) { /* Matrix multiply input with n_win[][] matrix */
                    sum = 0.0f;
                    for(j = 0; j< 32; j++) 
                            sum += g_synth_n_win[i][j] * s_vec[j];
                    v_vec[ch][i] = sum;
                } /* end for(i... */
                for(i = 0; i< 8; i++) { /* Build the U vector */
                    for(j = 0; j< 32; j++) { /* <<7 == *128 */
                        u_vec[(i << 6) + j]      = v_vec[ch][(i << 7) + j];
                        u_vec[(i << 6) + j + 32] = v_vec[ch][(i << 7) + j + 96];
                    }
                } /* end for(i... */
                for(i = 0; i< 512; i++) /* Window by u_vec[i] with g_synth_dtbl[i] */
                    u_vec[i] = u_vec[i] * (float)g_synth_dtbl[i];
                for(i = 0; i< 32; i++) { /* Calc 32 samples,store in outdata vector */
                    sum = 0.0f;
                    for(j = 0; j< 16; j++) /* sum += u_vec[j*32 + i]; */
                        sum += u_vec[(j << 5) + i];
                      /* sum now contains time sample 32*ss+i. Convert to 16-bit signed int */
                    samp = Convert.ToInt32(sum * 32767.0f);
                    if (samp > 32767) 
                        samp = 32767;
                    else if(samp< -32767) 
                        samp = -32767;
                    samp &= 0xffff;
                    if(ch == 0) {  /* This function must be called for channel 0 first */
                    /* We always run in stereo mode,& duplicate channels here for mono */
                        if(nch == 1) {
                            outdata[32 * ss + i] = (uint)((samp << 16) | (samp));
                        }
                        else{
                            outdata[32 * ss + i] = (uint)(samp << 16);
                        }
                    }
                    else
                    {
                        outdata[32 * ss + i] |= (uint)samp;
                    }
                } /* end for(i... */
            } /* end for(ss... */
            //header.FrameMainData.Granules[gr].Channels[ch].OutputData = outdata;
            //header.FrameMainData.Granules[gr].OutputData = outdata;
            return; /* Done */
        }

        /**Description: decodes a layer 3 bitstream into audio samples.
        * Parameters: Stream handle,outdata vector.
        * Return value: PDMP3_OK or PDMP3_ERR if the frame contains errors.
        * Author: Krister Lagerström(krister@kmlager.com) **/
        int DecodeL3(ref MP3FrameHeader header, ref BitReader reader)
        {
            uint gr, ch, nch;

            //for (int g1 = 0; g1 < 2; g1++)
            //{
            //    for (int c1 = 0; c1 < 2; c1++)
            //    {
            //        Console.WriteLine();
            //        Console.WriteLine($"Frame:{johnnyCounter}, Gr:{g1}, Ch:{c1}");
            //        for (int z = 0; z < 576; z++)
            //        {
            //            Console.Write("{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[g1][c1][z]));
            //        }
            //    }
            //}
            //Console.WriteLine();

            /* Number of channels(1 for mono and 2 for stereo) */
            nch = (uint)(header.ChannelMode == MP3FrameHeader.ChannelModeType.SingleChannelMono ? 1 : 2);
            for (gr = 0; gr < 2; gr++)
            {
                for (ch = 0; ch < nch; ch++)
                {
                    //dmp_scf(&id->g_side_info, &id->g_main_data, gr, ch); //noop unless debug
                    //dmp_huff(&id->g_main_data, gr, ch); //noop unless debug
                    L3_Requantize(ref header, gr, ch); /* Requantize samples */

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Requantize");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                    //dmp_samples(&id->g_main_data, gr, ch, 0); //noop unless debug
                    L3_Reorder(ref header, gr, ch); /* Reorder short blocks */

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Reorder");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                } /* end for(ch... */
                L3_Stereo(ref header, gr); /* Stereo processing */

                //Console.WriteLine();
                //Console.WriteLine("L3_Stereo");
                //for (int c1 = 0; c1 < 2; c1++)
                //{
                //    Console.WriteLine();
                //    Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{c1}");
                //    for (int z = 0; z < 576; z++)
                //    {
                //        Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][c1][z]), z);
                //    }
                //}

                //dmp_samples(&id->g_main_data, gr, 0, 1); //noop unless debug
                //dmp_samples(&id->g_main_data, gr, 1, 1); //noop unless debug
                for (ch = 0; ch < nch; ch++)
                {
                    L3_Antialias(ref header, gr, ch); /* Antialias */

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Antialias");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                    //dmp_samples(&id->g_main_data, gr, ch, 2); //noop unless debug
                    L3_Hybrid_Synthesis(ref header, gr, ch); /*(IMDCT,windowing,overlapp add) */

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Hybrid_Synthesis");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                    L3_Frequency_Inversion(ref header, gr, ch); /* Frequency inversion */
                    //dmp_samples(&id->g_main_data, gr, ch, 3); //noop unless debug
                    //L3_Subband_Synthesis(ref header, gr, ch, id->out[gr]); /* Polyphase subband synthesis */

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Frequency_Inversion");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                    uint[] outdata = header.FrameMainData.Granules[gr].OutputData;
                    L3_Subband_Synthesis(ref header, gr, ch, ref outdata); /* Polyphase subband synthesis */
                    header.FrameMainData.Granules[gr].OutputData = outdata;

                    //Console.WriteLine();
                    //Console.WriteLine("L3_Subband_Synthesis");
                    //Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}, Ch:{ch}");
                    //for (int z = 0; z < 576; z++)
                    //{
                    //    Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
                    //}

                } /* end for(ch... */


                //# ifdef DEBUG
                //                {
                //                    int i, ctr = 0;
                //                    printf("PCM:\n");
                //                    for (i = 0; i < 576; i++)
                //                    {
                //                        printf("%d: %d\n", ctr++, (out[i] >> 16) & 0xffff);
                //            if (nch == 2) printf("%d: %d\n", ctr++, out[i] & 0xffff);
                //        }
                //    }
                //#endif /* DEBUG */
            } /* end for(gr... */

            //for(gr=0;gr<2;gr++)
            //{
            //    Console.WriteLine();
            //    Console.WriteLine("Output");
            //    Console.WriteLine($"Frame:{johnnyCounter}, Gr:{gr}");
            //    for (int z = 0; z < 576; z++)
            //    {
            //        //Console.Write("{1}:{0}, ", string.Format("{0:0.00###########}", header.FrameMainData.IS[gr][ch][z]), z);
            //        Console.Write("{0}:{1}, ", z, (int)header.FrameMainData.Granules[gr].OutputData[z]);
            //    }
            //}

            return (0);   /* Done */
        }


        int HuffmanDecode(ref MP3FrameHeader header, ref BitReader reader, ref MainData mainData, uint table_num, ref int x, ref int y, ref int v, ref int w)
        {
            uint point = 0, error = 1, bitsleft = 32, //=16??
                treelen = Tables.AllTables[table_num].TreeLen,
                linbits = Tables.AllTables[table_num].LinBits;

            //treelen = g_huffman_main[table_num].treelen,
            //linbits = g_huffman_main[table_num].linbits;

            //treelen = g_huffman_main[table_num].treelen;
            treelen = Tables.AllTables[table_num].TreeLen;
            if (treelen == 0)
            { /* Check for empty tables */
                x = y = v = w = 0;
                //return (PDMP3_OK);
                return 0;
            }
            //ushort* htptr = g_huffman_main[table_num].hufftable;
            ushort[] htptr = Tables.AllTables[table_num].Table;
            do
            {   /* Start reading the Huffman code word,bit by bit */
                /* Check if we've matched a code word */
                if ((htptr[point] & 0xff00) == 0)
                {
                    error = 0;
                    x = (htptr[point] >> 4) & 0xf;
                    y = htptr[point] & 0xf;
                    break;
                }
                //if (Get_Main_Bit(id))
                if (reader.ReadBit().ValueBoolean)
                { /* Go right in tree */
                    while ((htptr[point] & 0xff) >= 250)
                        point += (uint)(htptr[point] & 0xff);
                    point += (uint)(htptr[point] & 0xff);
                }
                else
                { /* Go left in tree */
                    while ((htptr[point] >> 8) >= 250)
                        point += (uint)(htptr[point] >> 8);
                    point += (uint)(htptr[point] >> 8);
                }
            } while ((--bitsleft > 0) && (point < treelen));
            if (error == 1)
            {  /* Check for error. */
                //ERR("Illegal Huff code in data. bleft = %d,point = %d. tab = %d.",
                //    bitsleft, point, table_num);
                x = y = 0;
                throw new Exception($"Illegal Huffman code in data. bleft = {bitsleft},point = {point}. tab = {table_num}.");
            }
            if (table_num > 31)
            {  /* Process sign encodings for quadruples tables. */
                v = (y >> 3) & 1;
                w = (y >> 2) & 1;
                x = (y >> 1) & 1;
                y = y & 1;
                //if ((v > 0) && (Get_Main_Bit(id) == 1)) v = -v;
                //if ((w > 0) && (Get_Main_Bit(id) == 1)) w = -w;
                //if ((x > 0) && (Get_Main_Bit(id) == 1)) x = -x;
                //if ((y > 0) && (Get_Main_Bit(id) == 1)) y = -y;
                if ((v > 0) && (reader.ReadBit().ValueBoolean == true)) v = -v;
                if ((w > 0) && (reader.ReadBit().ValueBoolean == true)) w = -w;
                if ((x > 0) && (reader.ReadBit().ValueBoolean == true)) x = -x;
                if ((y > 0) && (reader.ReadBit().ValueBoolean == true)) y = -y;
            }
            else
            {
                //if ((linbits > 0) && (x == 15)) x += Get_Main_Bits(id, linbits);/* Get linbits */
                //if ((x > 0) && (Get_Main_Bit(id) == 1)) x = -x; /* Get sign bit */
                //if ((linbits > 0) && (y == 15)) y += Get_Main_Bits(id, linbits);/* Get linbits */
                //if ((y > 0) && (Get_Main_Bit(id) == 1)) y = -y;/* Get sign bit */
                if ((linbits > 0) && (x == 15)) x += (int)reader.ReadBitsToCode((int)linbits);/* Get linbits */
                if ((x > 0) && (reader.ReadBit().ValueBoolean == true)) x = -x; /* Get sign bit */
                if ((linbits > 0) && (y == 15)) y += (int)reader.ReadBitsToCode((int)linbits);/* Get linbits */
                if ((y > 0) && (reader.ReadBit().ValueBoolean == true)) y = -y;/* Get sign bit */
            }
            //return (error ? PDMP3_ERR : PDMP3_OK);  /* Done */
            return (error == 1 ? -1 : 0);  /* Done */
        }

        void ReadHuffman(ref MP3FrameHeader header, ref BitReader reader, ref MainData mainData, int part_2_start, int gr, int ch)
        {
            int x = 0, y = 0, v = 0, w = 0;
            uint table_num, is_pos, bit_pos_end, sfreq;
            uint region_1_start, region_2_start; /* region_0_start = 0 */

            /* Check that there is any data to decode. If not,zero the array. */
            //if (id->g_side_info.part2_3_length[gr][ch] == 0)
            //{
            //    for (is_pos = 0; is_pos < 576; is_pos++)
            //        id->g_main_data.is[gr][ch][is_pos] = 0.0;
            //    return;
            //}
            if (header.FrameSideInformation.Granules[gr].Channels[ch].Part2_3_Length == 0)
            {
                for (is_pos = 0; is_pos < 576; is_pos++)
                    mainData.IS[gr][ch][is_pos] = 0.0f;
                //id->g_main_data.is[gr][ch][is_pos] = 0.0;
                return;
            }

            /* Calculate bit_pos_end which is the index of the last bit for this part. */
            //bit_pos_end = part_2_start + id->g_side_info.part2_3_length[gr][ch] - 1;
            bit_pos_end = (uint)part_2_start + (uint)header.FrameSideInformation.Granules[gr].Channels[ch].Part2_3_Length - 1;
            /* Determine region boundaries */
            if ((header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag == 1) &&
               (header.FrameSideInformation.Granules[gr].Channels[ch].BlockType == 2))
            {
                region_1_start = 36;  /* sfb[9/3]*3=36 */
                region_2_start = 576; /* No Region2 for short block case. */
            }
            else
            {
                //sfreq = id->g_frame_header.sampling_frequency;
                sfreq = (uint)header.SamplingRateLookupIndex;
                region_1_start =
                    ScaleFactorBandIndices.AllBandIndices[sfreq].L[header.FrameSideInformation.Granules[gr].Channels[ch].Region0Count + 1];
                //g_sf_band_indices[sfreq].l[header.FrameSideInformation.Granules[gr].Channels[ch].Region0Count + 1];
                region_2_start =
                    ScaleFactorBandIndices.AllBandIndices[sfreq].L[header.FrameSideInformation.Granules[gr].Channels[ch].Region0Count +
                    header.FrameSideInformation.Granules[gr].Channels[ch].Region1Count + 2];
                //g_sf_band_indices[sfreq].l[header.FrameSideInformation.Granules[gr].Channels[ch].Region0Count +
            }
            /* Read big_values using tables according to region_x_start */
            for (is_pos = 0; is_pos < header.FrameSideInformation.Granules[gr].Channels[ch].BigValues * 2; is_pos++)
            {
                if (is_pos < region_1_start)
                {
                    //table_num = id->g_side_info.table_select[gr][ch][0];
                    table_num = (uint)header.FrameSideInformation.Granules[gr].Channels[ch].TableSelect[0];
                }
                else if (is_pos < region_2_start)
                {
                    //table_num = id->g_side_info.table_select[gr][ch][1];
                    table_num = (uint)header.FrameSideInformation.Granules[gr].Channels[ch].TableSelect[1];
                }
                else
                {
                    //table_num = id->g_side_info.table_select[gr][ch][2];
                    table_num = (uint)header.FrameSideInformation.Granules[gr].Channels[ch].TableSelect[2];
                }
                /* Get next Huffman coded words */
                HuffmanDecode(ref header, ref reader, ref mainData, table_num, ref x, ref y, ref v, ref w);
                /* In the big_values area there are two freq lines per Huffman word */
                //id->g_main_data.is[gr][ch][is_pos++] = x;
                //id->g_main_data.is[gr][ch][is_pos] = y;
                mainData.IS[gr][ch][is_pos++] = x;
                mainData.IS[gr][ch][is_pos] = y;
            }
            /* Read small values until is_pos = 576 or we run out of huffman data */
            //table_num = id->g_side_info.count1table_select[gr][ch] + 32;
            table_num = (uint)header.FrameSideInformation.Granules[gr].Channels[ch].Count1TableSelect + 32;
            for (is_pos = (uint)header.FrameSideInformation.Granules[gr].Channels[ch].BigValues * 2;
                (is_pos <= 572) && (GetMainPos(ref reader) <= bit_pos_end); is_pos++)
            {
                /* Get next Huffman coded words */
                HuffmanDecode(ref header, ref reader, ref mainData, table_num, ref x, ref y, ref v, ref w);
                //id->g_main_data.is[gr][ch][is_pos++] = v;
                mainData.IS[gr][ch][is_pos++] = v;
                if (is_pos >= 576) break;
                mainData.IS[gr][ch][is_pos++] = w;
                if (is_pos >= 576) break;
                mainData.IS[gr][ch][is_pos++] = x;
                if (is_pos >= 576) break;
                mainData.IS[gr][ch][is_pos] = y;
            }
            /* Check that we didn't read past the end of this section */
            if (GetMainPos(ref reader) > (bit_pos_end + 1)) /* Remove last words read */
                is_pos -= 4;
            /* Setup count1 which is the index of the first sample in the rzero reg. */
            //id->g_side_info.count1[gr][ch] = is_pos;
            header.FrameSideInformation.Granules[gr].Channels[ch].Count1 = is_pos;
            /* Zero out the last part if necessary */
            for (/* is_pos comes from last for-loop */; is_pos < 576; is_pos++)
                mainData.IS[gr][ch][is_pos] = 0.0f;
                //id->g_main_data.is[gr][ch][is_pos] = 0.0;
            /* Set the bitpos to point to the next part to read */
            SetMainPos(ref reader, bit_pos_end + 1);
            return;  /* Done */
        }

        int[] ReadMainData(ref MP3FrameHeader header, ref BitReader reader)
        {
            int nch = header.NumberOfChannels;
            int sfb = 0;
            int nbits = 0;
            int slen1 = 0, slen2 = 0;
            int win = 0;
            int part_2_start = 0;
            MainData mainData = new MainData(nch);

            int counter = 0;
            int counter2 = 0;

            for (int gr = 0; gr < 2; gr++)
            {
                for (int ch = 0; ch < nch; ch++)
                {
                    // Ensure the right position in the buffer
                    //part_2_start = Get_Main_Pos(id);
                    part_2_start = reader.Position;

                    /* Number of bits in the bitstream for the bands */
                    slen1 = header.FrameSideInformation.Granules[gr].Channels[ch].SLen1;
                    slen2 = header.FrameSideInformation.Granules[gr].Channels[ch].SLen2;
                    //slen1 = mpeg1_scalefac_sizes[id->g_side_info.scalefac_compress[gr][ch]][0];
                    //slen2 = mpeg1_scalefac_sizes[id->g_side_info.scalefac_compress[gr][ch]][1];
                    //printf("gr:%d, ch:%d, slen1:%d, slen2:%d\r\n", gr, ch, slen1, slen2);
                    ulong win_switch_flag = header.FrameSideInformation.Granules[gr].Channels[ch].WindowsSwitchingFlag;
                    ulong block_type = header.FrameSideInformation.Granules[gr].Channels[ch].BlockType;
                    ulong mixed_block_flag = header.FrameSideInformation.Granules[gr].Channels[ch].MixedBlockFlag;

                    if ((win_switch_flag != 0) && (block_type == 2))
                    {
                        if (mixed_block_flag != 0)
                        {
                            for (sfb = 0; sfb < 8; sfb++)
                            {
                                mainData.ScaleFac_Long[gr][ch][sfb] = (byte)reader.ReadBitsToCode(slen1);
                                counter+=slen1;
                            }
                            //id->g_main_data.scalefac_l[gr][ch][sfb] = Get_Main_Bits(id, slen1);
                            for (sfb = 3; sfb < 12; sfb++)
                            {
                                nbits = (sfb < 6) ? slen1 : slen2;/*slen1 for band 3-5,slen2 for 6-11*/
                                for (win = 0; win < 3; win++)
                                {
                                    mainData.ScaleFac_Short[gr][ch][sfb][win] = (byte)reader.ReadBitsToCode(nbits);
                                    counter+=nbits;
                                }
                                //id->g_main_data.scalefac_s[gr][ch][sfb][win] = Get_Main_Bits(id, nbits);
                            }
                        }
                        else
                        {
                            for (sfb = 0; sfb < 12; sfb++)
                            {
                                nbits = (sfb < 6) ? slen1 : slen2;/*slen1 for band 3-5,slen2 for 6-11*/
                                for (win = 0; win < 3; win++)
                                {
                                    mainData.ScaleFac_Short[gr][ch][sfb][win] = (byte)reader.ReadBitsToCode(nbits);
                                    counter+=nbits;
                                }
                                //id->g_main_data.scalefac_s[gr][ch][sfb][win] = Get_Main_Bits(id, nbits);
                            }
                        }
                    }
                    else
                    { /* block_type == 0 if winswitch == 0 */
                        /* Scale factor bands 0-5 */
                        //if ((id->g_side_info.scfsi[ch][0] == 0) || (gr == 0))
                        if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][0] == false) || (gr == 0))
                        {
                            for (sfb = 0; sfb < 6; sfb++)
                            {
                                mainData.ScaleFac_Long[gr][ch][sfb] = (byte)reader.ReadBitsToCode(slen1);
                                counter+=slen1;
                            }
                                //id->g_main_data.scalefac_l[gr][ch][sfb] = Get_Main_Bits(id, slen1);
                        }
                        else if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][0] == true) && (gr == 1))
                        //else if ((id->g_side_info.scfsi[ch][0] == 1) && (gr == 1))
                        {
                            /* Copy scalefactors from granule 0 to granule 1 */
                            for (sfb = 0; sfb < 6; sfb++)
                            {
                                mainData.ScaleFac_Long[1][ch][sfb] = mainData.ScaleFac_Long[0][ch][sfb];
                                counter2+=slen1;
                                //counter++;
                            }
                            //id->g_main_data.scalefac_l[1][ch][sfb] = id->g_main_data.scalefac_l[0][ch][sfb];
                        }
                        /* Scale factor bands 6-10 */
                        //if ((id->g_side_info.scfsi[ch][1] == 0) || (gr == 0))
                        if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][1] == false) || (gr == 0))
                        {
                            for (sfb = 6; sfb < 11; sfb++)
                            {
                                mainData.ScaleFac_Long[gr][ch][sfb] = (byte)reader.ReadBitsToCode(slen1);
                                counter += slen1;
                            }
                                //id->g_main_data.scalefac_l[gr][ch][sfb] = Get_Main_Bits(id, slen1);
                        }
                        //else if ((id->g_side_info.scfsi[ch][1] == 1) && (gr == 1))
                        else if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][1] == true) && (gr == 1))
                        {
                            /* Copy scalefactors from granule 0 to granule 1 */
                            for (sfb = 6; sfb < 11; sfb++)
                            {
                                mainData.ScaleFac_Long[1][ch][sfb] = mainData.ScaleFac_Long[0][ch][sfb];
                                counter2+=slen1;
                            }
                            //id->g_main_data.scalefac_l[1][ch][sfb] = id->g_main_data.scalefac_l[0][ch][sfb];
                        }
                        /* Scale factor bands 11-15 */
                        //if ((id->g_side_info.scfsi[ch][2] == 0) || (gr == 0))
                        if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][2] == false) || (gr == 0))
                        {
                            for (sfb = 11; sfb < 16; sfb++)
                            {
                                mainData.ScaleFac_Long[gr][ch][sfb] = (byte)reader.ReadBitsToCode(slen2);
                                counter += slen2;
                            }
                            //id->g_main_data.scalefac_l[gr][ch][sfb] = Get_Main_Bits(id, slen2);
                        }
                        //else if ((id->g_side_info.scfsi[ch][2] == 1) && (gr == 1))
                        else if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][2] == true) && (gr == 1))
                        {
                            /* Copy scalefactors from granule 0 to granule 1 */
                            for (sfb = 11; sfb < 16; sfb++)
                            {
                                mainData.ScaleFac_Long[1][ch][sfb] = mainData.ScaleFac_Long[0][ch][sfb];
                                counter2+=slen2;
                            }
                            //id->g_main_data.scalefac_l[1][ch][sfb] = id->g_main_data.scalefac_l[0][ch][sfb];
                        }
                        /* Scale factor bands 16-20 */
                        //if ((id->g_side_info.scfsi[ch][3] == 0) || (gr == 0))
                        if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][3] == false) || (gr == 0))
                        {
                            for (sfb = 16; sfb < 21; sfb++)
                            {
                                mainData.ScaleFac_Long[gr][ch][sfb] = (byte)reader.ReadBitsToCode(slen2);
                                counter += slen2;
                            }
                            //id->g_main_data.scalefac_l[gr][ch][sfb] = Get_Main_Bits(id, slen2);
                        }
                        //else if ((id->g_side_info.scfsi[ch][3] == 1) && (gr == 1))
                        else if ((header.FrameSideInformation.ScaleFactorSelectionInformation[ch][3] == true) && (gr == 1))
                        {
                            /* Copy scalefactors from granule 0 to granule 1 */
                            for (sfb = 16; sfb < 21; sfb++)
                            {
                                mainData.ScaleFac_Long[1][ch][sfb] = mainData.ScaleFac_Long[0][ch][sfb];
                                counter2+=slen2;
                            }
                            //id->g_main_data.scalefac_l[1][ch][sfb] = id->g_main_data.scalefac_l[0][ch][sfb];
                        }
                    }
                    /* Read Huffman coded data. Skip stuffing bits. */
                    //Read_Huffman(id, part_2_start, gr, ch);
                    ReadHuffman(ref header, ref reader, ref mainData, part_2_start, gr, ch);
                } /* end for(gr... */
            } /* end for(ch... */
            header.FrameMainData = mainData;

            return new int[] { counter, counter2 };
        }

        public HuffmanTables Tables { get; set; }

        public List<byte> RawSound { get; set; }

        void AddRawSound(uint data)
        {
            //ushort lo = (ushort)(data & 0xffff);
            //ushort hi = (ushort)((data & 0xffff0000) >> 16);
            byte b1 = (byte)((data & 0xff000000) >> 24);
            byte b2 = (byte)((data & 0x00ff0000) >> 16);
            byte b3 = (byte)((data & 0x0000ff00) >> 8);
            byte b4 = (byte)((data & 0x000000ff) >> 0);

            RawSound.Add(b4);
            RawSound.Add(b3);
            RawSound.Add(b2);
            RawSound.Add(b1);
        }

        int johnnyCounter = 0;

        public void Decode(string fileName)
        {
            byte[] allBytes = File.ReadAllBytes(fileName);
            BitReader b = new BitReader(allBytes, true);
            Tables = new HuffmanTables();
            ReadID3Tag(b, false, true);
            int endPosition = b.Position = b.Length - 128 * 8;

            b.Position = 0;
            ReadID3v2Tag(b);

            List<MP3FrameHeader> frameHeaders = new List<MP3FrameHeader>();
            bool done = false;
            while(!done)
            {
                var frameHeader = ReadHeader(b);
                frameHeaders.Add(frameHeader);
                if (b.Position >= endPosition)
                    done = true;
            }

            ReadInfoHeader(frameHeaders[0]);
            List<byte> mainDataBytes = new List<byte>();
            //ulong totalMainBitsSum = 0;
            //ulong maxMainDataBegin = 0;
            byte[] previousData = null;
            byte[] previousData2 = null;
            byte[] previousData3 = null;

            RawSound = new List<byte>();
            //RawSound = new RawSoundData<uint>();
            //RawSound.Samples = new List<uint>();

            for (int z = 1; z < frameHeaders.Count; z++)
            {
                johnnyCounter = z;

                BitReader reader = new BitReader(frameHeaders[z].FrameData, true);
                var sideInfo = ReadSideInformation(frameHeaders[z], ref reader);
                int pos = reader.Position / 8;
                frameHeaders[z].FrameData = frameHeaders[z].FrameData.GetSublength(pos);
                frameHeaders[z].FrameSideInformation = sideInfo;
                frameHeaders[z].FrameMainData = new MainData(frameHeaders[z].NumberOfChannels);

                //byte[] dataBytes = reader.ReadBytes(reader.Length / 8 - reader.Position / 8);
                byte[] dataBytes = frameHeaders[z].FrameData;
                //previousData = frameHeaders[z - 1].FrameData;
                //if (z >= 2)
                //    previousData2 = frameHeaders[z - 2].FrameData;
                //if (z >= 3)
                //    previousData3 = frameHeaders[z - 3].FrameData;

                int mainDataLeft = (int)sideInfo.MainData;
                mainDataBytes = new List<byte>();
                if (mainDataLeft > 0)
                {
                    int idx = z - 1;
                    bool done1 = false;
                    while (!done1)
                    {
                        int prevLength = (frameHeaders[idx].FrameData.Length);
                        if (mainDataLeft > prevLength)
                        {
                            mainDataLeft -= prevLength;
                            idx--;
                        }
                        else
                        {
                            done1 = true;
                            mainDataBytes.AddRange(frameHeaders[idx].FrameData.GetSublength(prevLength - mainDataLeft));
                            idx++;
                            for(;idx<=z-1;idx++)
                                mainDataBytes.AddRange(frameHeaders[idx].FrameData);
                        }
                    }
                }
                mainDataBytes.AddRange(dataBytes);

                //int mainDataBegin = (previousData.Length) - mainDataLeft;
                //byte[] previousDataBegin = null;
                //byte[] previousDataBegin2 = null;
                //byte[] previousDataBegin3 = null;
                //if (mainDataBegin < 0)
                //{
                //    previousDataBegin = previousData;
                //    mainDataLeft -= previousData.Length;
                //    mainDataBegin = previousData2.Length - mainDataLeft;
                //    if (mainDataBegin < 0)
                //    {
                //        mainDataLeft = (int)sideInfo.MainData - previousData.Length - previousData2.Length;
                //        mainDataBegin = previousData3.Length - mainDataLeft;
                //        previousDataBegin3 = previousData3.GetSublength(mainDataBegin);
                //        mainDataBytes.AddRange(previousDataBegin3);
                //        mainDataBytes.AddRange(previousData2);
                //        mainDataBytes.AddRange(previousData);
                //    }
                //    else
                //    {
                //        previousDataBegin2 = previousData2.GetSublength(mainDataBegin);
                //        mainDataBytes.AddRange(previousDataBegin2);
                //        mainDataBytes.AddRange(previousDataBegin);
                //    }
                //}
                //else
                //{
                //    previousDataBegin = previousData.GetSublength(mainDataBegin);
                //    mainDataBytes.AddRange(previousDataBegin);
                //}
                //mainDataBytes.AddRange(dataBytes);

                //ulong part2_3_Sum = sideInfo.Granules[0].Channels[0].Part2_3_Length + sideInfo.Granules[0].Channels[1].Part2_3_Length + sideInfo.Granules[1].Channels[0].Part2_3_Length + sideInfo.Granules[1].Channels[1].Part2_3_Length;
                //totalMainBitsSum += part2_3_Sum;

                //int part2_3_Sum_Copy = (int)part2_3_Sum;
                //part2_3_Sum_Copy -= ((int)sideInfo.MainData * 8);

                BitReader mainDataReader = new BitReader(mainDataBytes.ToArray(), true);

                int counter2 = 0;
                for (int gr = 0; gr < 2; gr++)
                {
                    for (int ch = 0; ch < frameHeaders[z].NumberOfChannels; ch++)
                    {
                        counter2 += sideInfo.Granules[gr].Channels[ch].Part2_Length;
                    } 
                }

                if (z == 1)
                {
                    frameHeaders[z].HSynthInit = 1;
                    frameHeaders[z].SynthInit = 1;
                }
                else
                {
                    frameHeaders[z].HSynthInit = 0;
                    frameHeaders[z].SynthInit = 0;
                }

                MP3FrameHeader header = frameHeaders[z];
                int[] counters = ReadMainData(ref header, ref mainDataReader);
                //Console.WriteLine(z);
                DecodeL3(ref header, ref mainDataReader);
                frameHeaders[z] = header;

                for (int gr = 0; gr < 2; gr++)
                {
                    if (frameHeaders[z].FrameMainData.Granules[gr].OutputData != null)
                    {
                        for (int i = 0; i < 576; i++)
                            AddRawSound(frameHeaders[z].FrameMainData.Granules[gr].OutputData[i]);
                        //RawSound.Samples.AddRange(frameHeaders[z].FrameMainData.Granules[gr].OutputData);
                    }
                }

                if (z > 0 && z % 1000 == 0)
                    Console.WriteLine($"{z}: Counter1:{counters[0]+counters[1]}, Counter2:{counter2}");
                if (counters[0]+counters[1] != counter2)
                {
                    Console.WriteLine("******* MATCHUP ISSUE ********");
                    for (int gr = 0; gr < 2; gr++)
                        for (int band = 0; band < 4; band++)
                            Console.Write($"{sideInfo.ScaleFactorSelectionInformation[gr][band]}, ");
                    Console.WriteLine();
                    Console.WriteLine($"***{z}***: Counters0+1:{counters[0] + counters[1]}, Counter2:{counter2}");
                    Console.WriteLine();
                }
            }

            WAV wav = new WAV();
            wav.Save(@"c:\users\johnr\documents\outputWave1-ABR2.wav", RawSound);
        }
    }
}