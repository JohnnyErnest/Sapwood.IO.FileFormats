using System;
using System.Collections.Generic;
using System.Text;

namespace Sapwood.IO.FileFormats.Imaging
{
    public class RGBAColor<T>
    {
        public T R { get; set; }
        public T G { get; set; }
        public T B { get; set; }
        public T A { get; set; }
        public RGBAColor(T r, T g, T b, T a)
        {
            R = r; G = g; B = b; A = a;
        }

        public override string ToString()
        {
            return $"[RGBColor: ({R}, {G}, {B}, {A})]";
        }
    }
}
