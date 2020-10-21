﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Definitions.Points;
using LiveCharts.Definitions.Series;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf.Points;
using LiveCharts.Dtos;

namespace LiveCharts.Wpf
{
    using LiveCharts.Charts;
    using System.Linq;
    using System.Runtime.CompilerServices;


    internal class AccelCandlePointView : CandlePointView
    {
        public string Label { get; set; }


        
        public override void DrawOrMove(ChartPoint previousDrawn, ChartPoint current, int index, ChartCore chart)
        {
        }

        public override void RemoveFromView(ChartCore chart)
        {
        }
        
    }


    public class AccelCandleSeries : CandleSeries
    {
        #region Overridden Methods

        /// <summary>
        /// Gets the view of a given point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override IChartPointView GetPointView(ChartPoint point, string label)
        {
            var pbv = (AccelCandlePointView)point.View;

            if (pbv == null)
            {
                pbv = new AccelCandlePointView
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

                Model.Chart.View.AddToDrawMargin(m_SeriesAccelView);

                var wpfChart = Model.Chart.View as Chart;
                wpfChart.AttachHoverableEventTo(m_SeriesAccelView);
            }
            m_SeriesAccelView.InvalidateVisual();
        }
        private _AccelViewElement m_SeriesAccelView;


        #endregion







        #region Bulk rendering element and method

        private ChartPoint HoverringChartPoint
        {
            get { return m_HoverringChartPoint; }
            set
            {
                if (m_HoverringChartPoint != value)
                {
                    m_HoverringChartPoint = value;
                    m_SeriesAccelView?.InvalidateVisual();
                }
            }
        }
        private ChartPoint m_HoverringChartPoint;


        private ChartPoint _HitTest(CorePoint pt)
        {
            ChartPoint hitChartPoint = null;
/*
            var chartPointList = this.ActualValues.GetPoints(this,
                new CoreRectangle(0, 0, Model.Chart.DrawMargin.Width, Model.Chart.DrawMargin.Height));

            foreach (var current in chartPointList)
            {
                if (current.HitTest(pt, GetPointDiameter() + StrokeThickness))
                {
                    hitChartPoint = current;
                    break;
                }
            }
*/
            return hitChartPoint;
        }


        private void _OnHover(ChartPoint point)
        {
            HoverringChartPoint = point;
        }


        private void _OnHoverLeave()
        {
            HoverringChartPoint = null;
        }


        private void _Render(DrawingContext drawingContext)
        {
            if (Visibility == Visibility.Visible)
            {
                Brush brushStroke = Stroke.Clone();
                brushStroke.Freeze();

                Brush brushFill = Fill.Clone();
                brushFill.Freeze();



                /*
                Brush brushPointForeground = PointForeground.Clone();
                brushPointForeground.Freeze();
                */

                Brush brushIncrease = IncreaseBrush.Clone();
                brushIncrease.Freeze();

                Brush brushDecrease = DecreaseBrush.Clone();
                brushDecrease.Freeze();


                Pen penIncrease = new Pen(IncreaseBrush, StrokeThickness + (HoverringChartPoint != null ? 1d : 0));
                penIncrease.DashStyle = new DashStyle(StrokeDashArray, 0);
                penIncrease.Freeze();

                Pen penDecrease = new Pen(DecreaseBrush, StrokeThickness + (HoverringChartPoint != null ? 1d : 0));
                penDecrease.DashStyle = new DashStyle(StrokeDashArray, 0);
                penDecrease.Freeze();


                var chartPointList = this.ActualValues.GetPoints(this,
                    new CoreRectangle(0, 0, Model.Chart.DrawMargin.Width, Model.Chart.DrawMargin.Height));


                // Draw candle

                ChartPoint previous = null;
                foreach (var current in chartPointList)
                {
                    var currentView = current.View as AccelCandlePointView;

                    var center = currentView.Left + currentView.Width / 2;

                    var penLine = current.Open <= current.Close ? penIncrease : penDecrease;
                    var brushRect= current.Open <= current.Close ? brushIncrease : brushDecrease;
                    var penRect = current.Open <= current.Close ? penIncrease : penDecrease;

                    if (this.ColoringRules != null)
                    {
                        foreach (var rule in this.ColoringRules)
                        {
                            if (!rule.Condition(current, previous)) continue;

                            penLine = penLine.Clone();
                            penLine.Brush = rule.Stroke;
                            penLine.Freeze();

                            brushRect = rule.Fill.Clone();
                            brushRect.Freeze();

                            penRect = penRect.Clone();
                            penRect.Brush = rule.Stroke;
                            penRect.Freeze();

                            break;
                        }
                    }

                    drawingContext.DrawLine(
                        penLine
                        , new Point(center, currentView.High)
                        , new Point(center, currentView.Low));


                    drawingContext.DrawRectangle(brushRect, penRect, 
                        new Rect(
                            currentView.Left
                            , Math.Min(currentView.Open, currentView.Close)
                            , currentView.Width
                            , Math.Abs(currentView.Open - currentView.Close)
                        ));

                    previous = current;
                }

                /*
                // Draw path line

                drawingContext.DrawGeometry(brushFill, penStroke, Path.Data);



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
                    foreach (var current in chartPointList)
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

                    foreach (var current in chartPointList)
                    {
                        var pointView = current.View as AccelHorizontalBezierPointView;

                        FormattedText formattedText = new FormattedText(
                                pointView.Label,
                                System.Globalization.CultureInfo.CurrentCulture,
                                FlowDirection.LeftToRight,
                                typeFace,
                                FontSize,
                                brushFg);

                        var xl = CorrectXLabel(current.ChartLocation.X - formattedText.Width * .5, Model.Chart, formattedText.Width);
                        var yl = CorrectYLabel(current.ChartLocation.Y - formattedText.Height * .5, Model.Chart, formattedText.Height);

                        drawingContext.DrawText(formattedText,
                            new Point(xl, yl));
                    }

                }
                */
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
            if (desiredPosition + textHeight > chart.DrawMargin.Height)
                desiredPosition -= desiredPosition + textHeight - chart.DrawMargin.Height + 2;

            if (desiredPosition < 0) desiredPosition = 0;

            return desiredPosition;
        }



        private class _AccelViewElement : FrameworkElement, ISeriesAccelView
        {
            public _AccelViewElement(AccelCandleSeries owner)
            {
                _owner = owner;
            }
            private AccelCandleSeries _owner;


            public void DrawOrMove()
            {
                this.InvalidateVisual();
                /*
                Task.Run(async () =>
                {
                    //await Task.Delay(1);
                    await Dispatcher.BeginInvoke((Action)this.InvalidateVisual);
                });
                */
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                System.Diagnostics.Debug.Print($"Called OnRender {DateTime.Now}");

                base.OnRender(drawingContext);
                _owner._Render(drawingContext);
            }

            public ChartPoint HitTest(CorePoint pt)
            {
                return _owner._HitTest(pt);
            }

            public void OnHover(ChartPoint point)
            {
                _owner._OnHover(point);
            }

            public void OnHoverLeave()
            {
                _owner._OnHoverLeave();
            }
        }

        #endregion

    }
}
