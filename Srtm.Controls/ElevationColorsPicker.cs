using Srtm.Colors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Srtm.Controls
{
    public class ElevationColorsPicker : Grid
    {
        static ElevationColorsPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ElevationColorsPicker), new FrameworkPropertyMetadata(typeof(ElevationColorsPicker)));
        }

        #region Fields
        private Rectangle gradientRectangle;
        private Canvas infoPanel;
        private TextBox txtMaxHeight;
        private ElevationColors colors;
        private List<Ellipse> drawnPoints;
        private List<Label> heightLabels;
        private Rectangle seaLevelMarker;

        #region Context menu with menu items
        private ContextMenu contextMenuColorPoints;
        private MenuItem menuItemDelete;
        GradientColorPoint pointAtContextMenu;
        #endregion

        // There is no WPF-ColorDialog, so we use the regular WinForms dialog:
        private System.Windows.Forms.ColorDialog colorDialog;
        #endregion

        #region Constructor and Initialization
        public ElevationColorsPicker()
        {
            colors = new ElevationColors(ElevationColors.DefaultGradient);

            gradientRectangle = new Rectangle();
            gradientRectangle.MouseLeftButtonDown += gradientRectangle_MouseLeftButtonDown;

            this.ContextMenu = CreateContextMenu();
            contextMenuColorPoints = CreateContextMenuColorPoints();

            infoPanel = new Canvas();
            txtMaxHeight = new TextBox();
            txtMaxHeight.Text = colors.MaxHeight.ToString();
            txtMaxHeight.MaxLength = 4;
            txtMaxHeight.Width = 40;
            txtMaxHeight.TextChanged += txtMaxHeight_TextChanged;

            drawnPoints = new List<Ellipse>();
            heightLabels = new List<Label>();

            colorDialog = new System.Windows.Forms.ColorDialog();
            colorDialog.FullOpen = true;
            colorDialog.CustomColors = GetDialogCustomColors();

            infoPanel.Children.Add(txtMaxHeight);

            Children.Add(gradientRectangle);
            Children.Add(infoPanel);

            DrawGradient();
        }

        private ContextMenu CreateContextMenu()
        {
            return new ContextMenu()
            {
                Items =
                    {
                        CreateMenuItem("Add point", ((sender, e) => AddColorPointAtMousePos(GetPos(e.Source)))),
                        CreateMenuItem("Copy text representation to clipboard",
                        (sender, e) => Clipboard.SetText(colors.ToString()))
                    }
            };
        }

        private ContextMenu CreateContextMenuColorPoints()
        {
            // This menu item can be en-/disabled, therefore we need to keep a reference to it:
            menuItemDelete = CreateMenuItem("Delete point", ((sender, e) => DeleteColorPoint(pointAtContextMenu)));

            return new ContextMenu()
            {
                Items =
                    {
                        CreateMenuItem("Edit point", ((sender, e) => EditColor(pointAtContextMenu))),
                        menuItemDelete,
                    }
            };
        }

        private MenuItem CreateMenuItem(string header, RoutedEventHandler eventHandler)
        {
            MenuItem result = new MenuItem() { Header = header };
            result.Click += eventHandler;
            return result;
        }
        private int[] GetDialogCustomColors()
        {
            List<int> result = new List<int>();

            foreach (var point in colors.Points)
            {
                System.Drawing.Color color = System.Drawing.Color.FromArgb(0,
                    point.Color.R,
                    point.Color.G,
                    point.Color.B);
                result.Add(System.Drawing.ColorTranslator.ToOle(color));
            }

            return result.ToArray();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            AdjustSize();

            UpdateGradientAndPoints();

            DrawSeaLevelMarker();
            DrawHeightLabels();
        }

        private const double InfoPanelRatio = 0.61;
        private double GradientWidth
        {
            get
            {
                return this.ActualWidth * (1.0 - InfoPanelRatio);
            }
        }
        private double InfoPanelWidth
        {
            get
            {
                return this.ActualWidth * InfoPanelRatio;
            }
        }

        private void AdjustSize()
        {
            gradientRectangle.Margin = new Thickness(0, 0, InfoPanelWidth, 0);
            infoPanel.Margin = new Thickness(GradientWidth, 0, 0, 0);
        }
        #endregion

        #region Public methods, events and properties
        public event EventHandler<EventArgs> GradientChanged;

        public string XamlBrushText
        {
            get
            {
                StringBuilder result = new StringBuilder();

                result.Append("<LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"1,0\">\r");
                foreach (var point in colors.Points)
                {
                    result.Append(String.Format("    <GradientStop Color=\"{0}\" Offset=\"{1}\" />\r",
                        point.Color.ToString(), point.Offset.ToString())); // TODO: decimal separator
                }
                result.Append("</LinearGradientBrush>\r");

                return result.ToString();
            }
        }

        public ElevationColors Colors { get => colors; set => colors = value; }
        #endregion

        #region Draw gradient, points and marker
        private void UpdateGradientAndPoints()
        {
            DrawGradient();
            DrawPoints();

            OnGradientChanged();
        }

        private void OnGradientChanged()
        {
            if (GradientChanged != null)
            {
                GradientChanged(this, new EventArgs());
            }
        }

        private void DrawGradient()
        {
            gradientRectangle.Height = this.ActualHeight;

            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0.5, 1.0);
            brush.EndPoint = new Point(0.5, 0.0);

            foreach (GradientColorPoint colorPoint in colors.Points)
            {
                brush.GradientStops.Add(new GradientStop(colorPoint.Color, colorPoint.Offset));
            }

            gradientRectangle.Fill = brush;
        }

        private void DrawSeaLevelMarker()
        {
            if (seaLevelMarker == null)
            {
                seaLevelMarker = new Rectangle()
                {
                    Width = GradientWidth * 0.4,
                    Height = 6.0,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stroke = Brushes.Black,
                    Fill = Brushes.CornflowerBlue,
                };

                seaLevelMarker.MouseDown += seaLevelMarker_MouseDown;
                seaLevelMarker.MouseMove += seaLevelMarker_MouseMove;
                seaLevelMarker.MouseLeftButtonUp += seaLevelMarker_MouseLeftButtonUp;

                Children.Add(seaLevelMarker);
            }

            seaLevelMarker.Margin = new Thickness(
                0,
                this.ActualHeight * (1 - colors.SeaLevelOffset),
                InfoPanelWidth,
                0);
        }

        private void DrawPoints()
        {
            DeleteVisiblePoints();
            DrawNewPoints();
        }

        private void DeleteVisiblePoints()
        {
            foreach (Ellipse p in drawnPoints)
            {
                Children.Remove(p);
            }
            drawnPoints.Clear();
        }

        private void DrawNewPoints()
        {
            const int size = 10;

            foreach (GradientColorPoint colorPoint in colors.Points)
            {
                Ellipse point = new Ellipse()
                {
                    Width = size,
                    Height = size,
                    Margin = new Thickness(
                        GradientWidth / 2 - size / 2,
                        Math.Min((this.ActualHeight - size / 2) * (1.0 - colorPoint.Offset),
                                  this.ActualHeight - size),
                        0,
                        0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Stroke = Brushes.Black,
                    Fill = colorPoint.IsFixed ? Brushes.RosyBrown : Brushes.AntiqueWhite,
                    Tag = colorPoint,
                    //<Ellipse Width="8" Height="8" Margin="62.0,0,0,101.5" HorizontalAlignment="Left" VerticalAlignment="Bottom" Stroke="Black" Fill="AntiqueWhite" 
                    // MouseDown="Ellipse_MouseDown" MouseMove="Ellipse_MouseMove" MouseUp="Ellipse_MouseUp" Name="firstPoint">
                };
                point.MouseDown += colorPoint_MouseDown;
                point.MouseMove += colorPoint_MouseMove;
                point.MouseLeftButtonUp += colorPoint_MouseLeftButtonUp;
                point.ContextMenu = contextMenuColorPoints;
                point.ContextMenuOpening += new ContextMenuEventHandler(point_ContextMenuOpening);

                Children.Add(point);
                drawnPoints.Add(point);
            }
        }

        void point_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            pointAtContextMenu = (e.OriginalSource as Ellipse).Tag as GradientColorPoint;
            menuItemDelete.IsEnabled = !pointAtContextMenu.IsFixed;
        }

        #endregion

        #region Working with the color points
        private void AddColorPoint(double offset)
        {
            Color selectedColor = System.Windows.Media.Colors.Black;

            if (LetSelectColor(ref selectedColor))
            {
                colors.Add(selectedColor, offset);
                UpdateGradientAndPoints();
            }
        }

        private void EditColor(GradientColorPoint colorPoint)
        {
            Color selectedColor = System.Windows.Media.Colors.Black;

            if (LetSelectColor(ref selectedColor))
            {
                colorPoint.Color = selectedColor;
                UpdateGradientAndPoints();
            }
        }

        private void EditColorPointAtMousePos(Point p)
        {
            EditColor(null);
        }

        private void DeleteColorPointAtMousePos(Point p)
        {
            throw new NotImplementedException();
        }

        private void AddColorPointAtMousePos(Point p)
        {
            AddColorPoint(1 - p.Y / this.ActualHeight);
        }

        private Point GetPos(object contextMenuItem)
        {
            return ((contextMenuItem as FrameworkElement).Parent as ContextMenu).TranslatePoint(new Point(0, 0), this);
        }

        private void DeleteColorPoint(GradientColorPoint colorPointToDelete)
        {
            colors.Remove(colorPointToDelete);
            UpdateGradientAndPoints();
        }

        private void UpdateGradientColor(GradientColorPoint colorPointToUpdate, double newOffset)
        {
            colorPointToUpdate.Offset = newOffset;

            colors.SortByOffset();
            UpdateGradientAndPoints();
        }
        #endregion

        #region Labels
        private void DrawHeightLabels()
        {
            DeleteOldHeightLabels();
            DrawNewHeightLabels();
        }

        private void DeleteOldHeightLabels()
        {
            foreach (Label label in heightLabels)
            {
                infoPanel.Children.Remove(label);
            }
            heightLabels.Clear();
        }

        private void DrawNewHeightLabels()
        {
            if (colors.MaxHeight == 0)
            {
                return;
            }

            double exactInterval = (1 - colors.SeaLevelOffset) * colors.MaxHeight / 10;

            int interval = (int)Math.Pow(10.0, Math.Round(Math.Log10(exactInterval), 0));
            if (interval == 0)
            {
                return;
            }

            int height = 0;
            while (height < colors.MaxHeight)
            {
                double offset = (double)height / colors.MaxHeight + colors.SeaLevelOffset;
                double y = this.ActualHeight * (1.0 - offset) - 11;

                // Also stop if the text box is reached:
                if (y < txtMaxHeight.ActualHeight + txtMaxHeight.Margin.Top)
                {
                    break;
                }

                Label label = new Label()
                {
                    Margin = new Thickness(0, y, 0, 0),
                    Content = height.ToString()
                };

                infoPanel.Children.Add(label);
                heightLabels.Add(label);

                height += interval;
            }
        }
        #endregion

        #region Dialog
        private bool LetSelectColor(ref Color color)
        {
            bool result;

            colorDialog.Color = System.Drawing.Color.FromArgb(color.R, color.G, color.B);

            result = colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;

            if (result)
            {
                // The types are different (System.Drawing.Color and System.Windows.Media.Color):
                color = Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            }

            return result;
        }

        #endregion

        #region Events for dragging the sea level marker
        void seaLevelMarker_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle marker = e.Source as Rectangle;

            if (e.ClickCount == 1 && e.LeftButton == MouseButtonState.Pressed)
            {
                marker.CaptureMouse();
            }
        }

        void seaLevelMarker_MouseMove(object sender, MouseEventArgs e)
        {
            Rectangle marker = e.Source as Rectangle;

            if (marker.IsMouseCaptured)
            {
                double newPos = e.GetPosition(this).Y - marker.Height / 2;

                newPos = Math.Min(this.ActualHeight - gradientRectangle.Margin.Top - marker.Height,
                         Math.Max(gradientRectangle.Margin.Bottom, newPos));

                Console.WriteLine(newPos);

                marker.Margin = new Thickness(
                    marker.Margin.Left,
                    newPos,
                    marker.Margin.Right, marker.Margin.Bottom);
            }
        }

        void seaLevelMarker_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle marker = e.Source as Rectangle;

            if (marker.IsMouseCaptured)
            {
                Mouse.Capture(null);

                double newOffset = 1 - (e.GetPosition(gradientRectangle).Y - marker.Height / 2) / gradientRectangle.ActualHeight;
                newOffset = Math.Max(newOffset, marker.Height / gradientRectangle.ActualHeight);

                colors.SeaLevelOffset = newOffset;

                DrawHeightLabels();
            }
        }
        #endregion

        #region Events for dragging and editing color points
        private void colorPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Ellipse point = e.Source as Ellipse; if (point == null) { return; }

            GradientColorPoint colorPoint = (GradientColorPoint)point.Tag;

            if (e.ClickCount == 1)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    //EditColor(colorPoint);
                }
                else if (e.LeftButton == MouseButtonState.Pressed && !colorPoint.IsFixed)
                {
                    point.CaptureMouse();
                }
            }
            else if (e.ClickCount == 2 && !colorPoint.IsFixed)
            {
                // Nothing - do not delete color point here because it can happen too easily and unintentionally 
                // DeleteColorPoint(colorPoint);
            }
        }

        private void colorPoint_MouseMove(object sender, MouseEventArgs e)
        {
            Ellipse point = e.Source as Ellipse; if (point == null) { return; }

            if (point.IsMouseCaptured)
            {
                double newPos = e.GetPosition(this).Y - point.Height / 2;

                newPos = Math.Min(this.ActualHeight - gradientRectangle.Margin.Top - point.Height,
                         Math.Max(gradientRectangle.Margin.Bottom, newPos));

                point.Margin = new Thickness(
                    point.Margin.Left,
                    newPos,
                    point.Margin.Right, point.Margin.Bottom);
            }
        }

        private void colorPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Ellipse point = e.Source as Ellipse; if (point == null) { return; }

            if (point.IsMouseCaptured)
            {
                Mouse.Capture(null);

                double newOffset = 1 - e.GetPosition(gradientRectangle).Y / gradientRectangle.ActualHeight;
                UpdateGradientColor((GradientColorPoint)point.Tag, newOffset);
            }
        }

        void gradientRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                AddColorPoint(1 - e.GetPosition(gradientRectangle).Y / gradientRectangle.ActualHeight);
            }
        }
        #endregion
        void txtMaxHeight_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                int newValue;

                if (!Int32.TryParse(txtMaxHeight.Text, out newValue))
                {
                    throw new ArgumentException("Value must be an integer.");
                }
                if (newValue < 1 || newValue > 9000)
                {
                    throw new ArgumentOutOfRangeException("Value must be in the range from 0 to 9000");
                }

                colors.MaxHeight = newValue;
                txtMaxHeight.Background = Brushes.White;

                DrawHeightLabels();
            }
            catch
            {
                txtMaxHeight.Background = Brushes.Red;
            }
        }
    }
}
