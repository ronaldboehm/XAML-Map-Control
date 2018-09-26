using System;

namespace SrtmMapLayer
{
    /// <summary>
    /// Contains the heightmap data read from an ArcInfo ASCII file.
    /// </summary>
    [Serializable]
    public class DataTile
    {
        public DataTile(int columns, int rows)
        {
            Columns = columns;
            Rows    = rows;

            Values  = new Int16[columns, rows];
        }

        public string Filename { get; set; }

        public int Columns { get; private set; }
        public int Rows    { get; private set; }

        /// <summary>
        /// X of the lower left (sout-west) corner of the tile.
        /// </summary>
        public double LowerLeftX { get; internal set; }
        
        /// <summary>
        /// Y of the lower left (sout-west) corner of the tile.
        /// </summary>
        public double LowerLeftY { get; internal set; }
        
        public double UpperRightX { get { return LowerLeftX + CellSize * Columns; } }
        public double UpperRightY { get { return LowerLeftY + CellSize * Rows;    } }
        
        /// <summary>
        /// The size of each cell (one value point) in this tile.
        /// </summary>
        /// <remarks>
        /// Not the size of the tile! this can be calculated as CellSize * Columns resp. as CellSize * Rows.
        /// </remarks>
        public double CellSize { get; internal set; }
        public Int16 NoDataValue { get; internal set; }

        public Int16[,] Values { get; internal set; }

        public Int16 Value(double longitude, double latitude)
        {
            return 0;
        }

        public NormalizedValues Normalized
        {
            get
            {
                if (normalized == null)
                {
                    normalized = new NormalizedValues(this);
                }
                return normalized;
            }
        }
        [NonSerialized]
        private NormalizedValues normalized;
        public class NormalizedValues
        {
            private DataTile tile;
            private double normalizeFactor;

            internal NormalizedValues(DataTile tile)
            {
                this.tile = tile;
                normalizeFactor = 1.0f / tile.Max;
            }

            /// <summary>
            /// Returns values between 0 (or lower if there are altitudes below 0) and 1 
            /// instead of the absolute altitude values.
            /// </summary>
            public double this[int x, int y]
            {
                get
                {
                    return normalizeFactor * tile.Values[x, y];
                }

            }
        }

        public Int16 Min
        {
            get
            {
                Int16 result = Int16.MaxValue;

                for (int x = 0; x < Columns; x++)
                {
                    for (int y = 0; y < Rows; y++)
                    {
                        if (Values[x, y] < result)
                            result = Values[x, y];
                    }
                }

                return result;
            }
        }

        public Int16 Max
        {
            get
            {
                Int16 result = Int16.MinValue;

                for (int x = 0; x < Columns; x++)
                {
                    for (int y = 0; y < Rows; y++)
                    {
                        if (Values[x, y] > result)
                            result = Values[x, y];
                    }
                }

                return result;
            }
        }

        public bool Contains(double x, double y)
        {
            return
                (LowerLeftX <= x && x < UpperRightX) &&
                (LowerLeftY <= y && y < UpperRightY);
        }
    }
}
