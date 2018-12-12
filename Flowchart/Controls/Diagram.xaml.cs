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

        private (double, double, bool) _dragHelper = (0, 0, true);
        public Image Preview { get; set; }

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
            _dragHelper = (0, 0, true);

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

            UpdateDragdropPreview(Highlight);

            Point topLeft = new Point(
                (dragPos.X - relative.X).Limit(0, RootGrid.ActualWidth),
                (dragPos.Y - relative.Y).Limit(0, RootGrid.ActualHeight));

            Point gridPos = GetPositionInGrid(topLeft);

            var newPos = (gridPos.X, gridPos.Y, false);

            if (newPos.Item1 == _dragHelper.Item1 && newPos.Item2 == _dragHelper.Item2)
            {
                if (_dragHelper.Item3) return;
                else newPos.Item3 = true;
            }

            _dragHelper = newPos;

            Grid.SetRow(Highlight, (int)gridPos.Y);
            Grid.SetColumn(Highlight, (int)gridPos.X);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="rowSpan"></param>
        /// <param name="columnSpan"></param>
        public void InitDragDrop(RenderTargetBitmap bmp, int rowSpan, int columnSpan)
        {
            this.Preview = new Image
            {
                Source = bmp,
                IsHitTestVisible = false,
                Visibility = Visibility.Visible
            };

            this.RootCanvas.Children.Add(this.Preview);

            Grid.SetRowSpan(this.Highlight, rowSpan);
            Grid.SetColumnSpan(this.Highlight, columnSpan);
            this.Highlight.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 
        /// </summary>
        public void EndDragDrop()
        {
            this.RootCanvas.Children.Remove(this.Preview);
            this.Highlight.Visibility = Visibility.Collapsed;
        }

        private void UpdateDragdropPreview(UIElement elem)
        {
            Rect nodePosition = new Rect(
                Grid.GetColumn(elem),
                Grid.GetRow(elem),
                Grid.GetColumnSpan(elem) - 1,
                Grid.GetRowSpan(elem) - 1);

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
