using MapControl;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SrtmMapLayer
{
    public class TileImageLoader : ITileImageLoader
    {
        private readonly ConcurrentStack<Tile> pendingTiles = new ConcurrentStack<Tile>();
        private readonly DataTileProvider srtmTileProvider = new DataTileProvider("E:\\Temp");
        private readonly TileRenderer renderer = new TileRenderer();

        public TileImageLoader()
        {
            var task = Task.Run(() => LoadTiles());
        }

        public void LoadTilesAsync(MapTileLayer tileLayer)
        {
            pendingTiles.Clear();

            var tiles = tileLayer.Tiles.Where(t => t.Pending);
            if (!tiles.Any())
                return;

            pendingTiles.PushRange(tiles.Reverse().ToArray());
        }

        private void LoadTiles()
        {
            while (true)
            {
                while (pendingTiles.TryPop(out Tile tile))
                { 
                    tile.Pending = false;

                    try
                    {
                        RenderTileImage(tile);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("SrtmMapLayer.TileImageLoader: {0}/{1}/{2}: {3}", tile.ZoomLevel, tile.XIndex, tile.Y, ex.Message);
                    }
                }

                System.Threading.Thread.Sleep(500);
            }
        }

        private void RenderTileImage(Tile tile)
        {
            var tileInfo = new MapTileInfo(tile);

            var srtmData = srtmTileProvider.Get(tileInfo.LowerLeft, tileInfo.UpperRight);
            if (srtmData == null)
                return;

            var image = renderer.Render(srtmData, tileInfo);

            tile.Image.Dispatcher.InvokeAsync(() => tile.SetImage(image));
        }
    }
}
