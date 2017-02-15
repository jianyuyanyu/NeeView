﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeeView
{
    /// <summary>
    /// 標準画像ローダー
    /// </summary>
    public class DefaultBitmapLoader : IBitmapLoader
    {
        #region 開発用
        [Conditional("DEBUG")]
        private void DumpMetaData(string prefix, BitmapMetadata metadata)
        {
            ImageMetadata im = metadata;

            foreach (var name in metadata)
            {
                string query;

                try
                {
                    query = prefix + "(" + metadata.Format + ")" + name;
                }
                catch
                {
                    query = prefix + name;
                }

                if (metadata.ContainsQuery(name))
                {
                    var element = metadata.GetQuery(name);
                    if (element is BitmapMetadata)
                    {
                        DumpMetaData(query, (BitmapMetadata)element);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"{query}: {element?.ToString()}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"{prefix}: {name}");
                }
            }
        }
        #endregion

        // 有効判定
        public bool IsEnabled => true;

        // Thumbnail読み込み
        public BitmapContent LoadThmbnail(Stream stream, ArchiveEntry entry, bool allowExifOrientation, int size)
        {
            var resource = new BitmapContent();

            BitmapSource source = null;
            BitmapMetadata metadata = null;
            FileBasicInfo info = new FileBasicInfo();

            bool isLargeWidth = false;

            try
            {
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
                source = frame.Thumbnail;

                if (frame.PixelWidth <= size && frame.PixelHeight <= size)
                {
                    return null;
                }

                isLargeWidth = frame.PixelWidth > frame.PixelHeight;

                if (source != null)
                {
                    source.Freeze();
                    metadata = frame.Metadata as BitmapMetadata;
                    info.Decoder = frame.Decoder.CodecInfo.FriendlyName;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            // BitmapFrameが失敗する場合はBitmapImageでデコード
            if (source == null)
            {
                stream.Seek(0, SeekOrigin.Begin);
                BitmapImage bmpImage = new BitmapImage();

                bmpImage.BeginInit();
                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                bmpImage.StreamSource = stream;

                if (isLargeWidth)
                    bmpImage.DecodePixelWidth = size;
                else
                    bmpImage.DecodePixelHeight = size;

                bmpImage.EndInit();
                bmpImage.Freeze();

                source = bmpImage;
                metadata = null;
                info.Decoder = ".Net BitmapImage";
            }

            info.FileSize = entry.FileSize;
            info.LastWriteTime = entry.LastWriteTime;
            info.Metadata = metadata;

            resource.Source = (allowExifOrientation && metadata != null) ? OrientationWithExif(source, new ExifAccessor(metadata)) : source;
            resource.Info = info;

            return resource;
        }

        // Bitmap読み込み
        public BitmapContent Load(Stream stream, ArchiveEntry entry, bool allowExifOrientation)
        {
            var resource = new BitmapContent();

            BitmapSource source = null;
            BitmapMetadata metadata = null;
            FileBasicInfo info = new FileBasicInfo();

            // まずは BitmapFrame でデコード
            var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            bitmapFrame.Freeze();

            if (bitmapFrame == null) return null;
            source = bitmapFrame;
            metadata = bitmapFrame.Metadata as BitmapMetadata;
            info.Decoder = bitmapFrame.Decoder.CodecInfo.FriendlyName;
            info.FileSize = entry.FileSize;
            info.LastWriteTime = entry.LastWriteTime;
            info.Metadata = metadata;

            resource.Source = (allowExifOrientation && metadata != null) ? OrientationWithExif(source, new ExifAccessor(metadata)) : source;
            resource.Info = info;

            return resource;
        }


        // Bitmap読み込み
        public BitmapContent LoadFromFile(string fileName, ArchiveEntry entry, bool allowExifOrientation)
        {
            BitmapContent resource;
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                resource = Load(stream, entry, allowExifOrientation);
                if (resource == null) return null;
            }

            return resource;
        }


        // TODO: 回転情報反映の高速化
        private BitmapSource OrientationWithExif(BitmapSource source, ExifAccessor exif)
        {
            BitmapSource bmp;

            switch (exif.Orientation)
            {
                default:
                case 1: //Horizontal(normal)
                    bmp = source;
                    break;
                case 2: // Mirror horizontal
                    bmp = new TransformedBitmap(source, new ScaleTransform(-1, 1));
                    break;
                case 3: // Rotate 180
                    bmp = new TransformedBitmap(source, new RotateTransform(180));
                    break;
                case 4: //Mirror vertical
                    bmp = new TransformedBitmap(source, new ScaleTransform(1, -1));
                    break;
                case 5: // Mirror horizontal and rotate 270 CW
                    var group5 = new TransformGroup();
                    group5.Children.Add(new ScaleTransform(-1, 1));
                    group5.Children.Add(new RotateTransform(270));
                    bmp = new TransformedBitmap(source, group5);
                    break;
                case 6: //Rotate 90 CW
                    bmp = new TransformedBitmap(source, new RotateTransform(90));
                    break;
                case 7: // Mirror horizontal and rotate 90 CW
                    var group7 = new TransformGroup();
                    group7.Children.Add(new ScaleTransform(-1, 1));
                    group7.Children.Add(new RotateTransform(90));
                    bmp = new TransformedBitmap(source, group7);
                    break;
                case 8: // Rotate 270 CW
                    bmp = new TransformedBitmap(source, new RotateTransform(270));
                    break;
            }

            bmp.Freeze();
            return bmp;
        }


        // 対応拡張子取得
        public static Dictionary<string, string> GetExtensions()
        {
            var dictionary = new Dictionary<string, string>();

            // 標準
            dictionary.Add("BMP Decoder", ".bmp,.dib,.rle");
            dictionary.Add("GIF Decoder", ".gif");
            dictionary.Add("ICO Decoder", ".ico,.icon");
            dictionary.Add("JPEG Decoder", ".jpeg,.jpe,.jpg,.jfif,.exif");
            dictionary.Add("PNG Decoder", ".png");
            dictionary.Add("TIFF Decoder", ".tiff,.tif");
            dictionary.Add("WMPhoto Decoder", ".wdp,.jxr");
            dictionary.Add("DDS Decoder", ".dds"); // (微妙..)

            // WIC
            try
            {
                var wics = Utility.WicDecoders.ListUp();
                dictionary = dictionary.Concat(wics).ToDictionary(x => x.Key, x => x.Value);
            }
            catch { } // 失敗しても動くように例外スルー

            return dictionary;
        }
    }
}
