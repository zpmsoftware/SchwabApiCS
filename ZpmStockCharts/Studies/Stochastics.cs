using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class Stochastics : Study
    {
        public Stochastics_Values StochasticsValues { get; set; }

        public Stochastics(int kPeriods, int dPeriods, int kSmoothing, int method, Brush color)
            : base(color)
        {
            StochasticsValues = new Stochastics_Values(kPeriods, dPeriods, kSmoothing, method);
        }

        public override string StudyDescription() => StochasticsValues.ToString();
        public override string StudyToolTip() => StudyDescription() + " - Stochastics.";

        public override int PrependCandlesNeeded =>
            Math.Max(StochasticsValues.KPeriods + StochasticsValues.KSmoothing + StochasticsValues.DPeriods - 2, 0);

        public override void Calculate(CandleSet cs)
        {
            StochasticsValues.Calculate(cs.Candles);
            this.Values = StochasticsValues.K;
        }

        public override void CalculateLast2(CandleSet candleSet_)
        {
            StochasticsValues.CalculateLast2(candleSet_.Candles);
            this.Values = StochasticsValues.K;
        }

        public override string DisplayValue(int idx)
        {
            return $"K {StochasticsValues.K[idx]:N2}, D {StochasticsValues.D[idx]:N2}";
        }

        public override void Draw(PriceChart chart)
        {
            UiElements.Clear();
            if (StochasticsValues.K == null || StochasticsValues.D == null) return;

            int startIdx = chart.StartCandle + chart.Cset.StartTimeIndex;
            var k = StochasticsValues.K;
            var d = StochasticsValues.D;

            if (startIdx >= k.Length) return;

            // Horizontal 30/70 lines
            double y70 = chart.LAxisPosition(70, 100);
            double y30 = chart.LAxisPosition(30, 100);
            UiElements.Add(chart.ChartLine(Brushes.Gray, 0, chart.ChartArea.ActualWidth, y70, y70, 0.5));
            UiElements.Add(chart.ChartLine(Brushes.Gray, 0, chart.ChartArea.ActualWidth, y30, y30, 0.5));

            var perUnit = (chart.ChartArea.ActualHeight + 200) / 100.0;
            double x1 = chart.ChartCandleCenter(0);
            double ky1 = (100 - k[startIdx]) * perUnit - 100;
            double dy1 = (100 - d[startIdx]) * perUnit - 100;

            for (int x = 0; x + startIdx < k.Length && x < chart.NbrCandles; x++)
            {
                int idx = x + startIdx;

                // %K line
                var lnK = chart.ChartLine(Color);
                lnK.X1 = x1; lnK.Y1 = ky1;
                lnK.X2 = chart.ChartCandleCenter(x);
                lnK.Y2 = chart.LAxisPosition(k[idx], 100);
                UiElements.Add(lnK);

                // %D line (yellow)
                var lnD = chart.ChartLine(Brushes.Yellow);
                lnD.X1 = x1; lnD.Y1 = dy1;
                lnD.X2 = lnK.X2;
                lnD.Y2 = chart.LAxisPosition(d[idx], 100);
                UiElements.Add(lnD);

                x1 = lnK.X2;
                ky1 = lnK.Y2;
                dy1 = lnD.Y2;
            }
        }

        public class Stochastics_Values
        {
            public int KPeriods { get; }
            public int DPeriods { get; }
            public int KSmoothing { get; }
            public int Method { get; } // 0 = SMA, 1 = EMA

            public double[] RawK;
            public double[] K;
            public double[] D;
            public int usedLength;

            public Stochastics_Values(int kPeriods, int dPeriods, int kSmoothing, int method)
            {
                KPeriods = kPeriods;
                DPeriods = dPeriods;
                KSmoothing = kSmoothing;
                Method = method;
            }

            public override string ToString()
                => $"Stoch({KPeriods},{DPeriods},{KSmoothing},{(Method == 0 ? "SMA" : "EMA")})";

            // ——————————————————— Moving Average Helper ———————————————————
            private double MovingAverageUpdate(double[] source, int index, int period, double prevAvg)
            {
                if (Method == 0) // SMA
                {
                    int start = Math.Max(0, index - period + 1);
                    double sum = 0;
                    for (int i = start; i <= index; i++) sum += source[i];
                    return sum / (index - start + 1);
                }
                else // EMA
                {
                    double alpha = 2.0 / (period + 1);
                    return index == 0 ? source[0] : alpha * source[index] + (1 - alpha) * prevAvg;
                }
            }

            // ——————————————————— Full Recalculation ———————————————————
            public void Calculate(List<Candle> candles)
            {
                if (candles == null || candles.Count == 0) return;

                int len = candles.Count;

                // Always create fresh arrays → guaranteed initialized
                RawK = new double[len];
                K = new double[len];
                D = new double[len];
                usedLength = len;

                // ... RawK calculation unchanged ...

                for (int i = 0; i < len; i++)
                {
                    int start = Math.Max(0, i - KPeriods + 1);
                    double highestHigh = double.MinValue;
                    double lowestLow = double.MaxValue;

                    for (int j = start; j <= i; j++)
                    {
                        highestHigh = Math.Max(highestHigh, candles[j].High);
                        lowestLow = Math.Min(lowestLow, candles[j].Low);
                    }

                    double range = highestHigh - lowestLow;
                    RawK[i] = range <= 0 ? 50.0 : 100.0 * (candles[i].Close - lowestLow) / range;
                }

                for (int i = 0; i < len; i++)
                {
                    K[i] = KSmoothing <= 1
                        ? RawK[i]
                        : MovingAverageUpdate(RawK, i, KSmoothing, i > 0 ? K[i - 1] : RawK[i]);

                    D[i] = DPeriods <= 1
                        ? K[i]
                        : MovingAverageUpdate(K, i, DPeriods, i > 0 ? D[i - 1] : K[i]);
                }
            }


            // ——————————————————— Real-Time Incremental Update (every tick) ———————————————————
            public void CalculateLast2(List<Candle> candles)
            {
                if (candles?.Count < 1) return;

                int len = candles.Count;

                // Full recalc on first run or if data shrank drastically
                if (RawK == null || usedLength > len || len < KPeriods + KSmoothing + DPeriods)
                {
                    Calculate(candles);
                    return;
                }

                int oldLength = RawK.Length;

                // Resize arrays if new bars were added
                if (oldLength < len)
                {
                    int newlength = len + 50;
                    Array_Resize(ref RawK, ref K, ref D, newlength); // sets new values to 0
                }

                // Recalculate RawK only for the affected recent window
                int window = KPeriods + KSmoothing + DPeriods + 5;
                int from = Math.Max(0, len - window);

                int earliestChanged = len;

                for (int i = from; i < len; i++)
                {
                    int lookback = Math.Max(0, i - KPeriods + 1);
                    double hh = double.MinValue, ll = double.MaxValue;
                    for (int j = lookback; j <= i; j++)
                    {
                        hh = Math.Max(hh, candles[j].High);
                        ll = Math.Min(ll, candles[j].Low);
                    }

                    double range = hh - ll;
                    double newRawK = range <= 0 ? 50.0 : 100.0 * (candles[i].Close - ll) / range;

                    if (Math.Abs(newRawK - RawK[i]) > 1e-12)
                        earliestChanged = Math.Min(earliestChanged, i);

                    RawK[i] = newRawK;
                }

                if (earliestChanged == len)
                    earliestChanged = Math.Max(0, len - 2); // force at least last two

                int smoothFrom = Math.Max(0, earliestChanged - Math.Max(KSmoothing, DPeriods));

                for (int i = smoothFrom; i < len; i++)
                {
                    K[i] = KSmoothing <= 1
                        ? RawK[i]
                        : MovingAverageUpdate(RawK, i, KSmoothing, i > 0 ? K[i - 1] : RawK[i]);

                    D[i] = DPeriods <= 1
                        ? K[i]
                        : MovingAverageUpdate(K, i, DPeriods, i > 0 ? D[i - 1] : K[i]);
                }
            }

            public int Cross(int idx)
            {
                if (idx <= 0 || idx >= K.Length) return 0;
                bool wasAbove = K[idx - 1] >= D[idx - 1];
                bool nowAbove = K[idx] > D[idx];
                return nowAbove && !wasAbove ? 1 : !nowAbove && wasAbove ? -1 : 0;
            }
        }
    }
}