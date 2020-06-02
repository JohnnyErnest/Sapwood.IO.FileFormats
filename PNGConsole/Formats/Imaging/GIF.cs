using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using PNGConsole.Algorithms;
using Sapwood.IO.FileFormats.Algorithms;
using Sapwood.IO.FileFormats.Extensions;
using Sapwood.IO.FileFormats.Imaging;

namespace Sapwood.IO.FileFormats.Formats.Imaging
{
    public class GIF
    {
        public class Header
        {
            public string Signature { get; set; }
            public string Version { get; set; }

            public ushort LogicalScreenWidth { get; set; }
            public ushort LogicalScreenHeight { get; set; }
            public byte GlobalColorTableFlag { get; set; }
            public byte ColorResolution { get; set; }
            public byte SortFlag { get; set; }
            public byte GlobalColorTableSize { get; set; }
            public byte BackgroundColorIndex { get; set; }
            public byte PixelAspectRatio { get; set; }
            public List<List<RGBAColor<byte>>> GlobalPalettes { get; set; }
            public List<ImageDescriptor> ImageDescriptors { get; set; }

            public Header(BinaryReader input)
            {
                Signature = Encoding.Default.GetString(input.ReadBytes(3));
                Version = Encoding.Default.GetString(input.ReadBytes(3));
                LogicalScreenWidth = (ushort)input.ReadBytes(2).GetUIntLE(0);
                LogicalScreenHeight = (ushort)input.ReadBytes(2).GetUIntLE(0);
                byte packedByte = input.ReadByte();
                GlobalColorTableFlag = (byte)((packedByte >> 7) & 1);
                ColorResolution = (byte)((packedByte >> 4) & 0b111);
                SortFlag = (byte)((packedByte >> 3) & 1);
                GlobalColorTableSize = (byte)(packedByte & 0b111);
                BackgroundColorIndex = input.ReadByte();
                PixelAspectRatio = input.ReadByte();

                if (GlobalColorTableFlag == 1)
                {
                    int globalColorTableByteCount = 3 * (int)Math.Pow((double)2, (double)(GlobalColorTableSize + 1));
                    int idx = 0;
                    List<RGBAColor<byte>> colors = new List<RGBAColor<byte>>();
                    for (int z=0;z< globalColorTableByteCount;z+=3)
                    {
                        byte r = input.ReadByte();
                        byte g = input.ReadByte();
                        byte b = input.ReadByte();
                        RGBAColor<byte> color = new RGBAColor<byte>(r, g, b, 255);
                        colors.Add(color);
                        idx++;
                    }
                    if (GlobalPalettes == null)
                    {
                        GlobalPalettes = new List<List<RGBAColor<byte>>>();
                        GlobalPalettes.Add(colors);
                    }
                }
            }

            public class ImageDescriptor
            {
                public ushort ImageLeftPosition { get; set; }
                public ushort ImageTopPosition { get; set; }
                public ushort ImageWidth { get; set; }
                public ushort ImageHeight { get; set; }
                public byte LocalColorTableFlag { get; set; }
                public byte InterlaceFlag { get; set; }
                public byte SortFlag { get; set; }
                public byte Reserved { get; set; }
                public byte LocalColorTableSize { get; set; }
                public List<RGBAColor<byte>> LocalColorPalette { get; set; }
                public Header Parent { get; set; }

