// <copyright file="ATR.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

// ======================================================================================
// ATR - Average True Range
// ======================================================================================

namespace Studies
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

        public override void Calculate(CandleSet pbs)
        {
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            Decimals = pbs.Decimals;
            DecimalFormat = "N" + Decimals.ToString();

            //if (loadTime != pbs.LoadTime)
            //{
            //    loadTime = pbs.LoadTime;
            //}

            var pb = pbs.Candles;

            if (Values == null || Values.Length != pb.Count)
                Values = new double[pb.Count];

            Values[0] = Math.Abs(pb[0].High - pb[0].Low); // special case for first one

            for (int x = 1; x < pb.Count; x++)
            {
                Values[x] = Math.Max(pb[x - 1].Close, pb[x].High) - Math.Min(pb[x - 1].Close, pb[x].Low);

                /* line above is equivalent to this code.  
                var a = pb[x].High - pb[x].Low;
                var b = pb[x].High - pb[x-1].Close;
                var c = pb[x-1].Close - pb[x].Low;

                if (b > a)
                    a = b;
                if (c > a)
                    a = c;

                if (a <0 || a1!= a )
                {
                    var xx = 1;
                }
                */
            }

            int z = 0;
            for (int x = 1; x < pb.Count; x++)
            {
                if (x == 1)
                {
                    for (int y = 2; y <= Periods; y++)
                        Values[1] += Values[y];
                    Values[1] = Values[1] / Periods;
                }
                else
                {
                    if (x < Periods)
                        z = x;
                    Values[x] = (Values[x] + (Values[x - 1] * z)) / (z + 1);
                }
            }
        }

        public void CalculateX(CandleSet pbs)
        {
            PeriodsLastCalculated = Periods;

            double ema = 2.0 / (Periods + 1); // ems is the smoothing constant

            /*
            if (loadTime != pbs.LoadTime)
            {
                loadTime = pbs.LoadTime;
            }*/

            var pb = pbs.Candles;

            if (Values == null || Values.Length != pb.Count)
                Values = new double[pb.Count];

            Values[0] = Math.Abs(pb[0].High - pb[0].Low); // special case for first one

            int z = 0;
            for (int x = 1; x < pb.Count; x++)
            {
                var a = Math.Max(pb[x - 1].Close, pb[x].High) - Math.Min(pb[x - 1].Close, pb[x].Low);

                /* line above is equivalent to this code.  
                var a = pb[x].High - pb[x].Low;
                var b = pb[x].High - pb[x-1].Close;
                var c = pb[x-1].Close - pb[x].Low;

                if (b > a)
                    a = b;
                if (c > a)
                    a = c;

                if (a <0 || a1!= a )
                {
                    var xx = 1;
                }
                */

                if (x < Periods)
                    z = x;
                Values[x] = (a + (Values[x - 1] * z)) / (z + 1);
            }
        }
    }
}
