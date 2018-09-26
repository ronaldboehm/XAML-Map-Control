using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Srtm.Colors
{
    public class GradientColorPoint
    {
        private double offset;

        public GradientColorPoint(Color color, double offset, bool isSeaLevel)
        {
            Color = color;
            Offset = offset;
            IsSeaLevel = isSeaLevel;
        }

        public GradientColorPoint(Color color, double offset)
        {
            Color = color;
            Offset = offset;
            IsSeaLevel = false;
        }

        public bool IsSeaLevel { get; private set; }
        public Color Color { get; set; }
        public double Offset
        {
            get { return offset; }
            set
            {
                if (value < 0.0) value = 0.0;
                if (value > 1.0) value = 1.0;

                offset = value;
            }
        }
        /// <summary>
        /// The points at the left and right are fixed and cannot be moved.
        /// </summary>
        public bool IsFixed { get { return offset == 0.0 || offset == 1.0; } }

        //private static IFormatProvider offsetFormatProvider = new 
        public override string ToString()
        {
            return String.Format("Color.FromRgb({0,3}, {1,3}, {2,3}), {3}",
                Color.R, Color.G, Color.B, Offset.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public class GradientColorPoints : List<GradientColorPoint>
    {
        public void Add(Color color, double offset, bool isSeaLevel)
        {
            Add(new GradientColorPoint(color, offset, isSeaLevel));
        }
        public void Add(Color color, double offset)
        {
            Add(new GradientColorPoint(color, offset));
        }

        public void SortByOffset()
        {
            Sort(delegate (GradientColorPoint c1, GradientColorPoint c2) { return c1.Offset.CompareTo(c2.Offset); });
        }
    }
}
