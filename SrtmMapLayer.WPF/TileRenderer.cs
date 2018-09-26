using SrtmMapLayer.Colors;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SrtmMapLayer
{
    public class TileRenderer
    {
        private IElevationColors colors;

        int mapTileWidth;
        int mapTileHeight;
        double dpi = 96;
        PixelFormat format = PixelFormats.Pbgra32;

        public TileRenderer() : 
            this(mapTileWidth: 256, mapTileHeight: 256, colors: new ElevationColors(ElevationColors.NaturalDarkGradient))
        {
        }

        public TileRenderer(int mapTileWidth, int mapTileHeight, ElevationColors colors)
        {
            this.colors        = new CachedElevationColors(colors);
            this.mapTileWidth  = mapTileWidth;
            this.mapTileHeight = mapTileHeight;
        }

        public BitmapSource Render(DataTile data, IMapTileInfo mapTile)
        {
            RenderTargetBitmap result = new RenderTargetBitmap(this.mapTileWidth, this.mapTileHeight, dpi, dpi, PixelFormats.Pbgra32);

            result.Render(GetVisual(data, mapTile));
            result.Freeze();

            return result;
        }

        public DrawingVisual GetVisual(DataTile data, IMapTileInfo mapTile)
        {
            Rect rect = new Rect(0, 0, mapTileWidth, mapTileHeight);

            DrawingVisual visual = new DrawingVisual();
            DrawingContext context = visual.RenderOpen();

            context.DrawImage(GetColoredBackground(data, mapTile), rect);
            context.DrawImage(GetReliefShading(data, mapTile), rect);

            context.Close();

            return visual;
        }

        private BitmapSource GetColoredBackground(DataTile data, IMapTileInfo mapTile)
        {
            return GetBitmap(data, mapTile, GetBackgroundColor);
        }

        private BitmapSource GetReliefShading(DataTile data, IMapTileInfo mapTile)
        {
            return GetBitmap(data, mapTile, GetReliefShadingColor);
        }

        private delegate Color GetColorDelegate(DataTile data, IMapTileInfo mapTile, int x, int y);
        private BitmapSource GetBitmap(DataTile data, IMapTileInfo mapTile, GetColorDelegate getColor)
        {
            // stride should be aligned on 32-bit boundaries - see 
            // http://stackoverflow.com/questions/1983781/why-does-bitmapsource-create-throw-an-argumentexception
            int stride = (mapTileWidth * format.BitsPerPixel) / 8;
            int step = format.BitsPerPixel / 8;

            byte[] pixels = new byte[mapTileHeight * stride];

            for (int y = 0; y < mapTileHeight; y++)
            {
                for (int x = 0; x < mapTileWidth; x++)
                {
                    pixels.SetPixel(x * step + y * stride, getColor(data, mapTile, x, y));
                }
            }

            return BitmapSource.Create(mapTileWidth, mapTileHeight, dpi, dpi, format, null, pixels, stride);
        }

        private Color GetBackgroundColor(DataTile data, IMapTileInfo mapTile, int x, int y)
        {
            int scaledX = (int)(x * data.Columns / mapTileWidth);
            int scaledY = (int)(y * data.Rows / mapTileHeight);

            return colors[data.Values[scaledX, scaledY]];
        }

        private Color GetReliefShadingColor(DataTile data, IMapTileInfo mapTile, int x, int y)
        {
            int scaledX = (int)(x * data.Columns / mapTileWidth);
            int scaledY = (int)(y * data.Rows / mapTileHeight);

            int v, a;
            int diff = 0;

            if (scaledX - 2 > 0)
            {
                diff += (2 * data.Values[scaledX, scaledY] - data.Values[scaledX - 1, scaledY] +
                         1 * data.Values[scaledX, scaledY] - data.Values[scaledX - 2, scaledY]);
            }
            if (scaledY + 2 < data.Rows - 1)
            {
                diff -= (2 * data.Values[scaledX, scaledY] - data.Values[scaledX, scaledY + 1] +
                         1 * data.Values[scaledX, scaledY] - data.Values[scaledX, scaledY + 2]);
            }

            diff = (int)(diff ^ 3 / 4);
            if (diff < 1 && diff > -1) diff = 0;
            diff = ((int)Math.Sqrt(Math.Abs(diff))) * Math.Sign(diff) * 4;

            // Nur schwarz-weiß:

            v = (int)(data.Normalized[scaledX, scaledY] * 2.3 * (100 + diff)) * 0 + diff + 64;
            a = 255;
            if (v < 0) { v = 0; } else if (v > 255) { v = 255; }
            if (a < 0) { a = 0; } else if (a > 255) { a = 255; }

            // Als Hintergrund:
            //v = (int)(data.Normalized[scaledX, scaledY] * 0.7 * (100 + diff)) + 3 * diff + 16;
            //if (v < 0) { v = 0; } else if (v > 255) { v = 255; }
            //a = 64 + Math.Abs(v - 128) / 4;
            a = 64 + Math.Abs(v - 128) / 2;
            //a = 64 + Math.Abs(v - 64);

            return Color.FromArgb((byte)a, (byte)v, (byte)v, (byte)v);
        }

        private static void SaveBitmapSource(BitmapSource source, string filename)
        {
            FileStream stream = new FileStream(filename, FileMode.Create);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(stream);
            stream.Close();
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
