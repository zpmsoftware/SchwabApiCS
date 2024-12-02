// <copyright file="OBV.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace Studies
{
    public class OBV : Study
    {
        private double MaxValue = 0;
        private double MinValue = 0;
        private List<Candle> candles;

        public OBV(Brush color)
            : base(color)
        {
        }

        public override int PrependCandlesNeeded { get { return 0; } } // required to get a good value for the first chart candle

        public override string StudyDescription()
        {
            return "On Balance Volume";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription();
        }

        public override void Calculate(CandleSet candleSet)
        {
            TimeLastCalculated = DateTime.Now;

            candles = candleSet.Candles;
            Values = new double[candles.Count];

            // calculate
            Values[0] = candles[0].Volume;
            MaxValue = Values[0];
            MinValue = Values[0];

            for (int x = 1; x < candles.Count; x++)
            {
                if (candles[x].Close > candles[x-1].Close)
                    Values[x] = Values[x-1] + candles[x].Volume;
                else if (candles[x].Close < candles[x-1].Close)
                    Values[x] = Values[x - 1] - candles[x].Volume;
                else
                    Values[x] = Values[x - 1];

                if (Values[x] > MaxValue)
                    MaxValue = Values[x];
                if (Values[x] < MinValue)
                    MinValue = Values[x];
            }
        }

        public override void Draw(PriceChart chart)
        {
            int startCandle = chart.StartCandle + chart.Cset.StartTimeIndex;
            double unitFactor = chart.CalcFactor(MaxValue, MinValue);
            double x1 = chart.ChartCandleCenter(0);
            double y1 = CalcTop(Values[startCandle], unitFactor);
            double offset = chart.CalcTop(candles[startCandle].Close);
            UiElements.Clear();

            for (int x = 1; x+ startCandle < Values.Length && x < chart.NbrCandles; x++)
            {
                var ln = chart.ChartLine(Color);
                ln.X1 = x1;
                ln.Y1 = y1;
                ln.X2 = chart.ChartCandleCenter(x);
                ln.Y2 = CalcTop(Values[x + startCandle], unitFactor);
                UiElements.Add(ln);
                x1 = ln.X2;
                y1 = ln.Y2;
            }
        }

        public double CalcTop(double value, double unitFactor)
        {
            return ((MaxValue - value) * unitFactor) - 30;
        }
    }
}
