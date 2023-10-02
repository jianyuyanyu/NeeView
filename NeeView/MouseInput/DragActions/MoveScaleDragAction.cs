﻿using NeeView.PageFrames;
using System;
using System.Diagnostics;
using System.Windows;

namespace NeeView
{
    public class MoveScaleDragAction : DragAction
    {
        public MoveScaleDragAction()
        {
            Note = Properties.Resources.DragActionType_MoveScale;
            DragActionCategory = DragActionCategory.Point;

            ParameterSource = new DragActionParameterSource(typeof(MoveScaleDragActionParameter));
        }

        public override DragActionControl CreateControl(DragTransformContext context)
        {
            return new ActionControl(context, this);
        }

        public class ActionControl : DragActionControl
        {
            private readonly DragTransform _transformControl;
            private readonly MoveScaleDragActionParameter _parameter;

            public ActionControl(DragTransformContext context, DragAction source) : base(context, source)
            {
                _transformControl = new DragTransform(context);
                _parameter = Parameter as MoveScaleDragActionParameter ?? throw new ArgumentNullException(nameof(source));
            }

            public override void ExecuteBegin()
            {
                _transformControl.ResetInertia();
            }

            // 移動(速度スケール依存)
            public override void Execute()
            {
                var delta = Context.Last - Context.Old;
                var scale = GetScale();
                _transformControl.DoMove(delta * scale, TimeSpan.Zero);
            }

            public override void ExecuteEnd(ISpeedometer? speedometer, bool continued)
            {
                Debug.Assert(speedometer is not null);
                if (continued) return;
                if (!_parameter.IsInertiaEnabled) return;

                _transformControl.DoInertia(speedometer.GetVelocity(), InertiaTools.GetAcceleration(Config.Current.Mouse.InertiaSensitivity));
            }

            private double GetScale()
            {
                // TODO: 連結モードでは３倍固定にしたい
                var scaleX = Context.ContentRect.Width / Context.ViewRect.Width;
                var scaleY = Context.ContentRect.Height / Context.ViewRect.Height;
                var scale = (scaleX > scaleY ? scaleX : scaleY) * _parameter.Sensitivity;
                scale = scale < 1.0 ? 1.0 : scale;
                return scale;
            }

        }
    }


}