using System;
using System.Windows.Media;
using System.Collections.Generic;

namespace ZpmPriceCharts.Studies
{
    public class ADX : Study
    {
        public int Periods { get; set; }
        public double[] DIPlus { get; private set; }
        public double[] DIMinus { get; private set; }
        private CandleSet candleSet;
        private int PeriodsLastCalculated = 0;

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

        public override int PrependCandlesNeeded => Periods * 2;

        public override string StudyDescription() => $"ADX({Periods})";

        public override string StudyToolTip() => $"{StudyDescription()} - Average Directional Index";

        public override void Calculate(CandleSet candleSet_)
        {
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            candleSet = candleSet_;
            List<Candle> candles = candleSet.Candles;

            if (candles.Count < Periods + 1)
            {
                Values = new double[candles.Count];
                DIPlus = new double[candles.Count];
                DIMinus = new double[candles.Count];
                DecimalFormat = "N2";
                return;
            }

            Values = new double[candles.Count];
            DIPlus = new double[candles.Count];
            DIMinus = new double[candles.Count];
            DecimalFormat = "N2";

            double[] plusDM = new double[candles.Count];
            double[] minusDM = new double[candles.Count];
            double[] tr = new double[candles.Count];
            double[] smoothedPlusDM = new double[candles.Count];
            double[] smoothedMinusDM = new double[candles.Count];
            double[] smoothedTR = new double[candles.Count];
            double[] dx = new double[candles.Count];

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

            // First period sums
            for (int i = 1; i <= Periods; i++)
            {
                smoothedPlusDM[Periods] += plusDM[i];
                smoothedMinusDM[Periods] += minusDM[i];
                smoothedTR[Periods] += tr[i];
            }

            // Smooth remaining periods
            for (int i = Periods + 1; i < candles.Count; i++)
            {
                smoothedPlusDM[i] = (smoothedPlusDM[i - 1] * (Periods - 1) + plusDM[i]) / Periods;
                smoothedMinusDM[i] = (smoothedMinusDM[i - 1] * (Periods - 1) + minusDM[i]) / Periods;
                smoothedTR[i] = (smoothedTR[i - 1] * (Periods - 1) + tr[i]) / Periods;
            }

            // Calculate DI and DX
            for (int i = Periods; i < candles.Count; i++)
            {
                DIPlus[i] = smoothedTR[i] != 0 ? (smoothedPlusDM[i] / smoothedTR[i]) * 100 : 0;
                DIMinus[i] = smoothedTR[i] != 0 ? (smoothedMinusDM[i] / smoothedTR[i]) * 100 : 0;
                double diSum = DIPlus[i] + DIMinus[i];
                double diDiff = Math.Abs(DIPlus[i] - DIMinus[i]);
                dx[i] = diSum != 0 ? (diDiff / diSum) * 100 : 0;
            }

            // Calculate ADX
            if (candles.Count >= Periods * 2)
            {
                double sumDX = 0;
                for (int i = Periods; i < Periods * 2; i++)
                {
                    sumDX += dx[i];
                }
                Values[Periods * 2 - 1] = sumDX / Periods;

                for (int i = Periods * 2; i < candles.Count; i++)
                {
                    Values[i] = (Values[i - 1] * (Periods - 1) + dx[i]) / Periods;
                }
            }

            // Fill early values
            for (int i = 0; i < Periods * 2 - 1; i++)
            {
                Values[i] = 0;
                DIPlus[i] = 0;
                DIMinus[i] = 0;
            }
        }

        public override string DisplayValue(int idx)
        {
            return $"{this[idx].ToString(DecimalFormat)}, DI+ {DIPlus[idx].ToString("N2")}, DI- {DIMinus[idx].ToString("N2")}";
        }

        // Draw method remains largely the same, just update property names
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
