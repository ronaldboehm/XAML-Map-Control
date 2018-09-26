using MapControl;
using Srtm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SrtmMapLayer
{
    public class HeightMapProvider
    {
        private string dataDirectory;

        private ArcAsciiGridFileReader reader = new ArcAsciiGridFileReader();
        private List<HeightMapHeader> headers = new List<HeightMapHeader>();
        private List<HeightMap> heightMaps = new List<HeightMap>();

        public HeightMapProvider(string dataDirectory)
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

        internal HeightMapHeader HeaderForTile(Location lowerLeft, Location upperRight)
        {
            return headers.FirstOrDefault(h => h.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && h.Contains(upperRight.Longitude, upperRight.Latitude));
        }

        internal HeightMap Get(Location lowerLeft, Location upperRight)
        {
            // already in memory? 
            var heightMap = heightMaps.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
            // then take that one:
            if (heightMap != null)
                return heightMap;

            // not in memory, but in the directory?
            var header = headers.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
            if (header == null)
                return null;

            // then read it now...
            ReadFromFile(header.Filename);

            // ... and return it:
            return heightMaps.FirstOrDefault(tile => tile.Contains(lowerLeft.Longitude, lowerLeft.Latitude) && tile.Contains(upperRight.Longitude, upperRight.Latitude));
        }

        private object lockObject = new Object();
        private void ReadFromFile(string filename)
        {
            lock (lockObject)
            {
                // ignore multiple requests for the same tile:
                if (heightMaps.Any(tile => tile.Filename.Equals(filename)))
                    return;

                heightMaps.Add(reader.ReadHeightMap(filename));
            }
        }
    }
}
