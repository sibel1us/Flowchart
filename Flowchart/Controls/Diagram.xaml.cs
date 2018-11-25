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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Flowchart
{
    /// <summary>
    /// Interaction logic for Diagram.xaml
    /// </summary>
    [ContentProperty(nameof(Children))]
    public partial class Diagram : UserControl, INotifyPropertyChanged
    {
        public event EventHandler<DiagramGridChangedEventArgs> DiagramSizeChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private Node _draggedNode = null;
        public Node DraggedNode
        {
            get => _draggedNode;
            set
            {
                if (value != _draggedNode)
                {
                    _draggedNode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DraggedNode)));
                }
            }
        }

        private Node _resizingNode = null;
        public Node ResizingNode
        {
            get => _resizingNode;
            set
            {
                if (value != _resizingNode)
                {
                    _resizingNode = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ResizingNode)));
                }
            }
        }

        /// <summary>
        /// Children of the root grid.
        /// </summary>
        public UIElementCollection Children
        {
            get { return (UIElementCollection)GetValue(ChildrenProperty.DependencyProperty); }
            private set { SetValue(ChildrenProperty, value); }
        }

        /// <summary>
        /// Binds the diagram's children to <see cref="RootGrid"/>'s children.
        /// </summary>
        public static readonly DependencyPropertyKey ChildrenProperty =
            DependencyProperty.RegisterReadOnly(
                nameof(Children),
                typeof(UIElementCollection),
                typeof(Diagram),
                new PropertyMetadata());

        /// <summary>
        /// Amount of grid rows in the diagram.
        /// </summary>
        public int Rows
        {
            get => RootGrid.RowDefinitions.Count;
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, Properties.Settings.Default.MaxDiagramHeight);

                int delta = value - Rows;

                if (delta == 0) return;

                if (delta > 0)
                    for (int i = 0; i < delta; i++)
                        RootGrid.RowDefinitions.Add(new RowDefinition());

                else
                    for (int i = 0; i < -delta; i++)
                        RootGrid.RowDefinitions.RemoveAt(Rows - 1);

                DiagramSizeChanged?.Invoke(this, new DiagramGridChangedEventArgs(delta, 0));
            }
        }

        /// <summary>
        /// Amount of grid columns in the diagram.
        /// </summary>
        public int Columns
        {
            get => RootGrid.ColumnDefinitions.Count;
            set
            {
                value = Math.Max(value, 1);
                value = Math.Min(value, Properties.Settings.Default.MaxDiagramWidth);

                int delta = value - Columns;

                if (delta == 0) return;

                if (delta > 0)
                    for (int i = 0; i < delta; i++)
                        RootGrid.ColumnDefinitions.Add(new ColumnDefinition());

                else
                    for (int i = 0; i < -delta; i++)
                        RootGrid.ColumnDefinitions.RemoveAt(Columns - 1);

                DiagramSizeChanged?.Invoke(this, new DiagramGridChangedEventArgs(0, delta));
            }
        }

        public double NodeMargin { get; set; }

        /// <summary>
        /// Initializes a new diagram.
        /// </summary>
        public Diagram()
        {
            InitializeComponent();
            DataContext = this;
            Children = RootGrid.Children;
        }

        public void ScaleUp()
        {
            Rows = Rows * 2;
            Columns = Columns * 2;

            foreach (Node node in Children)
            {
                node.Row *= 2;
                node.Column *= 2;
                node.RowSpan *= 2;
                node.ColumnSpan *= 2;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RootGrid_Drop(object sender, DragEventArgs e)
        {
            _dragHelper = (0, 0, true);
            UpdateDragDrop(sender, e);
            // TODO: handle size constraints
        }

        private (double, double, bool) _dragHelper = (0, 0, true);

        /// <summary>
        /// Updates node position and layout while dragging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDragDrop(object sender, DragEventArgs e)
        {
            Node node = (Node)e.Data.GetData(nameof(Node));
            Point gridPos = GetPositionInGrid(e.GetPosition(RootGrid));

            var newPos = (gridPos.X, gridPos.Y, false);

            if (newPos.Item1 == _dragHelper.Item1 && newPos.Item2 == _dragHelper.Item2)
            {
                if (_dragHelper.Item3) return;
                else newPos.Item3 = true;
            }

            _dragHelper = newPos;

            node.Column = (int)gridPos.X - (node.ColumnSpan - 1);
            node.Row = (int)gridPos.Y;

            UpdateNodeStates(node);

        }

        private void UpdateNodeStates(Node node)
        {
            Rect nodePosition = new Rect(
                node.Column,
                node.Row,
                node.ColumnSpan - 1,
                node.RowSpan - 1);

            foreach (Node other in Children)
            {
                if (other == node) continue;

                Rect otherPosition = new Rect(
                    other.Column,
                    other.Row,
                    other.ColumnSpan - 1,
                    other.RowSpan - 1);

                other.Invalid = nodePosition.IntersectsWith(otherPosition);
            }
        }

        /// <summary>
        /// Gets the row and column of <paramref name="point"/> in <see cref="RootGrid"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Point GetPositionInGrid(Point point)
        {
            int row = 0;
            int column = 0;
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in RootGrid.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }

            foreach (var columnDefinition in RootGrid.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                column++;
            }

            return new Point(column, row);
        }
    }

    /// <summary>
    /// Provides data on the change whenever <see cref="Diagram"/>'s dimensions are changed.
    /// </summary>
    public class DiagramGridChangedEventArgs
    {
        public DiagramGridChangedEventArgs(int rowDelta, int columnDelta)
        {
            this.RowDelta = rowDelta;
            this.ColumnDelta = columnDelta;
        }

        public int RowDelta { get; }
        public int ColumnDelta { get; }
    }
}