                public ImageDescriptor(BinaryReader input, Header parent)
                {
                    ImageLeftPosition = (ushort)input.ReadBytes(2).GetUIntLE(0);
                    ImageTopPosition = (ushort)input.ReadBytes(2).GetUIntLE(0);
                    ImageWidth = (ushort)input.ReadBytes(2).GetUIntLE(0);
                    ImageHeight = (ushort)input.ReadBytes(2).GetUIntLE(0);
                    byte packedByte = input.ReadByte();
                    LocalColorTableFlag = (byte)((packedByte >> 7) & 1);
                    InterlaceFlag = (byte)((packedByte >> 6) & 1);
                    SortFlag = (byte)((packedByte >> 5) & 1);
                    Reserved = (byte)((packedByte >> 3) & 0b11);
                    LocalColorTableSize = (byte)((packedByte) & 0b111);

                    if (LocalColorTableFlag == 1)
                    {
                        int localColorTableByteCount = 3 * (int)Math.Pow((double)2, (double)(LocalColorTableFlag + 1));
                        int idx = 0;
                        List<RGBAColor<byte>> colors = new List<RGBAColor<byte>>();
                        for (int z = 0; z < localColorTableByteCount; z += 3)
                        {
                            byte r = input.ReadByte();
                            byte g = input.ReadByte();
                            byte b = input.ReadByte();
                            RGBAColor<byte> color = new RGBAColor<byte>(r, g, b, 255);
                            colors.Add(color);
                            idx++;
                        }
                        LocalColorPalette = new List<RGBAColor<byte>>();
                        colors.AddRange(colors);
                    }

                    if (parent.ImageDescriptors == null)
                        parent.ImageDescriptors = new List<ImageDescriptor>();
                    parent.ImageDescriptors.Add(this);
                    Parent = parent;
                }
            }

            public class DataObject
            {
                public Type DataType { get; set; }
                public object Value { get; set; }

                public override string ToString()
                {
                    return Value.ToString();
                }
            }


            public interface IExtension
            {
                Dictionary<string, DataObject> ToProperties();
                Dictionary<string, DataObject> ToProperties(byte[] data);
            }

            public class DataSubBlock
            {
                public IExtension ParentExtension { get; set; }
                public Header ParentHeader { get; set; }
                public byte BlockLength { get; set; }
                public byte[] BlockData { get; set; }
                public bool IsBlockTerminator { get; set; }

                public DataSubBlock(BinaryReader reader, Header parentHeader, IExtension parentExtension = null)
                {
                    if (parentExtension != null) ParentExtension = parentExtension;
                    ParentHeader = parentHeader;
                    BlockLength = reader.ReadByte();
                    IsBlockTerminator = false;
                    if (BlockLength == 0) IsBlockTerminator = true;
                    else BlockData = reader.ReadBytes(BlockLength);
                }

                public DataSubBlock(byte blockLength, byte[] blockData, Header parentHeader, IExtension parentExtension = null)
                {
                    if (parentExtension != null) ParentExtension = parentExtension;
                    ParentHeader = parentHeader;
                    BlockLength = BlockLength;
                    IsBlockTerminator = false;
                    if (BlockLength == 0) IsBlockTerminator = true;
                    else BlockData = blockData;
                }
            }

            public abstract class ExtensionBase : IExtension
            {
                public Header Parent { get; set; }

                public ExtensionBase(Header parent)
                {
                    Parent = parent;
                }

                public abstract Dictionary<string, DataObject> ToProperties();
                public abstract Dictionary<string, DataObject> ToProperties(byte[] data);
            }

            public class GraphicControlExtension : ExtensionBase
            {
                public byte BlockSize { get; set; }
                public byte Reserved { get; set; }
                public byte DisposalMethod { get; set; }
                public byte UserInputFlag { get; set; }
                public byte TransparentFlag { get; set; }
                public ushort DelayTime { get; set; }
                public byte TransparentColorIndex { get; set; }
                public GraphicControlExtension(Header parent) : base(parent)
                {

                }

                public GraphicControlExtension(Header parent, BinaryReader input) : base(parent)
                {
                    BlockSize = input.ReadByte();
                    byte packedFields = input.ReadByte();
                    Reserved = (byte)((packedFields >> 5) & 0b111);
                    DisposalMethod = (byte)((packedFields >> 2) & 0b111);
                    UserInputFlag = (byte)((packedFields >> 1) & 0b1);
                    TransparentColorIndex = (byte)((packedFields) & 0b1);
                    DelayTime = (ushort)(input.ReadBytes(2)).GetUIntLE(0);
                    TransparentColorIndex = input.ReadByte();
                    if (input.ReadByte() != 0)
                    {
                        throw new FileLoadException("Unexpected data in GIF image while reading Graphic Control Extension - did not find Block Terminator");
                    }
                }

