using Srtm;
using Srtm.Colors;
using System.Windows.Media.Imaging;

namespace SrtmMapLayer
{
    public class TileRenderer
    {
        private readonly int mapTileWidth;
        private readonly int mapTileHeight;

        private readonly Renderer renderer;

        public TileRenderer() : 
            this(mapTileWidth: 256, mapTileHeight: 256, colors: new ElevationColors(ElevationColors.DefaultGradient))
        {
        }

        public TileRenderer(int mapTileWidth, int mapTileHeight, ElevationColors colors)
        {
            this.renderer      = new Renderer(colors);
            this.mapTileWidth  = mapTileWidth;
            this.mapTileHeight = mapTileHeight;
        }

        public BitmapSource Render(HeightMap heightMap, IMapTileInfo mapTile)
        {
            return renderer.Render(
                heightMap, 
                new RenderingInfo
                {
                    Width        = mapTileWidth,
                    Height       = mapTileHeight,
                    MinLongitude = mapTile.LowerLeft.Longitude,
                    MaxLongitude = mapTile.UpperRight.Longitude,
                    MinLatitude  = mapTile.LowerLeft.Latitude,
                    MaxLatitude  = mapTile.UpperRight.Latitude,
                });
        }
    }
}
