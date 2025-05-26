// <copyright file="ADX.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.Windows.Media;

// ======================================================================================
// ADX - Average Directional Index
// ======================================================================================

namespace ZpmPriceCharts.Studies
{
    public class ADX : Study
    {
        public int Periods { get; set; }
        public int SmoothingPeriods { get; set; }

        public double[] DIPlus;
        public double[] DIMinus;
        private CandleSet candleSet;
        private List<Candle> candlesLastUsed;
        private int PeriodsLastCalculated = 0;

        // Intermediate values stored as fields
        private double[] plusDM;
        private double[] minusDM;
        private double[] tr;
        private double[] smoothedPlusDM;
        private double[] smoothedMinusDM;
        private double[] smoothedTR;
        private double[] dx;

        public ADX(Brush color) : base(color)
        {
            Periods = 14;
        }

        public ADX(int periods, int smoothingPeriods, Brush color) : base(color)
        {
            Periods = periods;
            SmoothingPeriods = smoothingPeriods;
        }

        public ADX(int periods, int smoothingPeriods, CandleSet candleSet_, Brush color) : base(color)
        {
            Periods = periods;
            SmoothingPeriods = smoothingPeriods;
            Calculate(candleSet_);
        }

        public override int PrependCandlesNeeded => Periods * 12; // Empirical rule for accuracy

        public override string StudyDescription() => $"ADX({Periods},{SmoothingPeriods})";

        public override string StudyToolTip() => $"{StudyDescription()} - Average Directional Index";

        public override void Calculate(CandleSet candleSet_)
        {
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            candleSet = candleSet_;
            List<Candle> candles = candleSet_.HeikinAshiCandles != null ? candleSet_.HeikinAshiCandles : candleSet.Candles;
            candlesLastUsed = candles;

            if (candles.Count < Periods + 1)
            {
                Values = new double[candles.Count];
                DIPlus = new double[candles.Count];
                DIMinus = new double[candles.Count];
                plusDM = new double[candles.Count];
                minusDM = new double[candles.Count];
                tr = new double[candles.Count];
                smoothedPlusDM = new double[candles.Count];
                smoothedMinusDM = new double[candles.Count];
                smoothedTR = new double[candles.Count];
                dx = new double[candles.Count];
                DecimalFormat = "N2";
                return;
            }

            // Warn if insufficient candles
            if (candles.Count < Periods * 12 && candleSet.Symbol == "SPY")
            {
                // Debug.WriteLine($"Warning: {candles.Count} candles provided, but {Periods * 12} recommended for accurate ADX({Periods}).");
            }

            Values = new double[candles.Count];
            DIPlus = new double[candles.Count];
            DIMinus = new double[candles.Count];
            plusDM = new double[candles.Count];
            minusDM = new double[candles.Count];
            tr = new double[candles.Count];
            smoothedPlusDM = new double[candles.Count];
            smoothedMinusDM = new double[candles.Count];
            smoothedTR = new double[candles.Count];
            dx = new double[candles.Count];
            DecimalFormat = "N2";

            // Calculate DM and TR
            for (int i = 1; i < candles.Count; i++)
            {
                double highMove = candles[i].High - candles[i - 1].High;
                double lowMove = candles[i - 1].Low - candles[i].Low;

                plusDM[i] = highMove > lowMove && highMove > 0 ? highMove : 0;
                minusDM[i] = lowMove > highMove && lowMove > 0 ? lowMove : 0;

                double tr1 = candles[i].High - candles[i].Low;
                double tr2 = Math.Abs(candles[i].High - candles[i - 1].Close);
                double tr3 = Math.Abs(candles[i].Low - candles[i - 1].Close);
                tr[i] = Math.Max(tr1, Math.Max(tr2, tr3));
            }

            // First period sums (sum from i=1 to i=Periods, store at Periods)
            // ---
            // ... (previous code unchanged until smoothing)

            // First period sums (sum from i=1 to i=Periods, store at Periods)
            for (int i = 1; i <= Periods; i++)
            {
                smoothedPlusDM[Periods] += plusDM[i];
                smoothedMinusDM[Periods] += minusDM[i];
                smoothedTR[Periods] += tr[i];
            }

            // Smooth remaining periods using Periods (not SmoothingPeriods)
            for (int i = Periods + 1; i < candles.Count; i++)
            {
                smoothedPlusDM[i] = (smoothedPlusDM[i - 1] * (Periods - 1) + plusDM[i]) / Periods;
                smoothedMinusDM[i] = (smoothedMinusDM[i - 1] * (Periods - 1) + minusDM[i]) / Periods;
                smoothedTR[i] = (smoothedTR[i - 1] * (Periods - 1) + tr[i]) / Periods;
            }

            // Calculate DI and DX
            for (int i = Periods; i < candles.Count; i++)
            {
                DIPlus[i] = smoothedTR[i] > 0.000001 ? (smoothedPlusDM[i] / smoothedTR[i]) * 100 : 0;
                DIMinus[i] = smoothedTR[i] > 0.000001 ? (smoothedMinusDM[i] / smoothedTR[i]) * 100 : 0;
                double diSum = DIPlus[i] + DIMinus[i];
                double diDiff = Math.Abs(DIPlus[i] - DIMinus[i]);
                dx[i] = diSum > 0.000001 ? (diDiff / diSum) * 100 : 0;
            }

            // Calculate ADX using SmoothingPeriods
            if (candles.Count >= Periods + SmoothingPeriods)
            {
                double sumDX = 0;
                for (int i = Periods; i < Periods + SmoothingPeriods; i++)
                {
                    sumDX += dx[i];
                }
                Values[Periods + SmoothingPeriods - 1] = sumDX / SmoothingPeriods;

                for (int i = Periods + SmoothingPeriods; i < candles.Count; i++)
                {
                    Values[i] = (Values[i - 1] * (SmoothingPeriods - 1) + dx[i]) / SmoothingPeriods;
                }
            }

            // Fill early values
            for (int i = 0; i < Periods + SmoothingPeriods - 1 && i < Values.Length; i++)
            {
                Values[i] = 0;
                DIPlus[i] = 0;
                DIMinus[i] = 0;
            }

        }

