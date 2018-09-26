using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Srtm
{
    /// <summary>
    /// Reads heightmap data from a file in ArcInfo ASCII Grid format.
    /// </summary>
    /// <remarks>
    /// ArcInfo ASCII Grid format
    /// =========================
    /// (from http://geotools.codehaus.org/ArcInfo+ASCII+Grid+format)
    /// ARC ASCIIGRID refers to a specifc interchange format developed for ARC/INFO rasters in ASCII 
    /// format. The format consists of a header that specifies the geographic domain and resolution, 
    /// followed by the actual grid cell values. Usually the file extension is .asc, but recent 
    /// versions of ESRI software also recognize the extension .grd. It looks like this:
    ///
    ///   ncols 157
    ///   nrows 171
    ///   xllcorner -156.08749650000
    ///   yllcorner 18.870890200000
    ///   cellsize 0.00833300
    ///   0 0 1 1 1 2 3 3 5 6 8 9 12 14 18 21 25 30 35 41 47 53
    ///   59 66 73 79 86 92 97 102 106 109 112 113 113 113 111 109 106
    ///   103 98 94 89 83 78 72 67 61 56 51 46 41 37 32 29 25 22 19
    ///   etc...
    ///
    /// Records 1 - 6 Geographic header
    ///
    /// Coordinates may be in decimal or integer format. DD:MM:SS format for geodetic coordinates is not supported.
    /// 
    /// ncols xxxxx
    /// ncols refers to the number of columns in the grid and xxxxx is the numerical value
    ///
    /// nrows xxxxx
    /// nrows refers to the number of rows in the grid and xxxxx is the numerical value
    ///
    /// xllcorner xxxxx
    /// xllcorner refers to the western edge of the grid and xxxxx is the numerical value
    /// 
    /// yllcorner xxxxx
    /// yllcorner refers to the southern edge of the grid and xxxxx is the numerical value
    /// 
    /// cellsize xxxxx
    /// cellsize refers to the resolution of the grid and xxxxx is the numerical value
    /// 
    /// nodata_value xxxxx
    /// nodata_value refers to the value that represents missing data and xxxxx is the numerical value. 
    /// 
    /// This is optional and your parser should not assume it will be present. Note: that if you need 
    /// a good value, the ESRI default is -9999.
    ///
    /// Record 7 -> end of file Data values
    ///
    /// These are the value of individual cell typically representing elevation of a particular area.
    /// xxx xxx xxx
    ///
    /// val(nox,noy) (f) = individual grid values, column varying fastest in integer format. Grid 
    /// values are stored as integers but can be read as floating point values.
    /// </remarks>
    public class ArcAsciiGridFileReader
    {
        private readonly NumberFormatInfo format;

        public ArcAsciiGridFileReader()
        {
            format = new NumberFormatInfo()
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ","
            };
        }

        #region Progress
        public event EventHandler<ProgressChangedEventArgs> Progress;
        internal delegate void ProgressDelegate(int index, int max);
        private void ReportProgress(int index, int max)
        {
            if (Progress != null)
            {
                Progress(this, new ProgressChangedEventArgs((100 * index) / max, null));
            }
        }
        #endregion

        #region ReadTile
        public HeightMap ReadHeightMap(string filename)
        {
            HeightMap heightMap;

            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    heightMap = CreateHeightMapFromHeader(reader);
                    heightMap.Filename = filename;

                    ValueReaderBase valueReader = new FastValueReader(heightMap, reader, ReportProgress);
                    valueReader.Read();
                }
            }

            return heightMap;
        }
        #endregion

        #region Reader helpers
        private HeightMap CreateHeightMapFromHeader(StreamReader reader)
        {
            // ncols 157
            // nrows 171
            // xllcorner -156.08749650000
            // yllcorner 18.870890200000
            // cellsize 0.00833300
            Int16 columns = ReadInt16("ncols", reader.ReadLine());
            Int16 rows = ReadInt16("nrows", reader.ReadLine());

            return new HeightMap(columns, rows)
            {
                LowerLeftX  = ReadFloat("xllcorner",    reader.ReadLine()),
                LowerLeftY  = ReadFloat("yllcorner",    reader.ReadLine()),
                CellSize    = ReadFloat("cellsize",     reader.ReadLine()),
                NoDataValue = ReadInt16("NODATA_value", reader.ReadLine()),
            };
        }

        private HeightMapHeader CreateHeader(StreamReader reader)
        {
            // skip columns/rows information:

            return new HeightMapHeader()
            {
                Columns = ReadInt16("ncols", reader.ReadLine()),
                Rows = ReadInt16("nrows", reader.ReadLine()),
                X = ReadFloat("xllcorner", reader.ReadLine()),
                Y = ReadFloat("yllcorner", reader.ReadLine()),
                CellSize = ReadFloat("cellsize", reader.ReadLine()),
            };
        }

        private Int16 ReadInt16(string expectedName, string line)
        {
            return Convert.ToInt16(ReadHeaderValue(expectedName, line));
        }

        private string ReadHeaderValue(string expectedName, string line)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new ArgumentException("line: Should contain name and value.");
            }
            if (!parts[0].Equals(expectedName, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new ArgumentException(String.Format("line: Should contain name {0}.", expectedName));
            }

            return parts[1].Trim();
        }

        private double ReadFloat(string expectedName, string line)
        {
            return Convert.ToDouble(ReadHeaderValue(expectedName, line), format);
        }
        #endregion

        #region ValueReader
        private abstract class ValueReaderBase
        {
            protected HeightMap heightMap;
            protected StreamReader reader;
            protected int column;
            protected int row;
            protected ProgressDelegate progress;

            internal ValueReaderBase(HeightMap heightMap, StreamReader reader, ProgressDelegate progress)
            {
                this.heightMap = heightMap;
                this.reader    = reader;
                this.progress  = progress;

                column = 0;
                row    = 0;
            }

            internal protected abstract void Read();

            protected void AdvanceColumn()
            {
                column++;
                if (column >= heightMap.Columns)
                {
                    column = 0;
                    row++;
                }
            }
        }

        /// <remarks>
        /// Shows clearly how the values are read from the lines in the file. 
        /// For better performance, use the FastValueReader.
        /// </remarks>
        private class SimpleValueReader : ValueReaderBase
        {
            internal SimpleValueReader(HeightMap tile, StreamReader reader, ProgressDelegate progress) :
                base(tile, reader, progress)
            { }

            internal protected override void Read()
            {
                while (!reader.EndOfStream)
                {
                    ReadValuesFromLine(reader.ReadLine());
                    progress(row, heightMap.Rows);
                }

                progress(heightMap.Rows, heightMap.Rows);
            }

            private void ReadValuesFromLine(string line)
            {
                foreach (string s in line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    heightMap.Values[column, row] = Convert.ToInt16(s);
                    AdvanceColumn();
                }
            }
        }

        /// <remarks>
        /// Changes from the old SimpleValueReader to improve performance:
        /// 
        ///  1. Set row and column directly instead of calculating the values, i.e.
        ///         AdvanceColumn()
        ///     instead of
        ///         arrayIndex++ 
        ///       with
        ///         private int arrayIndex = 0;
        ///         private int column { get { return arrayIndex % maxColumn; } }
        ///         private int row { get { return arrayIndex / maxColumn; } }
        ///
        ///  2. Copy the stream content to a buffer with fixed size and parse this
        ///     buffer "by hand" instead of reading lines, splitting into strings and
        ///     using Int16.Parse().
        /// 
        /// Together, these changes reduced the time required to load a file by about
        /// 65%, e.g. from 11.5 seconds to 4 seconds on one computer.
        /// </remarks>
        private class FastValueReader : ValueReaderBase
        {
            private const int bufferSize = 10000;
            private char[] buffer;

            internal FastValueReader(HeightMap tile, StreamReader reader, ProgressDelegate progress) :
                base(tile, reader, progress)
            {
                buffer = new char[bufferSize];
            }

            internal protected override void Read()
            {
                int currentValue = 0;

                bool negative = false;
                bool hasValue = false;

                int max = bufferSize;
                int index = 0;

                do
                {
                    if (index == max)
                    {
                        max = ReadIntoBuffer();
                        index = 0;

                        progress(row, heightMap.Rows);
                    }

                    if (buffer[index] == ' ' || buffer[index] == (char)13)
                    {
                        if (hasValue)
                        {
                            if (negative)
                            {
                                currentValue *= -1;
                            }

                            //parent.AddValue(currentValue);
                            heightMap.Values[column, row] = (short)currentValue;
                            AdvanceColumn();

                            negative = false;
                            currentValue = 0;
                            hasValue = false;
                        }
                    }
                    else if (buffer[index] == '-')
                    {
                        negative = true;
                    }
                    else if (buffer[index] >= '0' && buffer[index] <= '9')
                    {
                        currentValue = currentValue * 10 + (buffer[index] - 48);
                        hasValue = true;
                    }

                    index++;
                }
                while (index < max || max == bufferSize);

                progress(1, 1);
            }

            private int ReadIntoBuffer()
            {
                return reader.Read(buffer, 0, bufferSize);
            }
        }
        #endregion

        public HeightMapHeader ReadHeader(string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    var result = CreateHeader(reader);
                    result.Filename = filename;

                    return result;
                }
            }
        }
    }
}
