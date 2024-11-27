// <copyright file="RSI.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace Studies
{
    // Relative Strength Indicator
    public class RSI : Study
    {
        private int Periods;
        private int Overbought;
        private int Oversold;
        private Brush LimitColor;
        private Mode mode;

        public enum Mode
        {
            Close,
            High,
            Low
        }

        /// <summary>
        ///         /// RSI - Relative Strength Indicator
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="overbought"></param>
        /// <param name="oversold"></param>
        /// <param name="color"></param>
        /// <param name="limitColor"></param>
        /// <param name="mode_"></param>
        public RSI(int periods, int overbought, int oversold, Brush color, Brush limitColor, Mode mode_ = Studies.RSI.Mode.Close)
            : base(color)
        {
            Periods = periods;
            Overbought = overbought;
            Oversold = oversold;
            LimitColor = limitColor;
            mode = mode_;
        }

        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the firsst chart candle

        public override string StudyDescription()
        {
            switch (mode)
            {
                case Mode.Low: return "RSI-L(" + Periods.ToString() + ")";
                case Mode.High: return "RSI-H(" + Periods.ToString() + ")";
            }
            return "RSI(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - relative strength index.";
        }

        public override void Caclulate(CandleSet candleSet)
        {
            Values = GetValues(candleSet, Periods, mode);
        }


        public static double[] GetValues(CandleSet candleSet, int Periods, Mode mode = Studies.RSI.Mode.Close) 
        { 
            List<Candle> candles = candleSet.Candles;
            var values = new double[candles.Count];
            double avgGain = 0;
            double avgLoss = 0;
            double diff;

            for (int x = 1; x < candles.Count; x++)
            {
                switch (mode)
                {
                    case Mode.Low:
                        diff = candles[x].Low - candles[x - 1].Low;
                        break;
                    case Mode.High:
                        diff = candles[x].High - candles[x - 1].High;
                        break;
                    default: // close
                        diff = candles[x].Close - candles[x - 1].Close;
                        break;
                }

                if (x > Periods)
                {
                    if (diff > 0)
                    {
                        avgGain = (avgGain * (Periods - 1) + diff) / Periods;
                        avgLoss = (avgLoss * (Periods - 1)) / Periods;
                    }
                    else
                    {
                        avgGain = (avgGain * (Periods - 1)) / Periods;
                        avgLoss = (avgLoss * (Periods - 1) - diff) / Periods;
                    }
                    double rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                    values[x] = 100 - (100 / (1 + rs));
                }
                else if (x <= Periods)
                {
                    if (diff > 0)
                        avgGain += diff;
                    else
                        avgLoss -= diff;

                    if (x==Periods)
                    {
                        avgGain = avgGain / Periods;
                        avgLoss = avgLoss / Periods;
                        double rs = avgLoss == 0 ? 0 : avgGain / avgLoss;
                        values[x] = 100 - (100 / (1 + rs));
                    }
                }
            }
            return values;
        }

        public override void Draw(PriceChart chart)
        {
            UiElements.Clear();
            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index. Values[] has earlier dates, StartDateIndex lines up values with chart.

            if (startIdx < Values.Length)
            {
                var perUnit = (chart.ChartArea.ActualHeight+200)/100;
                double x1 = chart.ChartCandleCenter(0);
                double y1 =  ((100 - Values[startIdx]) * perUnit)-100;

                var y = chart.LAxisPosition(70, 100);
                var ln = chart.ChartLine(Color, 0, chart.ChartArea.ActualWidth, y, y, .5);
                UiElements.Add(ln);

                y = chart.LAxisPosition(30, 100);
                ln = chart.ChartLine(Color, 0, chart.ChartArea.ActualWidth, y, y, .5);
                UiElements.Add(ln);

                for (int x = 0; x + startIdx < Values.Length && x < chart.NbrCandles; x++)
                {
                    ln = chart.ChartLine(Color);
                    ln.X1 = x1;
                    ln.Y1 = y1;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = chart.LAxisPosition(Values[x + startIdx], 100);
                    UiElements.Add(ln);

                    x1 = ln.X2;
                    y1 = ln.Y2;
                }
            }
        }
    }
}
