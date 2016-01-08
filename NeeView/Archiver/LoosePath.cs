﻿// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeeView
{
    /// <summary>
    /// ファイルシステム規約に依存しないパス文字列ユーティリティ
    /// ファイル名に使用できない文字を含んだパスの解析用
    /// </summary>
    public static class LoosePath
    {
        //
        public static string GetFileName(string s)
        {
            return s.Split('\\', '/').Last();
        }

        //
        public static string GetPathRoot(string s)
        {
            var parts = s.Split('\\', '/');
            return parts.First();
        }

        //
        public static string GetDirectoryName(string s)
        {
            var parts = s.Split('\\', '/').ToList();
            parts.RemoveAt(parts.Count - 1);
            return string.Join("\\", parts);
        }

        //
        public static string GetExtension(string s)
        {
            return "." + s.Split('.').Last().ToLower();
        }

        //
        public static string Combine(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
                return s2;
            else
                return s1.TrimEnd('\\', '/') + "\\" + s2.TrimStart('\\', '/');
        }
    }
}
