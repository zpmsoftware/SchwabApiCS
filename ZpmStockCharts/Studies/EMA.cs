// <copyright file="EMA.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace Studies
{
    public class EMA : Study
    {
        // Exponential Moving Average

        public int Periods { get; private set; }
        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the first chart candle

        /// <summary>
        /// SMA - Exponential Moving Average on closing price
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="color"></param>
        public EMA(int periods, Brush color)
            : base(color)
        {
            Periods = periods;
            UseRightAxis = true;
        }

        public override string StudyDescription()
        {
            return "EMA(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - exponential moving average.";
        }

        public override void Caclulate(CandleSet candleSet)
        {
            Caclulate(candleSet, Periods);
        }

        public void Caclulate(CandleSet candleSet, int periods)
        {
            List<Candle> candles = candleSet.Candles;
            Values = new double[candles.Count];
            Periods = periods;

            if (Periods > 0)
            {
                double factor = 2.0 / (periods + 1);
                double factor2 = 1 - factor;
                double d = candles[0].Close;
                Values[0] = d;

                for (int x = 1; x < candles.Count; x++)
                {
                    if (x < Periods) // initial setup, use SMA
                    {
                        d += candles[x].Close;
                        Values[x] = d / (x + 1);
                    }
                    else
                    {
                        Values[x] = factor * candles[x].Close + Values[x - 1] * factor2;
                    }
                }
            }
        }
    }
}
