using Flowchart.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flowchart
{
    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Node : UserControl, INotifyPropertyChanged
    {
        private readonly ColorBrightnessConverter colorBrightnessConverter = new ColorBrightnessConverter();
        public event EventHandler<NodeDimensionsChangedEventArgs> NodeDimensionsChanged;
        public event EventHandler<NodePositionChangedEventArgs> NodePositionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The diagram this node belongs to.
        /// </summary>
        public Diagram Diagram => (Diagram)((Grid)this.Parent).Parent;

        /// <summary>
        /// Whether this node is currently being dragged (<see cref="Diagram.DraggedNode"/> equals this node).
        /// </summary>
        public bool IsDragged
        {
            get => Diagram.DraggedNode == this;
            set
            {
                Panel.SetZIndex(this, value ? 999 : 0);
                Diagram.DraggedNode = (value ? this : null);
            }
        }

        /// <summary>
        /// Whether this node is currently being dragged (<see cref="Diagram.DraggedNode"/> equals this node).
        /// </summary>
        public bool IsResizing
        {
            get => Diagram.ResizingNode == this;
            set
            {
                Panel.SetZIndex(this, value ? 999 : 0);
                Diagram.ResizingNode = (value ? this : null);
            }
        }

        private bool _invalid = false;

        /// <summary>
        /// Visual state that signals to the user that if the current action is completed, the node will be stashed.
        /// </summary>
        public bool Invalid
        {
            get => _invalid;
            set
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Invalid)));
                _invalid = value;
            }
        }

        /// <summary>
        /// Zero-based index of the node in parent grid's rows.
        /// </summary>
        public int Row
        {
            get => Grid.GetRow(this);
            set
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    value = Math.Max(value, 0);
                    value = Math.Min(value, (Diagram?.Rows ?? 1) - RowSpan);
                }

                int delta = value - Row;
                if (delta == 0) return;

                Grid.SetRow(this, value);
                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(delta, 0));
            }
        }

        /// <summary>
        /// Zero-based index of the node in parent grid's columns.
        /// </summary>
        public int Column
        {
            get => Grid.GetColumn(this);
            set
            {
                if (!DesignerProperties.GetIsInDesignMode(this))
                {
                    value = Math.Max(value, 0);
                    value = Math.Min(value, Diagram.Columns - ColumnSpan);
                }

                int delta = value - Column;
                if (delta == 0) return;

                Grid.SetColumn(this, value);
                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(0, delta));
            }
        }

        /// <summary>
        /// Height of the node in cells.
        /// </summary>
        public int RowSpan
        {
            get => Grid.GetRowSpan(this);
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, Properties.Settings.Default.MaxNodeHeight);

                int delta = value - RowSpan;
                if (delta == 0) return;

                Grid.SetRowSpan(this, value);
                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
            }
        }

        /// <summary>
        /// Width of the node in cells.
        /// </summary>
        public int ColumnSpan
        {
            get => Grid.GetColumnSpan(this);
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, Properties.Settings.Default.MaxNodeWidth);

                int delta = value - ColumnSpan;
                if (delta == 0) return;

                Grid.SetColumnSpan(this, value);
                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
            }
        }

        /// <summary>
        /// Root border's background color.
        /// </summary>
        public Brush NodeColor { get; set; }// = Brushes.Transparent;

        /// <summary>
        /// Initializes a new node control.
        /// </summary>
        public Node()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitiateDrag(MouseEventArgs e)
        {
            if (!IsDragged && e.LeftButton == MouseButtonState.Pressed)
            {
                // Save the dragged state to not drag other nodes when a drag passes over them.
                IsDragged = true;

                int prevRow = Row;
                int prevCol = Column;

                // TODO: replace the node with a serializable model
                DataObject data = new DataObject();
                data.SetData(nameof(Node), this);

                DragDropEffects result = DragDrop.DoDragDrop(this, data, DragDropEffects.Move);

                IsDragged = false;

                // Return to original position if drag didn't complete.
                if (result != DragDropEffects.Move)
                {
                    //Row = prevRow;
                    //Column = prevCol;
                }
            }
        }

        /// <summary>
        /// Handles the drag &amp; drop behavior for the node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Anchor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            InitiateDrag(e);
        }

        private void Root_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!e.Handled) InitiateDrag(e);
        }

        private void Root_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Root.Focus();
        }
    }

    /// <summary>
    /// Provides data for changes in <see cref="Node.RowSpan"/> and <see cref="Node.ColumnSpan"/>.
    /// </summary>
    public class NodeDimensionsChangedEventArgs
    {
        public NodeDimensionsChangedEventArgs(int rowDelta, int columnDelta)
        {
            this.RowDelta = rowDelta;
            this.ColumnDelta = columnDelta;
        }

        public int RowDelta { get; }
        public int ColumnDelta { get; }
    }

    /// <summary>
    /// Provides data for changes in <see cref="Node.Row"/> and <see cref="Node.Column"/>.
    /// </summary>
    public class NodePositionChangedEventArgs
    {
        public NodePositionChangedEventArgs(int rowDelta, int columnDelta)
        {
            this.RowDelta = rowDelta;
            this.ColumnDelta = columnDelta;
        }

        public int RowDelta { get; }
        public int ColumnDelta { get; }
    }

    /// <summary>
    /// Gets an opacity value depending on if a node is being dragged. If a dragged node is not
    /// the node in index 0, a lower opacity is returned.
    /// </summary>
    public class NodeOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Node thisNode = (Node)values[0];
            Node draggedNode = (Node)values[1];

            if (draggedNode == null || thisNode == draggedNode)
            {
                return Properties.Settings.Default.NodeOpacity;
            }
            else if (thisNode.Invalid == true)
            {
                return Properties.Settings.Default.InvalidNodeOpacity;
            }
            else
            {
                return Properties.Settings.Default.InactiveNodeOpacity;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new Node[] { null, null };
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ColorBrightnessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = (SolidColorBrush)values[0];
            double factor = (double)values[1];

            return new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0.0),
                EndPoint = new Point(0.5, 1.0),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(brush.Color.Scale(factor), 0.0),
                    new GradientStop(brush.Color.Scale(0.8 * factor), 1.0)
                }
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { new SolidColorBrush(Colors.Transparent), 1.0 };
        }
    }

    public class DoubleToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new CornerRadius((double)value / 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0.0;
        }
    }

    public class IsFocusedToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool focused)
            {
                return focused ? Colors.DeepSkyBlue : Colors.DarkGray;
            }

            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
