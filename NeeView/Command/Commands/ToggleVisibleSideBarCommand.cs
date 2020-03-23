﻿using System.Windows.Data;


namespace NeeView
{
    public class ToggleVisibleSideBarCommand : CommandElement
    {
        public ToggleVisibleSideBarCommand(string name) : base(name)
        {
            this.Group = Properties.Resources.CommandGroupWindow;
            this.Text = Properties.Resources.CommandToggleVisibleSideBar;
            this.MenuText = Properties.Resources.CommandToggleVisibleSideBarMenu;
            this.Note = Properties.Resources.CommandToggleVisibleSideBarNote;
            this.IsShowMessage = false;
        }

        public override Binding CreateIsCheckedBinding()
        {
            return new Binding(nameof(PanelsConfig.IsSideBarEnabled)) { Source = Config.Current.Layout.Panels };
        }

        public override string ExecuteMessage(CommandParameter param, object[] args, CommandOption option)
        {
            return Config.Current.Layout.Panels.IsSideBarEnabled ? Properties.Resources.CommandToggleVisibleSideBarOff : Properties.Resources.CommandToggleVisibleSideBarOn;
        }

        public override void Execute(CommandParameter param, object[] args, CommandOption option)
        {
            Config.Current.Layout.Panels.IsSideBarEnabled = !Config.Current.Layout.Panels.IsSideBarEnabled;
        }
    }
}
