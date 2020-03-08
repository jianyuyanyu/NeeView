﻿namespace NeeView
{
    public class ViewFlipHorizontalOnCommand : CommandElement
    {
        public ViewFlipHorizontalOnCommand(string name) : base(name)
        {
            this.Group = Properties.Resources.CommandGroupViewManipulation;
            this.Text = Properties.Resources.CommandViewFlipHorizontalOn;
            this.Note = Properties.Resources.CommandViewFlipHorizontalOnNote;
            this.IsShowMessage = false;
        }

        public override void Execute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            DragTransformControl.Current.FlipHorizontal(true);
        }
    }
}
