﻿using NeeView.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView
{
    public class ScriptUnit
    {
        private readonly ScriptUnitPool _pool;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public ScriptUnit(ScriptUnitPool pool)
        {
            if (pool is null) throw new ArgumentNullException(nameof(pool));

            _pool = pool;
        }

        public void Execute(object? sender, string path, string? argument)
        {
            Task.Run(() => ExecuteInner(sender, path, argument));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:未使用のパラメーターを削除します", Justification = "<保留中>")]
        private void ExecuteInner(object? sender, string path, string? argument)
        {
            var engine = new JavascriptEngine() { IsToastEnable = true };

            JavascriptEngineMap.Current.Add(engine);
            try
            {
                ////engine.Log($"Script: {LoosePath.GetFileName(path)} ...");
                engine.SetArgs(StringTools.SplitArgument(argument));
                engine.ExecuteFile(path, _cancellationTokenSource.Token);
                ////engine.Log($"Script: {LoosePath.GetFileName(path)} done.");
            }
            catch (Exception ex)
            {
                engine.ExceptionProcess(ex);
                ////engine.Log($"Script: {LoosePath.GetFileName(path)} stopped.");
            }
            finally
            {
                JavascriptEngineMap.Current.Remove(engine);
                AppDispatcher.BeginInvoke(() => CommandTable.Current.FlushInputGesture());
                _pool.Remove(this);
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
