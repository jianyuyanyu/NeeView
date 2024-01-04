﻿using System.Windows.Data;


namespace NeeView
{
    public class SetStretchModeUniformToSizeCommand : CommandElement
    {
        public SetStretchModeUniformToSizeCommand()
        {
            this.Group = Properties.Resources.CommandGroup_ImageScale;
            this.IsShowMessage = true;

            // SetStretchModeUniform
            this.ParameterSource = new CommandParameterSource(new StretchModeCommandParameter());
        }

        public override Binding CreateIsCheckedBinding()
        {
            return BindingGenerator.StretchMode(PageStretchMode.UniformToSize);
        }

        public override string ExecuteMessage(object? sender, CommandContext e)
        {
            return this.Text + (MainViewComponent.Current.ViewPropertyControl.TestStretchMode(PageStretchMode.UniformToSize, (e.Parameter.Cast<StretchModeCommandParameter>()).IsToggle) ? "" : " OFF");
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return !NowLoading.Current.IsDispNowLoading;
        }

        public override void Execute(object? sender, CommandContext e)
        {
            MainViewComponent.Current.ViewPropertyControl.SetStretchMode(PageStretchMode.UniformToSize, (e.Parameter.Cast<StretchModeCommandParameter>()).IsToggle, true);
        }
    }
}
