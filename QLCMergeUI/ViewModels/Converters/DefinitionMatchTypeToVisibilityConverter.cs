using QLCMerge.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
//using CommunityToolkit.M

namespace QLCMergeUI.ViewModels.Converters
{
    public class DefinitionMatchTypeToVisibilityConverter : IValueConverter //: BaseConverterOneWay<DefinitionMatchType, Visibility>
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if(value is DefinitionMatchType e)
            {
                return e == DefinitionMatchType.Matched ? Visibility.Hidden : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
