using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Flowchart.Helpers
{
    public static class Extensions
    {
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
