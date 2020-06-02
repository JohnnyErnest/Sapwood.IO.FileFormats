using Sapwood.IO.FileFormats.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Sapwood.IO.FileFormats.Formats.Imaging
{
    public class BMP
    {
        public class Header
        {
            public HeaderType HeaderTypeMeta { get; set; }
            public ulong BitmapFileSize { get; set; }
            public uint Reserved1 { get; set; }
            public uint Reserved2 { get; set; }
            public ulong DataOffset { get; set; }

            public Header(string headerType, ulong fileSize, uint reserved1, uint reserved2, ulong dataOffset)
            {
                switch(headerType.ToUpper())
                {
                    case "BM": HeaderTypeMeta = HeaderType.Windows; break;
                    case "BA": HeaderTypeMeta = HeaderType.OS2StructBitmapArray; break;
                    case "CI": HeaderTypeMeta = HeaderType.OS2StructColorIcon; break;
                    case "CP": HeaderTypeMeta = HeaderType.OS2ConstColorPointer; break;
                    case "IC": HeaderTypeMeta = HeaderType.OS2StructIcon; break;
                    case "PT": HeaderTypeMeta = HeaderType.OS2Pointer; break;
                    default: HeaderTypeMeta = HeaderType.Other; break;
                }
                BitmapFileSize = fileSize;
                Reserved1 = reserved1;
                Reserved2 = reserved2;
                DataOffset = dataOffset;
            }

            public enum HeaderType
            {
                Windows,
                OS2StructBitmapArray,
                OS2StructColorIcon,
                OS2ConstColorPointer,
                OS2StructIcon,
                OS2Pointer,
                Other
            }

            public class DIBHeaderWindows
            {
                public ulong Width { get; set; }
                public ulong Height { get; set; }
                public uint ColorPlanes { get; set; }
                public uint Bpp { get; set; }
                public ulong CompressionMethod { get; set; }
                public ulong ImageSize { get; set; }
                public ulong HorizontalPPM { get; set; }
                public ulong VerticalPPM { get; set; }
                public ulong NumberColors { get; set; }
                public ulong ImportantColors { get; set; }
            }

            public DIBHeaderWindows WindowsHeader { get; set; }
            public List<RGBAColor<byte>> Palette { get; set; }
        }

        public Header BMPHeader { get; set; }
        public RGBImage<byte> Image { get; set; }

        private uint GetUIntFromBytesLE(byte[] bytes, int offset)
        {
            return (uint)bytes[offset] | (uint)bytes[offset + 1] << 8;
        }

        private ulong GetULongFromBytesLE(byte[] bytes, int offset)
        {
            return (ulong)bytes[offset] | (ulong)bytes[offset + 1] << 8 | (ulong)bytes[offset + 2] << 16 | (ulong)bytes[offset + 3] << 24;
        }

        public BMP(string fileName)
        {
            FileStream stream = File.OpenRead(fileName);
            BinaryReader reader = new BinaryReader(stream);
            byte[] header = reader.ReadBytes(14);
            string headerType = "" + (char)header[0] + (char)header[1];
            ulong fileSize = GetULongFromBytesLE(header, 2);
            uint reserved1 = GetUIntFromBytesLE(header, 6);
            uint reserved2 = GetUIntFromBytesLE(header, 8);
            ulong dataOffset = GetULongFromBytesLE(header, 10);
            this.BMPHeader = new BMP.Header(headerType, fileSize, reserved1, reserved2, dataOffset);

            byte[] dibHeaderSizeBytes = reader.ReadBytes(4);
            ulong dibHeaderSize = GetULongFromBytesLE(dibHeaderSizeBytes, 0);

            int[] windowsDIBSizes = { 40, 52, 56, 108, 124 };
            if (windowsDIBSizes.Contains((int)dibHeaderSize))
            {
                byte[] dibBytes = reader.ReadBytes(36);
                ulong width = GetULongFromBytesLE(dibBytes, 0);
                ulong height = GetULongFromBytesLE(dibBytes, 4);
                uint colorPlanes = GetUIntFromBytesLE(dibBytes, 8);
                uint bpp = GetUIntFromBytesLE(dibBytes, 10);
                ulong compressionMethod = GetULongFromBytesLE(dibBytes, 12);
                ulong imageSize = GetULongFromBytesLE(dibBytes, 16);
                ulong horizontalPPM = GetULongFromBytesLE(dibBytes, 20);
                ulong verticalPPM = GetULongFromBytesLE(dibBytes, 24);
                ulong numberColors = GetULongFromBytesLE(dibBytes, 28);
                ulong importantColors = GetULongFromBytesLE(dibBytes, 32);

                this.BMPHeader.WindowsHeader = new Header.DIBHeaderWindows()
                {
                    Bpp = bpp, ColorPlanes = colorPlanes, CompressionMethod = compressionMethod,
                    Height = height, HorizontalPPM = horizontalPPM, ImageSize = imageSize, ImportantColors = importantColors,
                    NumberColors = numberColors, VerticalPPM = verticalPPM, Width = width
                };
            }

            ulong extraBytesLength = dataOffset - (14 + dibHeaderSize);
            byte[] extraBytes = reader.ReadBytes((int)extraBytesLength);

            if (this.BMPHeader.WindowsHeader.Bpp == 8 && extraBytes.Length == 1024)
            {
                this.BMPHeader.Palette = new List<RGBAColor<byte>>();
                int p = 0;
                for (int z=0;z<256;z++)
                {
                    RGBAColor<byte> color = new RGBAColor<byte>(extraBytes[p], extraBytes[p + 1], extraBytes[p + 2], 255);
                    this.BMPHeader.Palette.Add(color);
                    p += 4;
                }
            }
            else if (this.BMPHeader.WindowsHeader.Bpp == 4 && extraBytes.Length == 64)
            {
                this.BMPHeader.Palette = new List<RGBAColor<byte>>();
                int p = 0;
                for (int z = 0; z < 16; z++)
                {
                    RGBAColor<byte> color = new RGBAColor<byte>(extraBytes[p], extraBytes[p + 1], extraBytes[p + 2], 255);
                    this.BMPHeader.Palette.Add(color);
                    p += 4;
                }
            }
            else if (this.BMPHeader.WindowsHeader.Bpp == 1 && extraBytes.Length == 8)
            {
                this.BMPHeader.Palette = new List<RGBAColor<byte>>();
                int p = 0;
                for (int z = 0; z < 2; z++)
                {
                    RGBAColor<byte> color = new RGBAColor<byte>(extraBytes[p], extraBytes[p + 1], extraBytes[p + 2], 255);
                    this.BMPHeader.Palette.Add(color);
                    p += 4;
                }
            }

            if (reader.BaseStream.Position == (long)dataOffset)
            {
                if (this.BMPHeader.WindowsHeader != null)
                {
                    if (this.BMPHeader.WindowsHeader.CompressionMethod == 0)
                    {
                        uint width = (uint)this.BMPHeader.WindowsHeader.Width;
                        uint height = (uint)this.BMPHeader.WindowsHeader.Height;
                        int bpp = (int)this.BMPHeader.WindowsHeader.Bpp;
                        int rowSize = (int)((this.BMPHeader.WindowsHeader.Bpp * width + 31) / 32 * 4);
                        int stride = (int)((((width * bpp) + 31) & ~31) >> 3);

                        //int rowNumber = (int)height - 1;
                        int rowNumber = 0;
                        byte[] pixelBytes = reader.ReadBytes((int)(this.BMPHeader.WindowsHeader.ImageSize));
                        RGBImage<byte> image = new RGBImage<byte>((uint)width, (uint)height);

                        if (this.BMPHeader.WindowsHeader.Bpp >= 8)
                        {
                            uint bytesPerPixel = this.BMPHeader.WindowsHeader.Bpp / 8;
                            int p = 0;
                            for (uint y = 0; y < height; y++)
                            {
                                for (uint x = 0; x < width; x++)
                                {
                                    RGBAColor<byte> color = null;
                                    if (bytesPerPixel == 1)
                                    {
                                        byte r = pixelBytes[p];
                                        color = this.BMPHeader.Palette[r];
                                        p++;
                                    }
                                    else if (bytesPerPixel == 3)
                                    {
                                        byte r = pixelBytes[p];
                                        byte g = pixelBytes[p + 1];
                                        byte b = pixelBytes[p + 2];
                                        color = new RGBAColor<byte>(r, g, b, 255);
                                        p += 3;
                                    }
                                    else if (bytesPerPixel == 4)
                                    {
                                        byte r = pixelBytes[p];
                                        byte g = pixelBytes[p + 1];
                                        byte b = pixelBytes[p + 2];
                                        byte a = pixelBytes[p + 3];
                                        color = new RGBAColor<byte>(r, g, b, a);
                                        p += 4;
                                    }
                                    else
                                    {
                                        color = new RGBAColor<byte>(0, 0, 0, 0);
                                    }
                                    image.Colors[x, rowNumber] = color;
                                }
                                rowNumber++;
                                p = (int)y * stride;
                            }
                        }
                        else if (this.BMPHeader.WindowsHeader.Bpp == 4)
                        {
                            int p = 0;
                            for (uint y = 0; y < height; y++)
                            {
                                for (uint x = 0; x < width; x+=2)
                                {
                                    byte r = (byte)(pixelBytes[p] & 0xF);
                                    byte r2 = (byte)(pixelBytes[p] >> 4);
                                    RGBAColor<byte> color = null;
                                    RGBAColor<byte> color2 = null;
                                    color = this.BMPHeader.Palette[r];
                                    color2 = this.BMPHeader.Palette[r2];
                                    p++;
                                    image.Colors[x, rowNumber] = color;
                                    if (x + 1 < width)
                                        image.Colors[x+1, rowNumber] = color2;
                                }
                                rowNumber++;
                                p = (int)y * stride;
                            }
                        }
                        else if (this.BMPHeader.WindowsHeader.Bpp == 1)
                        {
                            int p = 0;
                            for (uint y = 0; y < height; y++)
                            {
                                for (uint x = 0; x < width; x += 8)
                                {
                                    byte[] pixels = new byte[8];
                                    RGBAColor<byte>[] color = new RGBAColor<byte>[8];
                                    for(int c=0;c<8;c++)
                                    {
                                        pixels[c] = (byte)((pixelBytes[p] >> c) & 0x1);
                                        if (x + c < width)
                                        {
                                            color[c] = this.BMPHeader.Palette[pixels[c]];
                                            image.Colors[x + c, rowNumber] = color[c];
                                        }
                                    }
                                    p++;
                                }
                                rowNumber++;
                                p = (int)y * stride;
                            }
                        }
                        else
                        {
                        }
                        this.Image = image;
                    }
                }
            }

            reader.Close();
            stream.Close();
        }
    }
}