        public void CalculateLast2(CandleSet candleSet_)
        {
            List<Candle> candles;
            if (candleSet_.HeikinAshiCandles != null)
            {
                candleSet_.PopulateHeikinAshiCandlesLast2();
                candles = candleSet_.HeikinAshiCandles;
            }
            else
                candles = candleSet_.Candles;

            if (candleSet_ != candleSet || candles != candlesLastUsed || Periods != PeriodsLastCalculated || Values == null)
            {
                Calculate(candleSet_);
                return;
            }

            if (candles.Count < Periods + 1)
            {
                if (Values.Length != candles.Count)
                {
                    Array.Resize(ref Values, candles.Count);
                    Array.Resize(ref DIPlus, candles.Count);
                    Array.Resize(ref DIMinus, candles.Count);
                    Array.Resize(ref plusDM, candles.Count);
                    Array.Resize(ref minusDM, candles.Count);
                    Array.Resize(ref tr, candles.Count);
                    Array.Resize(ref smoothedPlusDM, candles.Count);
                    Array.Resize(ref smoothedMinusDM, candles.Count);
                    Array.Resize(ref smoothedTR, candles.Count);
                    Array.Resize(ref dx, candles.Count);
                }
                for (int i = 0; i < candles.Count; i++)
                {
                    Values[i] = 0;
                    DIPlus[i] = 0;
                    DIMinus[i] = 0;
                }
                TimeLastCalculated = DateTime.Now;
                return;
            }

            int lastIdx = candles.Count - 1;
            int secondLastIdx = lastIdx - 1;

            if (Values.Length != candles.Count)
            {
                Array.Resize(ref Values, candles.Count);
                Array.Resize(ref DIPlus, candles.Count);
                Array.Resize(ref DIMinus, candles.Count);
                Array.Resize(ref plusDM, candles.Count);
                Array.Resize(ref minusDM, candles.Count);
                Array.Resize(ref tr, candles.Count);
                Array.Resize(ref smoothedPlusDM, candles.Count);
                Array.Resize(ref smoothedMinusDM, candles.Count);
                Array.Resize(ref smoothedTR, candles.Count);
                Array.Resize(ref dx, candles.Count);
            }

            // Calculate DM and TR for the last two candles
            for (int i = secondLastIdx; i <= lastIdx; i++)
            {
                double highMove = candles[i].High - candles[i - 1].High;
                double lowMove = candles[i - 1].Low - candles[i].Low;

                plusDM[i] = highMove > lowMove && highMove > 0 ? highMove : 0;
                minusDM[i] = lowMove > highMove && lowMove > 0 ? lowMove : 0;

                double tr1 = candles[i].High - candles[i].Low;
                double tr2 = Math.Abs(candles[i].High - candles[i - 1].Close);
                double tr3 = Math.Abs(candles[i].Low - candles[i - 1].Close);
                tr[i] = Math.Max(tr1, Math.Max(tr2, tr3));

                // Update smoothed values using Periods
                if (i >= Periods)
                {
                    smoothedPlusDM[i] = (smoothedPlusDM[i - 1] * (Periods - 1) + plusDM[i]) / Periods;
                    smoothedMinusDM[i] = (smoothedMinusDM[i - 1] * (Periods - 1) + minusDM[i]) / Periods;
                    smoothedTR[i] = (smoothedTR[i - 1] * (Periods - 1) + tr[i]) / Periods;

                    // Calculate DI and DX
                    DIPlus[i] = smoothedTR[i] > 0.000001 ? (smoothedPlusDM[i] / smoothedTR[i]) * 100 : 0;
                    DIMinus[i] = smoothedTR[i] > 0.000001 ? (smoothedMinusDM[i] / smoothedTR[i]) * 100 : 0;
                    double diSum = DIPlus[i] + DIMinus[i];
                    double diDiff = Math.Abs(DIPlus[i] - DIMinus[i]);
                    dx[i] = diSum > 0.000001 ? (diDiff / diSum) * 100 : 0;

                    // Update ADX if past initial period
                    if (i >= Periods + SmoothingPeriods - 1)
                    {
                        Values[i] = (Values[i - 1] * (SmoothingPeriods - 1) + dx[i]) / SmoothingPeriods;
                    }
                    else
                    {
                        Values[i] = 0;
                    }
                }
            }

            TimeLastCalculated = DateTime.Now;
        }

        public override string DisplayValue(int idx)
        {
            return $"{this[idx].ToString(DecimalFormat)}, DI+ {DIPlus[idx].ToString("N2")}, DI- {DIMinus[idx].ToString("N2")}";
        }

        public override void Draw(ZpmPriceCharts.PriceChart chart)
        {
            UiElements.Clear();
            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;

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
