﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeeView
{
    /// <summary>
    /// FolderListThumbnail.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderListThumbnail : UserControl
    {
        public FolderListThumbnail()
        {
            InitializeComponent();
        }

        private void Image_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            e.Handled = !ThumbnailProfile.Current.IsThumbnailPopup;
        }
    }


    [ValueConversion(typeof(ImageSource), typeof(ImageSource))]
    public class ImageSourceToThumbnailConverter : IValueConverter
    {
        private static readonly ImageSource _defaultThumbnail = MainWindow.Current.Resources["thumbnail_default"] as ImageSource;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
            {
                return _defaultThumbnail;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
