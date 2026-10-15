using System.Globalization;
using System.Windows.Data;
using InventoryApp.Models;

namespace InventoryApp.Helpers;

/// <summary>Converts a <see cref="MovementType"/> to its human readable label.</summary>
public sealed class MovementTypeNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is MovementType type ? type.ToDisplayName() : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
