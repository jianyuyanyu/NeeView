﻿using System.Windows.Data;


namespace NeeView
{
    public class ToggleVisibleFileInfoCommand : CommandElement
    {
        public ToggleVisibleFileInfoCommand() : base(CommandType.ToggleVisibleFileInfo)
        {
            this.Group = Properties.Resources.CommandGroupPanel;
            this.Text = Properties.Resources.CommandToggleVisibleFileInfo;
            this.MenuText = Properties.Resources.CommandToggleVisibleFileInfoMenu;
            this.Note = Properties.Resources.CommandToggleVisibleFileInfoNote;
            this.ShortCutKey = "I";
            this.IsShowMessage = false;
        }

        public override Binding CreateIsCheckedBinding()
        {
            return new Binding(nameof(SidePanel.IsVisibleFileInfo)) { Source = SidePanel.Current };
        }

        public override string ExecuteMessage(CommandParameter param, CommandOption option = CommandOption.None)
        {
            return SidePanel.Current.IsVisibleFileInfo ? Properties.Resources.CommandToggleVisibleFileInfoOff : Properties.Resources.CommandToggleVisibleFileInfoOn;
        }

        public override void Execute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            SidePanel.Current.ToggleVisibleFileInfo(option.HasFlag(CommandOption.ByMenu));
        }
    }
}
