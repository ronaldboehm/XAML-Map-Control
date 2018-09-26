using MapControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SrtmMapLayer
{
    public class DataTileProvider
    {
        private string dataDirectory;

        private ArcAsciiGridFileReader reader = new ArcAsciiGridFileReader();
        private List<DataTileHeader> headers = new List<DataTileHeader>();
        private List<DataTile> dataTiles = new List<DataTile>();

        public DataTileProvider(string dataDirectory)
        {
            this.dataDirectory = dataDirectory;

            Init();
        }

        private void Init()
        {
            Task.Run(() =>
            {
                var files = System.IO.Directory.EnumerateFiles(dataDirectory, "*.asc");
                foreach (var filename in files)
                {
                    headers.Add(reader.ReadHeader(filename));
                }
            });
        }


        internal DataTileHeader HeaderForTile(Location lowerLeft, Location upperRight)
        {
            return headers.FirstOrDefault(h => h.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && h.Contains(upperRight.Longitude, upperRight.Latitude));
        }

        internal DataTile Get(Location lowerLeft, Location upperRight)
        {
            // already in memory? 
            var result = dataTiles.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
            // then take that one:
            if (result != null)
                return result;

            // not in memory, but in the directory?
            var header = headers.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
            if (header == null)
                return null;

            // then read it now...
            ReadTile(header.Filename);

            // ... and return it:
            return dataTiles.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
        }

        private object lockObject = new Object();
        private void ReadTile(string filename)
        {
            lock (lockObject)
            {
                // ignore multiple requests for the same tile:
                if (dataTiles.Any(tile => tile.Filename.Equals(filename)))
                    return;

                dataTiles.Add(reader.ReadTile(filename));
            }
        }
    }
}
