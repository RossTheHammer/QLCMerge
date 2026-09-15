using QLCMerge.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace QLCMergeUI.ViewModels.Converters
{
    public class DefinitionMatchTypeToColorConverter : IValueConverter 
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DefinitionMatchType e)
            {
                switch(e)
                {
                    case DefinitionMatchType.Matched:
                        return Colors.DarkGreen;
                    case DefinitionMatchType.NameChange:
                        return Colors.DarkSlateBlue;
                    case DefinitionMatchType.Modified:
                        return Colors.DarkBlue;
                    case DefinitionMatchType.Divergent:
                        return Colors.DarkRed;
                }
            }
            return Colors.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
