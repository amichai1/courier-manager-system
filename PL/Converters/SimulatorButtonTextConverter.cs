using System.Globalization;
using System.Windows.Data;

namespace PL.Converters
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class SimulatorButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "⏹ Stop Simulator" : "▶ Start Simulator";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
