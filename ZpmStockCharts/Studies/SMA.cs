// <copyright file="SMA.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace Studies
{
    public class SMA : Study   
    {
        // Simple Moving Average

        public int Periods { get;  private set; }

        public override int PrependCandlesNeeded { get { return Periods-1; } } // required to get a good value for the firsst chart candle

        /// <summary>
        /// SMA - Simple Moving Average on closing price
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="color"></param>
        public SMA(int periods, Brush color)
            : base(color)
        {
            Periods = periods;
            UseRightAxis = true;
        }

        public override string StudyDescription()
        {
            return "SMA(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - simple moving average.";
        }

        public override void Calculate(CandleSet candleSet)
        {
            Calculate(candleSet, Periods);
            TimeLastCalculated = DateTime.Now;
        }

        public void Calculate(CandleSet candleSet, int periods)
        {
            List<Candle> candles = candleSet.Candles;
            Values = new double[candles.Count];
            Periods = periods;

            if (Periods > 0)
            {
                // calculate average
                Values[0] = candles[0].Close;
                for (int x = 1; x < candles.Count; x++)
                { // build totals
                    if (x >= Periods)
                        Values[x] = Values[x - 1] + candles[x].Close - candles[x - Periods].Close;  // add one in, take one out
                    else
                        Values[x] = Values[x - 1] + candles[x].Close;
                }
                for (int x = 1; x < candles.Count; x++)
                { // get average
                    if (x >= Periods)
                        Values[x] = Math.Round(Values[x] / Periods, 2);
                    else
                        Values[x] = Math.Round(Values[x] / (x + 1), 2);
                }
            }
        }
    }
}
