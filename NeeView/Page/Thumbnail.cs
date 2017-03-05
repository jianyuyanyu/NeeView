﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NeeView
{
    /// <summary>
    /// サムネイル.
    /// Jpegで保持し、必要に応じてBitmapSourceを生成
    /// </summary>
    public class Thumbnail : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// PropertyChanged event. 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 画像サイズ
        /// </summary>
        public static double Size { get; set; } = 256;

        /// <summary>
        /// 品質
        /// </summary>
        public static int Quality => Preference.Current.thumbnail_quality;

        /// <summary>
        /// 有効判定
        /// </summary>
        internal bool IsValid => (_image != null);

        /// <summary>
        /// 変更イベント
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// 参照イベント
        /// </summary>
        public event EventHandler Touched;

        /// <summary>
        /// Jpeg化された画像
        /// </summary>
        private byte[] _image;
        public byte[] Image
        {
            get { return _image; }
            set
            {
                if (_image != value)
                {
                    _image = value;
                    if (Image != null)
                    {
                        Changed?.Invoke(this, null);
                        Touched?.Invoke(this, null);
                        RaisePropertyChanged(nameof(BitmapSource));
                    }
                }
            }
        }

        /// <summary>
        /// View用Bitmapプロパティ
        /// </summary>
        public BitmapSource BitmapSource => CreateBitmap();

        /// <summary>
        /// 寿命間利用シリアルナンバー
        /// </summary>
        public int LifeSerial { get; set; }

        /// <summary>
        /// キャッシュ使用
        /// </summary>
        public bool IsSupprtedCache { get; set; }

        /// <summary>
        /// キャシュ用ヘッダ
        /// </summary>
        public ThumbnailCacheHeader _header { get; set; }

        /// <summary>
        /// キャッシュを使用してサムネイル生成を試みる
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dateTime"></param>
        /// <param name="length"></param>
        internal void Initialize(string name, long length, DateTime? dateTime)
        {
            if (IsValid || !IsSupprtedCache) return;

            var sw = new Stopwatch();
            sw.Start();
            _header = new ThumbnailCacheHeader(name, length, dateTime);
            var image = ThumbnailCache.Current.Load(_header);
            sw.Stop();
            Debug.WriteLine($"Cache Load: {IsValid}: {sw.ElapsedMilliseconds}ms");

            Image = image;
        }


        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="source"></param>
        internal void Initialize(BitmapSource source)
        {
            if (IsValid) return;

            //var sw = new Stopwatch();
            //sw.Start();
            var bitmapSource = Utility.NVGraphics.CreateThumbnail(source, new Size(Size, Size));
            var image = EncodeToJpeg(bitmapSource);
            //sw.Stop();
            //Debug.WriteLine($"Jpeg: {_image.Length / 1024}KB, {sw.ElapsedMilliseconds}ms");

            Image = image;

            if (IsSupprtedCache && _header != null)
            {
                var sw = new Stopwatch();
                sw.Start();
                ThumbnailCache.Current.Save(_header, _image);
                sw.Stop();
                Debug.WriteLine($"Cache Save: {sw.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// image無効
        /// </summary>
        public void Clear()
        {
            _image = null;
        }

        /// <summary>
        /// Touch
        /// </summary>
        public void Touch()
        {
            Touched?.Invoke(this, null);
        }

        /// <summary>
        /// BitmapSource取得
        /// </summary>
        /// <returns></returns>
        public BitmapSource CreateBitmap()
        {
            if (IsValid)
            {
                Touched?.Invoke(this, null);
                return DecodeFromJpeg(_image);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// BitmapSource to Jpeg
        /// </summary>
        private byte[] EncodeToJpeg(BitmapSource source)
        {
            using (var stream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = Quality;
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);

                stream.Flush();
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Jpeg to BitmapSource
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        private BitmapSource DecodeFromJpeg(byte[] image)
        {
            using (var stream = new MemoryStream(image, false))
            {
                JpegBitmapDecoder decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var bitmap = decoder.Frames[0];
                bitmap.Freeze();
                return bitmap;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _image = null;
            Changed = null;
            Touched = null;
            PropertyChanged = null;
        }
    }
}
