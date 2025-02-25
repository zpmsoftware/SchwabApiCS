// <copyright file="SMA.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class SMA : Study   
    {
        // Simple Moving Average

        public int Periods { get; set; }

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
            Values = Calculate(candleSet, Periods);
            TimeLastCalculated = DateTime.Now;
        }

        public static double[] Calculate(CandleSet candleSet, int periods)
        {
            List<Candle> candles = candleSet.Candles;
           var values = new double[candles.Count];

            if (periods > 0)
            {
                // calculate average
                values[0] = candles[0].Close;
                for (int x = 1; x < candles.Count; x++)
                { // build totals
                    if (x >= periods)
                        values[x] = values[x - 1] + candles[x].Close - candles[x - periods].Close;  // add one in, take one out
                    else
                        values[x] = values[x - 1] + candles[x].Close;
                }
                for (int x = 1; x < candles.Count; x++)
                { // get average
                    if (x >= periods)
                        values[x] = Math.Round(values[x] / periods, 2);
                    else
                        values[x] = Math.Round(values[x] / (x + 1), 2);
                }
            }
            return values;
        }

        /// <summary>
        /// Calculate SMA on an array of data, not a candle set.
        /// <param name="data"></param>
        /// <param name="periods"></param>
        /// <returns></returns>
        public static double[] Calculate(double[] data, int periods)
        {
            var values = new double[data.Length];

            if (periods > 0)
            {
                // calculate average
                values[0] = data[0];
                for (int x = 1; x < data.Length; x++)
                { // build totals
                    if (x >= periods)
                        values[x] = values[x - 1] + data[x] - data[x - periods];  // add one in, take one out
                    else
                        values[x] = values[x - 1] + data[x];
                }
                for (int x = 1; x < data.Length; x++)
                { // get average
                    if (x >= periods)
                        values[x] = Math.Round(values[x] / periods, 2);
                    else
                        values[x] = Math.Round(values[x] / (x + 1), 2);
                }
            }
            return values;
        }
    }
}
