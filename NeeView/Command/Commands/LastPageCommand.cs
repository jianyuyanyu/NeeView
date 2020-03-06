﻿namespace NeeView
{
    public class LastPageCommand : CommandElement
    {
        public LastPageCommand() : base(CommandType.LastPage)
        {
            this.Group = Properties.Resources.CommandGroupMove;
            this.Text = Properties.Resources.CommandLastPage;
            this.Note = Properties.Resources.CommandLastPageNote;
            this.ShortCutKey = "Ctrl+Left";
            this.MouseGesture = "UL";
            this.IsShowMessage = true;
            this.PairPartner = CommandType.FirstPage;

            // FirstPage
            this.ParameterSource = new CommandParameterSource(new ReversibleCommandParameter());
        }

        public override bool CanExecute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            return !NowLoading.Current.IsDispNowLoading;
        }

        public override void Execute(CommandParameter param, CommandOption option = CommandOption.None)
        {
            BookOperation.Current.LastPage();
        }
    }
}
