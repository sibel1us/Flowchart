using System;
using System.Collections.Generic;
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

        public Diagram Diagram => (Diagram)((Grid)this.Parent).Parent;

        public bool IsDraggable { get; set; } = true;
        public bool IsDragged
        {
            get => Diagram.DraggedNode == this;
            set => Diagram.DraggedNode = (value ? this : null);
        }

        public int Row
        {
            get => Grid.GetRow(this);
            set
            {
                if (value < 0)
                    return;

                // TODO
                if (value >= 64)
                    return;

                int delta = value - Row;
                if (delta == 0) return;
                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(delta, 0));
                Grid.SetRow(this, value);
            }
        }

        public int Column
        {
            get => Grid.GetColumn(this);
            set
            {
                if (value < 0)
                    return;

                // TODO
                if (value >= 64)
                    return;

                int delta = value - Column;
                if (delta == 0) return;
                NodePositionChanged?.Invoke(this, new NodePositionChangedEventArgs(0, delta));
                Grid.SetColumn(this, value);
            }
        }

        public int ColumnSpan
        {
            get => Grid.GetColumnSpan(this);
            set
            {
                if (value < 1)
                    return;

                // TODO
                if (value > 64)
                    return;

                int delta = value - ColumnSpan;
                if (delta == 0) return;
                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
                Grid.SetColumnSpan(this, value);
            }
        }

        public int RowSpan
        {
            get => Grid.GetRowSpan(this);
            set
            {
                if (value < 1)
                    return;

                // TODO
                if (value > 64)
                    return;

                int delta = value - RowSpan;
                if (delta == 0) return;
                NodeDimensionsChanged?.Invoke(this, new NodeDimensionsChangedEventArgs(0, delta));
                Grid.SetRowSpan(this, value);
            }
        }

        public Brush BackgroundColor
        {
            get => Root.Background;
            set => Root.Background = value;
        }

        public Node()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Anchor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsDraggable && !IsDragged && e.LeftButton == MouseButtonState.Pressed)
            {
                IsDragged = true;
                Opacity = 0.5;

                DataObject data = new DataObject();
                data.SetData(nameof(Node), this);
                DragDropEffects result = DragDrop.DoDragDrop(this, data, DragDropEffects.Move);

                Opacity = 16;
                IsDragged = false;
            }
        }
    }

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
}
