using System;
using System.Collections.Generic;
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
    public partial class Node : UserControl
    {
        public event EventHandler<NodeDimensionsChangedEventArgs> NodeDimensionsChanged;
        public event EventHandler<NodePositionChangedEventArgs> NodePositionChanged;

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
        /// Zero-based index of the node in parent grid's rows.
        /// </summary>
        public int Row
        {
            get => Grid.GetRow(this);
            set
            {
                value = Math.Max(value, 0);
                value = Math.Min(value, Diagram.Rows - 1);

                int delta = value - Row;
                if (delta == 0) return;

                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(delta, 0));
                Grid.SetRow(this, value);
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
                value = Math.Max(value, 0);
                value = Math.Min(value, Diagram.Columns - 1);

                int delta = value - Column;
                if (delta == 0) return;

                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(0, delta));
                Grid.SetColumn(this, value);
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

                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
                Grid.SetRowSpan(this, value);
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

                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
                Grid.SetColumnSpan(this, value);
            }
        }

        /// <summary>
        /// Root border's background color.
        /// </summary>
        public Brush BackgroundColor
        {
            get => Root.Background;
            set => Root.Background = value;
        }

        /// <summary>
        /// Initializes a new node control.
        /// </summary>
        public Node()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Handles the drag & drop behavior for the node.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Anchor_PreviewMouseMove(object sender, MouseEventArgs e)
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
                    Row = prevRow;
                    Column = prevCol;
                }
            }
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
    public class NodeNotDraggedToOpacityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] == null || values[0] == values[1])
            {
                return Properties.Settings.Default.NodeOpacity;
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

    public class ColorBrightnessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Media.Effects.PixelShader shader = new System.Windows.Media.Effects.PixelShader();

            SolidColorBrush brush = (SolidColorBrush)values[0];
            double factor = (double)values[1];

            Color color = brush.Color;

            return new SolidColorBrush(Color.FromRgb(
                Math.Min((byte)255, (byte)(color.R * factor)),
                Math.Min((byte)255, (byte)(color.G * factor)),
                Math.Min((byte)255, (byte)(color.B * factor))));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { new SolidColorBrush(Colors.Transparent), 1.0 };
        }
    }
}
