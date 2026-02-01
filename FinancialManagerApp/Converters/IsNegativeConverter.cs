using System;
using System.Globalization;
using System.Windows.Data;

namespace FinancialManagerApp.Converters
{
    // Konwerter sprawdza, czy liczba jest ujemna.
    // Zwraca TRUE (jeśli ujemna) lub FALSE (jeśli dodatnia/zero).
    // Używamy tego w XAML, aby zmienić kolor czcionki na czerwony dla wydatków.
    public class IsNegativeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Obsługa typu decimal (najczęstszy dla walut)
            if (value is decimal decimalValue)
            {
                return decimalValue < 0;
            }

            // Obsługa typu double
            if (value is double doubleValue)
            {
                return doubleValue < 0;
            }

            // Obsługa typu int
            if (value is int intValue)
            {
                return intValue < 0;
            }

            // Obsługa typu float
            if (value is float floatValue)
            {
                return floatValue < 0;
            }

            // Jeśli wartość jest nullem lub innym typem, uznajemy że nie jest ujemna
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}