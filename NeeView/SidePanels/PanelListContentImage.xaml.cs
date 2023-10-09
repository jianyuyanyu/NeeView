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
    /// Thumbnail..xaml の相互作用ロジック
    /// </summary>
    public partial class PanelListContentImage : UserControl
    {
        public PanelListContentImage()
        {
            InitializeComponent();
            this.Root.DataContext = this;

            this.Unloaded += PanelListContentImage_Unloaded;
        }

        private void PanelListContentImage_Unloaded(object sender, RoutedEventArgs e)
        {
            ThumbnailBitmap?.Dispose();
        }


        public IThumbnail Thumbnail
        {
            get { return (IThumbnail)GetValue(ThumbnailProperty); }
            set { SetValue(ThumbnailProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailProperty =
            DependencyProperty.Register("Thumbnail", typeof(IThumbnail), typeof(PanelListContentImage), new PropertyMetadata(null, OnThumbnailPropertyChanged));

        private static void OnThumbnailPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PanelListContentImage control)
            {
                control.UpdateThumbnailBitmap();
            }
        }


        public ThumbnailBitmap? ThumbnailBitmap
        {
            get { return (ThumbnailBitmap)GetValue(ThumbnailBitmapProperty); }
            set { SetValue(ThumbnailBitmapProperty, value); }
        }

        public static readonly DependencyProperty ThumbnailBitmapProperty =
            DependencyProperty.Register("ThumbnailBitmap", typeof(ThumbnailBitmap), typeof(PanelListContentImage), new PropertyMetadata(null));


        private void UpdateThumbnailBitmap()
        {
            var thumbnail = Thumbnail;

            if (ThumbnailBitmap?.Thumbnail == thumbnail) return;

            ThumbnailBitmap?.Dispose();
            ThumbnailBitmap = null;

            if (thumbnail is null) return;
            ThumbnailBitmap = new ThumbnailBitmap(thumbnail);
        }
    }


    public class BooleanToContentStretchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Config.Current.Panels.ContentItemProfile.ImageStretch;
            }
            else
            {
                return System.Windows.Media.Stretch.Uniform;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class BooleanToContentViewboxConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Config.Current.Panels.ContentItemProfile.Viewbox;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class BooleanToContentAlignmentYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Config.Current.Panels.ContentItemProfile.AlignmentY;
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ContentBackgroundBrushConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Brush brush)
            {
                if (brush is SolidColorBrush solidColorBrush && solidColorBrush.Color.A != 0)
                {
                    return brush;
                }
                else
                {
                    return Config.Current.Panels.ContentItemProfile.Background;
                }
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentProfileToolTopEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Config.Current.Panels.ContentItemProfile.IsImagePopupEnabled;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
