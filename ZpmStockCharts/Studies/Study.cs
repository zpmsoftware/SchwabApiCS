// <copyright file="Study.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public abstract class Study
    {
        protected double[] Values;
        public Brush Color;

        public List<System.Windows.UIElement> UiElements = new List<System.Windows.UIElement>();
        protected DateTime loadTime;
        protected string DecimalFormat = "N2"; // default
        protected bool UseRightAxis = false; // values are a price, sync to right axis
        public string Symbol { get; protected set; }

        public DateTime TimeLastCalculated { get; protected set; }

        protected Study(Brush color)
        {
            Color = color;
        }

        public abstract string StudyDescription(); // short description
        public abstract string StudyToolTip(); // longer description
        public abstract int PrependCandlesNeeded { get; } // default is none.

        public bool IsLoaded { get { return Values != null; } }

        public double this[int index]   // Indexer declaration  
        {
            get
            {
                return Values[index]; // if prependPrices are used, IdxOffset is the number of prepend prices
            }
        }
        
        public int Length { get {  return Values.Length; } }

        public virtual string DisplayValue(int idx)
        {
            return this[idx].ToString(DecimalFormat);  // [idx].ToString(DecimalFormat) + " " + d.Timestamp.ToString("MM/dd/yyyy")
        }

        public abstract void Calculate(ZpmPriceCharts.CandleSet candleSet);

        public virtual void Draw(ZpmPriceCharts.PriceChart chart)
        {
            if (chart.Cset == null || Values == null) // candles not loaded yet
                return;

            UiElements.Clear();
            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index

            if (startIdx < Values.Length)
            {
                var vMax = Values.Max() * 3;
                System.Windows.Shapes.Line ln;
                double x1 = chart.ChartCandleCenter(0);
                double y1 = UseRightAxis ? chart.ChartAreaY(this[startIdx]) :  chart.LAxisPosition(this[startIdx], vMax);

                for (int x =0; x + startIdx < this.Length && x < chart.NbrCandles; x++)
                {
                    ln = chart.ChartLine(Color);
                    ln.X1 = x1;
                    ln.Y1 = y1;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = UseRightAxis ? chart.ChartAreaY(this[x + startIdx]) : chart.LAxisPosition(this[x + startIdx], vMax);
                    UiElements.Add(ln);

                    x1 = ln.X2;
                    y1 = ln.Y2;
                }
            }
        }

        /// <summary>
        /// EMA 
        /// </summary>
        /// <param name="val">input series array</param>
        /// <param name="Periods">average periods</param>
        /// <returns>EMA of val</returns>
        public static double[] ExponentialMovingAverage(double[] val, int Periods)
        {
            // ema is the smoothing constant
            double ema = 2.0 / (Periods + 1);
            double[] y = new double[val.Length];
            y[0] = val[0];
            for (int i = 1; i < val.Length; i++) y[i] = ema * val[i] + (1 - ema) * y[i - 1];

            return y;
        }
    }
}
