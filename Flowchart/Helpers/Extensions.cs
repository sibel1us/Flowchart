using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Flowchart.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Ensures the the value is between <see cref="lower"/> and <see cref="upper"/> (inclusive).
        /// </summary>
        public static double Limit(this double val, double lower, double upper)
        {
            return Math.Max(lower, Math.Min(upper, val));
        }

        /// <summary>
        /// Ensures the the value is between <see cref="lower"/> and <see cref="upper"/> (inclusive).
        /// </summary>
        public static int Limit(this int val, int lower, int upper)
        {
            return Math.Max(lower, Math.Min(upper, val));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowDefinitions"></param>
        /// <param name="delta"></param>
        public static void SetCount(this RowDefinitionCollection rowDefinitions, int delta)
        {
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    rowDefinitions.Add(new RowDefinition());
                }
            }
            else
            {
                for (int i = 0; i < -delta; i++)
                {
                    rowDefinitions.RemoveAt(rowDefinitions.Count - 1);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columnDefinitions"></param>
        /// <param name="delta"></param>
        public static void SetCount(this ColumnDefinitionCollection columnDefinitions, int delta)
        {
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    columnDefinitions.Add(new ColumnDefinition());
                }
            }
            else
            {
                for (int i = 0; i < -delta; i++)
                {
                    columnDefinitions.RemoveAt(columnDefinitions.Count - 1);
                }
            }
        }

        public static Color Scale(this Color color, double factor)
        {
            factor = Math.Max(0.0, factor);

            return Color.FromRgb(
                Math.Min((byte)255, (byte)(color.R * factor)),
                Math.Min((byte)255, (byte)(color.G * factor)),
                Math.Min((byte)255, (byte)(color.B * factor)));
        }

        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            
            if (parentObject is null)
            {
                return null;
            }
            else if (parentObject is T parent)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }
    }
}
