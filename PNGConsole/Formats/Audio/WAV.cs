using Sapwood.IO.FileFormats.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sapwood.IO.FileFormats.Formats.Audio
{
    public class WAV
    {
        public WAV()
        {

        }

        public class Header
        {
            public byte[] RiffHeader { get; } = { (byte)'R', (byte)'I', (byte)'F', (byte)'F' };
            public uint ChunkSize { get; set; }
            public byte[] WaveHeader { get; } = { (byte)'W', (byte)'A', (byte)'V', (byte)'E' };
            public byte[] FmtHeader { get; } = { (byte)'f', (byte)'m', (byte)'t', (byte)' ' };
            public uint Subchunk1Size { get; set; }
            public ushort AudioFormat { get; set; }
            public ushort NumChannels { get; set; }
            public uint SampleRate { get; set; }
            public uint ByteRate { get; set; }
            public ushort BlockAlign { get; set; }
            public ushort BitsPerSample { get; set; }
            public byte[] DataHeader { get; } = { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };
            public uint Subchunk2Size { get; set; }

            public Header(uint dataLength, uint sampleRate = 44100, ushort channels = 2, ushort bitsPerSample = 16, uint subchunk1Size = 16, ushort blockAlign = 4, ushort audioFormat = 1)
            {
                NumChannels = channels;
                SampleRate = sampleRate;
                BitsPerSample = bitsPerSample;
                BlockAlign = blockAlign;
                ByteRate = BlockAlign * SampleRate;
                Subchunk1Size = subchunk1Size;
                Subchunk2Size = dataLength;
                AudioFormat = audioFormat;
                ChunkSize = 36 + Subchunk2Size;
            }

            public byte[] SerializeHeader()
            {
                List<byte> data = new List<byte>();
                data.AddRange(RiffHeader);
                data.AddRange(ChunkSize.GetBytesLE());
                data.AddRange(WaveHeader);

                data.AddRange(FmtHeader);
                data.AddRange(Subchunk1Size.GetBytesLE());
                data.AddRange(AudioFormat.GetBytesLE());
                data.AddRange(NumChannels.GetBytesLE());
                data.AddRange(SampleRate.GetBytesLE());
                data.AddRange(ByteRate.GetBytesLE());
                data.AddRange(BlockAlign.GetBytesLE());
                data.AddRange(BitsPerSample.GetBytesLE());

                data.AddRange(DataHeader);
                data.AddRange(Subchunk2Size.GetBytesLE());

                return data.ToArray();
            }
        }

        public void Save(string filename, List<byte> rawSoundData)
        {
            Header header = new Header((uint)rawSoundData.Count);
            byte[] headerBytes = header.SerializeHeader();
            byte[] dataArray = rawSoundData.ToArray();

            FileStream stream = File.OpenWrite(filename);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(dataArray, 0, dataArray.Length);
            stream.Flush();
            stream.Close();
        }
    }
}
