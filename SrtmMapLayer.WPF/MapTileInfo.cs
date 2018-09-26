using MapControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SrtmMapLayer
{
    public interface IMapTileInfo
    {
        Location LowerLeft  { get; }
        Location UpperRight { get; }
        Location Center     { get; }

        double TileXToWorldLongitude(int x);
        double TileYToWorldLatitude(int y);
    }

    public class MapTileInfo : IMapTileInfo
    {
        private Tile _tile;
        private double _widthInDegrees;
        private double _heightInDegrees;

        internal MapTileInfo(Tile tile)
        {
            this._tile = tile;

            this.LowerLeft  = TileToWorldLocation(tile.X    , tile.Y + 1, tile.ZoomLevel);
            this.UpperRight = TileToWorldLocation(tile.X + 1, tile.Y    , tile.ZoomLevel);

            this.Center = new Location(
                    latitude:  LowerLeft.Latitude  + (UpperRight.Latitude  - LowerLeft.Latitude)  / 2.0,
                    longitude: LowerLeft.Longitude + (UpperRight.Longitude - LowerLeft.Longitude) / 2.0);

            this._widthInDegrees  = UpperRight.Longitude - LowerLeft.Longitude;
            this._heightInDegrees = UpperRight.Latitude  - LowerLeft.Latitude;

            Console.WriteLine("Tile point for max latitude is " + LocationToTilePoint(this.UpperRight).Y);
            Console.WriteLine("Tile point for min latitude is " + LocationToTilePoint(this.LowerLeft).Y);
        }

        public Location LowerLeft  { get; private set; }
        public Location UpperRight { get; private set; }
        public Location Center     { get; private set; }
        public int Zoom            { get { return _tile.ZoomLevel; } }

        internal Point LocationToTilePoint(MapControl.Location location)
        {
            // points on the tiles are 0..255, 0..255, but if simply calculating ...
            //
            //   (float)((location.Latitude  - this.Center.Latitude)  / this._heightInDegrees * 256 + 127)
            //
            // ... then the results are:
            // 
            //   LocationToTilePoint(this.End).Y)   == 255 (ok)
            //   LocationToTilePoint(this.Start).Y) == -1 (not ok)
            // 
            // 
            return new Point(
                x: (float)((location.Longitude - this.LowerLeft.Longitude) / this._widthInDegrees  * 256),
                y: (float)((location.Latitude  - this.LowerLeft.Latitude)  / this._heightInDegrees * 256)
                );
        }

        // based on http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#C.23
        //private static PointF TileToWorldPos(double tile_x, double tile_y, int zoom)
        //{
        //    var p = new PointF();
        //    double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));

        //    p.X = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
        //    p.Y = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

        //    return p;
        //}

        private static Location TileToWorldLocation(double tile_x, double tile_y, int zoom)
        {
            var location = new Location();
            double n = Math.PI - ((2.0 * Math.PI * tile_y) / Math.Pow(2.0, zoom));

            location.Longitude = (float)((tile_x / Math.Pow(2.0, zoom) * 360.0) - 180.0);
            location.Latitude = (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));

            return location;
        }

        public double TileXToWorldLongitude(int x)
        {
            return (float)((x / Math.Pow(2.0, Zoom) * 360.0) - 180.0);
        }

        public double TileYToWorldLatitude(int y)
        {
            double n = Math.PI - ((2.0 * Math.PI * y) / Math.Pow(2.0, Zoom));
                
            return (float)(180.0 / Math.PI * Math.Atan(Math.Sinh(n)));
        }
    }

}
