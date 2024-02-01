﻿using NeeView.Windows.Property;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace NeeView
{
    public class CopyImageCommand : CommandElement
    {
        public CopyImageCommand()
        {
            this.Group = Properties.TextResources.GetString("CommandGroup.File");
            this.ShortCutKey = "Ctrl+Shift+C";
            this.IsShowMessage = true;
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return MainViewComponent.Current.ViewCopyImage.CanCopyImageToClipboard();
        }

        public override void Execute(object? sender, CommandContext e)
        {
            MainViewComponent.Current.ViewCopyImage.CopyImageToClipboard();
        }
    }

}
