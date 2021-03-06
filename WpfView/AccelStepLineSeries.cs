﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Definitions.Points;
using LiveCharts.Definitions.Series;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf.Components;
using LiveCharts.Wpf.Points;
using LiveCharts.Dtos;
using LiveCharts.Charts;


namespace LiveCharts.Wpf
{
    /// <summary>
    /// ChartPointView for Bulk rendering
    /// this dosen' have UIElement
    /// </summary>
    internal class AccelStepLinePointView : AccelPointView, IStepPointView
    {
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }

        public string Label { get; set; }

    }



    /// <summary>
    /// The Step line series that suppots Bulk rendering.
    /// 
    /// 
    /// </summary>
    public class AccelStepLineSeries : StepLineSeries
    {
        #region Overridden Methods

        /// <summary>
        /// Get the view of a chart point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override IChartPointView GetPointView(ChartPoint point, string label)
        {
            var pbv = (AccelStepLinePointView)point.View;

            if (pbv == null)
            {
                pbv = new AccelStepLinePointView
                {
                    IsNew = true,
                };
            }
            else
            {
                pbv.IsNew = false;
            }

            pbv.Label = label;

            return pbv;
        }


        /// <summary>
        /// This method runs when the update finishes
        /// </summary>
        public override void OnSeriesUpdatedFinish()
        {
            base.OnSeriesUpdatedFinish();

            if (m_SeriesAccelView == null)
            {
                m_SeriesAccelView = new _AccelViewElement(this);
                m_SeriesAccelView.MouseDown += _SeriesAccelView_MouseDown;
                m_SeriesAccelView.MouseUp += _SeriesAccelView_MouseUp;

                Model.Chart.View.AddToDrawMargin(m_SeriesAccelView);

                var wpfChart = Model.Chart.View as Chart;
                wpfChart.AttachHoverableEventTo(m_SeriesAccelView);

                Panel.SetZIndex(m_SeriesAccelView, Panel.GetZIndex(this));
            }
            m_SeriesAccelView.InvalidateVisual();
        }
        private _AccelViewElement m_SeriesAccelView;

        /// <summary>
        /// Erases series
        /// </summary>
        /// <param name="removeFromView"></param>
        public override void Erase(bool removeFromView = true)
        {
            if (m_SeriesAccelView != null)
            {
                m_SeriesAccelView.MouseDown -= _SeriesAccelView_MouseDown;
                m_SeriesAccelView.MouseUp -= _SeriesAccelView_MouseUp;

                Model?.Chart?.View?.RemoveFromDrawMargin(m_SeriesAccelView);
                m_SeriesAccelView = null;
            }

            base.Erase(removeFromView);
        }


        /// <summary>
        /// ビジュアルエレメントからのマウスイベントを、自身のものとしてハンドリング
        /// </summary>
        private void _SeriesAccelView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Fire_SeriesMouseDown(e);
        }
        private void _SeriesAccelView_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Fire_SeriesMouseUp(e);
        }

        #endregion


        #region Bulk rendering element and method

        private ChartPoint HoverringChartPoint
        {
            get { return m_HoverringChartPoint; }
            set
            {
                if ( m_HoverringChartPoint != value )
                {
                    m_HoverringChartPoint = value;
                    m_SeriesAccelView?.InvalidateVisual();
                }
            }
        }
        private ChartPoint m_HoverringChartPoint;


        /// <summary>
        /// prepate rendered chart points for quick access
        /// </summary>
        private IEnumerable<ChartPoint> RenderdChartPointList { get; set; }


        private void _Render(DrawingContext drawingContext)
        {
            if( Visibility == Visibility.Visible)
            {
                Brush brushStroke = Stroke.Clone();
                brushStroke.Freeze();

                Brush brushPointForeground = PointForeground.Clone();
                brushPointForeground.Freeze();

                Pen penStroke = new Pen(Stroke, StrokeThickness + (HoverringChartPoint != null ? 1d : 0));
                penStroke.DashStyle = new DashStyle(StrokeDashArray, 0);
                penStroke.Freeze();


                //prepat chart point list 
                this.RenderdChartPointList = this.ActualValues.GetPoints(this,
                    new CoreRectangle(0, 0, Model.Chart.DrawMargin.Width, Model.Chart.DrawMargin.Height));


                // Draw step line

                Pen penAlternativeStroke = penStroke.Clone();
                penAlternativeStroke.Brush = AlternativeStroke;
                penAlternativeStroke.Freeze();


                ChartPoint previous = null;
                foreach (var current in this.RenderdChartPointList)
                {
                    if (previous != null)
                    {
                        var currentView = current.View as AccelStepLinePointView;
                        if (currentView != null)
                        {
                            if (InvertedMode)
                            {
                                drawingContext.DrawLine(penAlternativeStroke
                                    , new Point(current.ChartLocation.X, current.ChartLocation.Y)
                                    , new Point(current.ChartLocation.X - currentView.DeltaX, current.ChartLocation.Y));

                                drawingContext.DrawLine(penStroke
                                    , new Point(current.ChartLocation.X - currentView.DeltaX, current.ChartLocation.Y)
                                    , new Point(current.ChartLocation.X - currentView.DeltaX, current.ChartLocation.Y - currentView.DeltaY));
                            }
                            else
                            {
                                drawingContext.DrawLine(penAlternativeStroke
                                    , new Point(current.ChartLocation.X, current.ChartLocation.Y)
                                    , new Point(current.ChartLocation.X, current.ChartLocation.Y - currentView.DeltaY));

                                drawingContext.DrawLine(penStroke
                                    , new Point(current.ChartLocation.X - currentView.DeltaX, current.ChartLocation.Y - currentView.DeltaY)
                                    , new Point(current.ChartLocation.X, current.ChartLocation.Y - currentView.DeltaY));

                            }
                        }
                    }

                    previous = current;
                }


                // Draw point geometry

                if (PointGeometry != null && Math.Abs(PointGeometrySize) > 0.1)
                {
                    var rect = PointGeometry.Bounds;
                    var offsetX = rect.X + rect.Width / 2d;
                    var offsetY = rect.Y + rect.Height / 2d;

                    var pgeoRate = Math.Max(0.1d, Math.Abs(PointGeometrySize) - StrokeThickness) / Math.Max(1d, Math.Max(rect.Width, rect.Height));

                    //prepate pen for scaled
                    Pen pgeoPenStroke = penStroke.Clone();
                    pgeoPenStroke.Thickness /= pgeoRate;
                    pgeoPenStroke.Freeze();

                    //prepare transforms
                    Transform transformOffset = new TranslateTransform(-offsetX, -offsetY);
                    transformOffset.Freeze();
                    Transform transformScale = new ScaleTransform(-pgeoRate, pgeoRate, offsetX, offsetY);
                    transformScale.Freeze();

                    drawingContext.PushTransform(transformOffset);
                    foreach (var current in this.RenderdChartPointList)
                    {
                        drawingContext.PushTransform(new TranslateTransform(current.ChartLocation.X, current.ChartLocation.Y));
                        drawingContext.PushTransform(transformScale);
                        drawingContext.DrawGeometry(
                            Object.ReferenceEquals(current, m_HoverringChartPoint) ? brushStroke : brushPointForeground
                            , pgeoPenStroke, PointGeometry);
                        drawingContext.Pop();
                        drawingContext.Pop();
                    }
                    drawingContext.Pop();
                }


                //Draw label

                if (DataLabels)
                {
                    Typeface typeFace = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

                    Brush brushFg = Foreground.Clone();
                    brushFg.Freeze();

                    CoreRectangle preRect = new CoreRectangle(0, 0, 0, 0);
                    foreach (var current in this.RenderdChartPointList)
                    {
                        var currentView = current.View as AccelStepLinePointView;
                        if (currentView != null)
                        {
                            //if text position is in the previous text rectange, skip draw it.
                            if (preRect.HitTest(current.ChartLocation, 0))
                            {
                                continue;
                            }

                            FormattedText formattedText = new FormattedText(
                                    currentView.Label,
                                    System.Globalization.CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight,
                                    typeFace,
                                    FontSize,
                                    brushFg, 99d);

                            var xl = CorrectXLabel(current.ChartLocation.X - formattedText.Width * .5, Model.Chart, formattedText.Width);
                            var yl = CorrectYLabel(current.ChartLocation.Y - formattedText.Height * .5, Model.Chart, formattedText.Height);

                            preRect = new CoreRectangle(xl, yl, formattedText.Width, formattedText.Height);

                            drawingContext.DrawText(formattedText,
                                new Point(xl, yl));
                        }
                    }

                }

            }

        }

        private double CorrectXLabel(double desiredPosition, ChartCore chart, Double textWidth)
        {
            if (desiredPosition + textWidth * .5 < -0.1) return -textWidth;

            if (desiredPosition + textWidth > chart.DrawMargin.Width)
                desiredPosition -= desiredPosition + textWidth - chart.DrawMargin.Width + 2;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }

        private double CorrectYLabel(double desiredPosition, ChartCore chart, Double textHeight)
        {
            desiredPosition -= (PointGeometry == null ? 0 : GetPointDiameter()) + textHeight * .5 + 2;

            if (desiredPosition + textHeight > chart.DrawMargin.Height)
                desiredPosition -= desiredPosition + textHeight - chart.DrawMargin.Height + 2;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }



        private ChartPoint _HitTest(CorePoint pt)
        {
            ChartPoint hitChartPoint = null;

            double hittestMargin = GetPointDiameter() + StrokeThickness + 2d;

            double currentDistance = Double.MaxValue;
            foreach (var current in this.RenderdChartPointList)
            {
                if (current.ChartLocation.HitTest(pt, hittestMargin))
                {
                    var d = (current.ChartLocation.X - pt.X) * (current.ChartLocation.X - pt.X) 
                            + (current.ChartLocation.Y - pt.Y) * (current.ChartLocation.Y - pt.Y) ;
                    if(d < currentDistance)
                    {
                        currentDistance = d;
                        hitChartPoint = current;
                    }
                }
            }
            return hitChartPoint;
        }


        private class _AccelViewElement : FrameworkElement, ISeriesAccelView
        {
            public _AccelViewElement(AccelStepLineSeries owner)
            {
                _owner = owner;
            }
            private AccelStepLineSeries _owner;

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);
                _owner._Render(drawingContext);
            }

            protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
            {
                Point pt = hitTestParameters.HitPoint;

                var cp = _owner._HitTest(new CorePoint(pt.X, pt.Y));
                if (cp != null)
                {
                    return new PointHitTestResult(this, pt);
                }
                return null;
            }

            public ChartPoint HitTestChartPoint(CorePoint pt)
            {
                return _owner._HitTest(pt);
            }

            public void OnHover(ChartPoint point)
            {
                _owner.HoverringChartPoint = point;
            }

            public void OnHoverLeave()
            {
                _owner.HoverringChartPoint = null;
            }
        }

        #endregion
    }
}
