﻿namespace NeeView
{
    public class SetFullScreenCommand : CommandElement
    {
        public SetFullScreenCommand(string name) : base(name)
        {
            this.Group = Properties.Resources.CommandGroupWindow;
            this.Text = Properties.Resources.CommandSetFullScreen;
            this.Note = Properties.Resources.CommandSetFullScreenNote;
            this.IsShowMessage = false;
        }

        public override void Execute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            WindowShape.Current.SetFullScreen(true);
        }
    }
}
