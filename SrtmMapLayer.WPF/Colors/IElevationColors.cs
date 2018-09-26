using System.Windows.Media;

namespace SrtmMapLayer.Colors
{
    public interface IElevationColors
    {
        Color this[int height] { get; }
    }
}
