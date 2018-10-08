using Srtm.Colors;
using Srtm.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfApplication
{
    public class SrtmElevationColors : IElevationColors
    {
        // to avoid using this singleton:
        // - modify the ElevationColorsPicker to support data binding
        // - bind the elevationColorsPicker in MainWindow.xaml to the MapViewModel
        // - pass this SrtmElevationColors wrapper to MapLayers as a parameter and
        // - in MapLayers, pass this parameter on to the SrtmMapLayer.MapLayer constructor
        public static SrtmElevationColors Instance { get; private set; } = new SrtmElevationColors();

        public ElevationColorsPicker Picker { get; set; }

        public Color this[int height] => Picker == null ? Colors.Transparent : Picker.Colors[height];
    }
}
