﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace NeeView
{
    public class MouseExGestureConverter : TypeConverter
    {
        private const char _modifiersDelimiter = '+';

        private static MouseExActionConverter _mouseActionConverter = new MouseExActionConverter();
        private static ModifierKeysConverter _modifierKeysConverter = new ModifierKeysConverter();
        private static ModifierMouseButtonsConverter _modifierMouseButtonsConverter = new ModifierMouseButtonsConverter();


        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object source)
        {
            if (source is string && source != null)
            {
                string fullName = ((string)source).Trim();
                string mouseActionToken;
                string modifierMouseButtonsToken;
                string modifiersToken;

                if (fullName == string.Empty)
                {
                    return new MouseExGesture(MouseExAction.None, ModifierKeys.None, ModifierMouseButtons.None);
                }

                int offset = fullName.LastIndexOf(_modifiersDelimiter);
                if (offset >= 0)
                {
                    string modifiers = fullName.Substring(0, offset);
                    mouseActionToken = fullName.Substring(offset + 1);

                    offset = modifiers.IndexOf("BUTTON", StringComparison.OrdinalIgnoreCase);
                    if (offset >= 0)
                    {
                        offset = modifiers.LastIndexOf(_modifiersDelimiter, offset);
                        if (offset >= 0)
                        {
                            modifiersToken = modifiers.Substring(0, offset);
                            modifierMouseButtonsToken = modifiers.Substring(offset + 1);
                        }
                        else
                        {
                            modifiersToken = string.Empty;
                            modifierMouseButtonsToken = modifiers;
                        }
                    }
                    else
                    {
                        modifiersToken = modifiers;
                        modifierMouseButtonsToken = string.Empty;
                    }

                }
                else
                {
                    modifiersToken = string.Empty;
                    modifierMouseButtonsToken = string.Empty;
                    mouseActionToken = fullName;
                }

                object mouseAction = _mouseActionConverter.ConvertFrom(context, culture, mouseActionToken);
                object modifierKeys = ModifierKeys.None;
                object modifierMouseButtons = ModifierMouseButtons.None;

                if (mouseAction != null)
                {
                    if (modifiersToken != string.Empty)
                    {
                        modifierKeys = _modifierKeysConverter.ConvertFrom(context, culture, modifiersToken);

                        if (!(modifierKeys is ModifierKeys))
                        {
                            modifierKeys = ModifierKeys.None;
                        }
                    }

                    if (modifierMouseButtonsToken != string.Empty)
                    {
                        modifierMouseButtons = _modifierMouseButtonsConverter.ConvertFrom(context, culture, modifierMouseButtonsToken);

                        if (!(modifierMouseButtons is ModifierMouseButtons))
                        {
                            modifierMouseButtons = ModifierMouseButtons.None;
                        }
                    }

                    return new MouseExGesture((MouseExAction)mouseAction, (ModifierKeys)modifierKeys, (ModifierMouseButtons)modifierMouseButtons);
                }
            }
            throw GetConvertFromException(source);
        }


        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (context?.Instance is MouseExGesture mouseGesture)
                {
                    return (ModifierKeysConverter.IsDefinedModifierKeys(mouseGesture.ModifierKeys)
                           && ModifierMouseButtonsConverter.IsDefinedModifierMouseButtons(mouseGesture.ModifierMouseButtons)
                           && MouseExActionConverter.IsDefinedMouseAction(mouseGesture.MouseExAction));
                }
            }
            return false;
        }


        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return string.Empty;
                }

                MouseExGesture mouseGesture = value as MouseExGesture;
                if (mouseGesture != null)
                {
                    string strGesture = "";

                    strGesture += _modifierKeysConverter.ConvertTo(context, culture, mouseGesture.ModifierKeys, destinationType) as string;
                    if (strGesture != string.Empty)
                    {
                        strGesture += _modifiersDelimiter;
                    }

                    var buttons = _modifierMouseButtonsConverter.ConvertTo(context, culture, mouseGesture.ModifierMouseButtons, destinationType) as string;
                    if (buttons != string.Empty)
                    {
                        strGesture += buttons;
                        strGesture += _modifiersDelimiter;
                    }

                    strGesture += _mouseActionConverter.ConvertTo(context, culture, mouseGesture.MouseExAction, destinationType) as string;

                    return strGesture;
                }
            }
            throw GetConvertToException(value, destinationType);
        }
    }
}
