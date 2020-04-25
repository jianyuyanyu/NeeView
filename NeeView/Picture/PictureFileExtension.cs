﻿using System.Collections.Generic;
using System.Linq;

namespace NeeView
{
    /// <summary>
    /// 画像ファイル拡張子
    /// </summary>
    public class PictureFileExtension
    {
        #region Fields

        private FileTypeCollection _defaultExtensoins = new FileTypeCollection();

        #endregion

        #region Constructors

        public PictureFileExtension()
        {
            UpdateDefaultSupprtedFileTypes();
        }

        #endregion

        public FileTypeCollection DefaultExtensions => _defaultExtensoins;

        public FileTypeCollection SusieExtensions => SusiePluginManager.Current.ImageExtensions;

        #region Methods

        // デフォルトローダーのサポート拡張子を更新
        private void UpdateDefaultSupprtedFileTypes()
        {
            var list = new List<string>();

            foreach (var pair in GetDefaultExtensions())
            {
                list.AddRange(pair.Value.Split(','));
            }

            _defaultExtensoins.Restore(list);
        }

        // 標準対応拡張子取得
        private Dictionary<string, string> GetDefaultExtensions()
        {
            Dictionary<string, string> dictionary;

            try
            {
                dictionary = WicDecoders.ListUp();
            }
            catch
            {
                // 失敗した場合は標準設定にする
                dictionary = new Dictionary<string, string>();
                dictionary.Add("BMP Decoder", ".bmp,.dib,.rle");
                dictionary.Add("GIF Decoder", ".gif");
                dictionary.Add("ICO Decoder", ".ico,.icon");
                dictionary.Add("JPEG Decoder", ".jpeg,.jpe,.jpg,.jfif,.exif");
                dictionary.Add("PNG Decoder", ".png");
                dictionary.Add("TIFF Decoder", ".tiff,.tif");
                dictionary.Add("WMPhoto Decoder", ".wdp,.jxr");
                dictionary.Add("DDS Decoder", ".dds");
            }

            return dictionary;
        }

        #endregion
    }
}
