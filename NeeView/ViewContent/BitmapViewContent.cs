﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NeeView
{
    /// <summary>
    /// Bitmap ViewContent
    /// </summary>
    public class BitmapViewContent : ViewContent
    {
        #region Fields

        private Rectangle _scaleRectangle;
        private Rectangle _pixeledRectangle;

        private BitmapSource _viewBitmap;

        #endregion

        #region Constructors

        public BitmapViewContent(ViewPage source) : base(source)
        {
        }

        #endregion

        #region Medhots

        //
        public void Initialize()
        {
            // binding parameter
            var parameter = CreateBindingParameter();

            // create view
            this.View = new ViewContentControl(CreateView(this.Source, parameter));

            // content setting
            var bitmapContent = this.Content as BitmapContent;
            this.Color = bitmapContent.Color;
        }

        //
        protected FrameworkElement CreateView(ViewPage source, ViewContentParameters parameter)
        {
            return CreateView(source, parameter, GetPicture()?.BitmapSource);
        }

        //
        protected FrameworkElement CreateView(ViewPage source, ViewContentParameters parameter, BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                _viewBitmap = null;
                _scaleRectangle = null;
                _pixeledRectangle = null;
                return null;
            }

            var grid = new Grid();
            grid.SetBinding(Grid.BackgroundProperty, parameter.PageBackgroundBrush);
            grid.UseLayoutRounding = true;

            _viewBitmap = bitmap;

            // scale bitmap
            {
                var rectangle = new Rectangle();
                rectangle.Fill = source.CreatePageImageBrush(bitmap, true);
                rectangle.SetBinding(RenderOptions.BitmapScalingModeProperty, parameter.BitmapScalingMode);
                rectangle.UseLayoutRounding = true;
                rectangle.SnapsToDevicePixels = true;

                _scaleRectangle = rectangle;

                grid.Children.Add(rectangle);
            }

            // pixeled bitmap
            {
                var canvas = new Canvas();
                canvas.UseLayoutRounding = true;

                var rectangle = new Rectangle();
                rectangle.Fill = source.CreatePageImageBrush(bitmap, true);
                RenderOptions.SetBitmapScalingMode(rectangle, BitmapScalingMode.NearestNeighbor);
                rectangle.UseLayoutRounding = true;
                rectangle.SnapsToDevicePixels = true;

                var viewSize = source.GetViewBitmapSize(bitmap);
                rectangle.Width = viewSize.Width;
                rectangle.Height = viewSize.Height;

                rectangle.RenderTransformOrigin = new Point(0, 0);

                _pixeledRectangle = rectangle;
                _pixeledRectangle.Visibility = Visibility.Collapsed;

                canvas.Children.Add(rectangle);
                grid.Children.Add(canvas);
            }

            return grid;
        }

        /// <summary>
        /// コンテンツ表示モード設定
        /// </summary>
        /// <param name="mode">表示モード</param>
        /// <param name="viewScale">Pixeled時に適用するスケール</param>
        public override void SetViewMode(ContentViewMode mode, double viewScale)
        {
            var sacaleInverse = 1.0 / viewScale;
            _pixeledRectangle.RenderTransform = new ScaleTransform(sacaleInverse, sacaleInverse);

            if (mode == ContentViewMode.Pixeled)
            {
                _scaleRectangle.Visibility = Visibility.Collapsed;
                _pixeledRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                _scaleRectangle.Visibility = Visibility.Visible;
                _pixeledRectangle.Visibility = Visibility.Collapsed;
            }
        }

        //
        public override bool IsBitmapScalingModeSupported() => true;


        //
        public Picture GetPicture()
        {
            return ((BitmapContent)this.Content)?.Picture;
        }

        //
        public BitmapSource GetViewBitmap()
        {
            return _viewBitmap;
        }

        //
        public bool IsDarty()
        {
            var pictureBitmap = GetPicture()?.BitmapSource;
            return _viewBitmap != pictureBitmap;
        }


        //
        public override Brush GetViewBrush()
        {
            return _scaleRectangle?.Fill;
        }



        //
        public override bool Rebuild(double scale)
        {
            var size = PictureProfile.Current.IsResizeFilterEnabled ? GetScaledSize(scale) : Size.Empty;

            // TODO: 判定サイズの修正
            if (ContentCanvas.Current.IsEnabledNearestNeighbor && (size.Width >= this.Source.Size.Width || size.Height >= this.Source.Size.Height))
            {
                size = Size.Empty;
            }

            return Rebuild(size);
        }

        //
        protected Size GetScaledSize(double scale)
        {
            return new Size(this.Width * scale * (this.IsHalf ? 2 : 1), this.Height * scale);
        }

        //
        protected bool Rebuild(Size size)
        {
            if (this.IsResizing) return false;

            this.IsResizing = true;

            Task.Run(() =>
            {
                try
                {
                    var picture = ((BitmapContent)this.Content)?.Picture;
                    picture?.CreateBitmapSource(size, CancellationToken.None);

                    // ##
                    this.Source.Page.DebugRaiseContentPropertyChanged();

                    AppDispatcher.Invoke(() =>
                    {
                        var content = CreateView(this.Source, CreateBindingParameter());
                        if (content != null)
                        {
                            this.View.SetContent(content);
                            ContentCanvas.Current.UpdateContentScalingMode(this);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    this.IsResizing = false;
                    ContentRebuild.Current.UpdateStatus();
                }
            });

            return true;
        }

        #endregion

        #region Static Methods

        public static BitmapViewContent Create(ViewPage source)
        {
            var viewContent = new BitmapViewContent(source);
            viewContent.Initialize();
            return viewContent;
        }

        #endregion
    }
}
