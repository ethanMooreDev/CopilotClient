using Microsoft.UI.Xaml.Data;
using System;

namespace CopilotClient.Converters;

public class DateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if(value is DateTime dt)
        {
            DateTime today = DateTime.Now;

            string dateStr;

            if(dt.Date == today.Date)
            {
                dateStr = "Today,";
            }
            else if(dt.Date == today.Date.AddDays(-1))
            {
                dateStr = "Yesterday,";
            }
            else
            {
                dateStr = dt.ToShortDateString();
            }

            return $"{dateStr} {dt.ToShortTimeString()}";
        }

        return "Time not supported";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // One way conversion, so just return
        return value;
    }
}
