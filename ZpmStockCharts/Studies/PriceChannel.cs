// <copyright file="PC.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class PriceChannel : Study
    {
        // Price Channel
        public double[] High { get; private set; }
        public double[] Low { get; private set; }

        public int Periods { get; private set; }
        public bool UseClosePrice { get; private set; }

        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the first chart candle

        /// <summary>
        /// SMA - Simple Moving Average on closing price
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="color"></param>
        public PriceChannel(int periods, Brush color, bool useClosePrice)
            : base(color)
        {
            Periods = periods;
            UseRightAxis = true;
            UseClosePrice = useClosePrice;
        }

        public override string StudyDescription()
        {
            return "PC(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - price channel.";
        }

        public override string DisplayValue(int idx)
        {
            return High[idx].ToString(DecimalFormat) + " - " + Low[idx].ToString(DecimalFormat);
        }

        public override void Calculate(CandleSet candleSet)
        {
            Calculate(candleSet, Periods, UseClosePrice);
            TimeLastCalculated = DateTime.Now;
        }

        public void Calculate(CandleSet candleSet, int periods, bool useClosePrice)
        {
            if (Periods == periods && this.TimeLastCalculated == candleSet.LoadTime)
                return; // No change

            List<Candle> candles = candleSet.Candles;
            Values = new double[candles.Count];
            Periods = periods;
            TimeLastCalculated = DateTime.Now;
            High = new double[candles.Count];
            Low = new double[candles.Count];

            if (Periods > 0)
            {
                int i;
                double high = 0;
                double low = 9999999;
                if (UseClosePrice) // base on close price only
                {
                    for (int x = periods; x < candles.Count; x++)
                    {
                        high = candles[x-periods].Close;
                        for (i = x - periods + 1; i < x; i++)
                        {
                            if (candles[i].Close > high)
                                high = candles[i].Close;
                        }
                        High[x] = high;

                        low = candles[x - periods].Close;
                        for (i = x - periods+1; i < x; i++)
                        {
                            if (candles[i].Close < low)
                                low = candles[i].Close;
                        }
                        Low[x] = low;
                    }
                }
                else
                {
                    for (int x = periods; x < candles.Count; x++)
                    {
                        high = candles[x - periods].High;
                        for (i = x - periods + 1; i < x; i++)
                        {
                            if (candles[i].High > high)
                                high = candles[i].High;
                        }
                        High[x] = high;

                        low = candles[x - periods].Low;
                        for (i = x - periods + 1; i < x; i++)
                        {
                            if (candles[i].Low < low)
                                low = candles[i].Low;
                        }
                        Low[x] = low;
                    }
                }
            }
        }

        public override void Draw(ZpmPriceCharts.PriceChart chart)
        {
            if (chart.Cset == null) // candles not loaded yet
                return;

            UiElements.Clear();
            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index

            if (startIdx < High.Length)
            {
                System.Windows.Shapes.Line ln, ln2;
                double x1 = chart.ChartCandleCenter(0);
                double yh = chart.ChartAreaY(High[startIdx]);
                double yl = chart.ChartAreaY(Low[startIdx]);

                for (int x = 0; x + startIdx < this.Length && x < chart.NbrCandles; x++)
                {
                    ln = chart.ChartLine(Color); // high
                    ln.X1 = x1;
                    ln.Y1 = yh;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = chart.ChartAreaY(High[x + startIdx]);
                    UiElements.Add(ln);

                    ln2 = chart.ChartLine(Color); // low
                    ln2.X1 = x1;
                    ln2.Y1 = yl;
                    ln2.X2 = ln.X2;
                    ln2.Y2 = chart.ChartAreaY(Low[x + startIdx]);
                    UiElements.Add(ln2);

                    x1 = ln.X2;
                    yh = ln.Y2;
                    yl = ln2.Y2;
                }
            }
        }
    }
}
