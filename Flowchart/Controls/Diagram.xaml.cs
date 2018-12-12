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

        public Image Preview { get; set; }

        //private (double, double, bool) _dragHelper = (0, 0, true);
        private List<double> _gridWidths = new List<double>();
        private List<double> _gridHeights = new List<double>();

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
                value = value.Limit(1, Properties.Settings.Default.MaxDiagramHeight);
                int delta = value - Rows;

                if (delta == 0) return;

                this.RootGrid.RowDefinitions.SetCount(delta);
                this.HighlightGrid.RowDefinitions.SetCount(delta);

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
                value = value.Limit(1, Properties.Settings.Default.MaxDiagramWidth);
                int delta = value - Columns;

                if (delta == 0) return;

                RootGrid.ColumnDefinitions.SetCount(delta);
                HighlightGrid.ColumnDefinitions.SetCount(delta);

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

        /// <summary>
        /// Doubles the width and height of the grid and scales all nodes appropriately.
        /// </summary>
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

        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            UpdateDragDrop(sender, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RootGrid_Drop(object sender, DragEventArgs e)
        {
            //_dragHelper = (0, 0, true);

            UpdateDragDrop(sender, e);
            // TODO: handle size constraints
        }


        /// <summary>
        /// Updates node position and layout while dragging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDragDrop(object sender, DragEventArgs e)
        {
            Point mouse = e.GetPosition(RootCanvas);
            Point relative = (Point)e.Data.GetData("Position");
            Point dragPos = e.GetPosition(RootGrid);

            Canvas.SetLeft(Preview, mouse.X - relative.X);
            Canvas.SetTop(Preview, mouse.Y - relative.Y);

            Point topLeft = new Point(
                (dragPos.X - relative.X).Limit(0.0, RootGrid.ActualWidth),
                (dragPos.Y - relative.Y).Limit(0.0, RootGrid.ActualHeight));

            Point gridPos = GetClosestPositionInGrid(topLeft);
            UpdateDragdropPreview(Highlight, gridPos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp">Image of the node to use in preview</param>
        /// <param name="rowSpan">Rowspan of the node being dragged</param>
        /// <param name="columnSpan">Columnspan of the node being dragged</param>
        public void InitDragDrop(RenderTargetBitmap bmp, int rowSpan, int columnSpan)
        {
            // Add drag/drop preview image
            this.RootCanvas.Children.Add(this.Preview = new Image
            {
                Source = bmp,
                IsHitTestVisible = false,
                Opacity = 0.8
            });

            // Set highlighter
            Grid.SetRowSpan(this.Highlight, rowSpan);
            Grid.SetColumnSpan(this.Highlight, columnSpan);
            this.Highlight.Visibility = Visibility.Visible;

            // Cache grid cell sizes
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;

            foreach (var rowDefinition in RootGrid.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                _gridHeights.Add(accumulatedHeight);
            }

            foreach (var columnDefinition in RootGrid.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                _gridWidths.Add(accumulatedWidth);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndDragDrop()
        {
            this.RootCanvas.Children.Remove(this.Preview);
            this.Highlight.Visibility = Visibility.Collapsed;

            _gridHeights.Clear();
            _gridWidths.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elem"></param>
        private void UpdateDragdropPreview(UIElement elem, Point gridPosition)
        {
            int rowSpan = Grid.GetRowSpan(elem);
            int columnSpan = Grid.GetColumnSpan(elem);

            Rect nodePosition = new Rect(
                Grid.GetColumn(elem),
                Grid.GetRow(elem),
                columnSpan - 1,
                rowSpan - 1);

            foreach (Node other in Children)
            {
                if (other == elem) continue;

                Rect otherPosition = new Rect(
                    other.Column,
                    other.Row,
                    other.ColumnSpan - 1,
                    other.RowSpan - 1);

                other.Invalid = nodePosition.IntersectsWith(otherPosition);
            }

            Grid.SetRow(elem, Math.Min((int)gridPosition.Y, this.Rows - rowSpan));
            Grid.SetColumn(elem, Math.Min((int)gridPosition.X, this.Columns - columnSpan));
        }


        /// <summary>
        /// Gets the closest row and column of <paramref name="point"/> in <see cref="RootGrid"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Point GetClosestPositionInGrid(Point point)
        {
            int row = 0;
            int column = 0;

            double center;
            double lastY = 0.0;
            double lastX = 0.0;

            foreach (var rowHeight in _gridHeights)
            {
                center = (lastY + rowHeight) / 2;

                if (center > point.Y)
                    break;

                row++;
                lastY = rowHeight;
            }

            foreach (var columnWidth in _gridWidths)
            {
                center = (lastX + columnWidth) / 2;

                if (center > point.X)
                    break;

                column++;
                lastX = columnWidth;
            }

            return new Point(column, row);
        }

        /// <summary>
        /// Gets the row and column of <paramref name="point"/> in <see cref="RootGrid"/>.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private Point GetAbsolutePositionInGrid(Point point)
        {
            int row = 0;
            int column = 0;

            foreach (var rowHeight in _gridHeights)
            {
                if (rowHeight > point.Y)
                    break;
                row++;
            }

            foreach (var columnWidth in _gridWidths)
            {
                if (columnWidth > point.X)
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
