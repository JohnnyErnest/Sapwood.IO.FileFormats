using System;
using System.Collections.Generic;
using System.Text;

namespace Sapwood.IO.FileFormats.Imaging
{
    public class RGBImage<T>
    {
        public RGBAColor<T>[,] Colors { get; set; }
        public RGBImage(uint width, uint height)
        {
            Colors = new RGBAColor<T>[width, height];
        }
    }
}
