// <copyright file="ADX.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

// ======================================================================================
// ADX - Average Directional Index
// ======================================================================================

namespace Studies
{
    // Advance Decline
    public class ADX : Study
    {
        public int Periods;
        private int Overbought;
        private int Oversold;
        private Brush LimitColor;
        public ATR? Atr { get; set; } = null;
        public override int PrependCandlesNeeded { get { return Periods * 3; } } // required to get a good value for the first chart candle


        private double[] di_plus;
        private double[] di_minus;

        private CandleSet candleSet; // one last used by Calculate()
        private int PeriodsLastCalculated = 0;

        /// <summary>
        /// ADX - Average Directional Index
        /// </summary>
        /// <param name="periods">#candles in period</param>
        public ADX(Brush color) : base(color)
        {
            Periods = 14;
        }

        public ADX(int periods, Brush color) : base(color)
        {
            Periods = periods;
        }

        public ADX(int periods, CandleSet candleSet_, Brush color) : base(color)
        {
            Periods = periods;
            Calculate(candleSet_);
        }

        public override string StudyDescription()
        {
            return "ADX(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - average directional index.";
        }


        public override void Calculate(CandleSet candleSet_)
        {
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            candleSet = candleSet_;

            List<Candle> candles = candleSet.Candles;
            Values = new double[candles.Count];
            di_plus = new double[candles.Count];
            di_minus = new double[candles.Count];

            double hiDiff;
            double lowDiff;
            double plusDM;
            double minusDM;
            double plusDI;
            double minusDI;
            double avgPlusDM = 0;
            double avgMinusDM = 0;
            double dx;

            if (Atr == null || Atr.Periods != Periods || Atr.TimeLastCalculated < candleSet.LoadTime)
            {
                Atr = new Studies.ATR(Periods, candleSet, System.Windows.Media.Brushes.LightBlue);
            }

            var PeriodsM1 = Periods - 1;

            try
            {
                for (int x = 1; x < candles.Count; x++)
                {
                    plusDM = Math.Max(candles[x].High - candles[x - 1].High, 0);
                    minusDM = Math.Max(candles[x - 1].Low - candles[x].Low, 0);

                    if (plusDM > minusDM)
                        minusDM = 0;
                    else if (plusDM < minusDM)
                        plusDM = 0;


                    if (x == 1)
                    {
                        avgPlusDM = plusDM;
                        avgMinusDM = minusDM;
                    }
                    else
                    {
                        avgPlusDM = (avgPlusDM * PeriodsM1 + plusDM) / Periods;
                        avgMinusDM = (avgMinusDM * PeriodsM1 + minusDM) / Periods;
                    }

                    plusDI = 100 * avgPlusDM / Atr[x];  // offset to start of Atr array
                    minusDI = 100 * avgMinusDM / Atr[x];

                    di_plus[x] = plusDI;
                    di_minus[x] = minusDI;

                    if (plusDI + minusDI == 0)
                        dx = 0;
                    else
                        dx = 100 * Math.Abs(plusDI - minusDI) / (plusDI + minusDI);

                    if (x == 1)
                    {
                        Values[x] = dx;
                        Values[0] = Values[1];
                    }
                    else
                        Values[x] = (Values[x - 1] * PeriodsM1 + dx) / Periods;
                }
            }
            catch (Exception ex)
            {
                var xx = 1;
            }
        }

        public override string DisplayValue(int idx)
        {
            return this[idx].ToString(DecimalFormat) + ", DI+ " + di_plus[idx].ToString("N2") + ", DI- " + di_minus[idx].ToString("N2"); 
        }

        public override void Draw(ZpmPriceCharts.PriceChart chart)
        {
            UiElements.Clear();
            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index. Values[] has earlier dates, StartDateIndex lines up values with chart.

            if (startIdx < Values.Length)
            {
                var y = chart.LAxisPosition(20, 100);
                var ln = chart.ChartLine(Color, 0, chart.ChartArea.ActualWidth, y, y, .5);
                UiElements.Add(ln);

                double x1 = chart.ChartCandleCenter(0);
                double y1 = chart.LAxisPosition(this[startIdx], 100);

                for (int x = 0; x + startIdx < this.Length && x < chart.NbrCandles; x++)
                {
                    ln = chart.ChartLine(Color);
                    ln.X1 = x1;
                    ln.Y1 = y1;
                    ln.X2 = chart.ChartCandleCenter(x);
                    ln.Y2 = chart.LAxisPosition(this[x + startIdx], 100);

                    UiElements.Add(ln);

                    x1 = ln.X2;
                    y1 = ln.Y2;
                }
            }
        }
    }
}
