﻿using System.Windows.Data;


namespace NeeView
{
    public class ToggleVisiblePageListCommand : CommandElement
    {
        public ToggleVisiblePageListCommand() : base(CommandType.ToggleVisiblePageList)
        {
            this.Group = Properties.Resources.CommandGroupPanel;
            this.Text = Properties.Resources.CommandToggleVisiblePageList;
            this.MenuText = Properties.Resources.CommandToggleVisiblePageListMenu;
            this.Note = Properties.Resources.CommandToggleVisiblePageListNote;
            this.ShortCutKey = "P";
            this.IsShowMessage = false;
        }

        public override Binding CreateIsCheckedBinding()
        {
            return new Binding(nameof(SidePanel.IsVisiblePageList)) { Source = SidePanel.Current };
        }

        public override string ExecuteMessage(CommandParameter param, CommandOption option = CommandOption.None)
        {
            return SidePanel.Current.IsVisiblePageList ? Properties.Resources.CommandToggleVisiblePageListOff : Properties.Resources.CommandToggleVisiblePageListOn;
        }

        public override void Execute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            SidePanel.Current.ToggleVisiblePageList(option.HasFlag(CommandOption.ByMenu));
        }
    }
}
