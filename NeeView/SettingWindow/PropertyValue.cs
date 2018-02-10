﻿using System.Windows;
using System.Windows.Data;

namespace NeeView
{
    /// <summary>
    /// 実値とBinding値をまとめて管理し、コントロールのプロパティに適用する。
    /// Binding値がないときには実値を適用する。
    /// </summary>
    /// <typeparam name="T">値の型</typeparam>
    public class PropertyValue<T>
    {
        private DependencyProperty _dependencyProperty;
        private T _value;
        private BindingBase _binding;

        public PropertyValue(DependencyProperty dp, T value)
        {
            _value = value;
            _dependencyProperty = dp;
        }

        public PropertyValue(DependencyProperty dp, BindingBase binding)
        {
            _binding = binding;
            _dependencyProperty = dp;
        }

        public void SetBind(UIElement element)
        {
            if (_binding != null)
            {
                BindingOperations.SetBinding(element, _dependencyProperty, _binding);
            }
            else
            {
                element.SetValue(_dependencyProperty, _value);
            }
        }
    }


    /// <summary>
    /// IsEnabledProperty の PropertyValue
    /// </summary>
    public class IsEnabledPropertyValue : PropertyValue<bool>
    {
        public IsEnabledPropertyValue(bool value)
            : base(UIElement.IsEnabledProperty, value)
        {
        }

        public IsEnabledPropertyValue(BindingBase binding)
            : base(UIElement.IsEnabledProperty, binding)
        {
        }

        public IsEnabledPropertyValue(object source, string path)
            : base(UIElement.IsEnabledProperty, new Binding(path) { Source = source })
        {
        }
    }

    /// <summary>
    /// VisibilityProperty の PropertyValue
    /// </summary>
    public class VisibilityPropertyValue : PropertyValue<Visibility>
    {
        public VisibilityPropertyValue(Visibility value)
            : base(UIElement.VisibilityProperty, value)
        {
        }

        public VisibilityPropertyValue(BindingBase binding)
            : base(UIElement.VisibilityProperty, binding)
        {
        }

        public VisibilityPropertyValue(object source, string path)
            : base(UIElement.VisibilityProperty, new Binding(path) { Source = source })
        {
        }
    }

}
