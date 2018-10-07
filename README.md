# Map with SRTM elevation data

Based on [Clemens Fischer](https://github.com/ClemensFischer)'s [XAML Map Control](https://github.com/ClemensFischer/XAML-Map-Control), 
this demo app shows how to add a map layer that renders custom tiles.
These custom tiles are based on [SRTM 90m Digital Elevation Data](https://cgiarcsi.community/data/srtm-90m-digital-elevation-database-v4-1/).

![Skye](img/Skye.png)

## Adding a custom map tile layer

To create a custom map tile layer:

* Add a new class derived from MapTileLayer with a custom TileImageLoader

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
        }

* In the [TileImageLoader](SrtmMapLayer.WPF/TileImageLoader.cs), implement ITileImageLoader.LoadTilesAsync()

Actual rendering happens in [Renderer.cs](Srtm/Renderer.cs) where the [elevation data](https://en.wikipedia.org/wiki/Digital_elevation_model)
is read from the [corresponding SRTM data tile](http://srtm.csi.cgiar.org/SELECTION/inputCoord.asp) and converted to a relief map.

## Known issues/bugs

This is more of a proof-of-concept for integrating other data sources with the XAML Map Control than a usable project and is not complete. 

Known problems/issues/limitations include:

* SRTM data tiles are expected to be saved to directory E:\Temp (set in [TileImageLoader.cs](SrtmMapLayer.WPF/TileImageLoader.cs))
* missing SRTM data tiles (which would be available at http://srtm.csi.cgiar.org/SELECTION/inputCoord.asp) are not downloaded automatically, only those already in the download directory are used 
* only map tiles that are within one SRTM data tile are rendered (in other words, those spanning two or more SRTM data tiles are not rendered)
* the calculation of coordinates based on the map tile to be displayed is incorrect
* This only works for the WPF application, the UWP code has not been updated
* ...