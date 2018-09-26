namespace Srtm
{
    public class DataTileHeader
    {
        public string Filename { get; set; }

        public int Columns { get; internal set; }
        public int Rows    { get; internal set; }
        
        public double X { get; internal set; }
        public double Y { get; internal set; }
        public double CellSize { get; internal set; }

        public bool Contains(double x, double y)
        {
            return
                (x >= X && x < X + CellSize * Columns) &&
                (y >= Y && y < Y + CellSize * Rows);
        }
    }
}
