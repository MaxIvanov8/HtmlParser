using System;
using System.Windows.Data;

namespace HtmlParser
{
    public class EqualityConverter:IMultiValueConverter
    {
	public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)  
        {  
            if(values[0]==values[1]) return true;
                else return false;
        }  
  
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)  
        {  
            throw new NotImplementedException();  
        }  
    }
}
