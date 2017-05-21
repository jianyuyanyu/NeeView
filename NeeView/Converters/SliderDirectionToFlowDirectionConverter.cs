﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Windows;
using System.Windows.Data;

namespace NeeView
{
    // コンバータ：サムネイル方向
    [ValueConversion(typeof(bool), typeof(FlowDirection))]
    public class SliderDirectionToFlowDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isReverse = false; ;
            if (value is bool)
            {
                isReverse = (bool)value;
            }
            else if (value is string)
            {
                bool.TryParse((string)value, out isReverse);
            }

            return isReverse ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
