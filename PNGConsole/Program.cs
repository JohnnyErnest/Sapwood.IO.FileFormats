using Sapwood.IO.FileFormats.Algorithms;
using Sapwood.IO.FileFormats.Collections;
using Sapwood.IO.FileFormats.Formats;
using Sapwood.IO.FileFormats.Formats.Audio;
using Sapwood.IO.FileFormats.Formats.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Sapwood.IO.FileFormats
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Default;
            Console.WriteLine($"\u001b[38;2;128;20;196m\u2580\u2581\u2582\u2583\u2584\u2585\u2586\u2587\u2588\u2589\u258A\u258B\u258C\u258D\u258E\u258F\u2590\u2591\u2592\u2593\u2594\u2595\u2596\u2597\u2598\u2599\u259A\u259B\u259C\u259D\u259E\u259F\u2615\u26Be\u001b[38;2;128;128;128m");
            Console.WriteLine($"\u001b[38;2;128;20;196m\u2580\u2581\u2582\u2583\u2584\u2585\u2586\u2587\u2588\u2589\u258A\u258B\u258C\u258D\u258E\u258F\u2590\u2591\u2592\u2593\u2594\u2595\u2596\u2597\u2598\u2599\u259A\u259B\u259C\u259D\u259E\u259F\u2615\u26Be\u001b[38;2;128;128;128m");
            Console.WriteLine($"\u001b[38;2;128;20;196m\u2580\u2581\u2582\u2583\u2584\u2585\u2586\u2587\u2588\u2589\u258A\u258B\u258C\u258D\u258E\u258F\u2590\u2591\u2592\u2593\u2594\u2595\u2596\u2597\u2598\u2599\u259A\u259B\u259C\u259D\u259E\u259F\u2615\u26Be\u001b[38;2;170;170;170m");
            Console.WriteLine("▀▁▂▃▄▅▆▇█▉▊▋▌▍▎▏▐░▒▓▔▕▖▗▘▙▚▛▜▝▞▟☕⚾");

            MP3 mp3 = new MP3();
            //mp3.Decode($@"C:\Users\johnr\Downloads\MBR\warez.zip\Mp3\Contra.mp3");
            mp3.Decode($@"C:\Users\johnr\Documents\outputWave1-ABR-192.mp3");
            return;

            DirectoryInfo directoryInfo = new DirectoryInfo($@"c:\users\johnr\pictures");
            //foreach (var file in directoryInfo.GetFiles())
            //{
            //    if (new string[] { ".jpg", ".jpeg", ".jfif" }.Contains(file.Extension.ToLower()))
            //    {
            //        Console.WriteLine($"File: {file.FullName}");
            //        JPEG jpg = new JPEG(file.FullName);
            //    }
            //}

            //JPEG jpeg = new JPEG($@"c:\users\johnr\pictures\Calculator1.jpg");
            JPEG jpeg = new JPEG($@"c:\users\johnr\pictures\68518b2cd5485d576cc34355ffb0027d.jpg");

            return;

            GIF gif = new GIF($@"c:\users\johnr\pictures\Calculator1.gif");

            LZW lzw = new LZW();

            BMP bmp = new BMP($@"c:\users\johnr\pictures\3Sphere_2c.bmp");

            directoryInfo = new DirectoryInfo($@"c:\users\johnr\pictures");
            foreach(var file in directoryInfo.GetFiles())
            {
                if (file.Extension.ToLower() == ".png")
                {
                    Console.WriteLine($"File: {file.FullName}");
                    PNG png = new PNG(file.FullName);
                    var header = png.DataChunks.Where(x => x.ChunkType.ToLower() == "ihdr").FirstOrDefault();
                    var properties = (header.DataChunkRepresentation as PNG.DataChunk.HeaderChunkRepresentation).ToProperties(header.Data);
                    //if ((byte)properties["ColorType"].Value == 6)
                    //    Console.ReadLine();
                }
            }
            //Console.WriteLine("Enter PNG Filename:");
            //string path = Console.ReadLine();
            //PNG png = new PNG(path);
        }
    }
}