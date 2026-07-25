using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Mawadi3Print.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string s)
        {
            var parts = s.Split('|');
            var brush = new SolidColorBrush(b ? ParseColor(parts[0]) : ParseColor(parts.Length > 1 ? parts[1] : "#E0E0E0"));
            brush.Freeze();
            return brush;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6 && hex.All(c => Uri.IsHexDigit(c)))
            return Color.FromRgb(
                byte.Parse(hex[..2], NumberStyles.HexNumber),
                byte.Parse(hex[2..4], NumberStyles.HexNumber),
                byte.Parse(hex[4..6], NumberStyles.HexNumber));

        return hex.ToLowerInvariant() switch
        {
            "green" => Colors.Green,
            "orange" => Colors.Orange,
            "red" => Colors.Red,
            "white" => Colors.White,
            "black" => Colors.Black,
            "gray" or "grey" => Colors.Gray,
            "transparent" => Colors.Transparent,
            _ => Colors.Transparent
        };
    }
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            var invert = parameter is string s && (s.Equals("invert", StringComparison.OrdinalIgnoreCase) || s == "-");
            return (b ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value!;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value!;
}

public class BoolToInverseVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(string), typeof(bool))]
public class RadioButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? parameter?.ToString() ?? "" : Binding.DoNothing;
}

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

[ValueConversion(typeof(string), typeof(System.Windows.FlowDirection))]
public class StringToFlowDirectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string lang && lang == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}