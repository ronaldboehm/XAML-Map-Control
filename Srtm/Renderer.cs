using Srtm.Colors;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Srtm
{
    public class Renderer
    {
        private readonly IElevationColors colors;

        private const double dpi = 96;
        private readonly PixelFormat format = PixelFormats.Pbgra32;

        public Renderer(ElevationColors colors)
        {
            this.colors = new CachedElevationColors(colors);

        }
        public BitmapSource Render(HeightMap heightMap, RenderingInfo rendering)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap(rendering.Width, rendering.Height, dpi, dpi, PixelFormats.Pbgra32);

            bitmap.Render(GetVisual(heightMap, rendering));
            bitmap.Freeze();

            return bitmap;
        }

        public DrawingVisual GetVisual(HeightMap heightMap, RenderingInfo rendering)
        {
            Rect rect = new Rect(0, 0, rendering.Width, rendering.Height);

            DrawingVisual visual = new DrawingVisual();

            DrawingContext context = visual.RenderOpen();
            try
            {
                context.DrawImage(GetColoredBackground(heightMap, rendering), rect);
                context.DrawImage(GetReliefShading(    heightMap, rendering), rect);

            }
            finally
            {
                context.Close();
            }

            return visual;
        }

        private BitmapSource GetColoredBackground(HeightMap heightMap, RenderingInfo rendering)
        {
            return GetBitmap(heightMap, rendering, GetBackgroundColor);
        }

        private BitmapSource GetReliefShading(HeightMap heightMap, RenderingInfo rendering)
        {
            return GetBitmap(heightMap, rendering, GetReliefShadingColor);
        }

        private delegate Color GetColorDelegate(HeightMap heightMap, RenderingInfo rendering, int x, int y);
        private BitmapSource GetBitmap(HeightMap heightMap, RenderingInfo rendering, GetColorDelegate getColor)
        {
            // stride should be aligned on 32-bit boundaries - see 
            // http://stackoverflow.com/questions/1983781/why-does-bitmapsource-create-throw-an-argumentexception
            int stride = (rendering.Width * format.BitsPerPixel) / 8;
            int step = format.BitsPerPixel / 8;

            byte[] pixels = new byte[rendering.Height * stride];

            for (int y = 0; y < rendering.Height; y++)
            {
                for (int x = 0; x < rendering.Width; x++)
                {
                    pixels.SetPixel(x * step + y * stride, getColor(heightMap, rendering, x, y));
                }
            }

            return BitmapSource.Create(rendering.Width, rendering.Height, dpi, dpi, format, null, pixels, stride);
        }

        private Color GetBackgroundColor(HeightMap heightMap, RenderingInfo rendering, int x, int y)
        {
            var (i, j) = ToIndex(heightMap, rendering, x, y);

            return colors[heightMap.Values[i, j]];
        }

        private static RenderingInfo renderingToBeLogged;
        private static (int i, int j) ToIndex(HeightMap heightMap, RenderingInfo rendering, int x, int y)
        {
            // y runs top-down (i.e. 0,0 is upper-left, not lower-left), so adjust:
            y = rendering.Height - y;

            var lon = rendering.MinLongitude + (x * (rendering.MaxLongitude - rendering.MinLongitude)) / rendering.Width;
            var lat = rendering.MaxLatitude + (y * (rendering.MaxLatitude - rendering.MinLatitude)) / rendering.Height;

            var i = (int)((lon - heightMap.LowerLeftX) / (heightMap.UpperRightX - heightMap.LowerLeftX) * heightMap.Columns);
            var j = (int)((lat - heightMap.LowerLeftY) / (heightMap.UpperRightY - heightMap.LowerLeftY) * heightMap.Rows);

            j = heightMap.Rows - j;

            if (renderingToBeLogged == null)
                renderingToBeLogged = rendering;

            if (renderingToBeLogged == rendering && x == 100 && y % 50 == 0)
                Console.WriteLine($"y = {rendering.Height - y}, lat = {lat:0.000}, j = {j}");

            return (i, j);
        }

        private Color GetReliefShadingColor(HeightMap heightMap, RenderingInfo rendering, int x, int y)
        {
            var (i, j) = ToIndex(heightMap, rendering, x, y);

            int v, a;
            int diff = 0;

            if (i - 2 > 0)
            {
                diff += (2 * heightMap.Values[i, j] - heightMap.Values[i - 1, j] +
                         1 * heightMap.Values[i, j] - heightMap.Values[i - 2, j]);
            }
            if (j + 2 < heightMap.Rows - 1)
            {
                diff -= (2 * heightMap.Values[i, j] - heightMap.Values[i, j + 1] +
                         1 * heightMap.Values[i, j] - heightMap.Values[i, j + 2]);
            }

            diff = (int)(diff ^ 3 / 4);
            if (diff < 1 && diff > -1) diff = 0;
            diff = ((int)Math.Sqrt(Math.Abs(diff))) * Math.Sign(diff) * 4;

            v = (int)(heightMap.Normalized[i, j] * 2.3 * (100 + diff)) * 0 + diff + 64;
            a = 255;
            if (v < 0) { v = 0; } else if (v > 255) { v = 255; }
            if (a < 0) { a = 0; } else if (a > 255) { a = 255; }

            //v = (int)(data.Normalized[scaledX, scaledY] * 0.7 * (100 + diff)) + 3 * diff + 16;
            //if (v < 0) { v = 0; } else if (v > 255) { v = 255; }
            //a = 64 + Math.Abs(v - 128) / 4;
            a = 64 + Math.Abs(v - 128) / 2;
            //a = 64 + Math.Abs(v - 64);

            return Color.FromArgb((byte)a, (byte)v, (byte)v, (byte)v);
        }
    }

    internal static class ByteArrayExtensions
    {
        internal static void SetPixel(this byte[] pixels, int pos, Color color)
        {
            pixels[pos + 0] = color.B;
            pixels[pos + 1] = color.G;
            pixels[pos + 2] = color.R;
            pixels[pos + 3] = color.A;
        }
    }
}
