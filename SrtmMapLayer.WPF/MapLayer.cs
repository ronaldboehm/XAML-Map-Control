using MapControl;

namespace SrtmMapLayer
{
    public class MapLayer : MapTileLayer
    {

        public MapLayer()
            : base(new TileImageLoader())
        {
            SourceName   = "SRTM Tiles";
            Description  = "SRTM Data";
            MinZoomLevel = 8;
            MaxZoomLevel = 18;

            // dummy to satisfy condition in MapTileLayer.UpdateTiles()
            TileSource = new TileSource();
        }

        // when using MapImageLayer (instead of MapTileLayer) as the base class, only override GetImageAsync():
        //
        //private readonly DataTileProvider srtmTileProvider = new DataTileProvider("E:\\Temp");
        //private readonly TileRenderer renderer = new TileRenderer();
        //
        //protected override Task<ImageSource> GetImageAsync(BoundingBox boundingBox)
        //{
        //    var srtmData = srtmTileProvider.Get(new Location(boundingBox.South, boundingBox.West), new Location(boundingBox.North, boundingBox.East));

        //    var x = renderer.Render(srtmData, null);

        //    return Task.FromResult((ImageSource)null);
        //}
    }
}
