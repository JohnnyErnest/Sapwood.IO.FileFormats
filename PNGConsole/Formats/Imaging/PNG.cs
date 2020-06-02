using Sapwood.IO.FileFormats.Algorithms;
using Sapwood.IO.FileFormats.Extensions;
using Sapwood.IO.FileFormats.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Sapwood.IO.FileFormats.Formats.Imaging
{
    public class PNG
    {
        public RGBImage<byte> Image { get; set; }
        public RGBImage<uint> Image16Bits { get; set; }
        public List<IDataChunk> DataChunks { get; set; }

        public class Signature
        {
            public byte[] SignatureBytes = { 137, 80, 78, 71, 13, 10, 26, 10 };

            public bool Compare(byte[] bytes)
            {
                if (bytes.Length != SignatureBytes.Length) return false;
                int pointer = 0;
                foreach (byte b in SignatureBytes)
                {
                    if (b.CompareTo(bytes[pointer]) != 0) return false;
                    pointer++;
                }
                return true;
            }

            public override string ToString()
            {
                return Encoding.Default.GetString(SignatureBytes);
            }
        }

        public interface IDataChunk
        {
            uint Length { get; set; }
            string ChunkType { get; set; }
            byte[] ChunkTypeBytes { get; set; }
            byte[] Data { get; set; }
            uint CRC { get; set; }
            IDataChunkRepresentation DataChunkRepresentation { get; set; }
        }

        public interface IDataChunkRepresentation
        {
            PNG Parent { get; set; }
            Dictionary<string, DataChunk.DataObject> ToProperties(byte[] data);
        }

        public class DataChunk : IDataChunk
        {
            public class DataObject
            {
                public Type DataType { get; set; }
                public object Value { get; set; }

                public override string ToString()
                {
                    return Value.ToString();
                }
            }

            public abstract class DataChunkRepresentationBase : IDataChunkRepresentation
            {
                public PNG Parent { get; set; }
                public DataChunkRepresentationBase(PNG parent)
                {
                    this.Parent = parent;
                }
                public abstract Dictionary<string, DataObject> ToProperties(byte[] data);
            }

            public class HeaderChunkRepresentation : DataChunkRepresentationBase
            {
                public HeaderChunkRepresentation(PNG parent) : base(parent)
                {
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    uint width = (uint)data.GetULongBE(0);
                    uint height = (uint)data.GetULongBE(4);
                    byte bitDepth = data[8];
                    byte colorType = data[9];
                    byte compressionMethod = data[10];
                    byte filterMethod = data[11];
                    byte interlaceMethod = data[12];

                    return new Dictionary<string, DataObject>()
                    {
                        { "Width", new DataObject{ DataType = typeof(uint), Value = width } },
                        { "Height", new DataObject{ DataType = typeof(uint), Value = height } },
                        { "BitDepth", new DataObject{ DataType = typeof(byte), Value = bitDepth } },
                        { "ColorType", new DataObject{ DataType = typeof(byte), Value = colorType } },
                        { "CompressionMethod", new DataObject{ DataType = typeof(byte), Value = compressionMethod } },
                        { "FilterMethod", new DataObject{ DataType = typeof(byte), Value = filterMethod } },
                        { "InterlaceMethod", new DataObject{ DataType = typeof(byte), Value = interlaceMethod } }
                    };
                }
            }

            public class GammaDataChunkRepresentation : DataChunkRepresentationBase
            {
                public GammaDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    uint gamma = (uint)data.GetULongBE(0);
                    return new Dictionary<string, DataObject>()
                    {
                        { "Gamma", new DataObject{ DataType = typeof(uint), Value = gamma } }
                    };
                }
            }

            public class PrimaryChromaticitiesAndWhitePoint : DataChunkRepresentationBase
            {
                public PrimaryChromaticitiesAndWhitePoint(PNG parent) : base(parent)
                {
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    uint whitePointX = (uint)data.GetULongBE(0);
                    uint whitePointY = (uint)data.GetULongBE(4);
                    uint redX = (uint)data.GetULongBE(8);
                    uint redY = (uint)data.GetULongBE(12);
                    uint greenX = (uint)data.GetULongBE(16);
                    uint greenY = (uint)data.GetULongBE(20);
                    uint blueX = (uint)data.GetULongBE(24);
                    uint blueY = (uint)data.GetULongBE(28);
                    return new Dictionary<string, DataObject>()
                    {
                        { "WhitePointX", new DataObject{ DataType = typeof(uint), Value = whitePointX } },
                        { "WhitePointY", new DataObject{ DataType = typeof(uint), Value = whitePointY } },
                        { "RedX", new DataObject{ DataType = typeof(uint), Value = redX } },
                        { "RedY", new DataObject{ DataType = typeof(uint), Value = redY } },
                        { "GreenX", new DataObject{ DataType = typeof(uint), Value = greenX } },
                        { "GreenY", new DataObject{ DataType = typeof(uint), Value = greenY } },
                        { "BlueX", new DataObject{ DataType = typeof(uint), Value = blueX } },
                        { "BlueY", new DataObject{ DataType = typeof(uint), Value = blueY } },
                    };
                }
            }

            public class StandardRGBDataChunkRepresentation : DataChunkRepresentationBase
            {
                //The sRGB chunk contains:
                //Rendering intent
                //1 byte
                //The following values are defined for rendering intent:
                //0 Perceptual: for images preferring good adaptation to the output device gamut at the expense of colorimetric accuracy, such as photographs.
                //1 Relative colorimetric: for images requiring colour appearance matching(relative to the output device white point), such as logos.
                //2 Saturation: for images preferring preservation of saturation at the expense of hue and lightness, such as charts and graphs.
                //3 Absolute colorimetric: for images requiring preservation of absolute colorimetry, such as previews of images destined for a different output device(proofs).
                //It is recommended that a PNG encoder that writes the sRGB chunk also write a gAMA chunk(and optionally a cHRM chunk) for compatibility with decoders that do not use the sRGB chunk.Only the following values shall be used.
                //gAMA
                //Gamma 45455
                //cHRM
                //White point x 31270
                //White point y 32900
                //Red x 64000
                //Red y 33000
                //Green x 30000
                //Green y 60000
                //Blue x 15000
                //Blue y 6000
                //When the sRGB chunk is present, it is recommended that decoders that recognize it and are capable of colour management[ICC] ignore the gAMA and cHRM chunks and use the sRGB chunk instead.Decoders that recognize the sRGB chunk but are not capable of colour management [ICC] are recommended to ignore the gAMA and cHRM chunks, and use the values given above as if they had appeared in gAMA and cHRM chunks.

                public StandardRGBDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    byte renderingIntent = data[0];
                    return new Dictionary<string, DataObject>()
                    {
                        { "RenderingIntent", new DataObject{ DataType = typeof(byte), Value = renderingIntent } }
                    };
                }
            }

            public class SignificantBitsDataChunkRepresentation : DataChunkRepresentationBase
            {
                public SignificantBitsDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    if (data.Length == 1)
                    {
                        return new Dictionary<string, DataObject>()
                        {
                            { "SignificantGreyscaleBits", new DataObject{ DataType = typeof(byte), Value = data[0] } }
                        };
                    }
                    else if (data.Length == 2)
                    {
                        return new Dictionary<string, DataObject>()
                        {
                            { "SignificantGreyscaleBits", new DataObject{ DataType = typeof(byte), Value = data[0] } },
                            { "SignificantAlphaBits", new DataObject{ DataType = typeof(byte), Value = data[1] } }
                        };
                    }
                    else if (data.Length == 3)
                    {
                        return new Dictionary<string, DataObject>()
                        {
                            { "SignificantRedBits", new DataObject{ DataType = typeof(byte), Value = data[0] } },
                            { "SignificantGreenBits", new DataObject{ DataType = typeof(byte), Value = data[1] } },
                            { "SignificantBlueBits", new DataObject{ DataType = typeof(byte), Value = data[2] } }
                        };
                    }
                    else if (data.Length == 4)
                    {
                        return new Dictionary<string, DataObject>()
                        {
                            { "SignificantRedBits", new DataObject{ DataType = typeof(byte), Value = data[0] } },
                            { "SignificantGreenBits", new DataObject{ DataType = typeof(byte), Value = data[1] } },
                            { "SignificantBlueBits", new DataObject{ DataType = typeof(byte), Value = data[2] } },
                            { "SignificantAlphaBits", new DataObject{ DataType = typeof(byte), Value = data[3] } }
                        };
                    }
                    else { return null; }
                }
            }

            public class EmbeddedICCProfileDataChunkRepresentation : DataChunkRepresentationBase
            {
                public EmbeddedICCProfileDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int counter = 0;
                    while (data[counter] != 0)
                    {
                        counter++;
                    }
                    byte[] nameBytes = new byte[counter];
                    Array.Copy(data, 0, nameBytes, 0, counter);
                    string name = Encoding.Default.GetString(nameBytes);
                    counter++;
                    byte compressionMethod = data[counter];
                    counter++;
                    int compressedBytesLength = data.Length - counter;
                    byte[] compressedBytes = new byte[compressedBytesLength];
                    Array.Copy(data, counter, compressedBytes, 0, compressedBytesLength);
                    MemoryStream memStream = new MemoryStream(compressedBytes);
                    memStream.Position = 2;
                    MemoryStream output = new MemoryStream();
                    DeflateStream stream = new DeflateStream(memStream, CompressionMode.Decompress);
                    stream.CopyTo(output);
                    output.Flush();
                    output.Position = 0;
                    byte[] uncompressedBytes = new byte[output.Length];
                    output.Read(uncompressedBytes, 0, (int)output.Length);
                    stream.Flush();
                    output.Close();
                    stream.Close();
                    memStream.Close();

                    return new Dictionary<string, DataObject>()
                    {
                        { "ICCProfileName", new DataObject{ DataType = typeof(string), Value = name } },
                        { "UncompressedBytes", new DataObject{ DataType=typeof(byte[]), Value = uncompressedBytes } }
                    };
                }
            }

            public class TextDataChunkRepresentation : DataChunkRepresentationBase
            {
                public TextDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int counter = 0;
                    while (data[counter] != 0)
                    {
                        counter++;
                    }
                    byte[] keywordBytes = new byte[counter];
                    Array.Copy(data, 0, keywordBytes, 0, counter);
                    string keyword = Encoding.Default.GetString(keywordBytes);
                    counter++;
                    int valueLength = data.Length - counter;
                    byte[] valueBytes = new byte[valueLength];
                    Array.Copy(data, counter, valueBytes, 0, valueLength);
                    string value = Encoding.Default.GetString(valueBytes);

                    return new Dictionary<string, DataObject>()
                    {
                        { "Keyword", new DataObject{ DataType = typeof(string), Value = keyword } },
                        { "Value", new DataObject{ DataType = typeof(string), Value = value } }
                    };
                }
            }

            public class ZippedTextDataChunkRepresentation : DataChunkRepresentationBase
            {
                public ZippedTextDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int counter = 0;
                    while (data[counter] != 0)
                    {
                        counter++;
                    }
                    byte[] keywordBytes = new byte[counter];
                    Array.Copy(data, 0, keywordBytes, 0, counter);
                    string keyword = Encoding.Default.GetString(keywordBytes);
                    counter++;
                    byte compressionMethod = data[counter];
                    counter++;
                    int valueLength = data.Length - counter;
                    byte[] valueBytes = new byte[valueLength];
                    Array.Copy(data, counter, valueBytes, 0, valueLength);
                    MemoryStream output = new MemoryStream();
                    MemoryStream memoryStream = new MemoryStream(valueBytes);
                    memoryStream.Position = 0;
                    DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
                    deflateStream.CopyTo(output);
                    deflateStream.Flush();
                    output.Position = 0;
                    byte[] outputBytes = new byte[output.Length];
                    output.Read(outputBytes, 0, (int)output.Length);
                    output.Flush();
                    output.Close();

                    return new Dictionary<string, DataObject>()
                    {
                        { "Keyword", new DataObject{ DataType = typeof(string), Value = keyword } },
                        { "Value", new DataObject{ DataType = typeof(string), Value = Encoding.Default.GetString(outputBytes) } }
                    };
                }
            }

            public class InternationalTextDataChunkRepresentation : DataChunkRepresentationBase
            {
                public InternationalTextDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int counter = 0;
                    while (data[counter] != 0)
                    {
                        counter++;
                    }
                    byte[] keywordBytes = new byte[counter];
                    Array.Copy(data, 0, keywordBytes, 0, counter);
                    string keyword = Encoding.Default.GetString(keywordBytes);
                    counter++;

                    byte compressionFlag = data[counter];
                    counter++;
                    byte compressionMethod = data[counter];
                    counter++;

                    int languageTagLength = 0;
                    int languageTagStart = counter;
                    while (data[counter] != 0)
                    {
                        counter++;
                        languageTagLength++;
                    }
                    byte[] languageTagBytes = new byte[languageTagLength];
                    Array.Copy(data, languageTagStart, languageTagBytes, 0, languageTagLength);
                    string languageTag = Encoding.UTF8.GetString(languageTagBytes);
                    counter++;

                    int translatedKeywordLength = 0;
                    int translatedKeywordStart = counter;
                    while (data[counter] != 0)
                    {
                        counter++;
                        translatedKeywordLength++;
                    }
                    byte[] translatedKeywordBytes = new byte[translatedKeywordLength];
                    Array.Copy(data, translatedKeywordStart, translatedKeywordBytes, 0, translatedKeywordLength);
                    string translatedKeyword = Encoding.UTF8.GetString(translatedKeywordBytes);
                    counter++;

                    string text = "";

                    if (compressionFlag == 0)
                    {
                        int valueLength = data.Length - counter;
                        byte[] valueBytes = new byte[valueLength];
                        Array.Copy(data, counter, valueBytes, 0, valueLength);
                        text = Encoding.UTF8.GetString(valueBytes);
                    }
                    else
                    {
                        int valueLength = data.Length - counter;
                        byte[] valueBytes = new byte[valueLength];
                        Array.Copy(data, counter, valueBytes, 0, valueLength);
                        MemoryStream output = new MemoryStream();
                        MemoryStream memoryStream = new MemoryStream(valueBytes);
                        memoryStream.Position = 0;
                        DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
                        deflateStream.CopyTo(output);
                        deflateStream.Flush();
                        output.Position = 0;
                        byte[] outputBytes = new byte[output.Length];
                        output.Read(outputBytes, 0, (int)output.Length);
                        output.Flush();
                        output.Close();
                        text = Encoding.UTF8.GetString(outputBytes);
                    }

                    return new Dictionary<string, DataObject>()
                    {
                        { "Keyword", new DataObject{ DataType = typeof(string), Value = keyword } },
                        { "TranslatedKeyword", new DataObject{ DataType = typeof(string), Value = translatedKeyword } },
                        { "LanguageTag", new DataObject{ DataType = typeof(string), Value = languageTag } },
                        { "Value", new DataObject{ DataType = typeof(string), Value = text } }
                    };
                }
            }


            public class HistogramDataChunkRepresentation : DataChunkRepresentationBase
            {
                public HistogramDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int p = 0;
                    uint[] frequencies = new uint[256];
                    for (int z = 0; z < 256; z++)
                    {
                        uint freq = data.GetUIntBE(p);
                        frequencies[z] = freq;
                        p += 2;
                    }

                    return new Dictionary<string, DataObject>()
                    {
                        { "Frequencies", new DataObject{ DataType = typeof(uint[]), Value = frequencies } }
                    };
                }
            }

            public class TimestampDataChunkRepresentation : DataChunkRepresentationBase
            {
                public TimestampDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int year = Convert.ToInt32(data.GetUIntBE(0));
                    int month = (int)data[2];
                    int day = (int)data[3];
                    int hour = (int)data[4];
                    int minute = (int)data[5];
                    int second = (int)data[6];
                    DateTime dateTime = new DateTime(year, month, day, hour, minute, second);

                    return new Dictionary<string, DataObject>()
                    {
                        { "DateTime", new DataObject{ DataType = typeof(DateTime), Value = dateTime } }
                    };
                }
            }

            public class PaletteDataChunkRepresentation : DataChunkRepresentationBase
            {
                public PaletteDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    RGBAColor<byte>[] colors = new RGBAColor<byte>[256];
                    int p = 0;
                    for (int z = 0; z < 256; z++)
                    {
                        RGBAColor<byte> color = new RGBAColor<byte>(data[p], data[p + 1], data[p + 2], 255);
                        p += 3;
                        colors[z] = color;
                    }

                    return new Dictionary<string, DataObject>()
                    {
                        { "Colors", new DataObject{ DataType = typeof(RGBAColor<byte>[]), Value = colors } }
                    };
                }
            }

            public class SuggestedPaletteDataChunkRepresentation : DataChunkRepresentationBase
            {
                public class PaletteEntryDepth8
                {
                    public RGBAColor<byte> Color { get; set; }
                    public uint RelativeFrequency { get; set; }
                }

                public class PaletteEntryDepth16
                {
                    public RGBAColor<uint> Color { get; set; }
                    public uint RelativeFrequency { get; set; }
                }

                public SuggestedPaletteDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    int counter = 0;
                    while (data[counter] != 0)
                    {
                        counter++;
                    }
                    byte[] paletteNameBytes = new byte[counter];
                    Array.Copy(data, 0, paletteNameBytes, 0, counter);
                    string paletteName = Encoding.Default.GetString(paletteNameBytes);
                    counter++;

                    byte sampleDepth = data[counter];
                    counter++;

                    int dataLeft = data.Length - counter;

                    int entries = (sampleDepth == 8) ? dataLeft / 6 : dataLeft / 10;

                    if (sampleDepth == 8)
                    {
                        List<PaletteEntryDepth8> palette = new List<PaletteEntryDepth8>();
                        for (int z = 0; z < entries; z++)
                        {
                            byte r = data[counter];
                            counter++;
                            byte g = data[counter];
                            counter++;
                            byte b = data[counter];
                            counter++;
                            byte a = data[counter];
                            counter++;
                            uint freq = data.GetUIntBE(counter);
                            counter += 2;
                            palette.Add(new PaletteEntryDepth8()
                            {
                                Color = new RGBAColor<byte>(r, g, b, a),
                                RelativeFrequency = freq
                            });
                        }

                        return new Dictionary<string, DataObject>()
                        {
                            { "PaletteEntriesDepth8", new DataObject{ DataType = typeof(PaletteEntryDepth8[]), Value = palette.ToArray() } }
                        };
                    }
                    else
                    {
                        List<PaletteEntryDepth16> palette = new List<PaletteEntryDepth16>();
                        for (int z = 0; z < entries; z++)
                        {
                            uint r = data.GetUIntBE(counter);
                            counter += 2;
                            uint g = data.GetUIntBE(counter); 
                            counter += 2;
                            uint b = data.GetUIntBE(counter); 
                            counter += 2;
                            uint a = data.GetUIntBE(counter); 
                            counter += 2;
                            uint freq = data.GetUIntBE(counter); 
                            counter += 2;
                            palette.Add(new PaletteEntryDepth16()
                            {
                                Color = new RGBAColor<uint>(r, g, b, a),
                                RelativeFrequency = freq
                            });
                        }

                        return new Dictionary<string, DataObject>()
                        {
                            { "PaletteEntriesDepth16", new DataObject{ DataType = typeof(PaletteEntryDepth16[]), Value = palette.ToArray() } }
                        };
                    }
                }
            }

            public class VirtualPageDataChunkRepresentation : DataChunkRepresentationBase
            {
                public VirtualPageDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    uint virtualPageWidth = (uint)data.GetULongBE(0);
                    uint virtualPageHeight = (uint)data.GetULongBE(4);
                    byte virtualPageUnits = data[8];
                    return new Dictionary<string, DataObject>()
                    {
                        { "VirtualPageWidth", new DataObject{ DataType = typeof(uint), Value = virtualPageWidth } },
                        { "VirtualPageHeight", new DataObject{ DataType = typeof(uint), Value = virtualPageHeight } },
                        { "VirtualPageUnits", new DataObject{ DataType = typeof(uint), Value = virtualPageUnits } }
                    };
                }
            }

            public class BackgroundColorDataChunkRepresentation : DataChunkRepresentationBase
            {
                public BackgroundColorDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    Dictionary<string, DataObject> returnValue = new Dictionary<string, DataObject>();
                    if (data.Length == 1)
                    {
                        returnValue.Add("PaletteIndex", new DataObject() { DataType = typeof(byte), Value = data[0] });
                    }
                    else if (data.Length == 2)
                    {
                        returnValue.Add("Greyscale", new DataObject() { DataType = typeof(uint), Value = ((uint)data[0] << 8 | (uint)data[1]) });
                    }
                    else
                    {
                        returnValue.Add("Color", new DataObject() { DataType = typeof(RGBAColor<byte>), Value = (new RGBAColor<byte>(data[0], data[1], data[2], 255)) });
                    }
                    return returnValue;
                }
            }

            public class TransparentColorDataChunkRepresentation : DataChunkRepresentationBase
            {
                public TransparentColorDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    Dictionary<string, DataObject> returnValue = new Dictionary<string, DataObject>();
                    if (data.Length == 1)
                    {
                        returnValue.Add("PaletteIndex", new DataObject() { DataType = typeof(byte), Value = data[0] });
                    }
                    else if (data.Length == 2)
                    {
                        returnValue.Add("Greyscale", new DataObject() { DataType = typeof(uint), Value = data.GetUIntBE(0) });;
                    }
                    else if (data.Length >= 3)
                    {
                        int p = 0;
                        for (int z = 0; z < 256; z++)
                        {
                            returnValue.Add("AlphaColor" + z, new DataObject() { DataType = typeof(RGBAColor<byte>), Value = (new RGBAColor<byte>(data[p + 0], data[p + 1], data[p + 2], 255)) });
                            p += 3;
                        }
                    }
                    return returnValue;
                }
            }

            public class PhysicalPixelDimensionsDataChunkRepresentation : DataChunkRepresentationBase
            {
                public PhysicalPixelDimensionsDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    uint ppux = (uint)data.GetUIntBE(0);
                    uint ppuy = (uint)data.GetUIntBE(4);
                    byte unitSpecification = data[8];
                    return new Dictionary<string, DataObject>()
                    {
                        { "PixelsPerUnitX", new DataObject{ DataType = typeof(uint), Value = ppux } },
                        { "PixelsPerUnitY", new DataObject{ DataType = typeof(uint), Value = ppuy } },
                        { "UnitSpecification", new DataObject{ DataType = typeof(byte), Value = unitSpecification } }
                    };
                }
            }

            public class ImageDataChunkRepresentation : DataChunkRepresentationBase
            {
                public ImageDataChunkRepresentation(PNG parent) : base(parent)
                {
                }
                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "Data", new DataObject{ DataType = typeof(byte[]), Value = data } },
                    };
                }
            }

            public uint Length { get; set; }
            public string ChunkType { get; set; }
            public byte[] ChunkTypeBytes { get; set; }
            public byte[] Data { get; set; }
            public uint CRC { get; set; }
            public bool CRCPassed { get; set; }
            public IDataChunkRepresentation DataChunkRepresentation { get; set; }
            public string PropertyString { get; set; }

            public DataChunk(BinaryReader reader, PNG parent)
            {
                byte[] lengthBytes = reader.ReadBytes(4);
                Length = (uint)lengthBytes.GetULongBE(0);

                byte[] typeBytes = reader.ReadBytes(4);
                ChunkTypeBytes = typeBytes;
                ChunkType = Encoding.Default.GetString(typeBytes);

                Data = reader.ReadBytes((int)Length);

                byte[] crcBytes = reader.ReadBytes(4);
                CRC = (uint)crcBytes.GetULongBE(0);

                byte[] checkBytes = new byte[Data.Length + typeBytes.Length];
                Array.Copy(typeBytes, 0, checkBytes, 0, typeBytes.Length);
                int dst = typeBytes.Length;
                Array.Copy(Data, 0, checkBytes, dst, Data.Length);

                CRC crc = new CRC();
                ulong crc_result = crc.crc(checkBytes, checkBytes.Length);
                if (CRC == crc_result)
                    CRCPassed = true;
                else CRCPassed = false;

                switch (ChunkType.ToLower())
                {
                    case "bkgd": DataChunkRepresentation = new BackgroundColorDataChunkRepresentation(parent); break;
                    case "chrm": DataChunkRepresentation = new PrimaryChromaticitiesAndWhitePoint(parent); break;
                    case "gama": DataChunkRepresentation = new GammaDataChunkRepresentation(parent); break;
                    case "hist": DataChunkRepresentation = new HistogramDataChunkRepresentation(parent); break;
                    case "iccp": DataChunkRepresentation = new EmbeddedICCProfileDataChunkRepresentation(parent); break;
                    case "idat": DataChunkRepresentation = new ImageDataChunkRepresentation(parent); break;
                    case "ihdr": DataChunkRepresentation = new HeaderChunkRepresentation(parent); break;
                    case "itxt": DataChunkRepresentation = new InternationalTextDataChunkRepresentation(parent); break;
                    case "phys": DataChunkRepresentation = new PhysicalPixelDimensionsDataChunkRepresentation(parent); break;
                    case "plte": DataChunkRepresentation = new PaletteDataChunkRepresentation(parent); break;
                    case "sbit": DataChunkRepresentation = new SignificantBitsDataChunkRepresentation(parent); break;
                    case "splt": DataChunkRepresentation = new SuggestedPaletteDataChunkRepresentation(parent); break;
                    case "srgb": DataChunkRepresentation = new StandardRGBDataChunkRepresentation(parent); break;
                    case "text": DataChunkRepresentation = new TextDataChunkRepresentation(parent); break;
                    case "time": DataChunkRepresentation = new TimestampDataChunkRepresentation(parent); break;
                    case "vpag": DataChunkRepresentation = new VirtualPageDataChunkRepresentation(parent); break;
                    case "ztxt": DataChunkRepresentation = new ZippedTextDataChunkRepresentation(parent); break;
                    default: break;
                }

                if (DataChunkRepresentation != null)
                {
                    StringBuilder sb = new StringBuilder();
                    var values = DataChunkRepresentation.ToProperties(Data);
                    foreach (var val in values)
                    {
                        sb.AppendLine(val.Key + " = " + val.Value.ToString());
                    }
                    PropertyString = sb.ToString();
                }

                if (!CRCPassed)
                {
                    throw new InvalidDataException($@"CRC Failure reading data chunk of type {ChunkType} - Expected CRC {CRC}, Actual Checksum Result {crc_result}");
                }
            }
        }

        ulong UIntPow(uint x, int times)
        {
            ulong val = x;
            for (int z = 2; z <= times; z++)
            {
                val *= x;
            }
            return val;
        }

        public ulong CalculateCRCPolynomial(uint x)
        {
            return 1 + (x) + (x * x) + UIntPow(x, 4) + UIntPow(x, 5) + UIntPow(x, 7) +
                UIntPow(x, 8) + UIntPow(x, 10) + UIntPow(x, 11) + UIntPow(x, 12) +
                UIntPow(x, 16) + UIntPow(x, 22) + UIntPow(x, 23) + UIntPow(x, 26) + UIntPow(x, 32);
        }

        public PNG(string fileName)
        {
            Signature sig = new Signature();
            FileStream fileStream = File.OpenRead(fileName);
            BinaryReader reader = new BinaryReader(fileStream);
            byte[] signature = reader.ReadBytes(8);
            Console.WriteLine(sig.Compare(signature) + " - " + sig.ToString());

            DataChunks = new List<IDataChunk>();

            List<byte[]> allDataBytes = new List<byte[]>();
            List<DataChunk> allChunks = new List<DataChunk>();
            while ((reader.BaseStream.Position < reader.BaseStream.Length))
            {
                DataChunk chunk = new DataChunk(reader, this);
                ulong chunkLen = CalculateCRCPolynomial((uint)chunk.Data.Length + 4);

                if (chunk.DataChunkRepresentation is DataChunk.ImageDataChunkRepresentation)
                {
                    allDataBytes.Add(chunk.Data);
                }
                else
                {
                    Console.WriteLine($"[{chunk.Length}],[{chunk.ChunkType}],[{chunk.CRC}],[{chunk.Data.Length}]");
                    if (!string.IsNullOrEmpty(chunk.PropertyString))
                    {
                        Console.WriteLine(chunk.PropertyString);
                    }
                    DataChunks.Add(chunk);
                    allChunks.Add(chunk);
                }
            }
            reader.Close();
            fileStream.Close();
            int allBytesCount = 0;
            for (int z = 0; z < allDataBytes.Count; z++)
            {
                allBytesCount += allDataBytes[z].Length;
            }
            byte[] dataBytes = new byte[allBytesCount];
            allBytesCount = 0;
            for (int z = 0; z < allDataBytes.Count; z++)
            {
                allDataBytes[z].CopyTo(dataBytes, allBytesCount);
                allBytesCount += allDataBytes[z].Length;
            }

            MemoryStream output = new MemoryStream();
            MemoryStream memoryStream = new MemoryStream(dataBytes);
            memoryStream.Position = 0;
            byte[] zlibHeaderBytes = new byte[2];
            memoryStream.Read(zlibHeaderBytes, 0, 2);
            DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
            deflateStream.CopyTo(output);
            deflateStream.Flush();
            output.Position = 0;
            byte[] outputBytes = new byte[output.Length];
            int arrLength = (int)output.Length;
            output.Read(outputBytes, 0, arrLength);

            MemoryStream streamReader = new MemoryStream(outputBytes);
            DataChunk header = allChunks.Where(x => x.ChunkType.ToLower() == "ihdr").FirstOrDefault();
            Dictionary<string, DataChunk.DataObject> headerProperties = header.DataChunkRepresentation.ToProperties(header.Data);
            uint width = (uint)headerProperties["Width"].Value;
            uint height = (uint)headerProperties["Height"].Value;
            byte colorType = (byte)headerProperties["ColorType"].Value;
            byte bitDepth = (byte)headerProperties["BitDepth"].Value;

            streamReader.Position = 0;

            if (bitDepth <= 8)
            {
                RGBImage<byte> image = new RGBImage<byte>(width, height);
                for (uint y = 0; y < height; y++)
                {
                    byte filterByte = (byte)streamReader.ReadByte();
                    if (filterByte == 0)
                    {
                        if (colorType == 2)
                        {
                            for (uint x = 0; x < width; x++)
                            {
                                byte r = (byte)streamReader.ReadByte();
                                byte g = (byte)streamReader.ReadByte();
                                byte b = (byte)streamReader.ReadByte();
                                RGBAColor<byte> color = new RGBAColor<byte>(r, g, b, 255);
                                image.Colors[x, y] = color;
                            }
                        }
                        else if (colorType == 6)
                        {
                            for (uint x = 0; x < width; x++)
                            {
                                byte r = (byte)streamReader.ReadByte();
                                byte g = (byte)streamReader.ReadByte();
                                byte b = (byte)streamReader.ReadByte();
                                byte a = (byte)streamReader.ReadByte();
                                RGBAColor<byte> color = new RGBAColor<byte>(r, g, b, a);
                                image.Colors[x, y] = color;
                            }
                        }
                    }
                }
                deflateStream.Close();
                memoryStream.Close();
                output.Close();
                streamReader.Close();
                Image = image;
            }
            else if (bitDepth == 16)
            {
                RGBImage<uint> image = new RGBImage<uint>(width, height);
                for (uint y = 0; y < height; y++)
                {
                    byte filterByte = (byte)streamReader.ReadByte();
                    if (filterByte == 0)
                    {
                        if (colorType == 2)
                        {
                            for (uint x = 0; x < width; x++)
                            {
                                uint r = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                uint g = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                uint b = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                RGBAColor<uint> color = new RGBAColor<uint>(r, g, b, 255);
                                image.Colors[x, y] = color;
                            }
                        }
                        else if (colorType == 6)
                        {
                            for (uint x = 0; x < width; x++)
                            {
                                uint r = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                uint g = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                uint b = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                uint a = (uint)streamReader.ReadByte() << 8 | (uint)streamReader.ReadByte();
                                RGBAColor<uint> color = new RGBAColor<uint>(r, g, b, a);
                                image.Colors[x, y] = color;
                            }
                        }
                    }
                }
                deflateStream.Close();
                memoryStream.Close();
                output.Close();
                streamReader.Close();
                Image16Bits = image;
            }
        }
    }
}