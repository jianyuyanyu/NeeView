﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace NeeView
{
    /// <summary>
    /// アプリ全体の設定
    /// </summary>
    public class Environment
    {
        // DPI倍率
        public Point DpiScaleFactor { get; private set; } = new Point(1, 1);

        // DPIのXY比率が等しい？
        public bool IsDpiSquare { get; private set; } = false;

        // DPI設定
        public void UpdateDpiScaleFactor(System.Windows.Media.Visual visual)
        {
            var dpiScaleFactor = DragExtensions.WPFUtil.GetDpiScaleFactor(visual);
            DpiScaleFactor = dpiScaleFactor;
            IsDpiSquare = DpiScaleFactor.X == DpiScaleFactor.Y;
        }
    }


    /// <summary>
    /// Model 共通コンテキスト (static)
    /// </summary>
    public static class ModelContext
    {
        public static Environment Environment { get; set; }

        public static JobEngine JobEngine { get; set; }

        public static SusieContext SusieContext { get; set; }
        public static Susie.Susie Susie => SusieContext.Susie;

        public static BookMementoCollection BookMementoCollection { get; set; }
        public static BookHistory BookHistory { get; set; }
        public static BookmarkCollection Bookmarks { get; set; }

        public static ArchiverManager ArchiverManager { get; set; }
        public static BitmapLoaderManager BitmapLoaderManager { get; set; }

        public static CommandTable CommandTable { get; set; }
        public static DragActionTable DragActionTable { get; set; }

        // RoutedCommand辞書
        public static Dictionary<CommandType, RoutedUICommand> BookCommands { get; set; } = new Dictionary<CommandType, RoutedUICommand>();

        public static Recycle Recycle { get; set; }




        //
        public static bool IsAutoGC { get; set; } = true;

        //
        public static void GarbageCollection()
        {
            if (!IsAutoGC) GC.Collect();
        }

        // 初期化
        public static void Initialize()
        {
            Environment = new Environment();

            // Jobワーカーサイズ
            JobEngine = new JobEngine();
            int jobWorkerSize;
            if (!int.TryParse(ConfigurationManager.AppSettings.Get("ThreadSize"), out jobWorkerSize))
            {
                jobWorkerSize = 2; // 標準サイズ
            }
            JobEngine.Start(jobWorkerSize);

            // ワイドページ判定用比率
            double wideRatio;
            if (!double.TryParse(ConfigurationManager.AppSettings.Get("WideRatio"), out wideRatio))
            {
                wideRatio = 1.0;
            }
            Page.WideRatio = wideRatio;

            //
            BookMementoCollection = new BookMementoCollection();
            BookHistory = new BookHistory();
            Bookmarks = new BookmarkCollection();

            ArchiverManager = new ArchiverManager();
            BitmapLoaderManager = new BitmapLoaderManager();

            CommandTable = new CommandTable();
            DragActionTable = new DragActionTable();

            SusieContext = new SusieContext();

            Recycle = new Recycle();


            // SevenZip対応拡張子設定
            ArchiverManager.UpdateSevenZipSupprtedFileTypes(ConfigurationManager.AppSettings.Get("SevenZipSupportFileType"));
        }


        // 終了処理
        public static void Terminate()
        {
            JobEngine.Dispose();
        }
    }


}

