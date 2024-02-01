﻿namespace NeeView
{
    public class DeleteFileCommand : CommandElement
    {
        public DeleteFileCommand()
        {
            this.Group = Properties.TextResources.GetString("CommandGroup.File");
            this.ShortCutKey = "Delete";
            this.IsShowMessage = false;
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return BookOperation.Current.Control.CanDeleteFile();
        }

        public override void Execute(object? sender, CommandContext e)
        {
            _ = BookOperation.Current.Control.DeleteFileAsync();
        }
    }
}
