﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeeView
{
    /// <summary>
    /// 画像ページ
    /// </summary>
    public class BitmapPage : Page
    {
        // ToString
        public override string ToString()
        {
            return LoosePath.GetFileName(FileName);
        }

        // コンストラクタ
        public BitmapPage(Archiver archiver, ArchiveEntry entry, string place)
        {
            Place = place;
            Entry = entry;
        }


        // Bitmapロード
        private BitmapContent LoadBitmap()
        {
            foreach (var loaderType in ModelContext.BitmapLoaderManager.OrderList)
            {
                try
                {
                    var bitmapLoader = BitmapLoaderManager.Create(loaderType);
                    BitmapContent bmp;
                    if (Entry.IsFileSystem)
                    {
                        bmp = bitmapLoader.LoadFromFile(Entry.GetFileSystemPath(), Entry, IsEnableExif);
                    }
                    else
                    {
                        using (var stream = Entry.OpenEntry())
                        {
                            bmp = bitmapLoader.Load(stream, Entry, IsEnableExif);
                        }
                    }

                    if (bmp != null)
                    {
                        if (bmp.Info != null) bmp.Info.Archiver = Entry.Archiver.ToString();
                        return bmp;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{e.Message}\nat '{FileName}' by {loaderType}");
                }
            }

            throw new ApplicationException("画像の読み込みに失敗しました");
        }


        // 開発用遅延
        [Conditional("DEBUG")]
        private void __Delay(int ms)
        {
            Thread.Sleep(ms);
        }


        // コンテンツをロードする
        protected override object LoadContent()
        {
            //__Delay(200);

            try
            {
                var bitmapContent = LoadBitmap();
                if (bitmapContent == null) throw new ApplicationException("cannot load by BitmapImge.");
                var bitmapSource = bitmapContent.Source;
                Width = bitmapSource.PixelWidth;
                Height = bitmapSource.PixelHeight;
                Color = bitmapSource.GetOneColor();

                // GIFアニメ用にファイル展開
                if (IsEnableAnimatedGif && LoosePath.GetExtension(FileName) == ".gif")
                {
                    return new AnimatedGifContent()
                    {
                        Uri = new Uri(CreateTempFile()),
                        BitmapContent = bitmapContent
                    };
                }
                else
                {
                    return bitmapContent;
                }
            }
            catch (Exception e)
            {
                Message = "Exception: " + e.Message;
                Width = 320;
                Height = 320 * 1.25;
                Color = Colors.Black;

                return new FilePageContent()
                {
                    Icon = FilePageIcon.Alart,
                    FileName = FileName,
                    Message = e.Message,

                    Info = new FileBasicInfo()
                };
            }
        }
    
        // ファイルの出力
        public override void Export(string path)
        {
            Entry.ExtractToFile(path, true);
        }
    }

    // アニメーションGIF用リソース
    public class AnimatedGifContent
    {
        public Uri Uri { get; set; }
        public BitmapContent BitmapContent { get; set; }
    }

}
