// <copyright file="ATR.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

// ======================================================================================
// ATR - Average True Range
// ======================================================================================

namespace ZpmPriceCharts.Studies
{
    public class ATR : Study
    {
        public int Periods { get; private set; } = 0;
        private int PeriodsLastCalculated = 0;
        private int Decimals = 2;

        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the firsst chart candle


        public ATR(Brush color) : base(color)
        {
            Periods = 14;
        }

        public ATR(int periods_, Brush color) : base(color)
        {
            Periods = periods_;
        }

        public ATR(int periods_, CandleSet pbs, Brush color) : base(color)
        {
            Periods = periods_;
            Calculate(pbs);
        }

        public override string StudyDescription()
        {
            return "ATR(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - average true range.";
        }

        public override void Calculate(CandleSet cs)
        {
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            Decimals = cs.Decimals;
            DecimalFormat = "N" + Decimals.ToString();

            var pb = cs.Candles;
            if (pb.Count < Periods) return;

            Values = new double[pb.Count];
            double[] tr = new double[pb.Count];
            tr[0] = Math.Abs(pb[0].High - pb[0].Low);

            for (int x = 1; x < pb.Count; x++)
            {
                tr[x] = Math.Max(pb[x - 1].Close, pb[x].High) - Math.Min(pb[x - 1].Close, pb[x].Low);
            }

            for (int x = 1; x < pb.Count; x++)
            {
                if (x == Periods)
                {
                    double sumTR = 0;
                    for (int y = 1; y <= Periods; y++)
                        sumTR += tr[y];
                    Values[x] = sumTR / Periods;
                }
                else if (x > Periods)
                {
                    Values[x] = (Values[x - 1] * (Periods - 1) + tr[x]) / Periods;
                }
            }

            for (int x = 0; x < Periods; x++)
            {
                Values[x] = Values[Periods];
            }
        }

        public void CalculateLast2(CandleSet cs)
        {
            if (Periods != PeriodsLastCalculated || Values == null)
            {
                Calculate(cs); // Full recalc if periods changed or uninitialized
                return;
            }

            var pb = cs.Candles;
            if (pb.Count < Periods + 1) // Minimum required for smoothing
            {
                if (Values.Length != pb.Count)
                {
                    Array.Resize(ref Values, pb.Count);
                }
                for (int x = 0; x < pb.Count; x++)
                {
                    Values[x] = 0;
                }
                TimeLastCalculated = DateTime.Now;
                return;
            }

            int lastIdx = pb.Count - 1;
            int secondLastIdx = lastIdx - 1;

            // Ensure Values array is sized correctly
            if (Values.Length != pb.Count)
            {
                Array.Resize(ref Values, pb.Count);
            }

            // Update true range and ATR for the last two candles
            for (int x = secondLastIdx; x <= lastIdx; x++)
            {
                double tr = Math.Max(pb[x - 1].Close, pb[x].High) - Math.Min(pb[x - 1].Close, pb[x].Low);
                Values[x] = (Values[x - 1] * (Periods - 1) + tr) / Periods;
            }

            TimeLastCalculated = DateTime.Now;
        }



        // ===========================================
        /// <summary>
        /// Calculate ATR for one period/day
        /// </summary>
        /// <param name="x"> index in pbs for bar to calculateatr for</param>
        /// <param name="cs">price bar set</param>
        /// <param name="Periods">atr periods</param>
        /// <returns></returns>
        public static double CalculateOneValue(int x, CandleSet cs, int periods)
        {
            if (periods <= 0 || x < periods || x >= cs.Candles.Count)
                return 0;

            var pb = cs.Candles;
            double[] tr = new double[cs.Candles.Count];

            // Calculate TR for all candles up to x
            tr[0] = Math.Abs(pb[0].High - pb[0].Low);
            for (int i = 1; i <= x; i++)
            {
                tr[i] = Math.Max(pb[i - 1].Close, pb[i].High) - Math.Min(pb[i - 1].Close, pb[i].Low);
            }

            // Wilder’s ATR
            double atr = 0;
            for (int i = 1; i <= x; i++)
            {
                if (i == periods)
                {
                    for (int j = 1; j <= periods; j++)
                        atr += tr[j];
                    atr /= periods; // Initial average
                }
                else if (i > periods)
                {
                    atr = (atr * (periods - 1) + tr[i]) / periods; // Wilder’s smoothing
                }
            }

            return Math.Round(atr, cs.Decimals); // Match precision to CandleSet
        }
    }
}
