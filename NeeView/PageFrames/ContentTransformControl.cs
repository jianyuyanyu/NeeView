﻿using System;
using System.Diagnostics;
using System.Windows;
using NeeView.Maths;

namespace NeeView.PageFrames
{

    // TODO: ページ移動とPointの初期化問題
    public class ContentTransformControl : ITransformControl
    {
        private PageFrameContainer _container;
        private Rect _containerRect;
        private ScrollLock _scrollLock;

        public ContentTransformControl(PageFrameContainer container, Rect viewRect, ScrollLock scrollLock)
        {
            _container = container;
            _containerRect = viewRect;
            _scrollLock = scrollLock;
        }


        public double Scale => _container.Transform.Scale;
        public double Angle => _container.Transform.Angle;
        public Point Point => _container.Transform.Point;
        public bool IsFlipHorizontal => _container.Transform.IsFlipHorizontal;
        public bool IsFlipVertical => _container.Transform.IsFlipVertical;


        public void SetFlipHorizontal(bool value, TimeSpan span)
        {
            _container.Transform.SetFlipHorizontal(value, span);
        }

        public void SetFlipVertical(bool value, TimeSpan span)
        {
            _container.Transform.SetFlipVertical(value, span);
        }

        public void SetScale(double value, TimeSpan span)
        {
            _container.Transform.SetScale(value, span);
            _scrollLock.Unlock();
        }

        public void SetAngle(double value, TimeSpan span)
        {
            _container.Transform.SetAngle(value, span);
            _scrollLock.Unlock();
        }

        public void SetPoint(Point value, TimeSpan span)
        {
            _container.Transform.SetPoint(value, span);
        }

        public void AddPoint(Vector value, TimeSpan span)
        {
            var contentRect = _container.GetContentRect();

            // scroll lock
            _scrollLock.Update(contentRect, _containerRect);
            var delta = _scrollLock.Limit(value);

            // scroll area limit
            var areaLimit = new ScrollAreaLimit(contentRect, _containerRect);
            delta = areaLimit.GetLimitContentMove(delta);

            _container.Transform.SetPoint(_container.Transform.Point + delta, span);
        }


        public void SnapView()
        {
            //if (!Config.Current.View.IsLimitMove) return;

            var contentRect = _container.GetContentRect();

            var areaLimit = new ScrollAreaLimit(contentRect, _containerRect);
            _container.Transform.SetPoint(areaLimit.SnapView(false), TimeSpan.Zero);
        }
    }





}