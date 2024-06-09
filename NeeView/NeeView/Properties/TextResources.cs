﻿using NeeLaboratory.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeView.Properties
{
    /// <summary>
    /// NeeView.Properties.Resources に代わるテキストリソース
    /// </summary>
    internal class TextResources
    {
        private static bool _initialized;


        public static CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

        public static FileLanguageResource LanguageResource { get; } = new();

        public static TextResourceManager Resource { get; } = new(LanguageResource);


        public static void Initialize(CultureInfo culture)
        {
            if (_initialized) throw new InvalidOperationException("Already initialized.");
            _initialized = true;

            Culture = culture;
            Resource.Load(culture);
            Resource.Add(new AppFileSource(new Uri("/NeeView/Properties/Append.restext", UriKind.Relative))); 
        }

        public static string GetString(string name)
        {
            return Resource.GetString(name) ?? "@" + name;
        }

        public static string? GetStringRaw(string name)
        {
            return Resource.GetString(name);
        }

        public static string? GetStringRaw(string name, CultureInfo culture)
        {
            return Resource.GetString(name, culture);
        }

        public static string GetCaseString(string name, string pattern)
        {
            return Resource.GetCaseString(name, pattern) ?? "@" + name;
        }

        public static string GetFormatString(string name, object? arg0)
        {
            var pattern = arg0?.ToString() ?? "";
            return string.Format(GetCaseString(name, pattern), arg0);
        }
    }
}
