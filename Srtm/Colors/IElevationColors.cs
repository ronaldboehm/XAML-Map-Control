using System.Windows.Media;

namespace Srtm.Colors
{
    public interface IElevationColors
    {
        Color this[int height] { get; }
    }
}
