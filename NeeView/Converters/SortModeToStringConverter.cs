﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Windows.Data;

namespace NeeView
{
    // コンバータ：ソートモード表示文字列
    [ValueConversion(typeof(PageSortMode), typeof(string))]
    public class SortModeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            PageSortMode mode = (PageSortMode)value;
            return mode.ToDispString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
