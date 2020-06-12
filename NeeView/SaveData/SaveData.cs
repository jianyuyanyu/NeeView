﻿using NeeView.Effects;
using NeeView.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


namespace NeeView
{
    public class SaveData
    {
        static SaveData() => Current = new SaveData();
        public static SaveData Current { get; }

        private string _settingFilenameToDelete;
        private string _historyFilenameToDelete;
        private string _bookmarkFilenameToDelete;
        private string _pagemarkFilenameToDelete;

        private SaveData()
        {
        }

        public const string UserSettingFileName = "UserSetting.json";
        public const string HistoryFileName = "History.json";
        public const string BookmarkFileName = "Bookmark.json";
        public const string PagemarkFileName = "Pagemark.json";

        public static string DefaultHistoryFilePath => Path.Combine(Environment.LocalApplicationDataPath, HistoryFileName);
        public static string DefaultBookmarkFilePath => Path.Combine(Environment.LocalApplicationDataPath, BookmarkFileName);
        public static string DefaultPagemarkFilePath => Path.Combine(Environment.LocalApplicationDataPath, PagemarkFileName);

        public string UserSettingFilePath => App.Current.Option.SettingFilename;
        public string HistoryFilePath => Config.Current.History.HistoryFilePath ?? DefaultHistoryFilePath;
        public string BookmarkFilePath => Config.Current.Bookmark.BookmarkFilePath ?? DefaultBookmarkFilePath;
        public string PagemarkFilePath => Config.Current.Pagemark.PagemarkFilePath ?? DefaultPagemarkFilePath;

        public bool IsEnableSave { get; set; } = true;


        // アプリ設定作成
        public UserSettingV1 CreateSetting()
        {
            var setting = new UserSettingV1();

            setting.App = App.Current.CreateMemento();

            setting.SusieMemento = SusiePluginManager.Current.CreateMemento();
            setting.CommandMememto = CommandTable.Current.CreateMemento();
            setting.DragActionMemento = DragActionTable.Current.CreateMemento();

            setting.Memento = new Models().CreateMemento();

            return setting;
        }

        #region Load


