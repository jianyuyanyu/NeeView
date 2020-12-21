﻿using NeeLaboratory;
using NeeView.Windows.Property;
using System;
using System.Runtime.Serialization;

namespace NeeView
{
    public class ViewScrollNTypeUpCommand : CommandElement
    {
        public ViewScrollNTypeUpCommand()
        {
            this.Group = Properties.Resources.CommandGroup_ViewManipulation;
            this.IsShowMessage = false;

            this.ParameterSource = new CommandParameterSource(new ViewScrollNTypeCommandParameter());
        }

        public override void Execute(object sender, CommandContext e)
        {
            MainViewComponent.Current.ViewController.ScrollNTypeUp((ViewScrollNTypeCommandParameter)e.Parameter);
        }
    }


    /// <summary>
    /// N字スクロール
    /// </summary>
    public class ViewScrollNTypeCommandParameter : ReversibleCommandParameter, IScrollNTypeParameter
    {
        private double _scroll = 1.0;
        private double _margin = 50;
        private double _scrollDuration = 0.2;

        [PropertyMember]
        public double Margin
        {
            get => _margin;
            set => SetProperty(ref _margin, Math.Max(value, 10));
        }

        [PropertyPercent]
        public double Scroll
        {
            get => _scroll;
            set => SetProperty(ref _scroll, MathUtility.Clamp(value, 0.0, 1.0));
        }

        [PropertyRange(0.0, 1.0, TickFrequency = 0.1, IsEditable = true)]
        public double ScrollDuration
        {
            get { return _scrollDuration; }
            set { SetProperty(ref _scrollDuration, Math.Max(value, 0.0)); }
        }
    }

}
