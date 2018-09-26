using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Srtm.Colors
{
    public class ElevationColors : IElevationColors
    {
        private GradientColorPoints points;
        private RenderTargetBitmap gradientBitmap;

        #region Gradients
        public static readonly Gradient SimpleGradient = Gradient.GetSimpleGradient();
        public static readonly Gradient NaturalGradient = Gradient.GetNaturalGradient();
        public static readonly Gradient NaturalDarkGradient = Gradient.GetNaturalDarkGradient();
        public static Gradient DefaultGradient { get { return NaturalGradient; } }

        public class Gradient
        {
            private GradientColorPoints points;
            private double seaLevelOffset;

            internal void Set(ElevationColors colors)
            {
                foreach (var point in points)
                {
                    colors.points.Add(point);
                }
                colors.SeaLevelOffset = seaLevelOffset;
            }

            internal static Gradient GetSimpleGradient()
            {
                return new Gradient()
                {
                    points = new GradientColorPoints()
                         {
                             { System.Windows.Media.Colors.Blue,  0.0 },
                             { System.Windows.Media.Colors.Green, 0.1 },
                             { System.Windows.Media.Colors.Gold,  0.5 },
                             { System.Windows.Media.Colors.Brown, 1.0 }

                         },
                    seaLevelOffset = 0.11
                };
            }
            internal static Gradient GetNaturalGradient()
            {
                return new Gradient()
                {
                    points = new GradientColorPoints()
                        {
                            { Color.FromRgb( 64,  70, 208), 0 },
                            { Color.FromRgb(  2,  99,  68), 0.001 },
                            { Color.FromRgb( 14, 122,  47), 0.01666666666667 },
                            { Color.FromRgb(229, 208, 119), 0.08333333333333 },
                            { Color.FromRgb(195, 138,  59), 0.16666666666666 },
                            { Color.FromRgb(165,  72,   2), 0.25 },
                            { Color.FromRgb(152,  56,   8), 0.333333333333333 },
                            { Color.FromRgb(129,  33,  33), 0.5 },
                            { Color.FromRgb(120,  72,  72), 0.583333333333333 },
                            { Color.FromRgb(125, 125, 125), 0.666666666666666 },
                            { Color.FromRgb(255, 255, 255), 1 },
                        },
                    seaLevelOffset = 0.01
                };
            }
            internal static Gradient GetNaturalDarkGradient()
            {
                return new Gradient()
                {
                    points = new GradientColorPoints()
                        {
                            { Color.FromRgb(0x58, 0xc6, 0xff), 0.0 },
                            { Color.FromRgb(0x58, 0xc6, 0xff), 0.113475177304965 },
                            { Color.FromRgb(0x35, 0x83, 0x2d), 0.125 },
                            { Color.FromRgb(0x35, 0x83, 0x2d), 0.184397163120567 },
                            { Color.FromRgb(0x4f, 0x99, 0x42), 0.283687943262411 },
                            { Color.FromRgb(0xde, 0xca, 0x5a), 0.457446808510638 },
                            { Color.FromRgb(0x6b, 0x45, 0x00), 0.723988439306358 },
                            { Color.FromRgb(0xdc, 0xcd, 0xa4), 0.86849710982659 },
                            { Color.FromRgb(0xfa, 0xfd, 0xff), 1.0 }
                        },
                    seaLevelOffset = 0.125
                };
            }
        }
        #endregion

        public ElevationColors() : this(ElevationColors.DefaultGradient) { }

        public ElevationColors(Gradient gradient)
        {
            if (gradient == null)
            {
                throw new ArgumentNullException();
            }

            points = new GradientColorPoints();
            gradient.Set(this);

            gradientBitmap = new RenderTargetBitmap(1000, 3, 96, 96, PixelFormats.Default);
            DrawGradientBitmap();
            gradientBitmap.Freeze();

            MaxHeight = 3000;
        }

        public ElevationColors Clone()
        {
            ElevationColors result = new ElevationColors();

            result.BeginUpdate();
            foreach (var colorPoint in this.points)
            {
                result.points.Add(colorPoint);
            }
            result.EndUpdate();

            result.SeaLevelOffset = SeaLevelOffset;
            result.MaxHeight = MaxHeight;

            return result;
        }

        public int MaxHeight { get; set; }

        public double SeaLevelOffset { get; set; }

        public GradientColorPoint[] Points
        {
            get
            {
                return points.ToArray();
            }
        }


        #region Update and Begin-/EndUpdate
        private bool isUpdating = false;
        public void BeginUpdate()
        {
            isUpdating = true;
        }

        public void EndUpdate()
        {
            isUpdating = false;
            DrawGradientBitmap();
        }

        private void DrawGradientBitmap()
        {
            LinearGradientBrush brush = new LinearGradientBrush();

            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint   = new Point(1, 0.5);

            foreach (GradientColorPoint colorPoint in points)
            {
                brush.GradientStops.Add(new GradientStop(colorPoint.Color, colorPoint.Offset));
            }

            DrawingVisual drawing = new DrawingVisual();
            DrawingContext context = drawing.RenderOpen();

            context.DrawRectangle(brush, null, new Rect(0, 0, gradientBitmap.Width, gradientBitmap.Height));
            context.Close();

            gradientBitmap.Render(drawing);
        }
        #endregion

        public void Add(Color color, double offset)
        {
            points.Add(color, offset);

            if (!isUpdating) DrawGradientBitmap();
        }

        public void Remove(GradientColorPoint colorPointToDelete)
        {
            points.Remove(colorPointToDelete);

            if (!isUpdating) DrawGradientBitmap();
        }

        internal void SortByOffset()
        {
            points.SortByOffset();

            DrawGradientBitmap();
        }

        public Color this[int height]
        {
            get
            {
                //    |------------------------------------------|
                //    0    (= SeaLevelOffset)                   1.0
                //    |----|-------------------------------------|
                //         0                                   9000 (= MaxHeight)
                //
                //       => x = height * (1 - SeaLevel) / MaxHeight + SeaLevel

                double x = height * (1 - SeaLevelOffset) / MaxHeight + SeaLevelOffset;

                byte[] pixels = GetColorPixels(x * 1000);
                return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
            }
        }

        private byte[] GetColorPixels(double pos)
        {
            // Convert coopdinates from WPF pixels to Bitmap pixels ...
            pos *= gradientBitmap.PixelWidth / gradientBitmap.Width;

            // ... and restrict them by the Bitmap bounds.
            if ((int)pos > gradientBitmap.PixelWidth - 1)
            {
                pos = gradientBitmap.PixelWidth - 1;
            }
            else if (pos < 0)
            {
                pos = 0;
            }

            byte[] pixels = new byte[4];
            int stride = (gradientBitmap.PixelWidth * gradientBitmap.Format.BitsPerPixel + 7) / 8;
            gradientBitmap.CopyPixels(new Int32Rect((int)pos, 1, 1, 1), pixels, stride, 0);

            return pixels;
        }

        public override string ToString()
        {
            // output should be similar to e.g.
            //    return new Gradient()
            //    {
            //        points = new GradientColorPoints()
            //            {
            //                { Colors.Blue, 0.0 },
            //                { Colors.Green, 0.1 },
            //                { Colors.Gold, 0.5},
            //                { Colors.Brown, 1.0 }
            //            },
            //        seaLevelOffset = 0.11
            //    };

            StringBuilder result = new StringBuilder();

            result.Append("points =\r\n");
            result.Append("    {\r\n");

            foreach (var point in this.Points)
            {
                result.Append("        { ");
                result.Append(point.ToString());
                result.Append(" }, \r\n");
            }

            result.Append("    },\r\n");

            result.Append("seaLevelOffset = ");
            result.Append(SeaLevelOffset.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return result.ToString();
        }
    }

    public class CachedElevationColors : IElevationColors
    {
        ElevationColors elevationColors;
        Dictionary<int, Color> cache;

        public CachedElevationColors(ElevationColors elevationColors)
        {
            this.elevationColors = elevationColors;
            this.cache = new Dictionary<int, Color>();
        }

        public Color this[int height]
        {
            get
            {
                if (cache.ContainsKey(height))
                {
                    return cache[height];
                }
                else
                {
                    Color result = elevationColors[height];
                    cache.Add(height, result);

                    return result;
                }
            }
        }
    }
}