        /// <summary>
        /// 設定の読み込み
        /// </summary>
        public UserSetting LoadUserSetting(bool cancellable)
        {
            if (App.Current.IsMainWindowLoaded)
            {
                Setting.SettingWindow.Current?.Cancel();
                MainWindowModel.Current.CloseCommandParameterDialog();
            }

            UserSetting setting;

            try
            {
                App.Current.SemaphoreWait();

                var filename = App.Current.Option.SettingFilename;
                var extension = Path.GetExtension(filename).ToLower();
                var filenameV1 = Path.ChangeExtension(filename, ".xml");

                var failedDialog = new LoadFailedDialog(Resources.NotifyLoadSettingFailed, Resources.NotifyLoadSettingFailedTitle);
                if (cancellable)
                {
                    failedDialog.CancelCommand = new UICommand(Resources.NotifyLoadSettingFailedButtonQuit) { Alignment = UICommandAlignment.Left };
                }

                if (extension == ".json" && File.Exists(filename))
                {
                    setting = SafetyLoad(UserSettingTools.Load, filename, failedDialog, true);
                }
                // before v.37
                else if (File.Exists(filenameV1))
                {
                    var settingV1 = SafetyLoad(UserSettingV1.LoadV1, filenameV1, failedDialog, true);
                    var settingV1Converted = settingV1.ConvertToV2();

                    var historyV1FilePath = Path.ChangeExtension(settingV1.App.HistoryFilePath ?? DefaultHistoryFilePath, ".xml");
                    var historyV1 = SafetyLoad(BookHistoryCollection.Memento.LoadV1, historyV1FilePath, null); // 一部の履歴設定を反映
                    historyV1?.RestoreConfig(settingV1Converted.Config);

                    var pagemarkV1FilePath = Path.ChangeExtension(settingV1.App.PagemarkFilePath ?? DefaultPagemarkFilePath, ".xml");
                    var pagemarkV1 = SafetyLoad(PagemarkCollection.Memento.LoadV1, pagemarkV1FilePath, null); // 一部のページマーク設定を反映
                    pagemarkV1?.RestoreConfig(settingV1Converted.Config);

                    _settingFilenameToDelete = filenameV1;
                    if (Path.GetExtension(App.Current.Option.SettingFilename).ToLower() == ".xml")
                    {
                        App.Current.Option.SettingFilename = Path.ChangeExtension(App.Current.Option.SettingFilename, ".json");
                    }

                    setting = settingV1Converted;
                }
                else
                {
                    setting = new UserSetting();
                }
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

            return setting;
        }


        // 履歴読み込み
        public void LoadHistory()
        {
            try
            {
                App.Current.SemaphoreWait();

                var filename = HistoryFilePath;
                var extension = Path.GetExtension(filename).ToLower();
                var filenameV1 = Path.ChangeExtension(filename, ".xml");
                var failedDialog = new LoadFailedDialog(Resources.NotifyLoadHistoryFailed, Resources.NotifyLoadHistoryFailedTitle);

                if (extension == ".json" && File.Exists(filename))
                {
                    BookHistoryCollection.Memento memento = SafetyLoad(BookHistoryCollection.Memento.Load, HistoryFilePath, failedDialog);
                    BookHistoryCollection.Current.Restore(memento, true);
                }
                // before v.37
                else if (File.Exists(filenameV1))
                {
                    BookHistoryCollection.Memento memento = SafetyLoad(BookHistoryCollection.Memento.LoadV1, filenameV1, failedDialog);
                    BookHistoryCollection.Current.Restore(memento, true);

                    _historyFilenameToDelete = filenameV1;
                    if (Path.GetExtension(HistoryFilePath).ToLower() == ".xml")
                    {
                        Config.Current.History.HistoryFilePath = Path.ChangeExtension(HistoryFilePath, ".json");
                    }
                }
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }

        // ブックマーク読み込み
        public void LoadBookmark()
        {
            try
            {
                App.Current.SemaphoreWait();

                var filename = BookmarkFilePath;
                var extension = Path.GetExtension(filename).ToLower();
                var filenameV1 = Path.ChangeExtension(filename, ".xml");
                var failedDialog = new LoadFailedDialog(Resources.NotifyLoadBookmarkFailed, Resources.NotifyLoadBookmarkFailedTitle);

                if (extension == ".json" && File.Exists(filename))
                {
                    BookmarkCollection.Memento memento = SafetyLoad(BookmarkCollection.Memento.Load, filename, failedDialog);
                    BookmarkCollection.Current.Restore(memento);
                }
                // before v.37
                else if (File.Exists(filenameV1))
                {
                    BookmarkCollection.Memento memento = SafetyLoad(BookmarkCollection.Memento.LoadV1, filenameV1, failedDialog);
                    BookmarkCollection.Current.Restore(memento);

                    _bookmarkFilenameToDelete = filenameV1;
                    if (Path.GetExtension(BookmarkFilePath).ToLower() == ".xml")
                    {
                        Config.Current.Bookmark.BookmarkFilePath = Path.ChangeExtension(BookmarkFilePath, ".json");
                    }
                }
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

        }

        // ページマーク読み込み
        public void LoadPagemark()
        {
            try
            {
                App.Current.SemaphoreWait();

                var filename = PagemarkFilePath;
                var extension = Path.GetExtension(filename).ToLower();
                var filenameV1 = Path.ChangeExtension(filename, ".xml");
                var failedDialog = new LoadFailedDialog(Resources.NotifyLoadPagemarkFailed, Resources.NotifyLoadPagemarkFailedTitle);

                if (extension == ".json" && File.Exists(filename))
                {
                    PagemarkCollection.Memento memento = SafetyLoad(PagemarkCollection.Memento.Load, filename, failedDialog);
                    PagemarkCollection.Current.Restore(memento);
                }
                // before v.37
                else if (File.Exists(filenameV1))
                {
                    PagemarkCollection.Memento memento = SafetyLoad(PagemarkCollection.Memento.LoadV1, filenameV1, failedDialog);
                    PagemarkCollection.Current.Restore(memento);

                    if (Path.GetExtension(PagemarkFilePath).ToLower() == ".xml")
                    {
                        Config.Current.Pagemark.PagemarkFilePath = Path.ChangeExtension(PagemarkFilePath, ".json");
                    }

                    _pagemarkFilenameToDelete = filenameV1;
                }
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }


        /// <summary>
        /// 正規ファイルの読み込みに失敗したらバックアップからの復元を試みる。
        /// エラー時にはダイアログ表示。選択によってはOperationCancelExceptionを発生させる。
        /// </summary>
        /// <param name="useDefault">データが読み込めなかった場合に初期化されたインスタンスを返す。falseの場合はnullを返す</param>
        private T SafetyLoad<T>(Func<string, T> load, string path, LoadFailedDialog loadFailedDialog, bool useDefault = false)
            where T : class, new()
        {
            try
            {
                var instance = SafetyLoad(load, path);
                return (instance is null && useDefault) ? new T() : instance;
            }
            catch (Exception ex)
            {
                if (loadFailedDialog != null)
                {
                    var result = loadFailedDialog.ShowDialog(ex);
                    if (result != true)
                    {
                        throw new OperationCanceledException();
                    }
                }

                return useDefault ? new T() : null;
            }
        }


        /// <summary>
        /// 正規ファイルの読み込みに失敗したらバックアップからの復元を試みる
        /// </summary>
        private T SafetyLoad<T>(Func<string, T> load, string path)
            where T : class, new()
        {
            var old = path + ".bak";

            if (File.Exists(path))
            {
                try
                {
                    return load(path);
                }
                catch
                {
                    if (File.Exists(old))
                    {
                        return load(old);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (File.Exists(old))
            {
                return load(old);
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// 設定の保存
        /// </summary>
        public void SaveUserSetting()
        {
            if (!IsEnableSave) return;

            try
            {
                App.Current.SemaphoreWait();
                SafetySave(UserSettingTools.Save, App.Current.Option.SettingFilename, Config.Current.System.IsSettingBackup);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

            RemoveLegacyUserSetting();
        }

        /// <summary>
        /// 必要であるならば、古い設定ファイルを削除
        /// </summary>
        public void RemoveLegacyUserSetting()
        {
            if (_settingFilenameToDelete == null) return;

            RemoveLegacyFile(_settingFilenameToDelete);
            _settingFilenameToDelete = null;
        }

        /// <summary>
        /// 古いファイルを削除
        /// </summary>
        private void RemoveLegacyFile(string filename)
        {
            try
            {
                App.Current.SemaphoreWait();

                Debug.WriteLine($"Remove: {filename}");
                FileIO.RemoveFile(filename);

                // バックアップファイルも削除
                var backup = filename + ".old";
                if (File.Exists(backup))
                {
                    Debug.WriteLine($"Remove: {backup}");
                    FileIO.RemoveFile(backup);
                }
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }


        // UserSettingV1の保存
        [Obsolete]
        [Conditional("DEBUG")]
        public void SaveUserSettingV1()
        {
            if (!IsEnableSave) return;

            // 設定
            var setting = CreateSetting();

            // ウィンドウ状態保存
            setting.WindowShape = WindowShape.Current.CreateMemento();

            // ウィンドウ座標保存
            setting.WindowPlacement = WindowPlacement.Current.CreateMemento();

            // 設定をファイルに保存
            try
            {
                App.Current.SemaphoreWait();
                SafetySave(setting.SaveV1, Path.ChangeExtension(App.Current.Option.SettingFilename, ".xml"), Config.Current.System.IsSettingBackup);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }

        // 履歴をファイルに保存
        public void SaveHistory()
        {
            if (!IsEnableSave) return;

            // 現在の本を履歴に登録
            BookHub.Current.SaveBookMemento(); // TODO: タイミングに問題有り？

            try
            {
                App.Current.SemaphoreWait();
                if (Config.Current.History.IsSaveHistory)
                {
                    var bookHistoryMemento = BookHistoryCollection.Current.CreateMemento();

                    try
                    {
                        var fileInfo = new FileInfo(HistoryFilePath);
                        if (fileInfo.Exists && fileInfo.LastWriteTime > App.Current.StartTime)
                        {
                            var failedDialog = new LoadFailedDialog(Resources.NotifyLoadHistoryFailed, Resources.NotifyLoadHistoryFailedTitle);
                            var margeMemento = SafetyLoad(BookHistoryCollection.Memento.Load, HistoryFilePath, failedDialog);
                            bookHistoryMemento.Merge(margeMemento);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                    SafetySave(bookHistoryMemento.Save, HistoryFilePath, false);
                }
                else
                {
                    FileIO.RemoveFile(HistoryFilePath);
                }
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

            RemoveLegacyHistory();
        }

        /// <summary>
        /// 必要であるならば、古い設定ファイルを削除
        /// </summary>
        public void RemoveLegacyHistory()
        {
            if (_historyFilenameToDelete == null) return;

            RemoveLegacyFile(_historyFilenameToDelete);
            _historyFilenameToDelete = null;
        }

        /// <summary>
        /// Bookmarkの保存
        /// </summary>
        public void SaveBookmark()
        {
            if (!IsEnableSave) return;
            if (!Config.Current.Bookmark.IsSaveBookmark) return;

            try
            {
                App.Current.SemaphoreWait();
                var bookmarkMemento = BookmarkCollection.Current.CreateMemento();
                SafetySave(bookmarkMemento.Save, BookmarkFilePath, false);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

            RemoveLegacyBookmark();
        }

        /// <summary>
        /// 必要であるならば、古い設定ファイルを削除
        /// </summary>
        public void RemoveLegacyBookmark()
        {
            if (_bookmarkFilenameToDelete == null) return;

            RemoveLegacyFile(_bookmarkFilenameToDelete);
            _bookmarkFilenameToDelete = null;
        }

        /// <summary>
        /// 必要であるならば、Bookmarkを削除
        /// </summary>
        public void RemoveBookmarkIfNotSave()
        {
            if (!IsEnableSave) return;
            if (Config.Current.Bookmark.IsSaveBookmark) return;

            try
            {
                App.Current.SemaphoreWait();
                FileIO.RemoveFile(BookmarkFilePath);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }

        /// <summary>
        /// Pagemarkの保存
        /// </summary>
        public void SavePagemark()
        {
            if (!IsEnableSave) return;
            if (!Config.Current.Pagemark.IsSavePagemark) return;

            try
            {
                App.Current.SemaphoreWait();
                var pagemarkMemento = PagemarkCollection.Current.CreateMemento();
                SafetySave(pagemarkMemento.Save, PagemarkFilePath, false);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }

            RemoveLegacyPagemark();
        }


        /// <summary>
        /// 必要であるならば、古い設定ファイルを削除
        /// </summary>
        public void RemoveLegacyPagemark()
        {
            if (_pagemarkFilenameToDelete == null) return;

            RemoveLegacyFile(_pagemarkFilenameToDelete);
            _pagemarkFilenameToDelete = null;
        }

        /// <summary>
        /// 必要であるならば、Pagemarkを削除
        /// </summary>
        public void RemovePagemarkIfNotSave()
        {
            if (!IsEnableSave) return;
            if (Config.Current.Pagemark.IsSavePagemark) return;

            try
            {
                App.Current.SemaphoreWait();
                FileIO.RemoveFile(PagemarkFilePath);
            }
            catch
            {
            }
            finally
            {
                App.Current.SemaphoreRelease();
            }
        }

        /// <summary>
        /// アプリ強制終了でもファイルがなるべく破壊されないような保存
        /// </summary>
        private void SafetySave(Action<string> save, string path, bool isBackup)
        {
            try
            {
                var oldPath = path + ".bak";
                var tmpPath = path + ".tmp";

                FileIO.RemoveFile(tmpPath);
                save(tmpPath);

                lock (App.Current.Lock)
                {
                    var newFile = new FileInfo(tmpPath);
                    var oldFile = new FileInfo(path);

                    if (oldFile.Exists)
                    {
                        FileIO.RemoveFile(oldPath);
                        oldFile.MoveTo(oldPath);
                    }

                    newFile.MoveTo(path);

                    if (!isBackup)
                    {
                        FileIO.RemoveFile(oldPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion
    }


    /// <summary>
    /// データロードエラーダイアログ
    /// </summary>
    public class LoadFailedDialog
    {
        public LoadFailedDialog(string title, string message)
        {
            Title = title;
            Message = message;
        }

        public string Title { get; set; }
        public string Message { get; set; }
        public UICommand OKCommand { get; set; } = UICommands.OK;
        public UICommand CancelCommand { get; set; }


        public bool ShowDialog(Exception ex)
        {
            var textBox = new System.Windows.Controls.TextBox()
            {
                IsReadOnly = true,
                Text = Message + System.Environment.NewLine + ex.Message,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            };

            var dialog = new MessageDialog(textBox, Title);
            dialog.SizeToContent = System.Windows.SizeToContent.Manual;
            dialog.Height = 320.0;
            dialog.ResizeMode = System.Windows.ResizeMode.CanResize;
            dialog.Commands.Add(OKCommand);
            if (CancelCommand != null)
            {
                dialog.Commands.Add(CancelCommand);
            }

            var result = dialog.ShowDialog();
            return result == OKCommand || CancelCommand == null;
        }
    }
}