                public override Dictionary<string, DataObject> ToProperties()
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "BlockSize", new DataObject() { DataType = typeof(byte), Value = BlockSize } },
                        { "Reserved", new DataObject() { DataType = typeof(byte), Value = Reserved } },
                        { "DisposalMethod", new DataObject() { DataType = typeof(byte), Value = DisposalMethod } },
                        { "UserInputFlag", new DataObject() { DataType = typeof(byte), Value = UserInputFlag } },
                        { "TransparentFlag", new DataObject() { DataType = typeof(byte), Value = TransparentFlag } },
                        { "DelayTime", new DataObject() { DataType = typeof(ushort), Value = DelayTime } },
                        { "TransparentColorIndex", new DataObject() { DataType = typeof(byte), Value = TransparentColorIndex } }
                    };
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "BlockSize", new DataObject() { DataType = typeof(byte), Value = BlockSize } },
                        { "Reserved", new DataObject() { DataType = typeof(byte), Value = Reserved } },
                        { "DisposalMethod", new DataObject() { DataType = typeof(byte), Value = DisposalMethod } },
                        { "UserInputFlag", new DataObject() { DataType = typeof(byte), Value = UserInputFlag } },
                        { "TransparentFlag", new DataObject() { DataType = typeof(byte), Value = TransparentFlag } },
                        { "DelayTime", new DataObject() { DataType = typeof(ushort), Value = DelayTime } },
                        { "TransparentColorIndex", new DataObject() { DataType = typeof(byte), Value = TransparentColorIndex } }
                    };
                }
            }

            public class CommentExtension : ExtensionBase
            {
                public List<DataSubBlock> SubBlocks { get; set; }

                public CommentExtension(Header parent) : base(parent)
                {

                }

                public CommentExtension(Header parent, BinaryReader input) : base(parent)
                {
                    SubBlocks = new List<DataSubBlock>();
                    bool isDone = false;
                    while(!isDone)
                    {
                        DataSubBlock subBlock = new DataSubBlock(input, parent, this);
                        if (subBlock.IsBlockTerminator)
                            isDone = true;
                        else 
                            SubBlocks.Add(subBlock);
                    }
                }

                public override Dictionary<string, DataObject> ToProperties()
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }
            }

            public class PlainTextExtension : ExtensionBase
            {
                public ushort TextGridLeftPosition { get; set; }
                public ushort TextGridTopPosition { get; set; }
                public ushort TextGridWidth { get; set; }
                public ushort TextGridHeight { get; set; }
                public byte CharacterCellWidth { get; set; }
                public byte CharacterCellHeight { get; set; }
                public byte TextForegroundColorIndex { get; set; }
                public byte TextBackgroundColorIndex { get; set; }
                public List<DataSubBlock> SubBlocks { get; set; }

                public PlainTextExtension(Header parent) : base(parent)
                {

                }

                public PlainTextExtension(Header parent, BinaryReader input) : base(parent)
                {
                    TextGridLeftPosition = (ushort)(input.ReadBytes(2)).GetUIntLE(0);
                    TextGridTopPosition = (ushort)(input.ReadBytes(2)).GetUIntLE(0);
                    TextGridWidth = (ushort)(input.ReadBytes(2)).GetUIntLE(0);
                    TextGridHeight = (ushort)(input.ReadBytes(2)).GetUIntLE(0);
                    CharacterCellWidth = input.ReadByte();
                    CharacterCellHeight = input.ReadByte();
                    TextForegroundColorIndex = input.ReadByte();
                    TextBackgroundColorIndex = input.ReadByte();

                    SubBlocks = new List<DataSubBlock>();
                    bool isDone = false;
                    while (!isDone)
                    {
                        DataSubBlock subBlock = new DataSubBlock(input, parent, this);
                        if (subBlock.IsBlockTerminator)
                            isDone = true;
                        else
                            SubBlocks.Add(subBlock);
                    }
                }

                public override Dictionary<string, DataObject> ToProperties()
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "TextGridLeftPosition", new DataObject() { DataType = typeof(ushort), Value = TextGridLeftPosition } },
                        { "TextGridTopPosition", new DataObject() { DataType = typeof(ushort), Value = TextGridTopPosition } },
                        { "TextGridWidth", new DataObject() { DataType = typeof(ushort), Value = TextGridWidth } },
                        { "TextGridHeight", new DataObject() { DataType = typeof(ushort), Value = TextGridHeight } },
                        { "CharacterCellWidth", new DataObject() { DataType = typeof(byte), Value = CharacterCellWidth } },
                        { "CharacterCellHeight", new DataObject() { DataType = typeof(byte), Value = CharacterCellHeight } },
                        { "TextForegroundColorIndex", new DataObject() { DataType = typeof(byte), Value = TextForegroundColorIndex } },
                        { "TextBackgroundColorIndex", new DataObject() { DataType = typeof(byte), Value = TextBackgroundColorIndex } },
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "TextGridLeftPosition", new DataObject() { DataType = typeof(ushort), Value = TextGridLeftPosition } },
                        { "TextGridTopPosition", new DataObject() { DataType = typeof(ushort), Value = TextGridTopPosition } },
                        { "TextGridWidth", new DataObject() { DataType = typeof(ushort), Value = TextGridWidth } },
                        { "TextGridHeight", new DataObject() { DataType = typeof(ushort), Value = TextGridHeight } },
                        { "CharacterCellWidth", new DataObject() { DataType = typeof(byte), Value = CharacterCellWidth } },
                        { "CharacterCellHeight", new DataObject() { DataType = typeof(byte), Value = CharacterCellHeight } },
                        { "TextForegroundColorIndex", new DataObject() { DataType = typeof(byte), Value = TextForegroundColorIndex } },
                        { "TextBackgroundColorIndex", new DataObject() { DataType = typeof(byte), Value = TextBackgroundColorIndex } },
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }
            }

            public class ApplicationExtension : ExtensionBase
            {
                public byte BlockSize { get; set; }
                public string ApplicationIdentifier { get; set; }
                public byte[] ApplicationAuthorizationCode { get; set; }
                public List<DataSubBlock> SubBlocks { get; set; }

                public ApplicationExtension(Header parent) : base(parent)
                {

                }

                public ApplicationExtension(Header parent, BinaryReader input) : base(parent)
                {
                    BlockSize = input.ReadByte();
                    ApplicationIdentifier = Encoding.Default.GetString(input.ReadBytes(8));
                    ApplicationAuthorizationCode = input.ReadBytes(3);

                    SubBlocks = new List<DataSubBlock>();
                    bool isDone = false;
                    while (!isDone)
                    {
                        DataSubBlock subBlock = new DataSubBlock(input, parent, this);
                        if (subBlock.IsBlockTerminator)
                            isDone = true;
                        else
                            SubBlocks.Add(subBlock);
                    }
                }

                public override Dictionary<string, DataObject> ToProperties()
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "BlockSize", new DataObject() { DataType = typeof(byte), Value = BlockSize } },
                        { "ApplicationIdentifier", new DataObject() { DataType = typeof(byte), Value = ApplicationIdentifier } },
                        { "ApplicationAuthorizationCode", new DataObject() { DataType = typeof(byte), Value = ApplicationAuthorizationCode } },
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }

                public override Dictionary<string, DataObject> ToProperties(byte[] data)
                {
                    return new Dictionary<string, DataObject>()
                    {
                        { "BlockSize", new DataObject() { DataType = typeof(byte), Value = BlockSize } },
                        { "ApplicationIdentifier", new DataObject() { DataType = typeof(byte), Value = ApplicationIdentifier } },
                        { "ApplicationAuthorizationCode", new DataObject() { DataType = typeof(byte), Value = ApplicationAuthorizationCode } },
                        { "SubBlocks", new DataObject() { DataType = typeof(List<DataSubBlock>), Value = SubBlocks } }
                    };
                }
            }
        }

        public List<Header.IExtension> Extensions { get; set; }

        public GIF(string fileName)
        {
            LZW lzw = new LZW();
            FileStream fileStream = File.OpenRead(fileName);
            BinaryReader reader = new BinaryReader(fileStream);
            Header header = new Header(reader);
            Extensions = new List<Header.IExtension>();
            bool eof = false;
            int ccount = 0, dcount = 0;
            while (!eof)
            {
                byte nextByte = reader.ReadByte();
                switch(nextByte)
                {
                    case 0x21:
                        // Extension
                        byte extensionTypeByte = reader.ReadByte();
                        switch(extensionTypeByte)
                        {
                            case 0xF9:
                                {
                                    Header.GraphicControlExtension extension = new Header.GraphicControlExtension(header, reader);
                                    Extensions.Add(extension);
                                }
                                break;
                            case 0xFE:
                                {
                                    Header.CommentExtension extension = new Header.CommentExtension(header, reader);
                                    Extensions.Add(extension);
                                }
                                break;
                            case 0x01:
                                {
                                    Header.PlainTextExtension extension = new Header.PlainTextExtension(header, reader);
                                    Extensions.Add(extension);
                                }
                                break;
                            case 0xFF:
                                {
                                    Header.ApplicationExtension extension = new Header.ApplicationExtension(header, reader);
                                    Extensions.Add(extension);
                                }
                                break;
                            case 0:
                                break;
                            default:
                                throw new FileLoadException("Invalid Extension data in GIF " + fileName);
                        }
                        break;
                    case 0x2C:
                        // Image Descriptor
                        Header.ImageDescriptor descriptor = new Header.ImageDescriptor(reader, header);
                        break;
                    case 0x3B:
                        // GIF Trailer
                        eof = true;
                        break;
                    case 0:
                        {
                            Console.WriteLine("Empty Block");
                        }
                        break;
                    default:
                        // Image Data
                        {
                            byte lzwMinimumSize = nextByte;
                            bool doneDataBlock = false;
                            byte dataBlockLength = 0;
                            List<int> allData = new List<int>();
                            List<List<byte>> allPackedData = new List<List<byte>>();
                            List<byte> allPackedData2 = new List<byte>();
                            while (!doneDataBlock)
                            {
                                dataBlockLength = reader.ReadByte();
                                if (dataBlockLength == 0)
                                {
                                    doneDataBlock = true;
                                    byte[] inputArray = allPackedData2.ToArray();
                                    byte[] outputArray = new byte[header.LogicalScreenWidth * header.LogicalScreenHeight];
                                    int needDataSize = header.LogicalScreenWidth * header.LogicalScreenHeight;
                                    byte[] outputArray2 = Algorithms.UniGif.UniGifLZW.DecodeGifLZW(new List<byte>(inputArray), lzwMinimumSize, needDataSize);

                                    ccount += inputArray.Length;
                                    dcount += outputArray.Length;
                                    Console.WriteLine($"[{ccount}] - [{dcount}]");

                                }
                                else
                                {
                                    byte[] dataBlock = reader.ReadBytes(dataBlockLength);
                                    allPackedData2.AddRange(dataBlock);
                                }
                            }
                        }
                        break;
                }
            }

            reader.Close();
            fileStream.Close();
        }
    }
}