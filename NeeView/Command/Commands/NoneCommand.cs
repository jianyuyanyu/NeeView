﻿namespace NeeView
{
    public class NoneCommand : CommandElement
    {
        public NoneCommand() : base("")
        {
            this.Group = Properties.TextResources.GetString("CommandGroup.None");
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return false;
        }

        public override void Execute(object? sender, CommandContext e)
        {
            return;
        }
    }
}
