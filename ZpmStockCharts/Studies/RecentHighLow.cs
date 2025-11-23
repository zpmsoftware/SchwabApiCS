// <copyright file="RecentHighLow.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class RecentHighLow : Study
    {
        private List<Candle> candles;
        public List<int> HighCandle;
        public List<int> LowCandle;
        public int TentativeHighCandle = 0;
        public int TentativeLowCandle = 0;

        public RecentHighLow(Brush color)
            : base(color)
        {
        }

        public override int PrependCandlesNeeded { get { return 3; } } // required to get a good value for the firsst chart candle

        public override string StudyDescription()
        {
            return "High/Low";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - mark meaningful highs and lows.";
        }

        public override void Calculate(CandleSet candleSet)
        {
            TimeLastCalculated = DateTime.Now;
            candles = candleSet.Candles;
            HighCandle = new List<int>();
            LowCandle = new List<int>();
            int x;

            DateTime breakPt = new DateTime(2023, 3, 21);

            // calculate
            int minCandles = 3;
            for (x = minCandles; x < candles.Count - minCandles; x++)
            {
                if (candles[x].Low < candles[x - 1].Low && candles[x].Low < candles[x - 2].Low && candles[x].Low < candles[x - 3].Low
                    && candles[x].Low <= candles[x + 1].Low && candles[x].Low <= candles[x + 2].Low && candles[x].Low <= candles[x + 3].Low)
                    AddLowCandle(x, candles);

                if (candles[x].High > candles[x - 1].High && candles[x].High > candles[x - 2].High && candles[x].High > candles[x - 3].High
                    && candles[x].High >= candles[x + 1].High && candles[x].High >= candles[x + 2].High && candles[x].High >= candles[x + 3].High)
                    AddHighCandle(x, candles);

                CalculateTentative();

            }
        }

        private void CalculateTentative()
        {
            if (LowCandle.Count > 0)
            {
                TentativeLowCandle = 0;
                int x = LowCandle[LowCandle.Count - 1];
                var low = candles[x].Low;
                for (x = x + 1; x < candles.Count; x++)
                {
                    if (candles[x].Low < low)
                    {
                        TentativeLowCandle = x;
                        low = candles[x].Low;
                    }
                }
            }

            if (HighCandle.Count > 0)
            {

                TentativeHighCandle = 0;
                int x = HighCandle[HighCandle.Count - 1];
                var high = candles[x].High;
                for (x = x + 1; x < candles.Count; x++)
                {
                    if (candles[x].High > high)
                    {
                        TentativeHighCandle = x;
                        high = candles[x].High;
                    }
                }
            }
        }

        public override void CalculateLast2(CandleSet candleSet_)
        {
            if (candles == null)
            {
                Calculate(candleSet_);
                return;
            }

            if (candles.Count < 7)
                return; // Not enough data

            // Update reference
            candles = candleSet_.Candles;
            int count = candles.Count;

            // We only need to check the last few candles: from count-6 to count-3
            // Because a candle at index i needs i-3 to i+3 to validate
            int startIdx = Math.Max(3, count - 6); // Ensure we don't go below 3
            int endIdx = count - 4;               // Last index we can fully validate (±3)

            // Step 1: Remove any existing high/low markers in the re-evaluation zone
            // These might now be invalid due to new candles
            //HighCandle.RemoveAll(idx => idx >= startIdx && idx <= endIdx);
            //LowCandle.RemoveAll(idx => idx >= startIdx && idx <= endIdx);

            // Step 2: Re-evaluate only the candles that can now be confirmed
            for (int x = startIdx; x <= endIdx; x++)
            {
                // Check for Low
                if (candles[x].Low < candles[x - 1].Low && candles[x].Low < candles[x - 2].Low && candles[x].Low < candles[x - 3].Low &&
                    candles[x].Low <= candles[x + 1].Low && candles[x].Low <= candles[x + 2].Low && candles[x].Low <= candles[x + 3].Low)
                {
                    if (LowCandle.Count > 0 && x > LowCandle.Last()) // check for duplicate
                        AddLowCandle(x, candles);
                }

                // Check for High
                if (candles[x].High > candles[x - 1].High && candles[x].High > candles[x - 2].High && candles[x].High > candles[x - 3].High &&
                    candles[x].High >= candles[x + 1].High && candles[x].High >= candles[x + 2].High && candles[x].High >= candles[x + 3].High)
                {
                    if (HighCandle.Count > 0 && x > HighCandle.Last()) // check for duplicate
                        AddHighCandle(x, candles);
                }
            }

            CalculateTentative();
            TimeLastCalculated = DateTime.Now;
        }

        private void AddLowCandle(int x, List<Candle> candles)
        {
            var lowIdx = LowCandle.Count - 1;
            var highIdx = HighCandle.Count - 1;

            /* for testing specific cases
            if (candleSet_.Symbol == "HOOD")
            {
                if (candleSet_.FrequencyType.FrequencyTypeId == CandleSet.FrequencyTypes.Minute1)
                {
                    if (candles[x].DateTime >= new DateTime(2025, 11, 7, 16, 14, 0))
                    {}
                }
            } */

            if (lowIdx >= 0 && highIdx >= 0)
            {
                if (LowCandle[lowIdx] > HighCandle[highIdx]) // last was low, don't want two in a row
                {
                    if (candles[x].Low < candles[LowCandle[lowIdx]].Low) // replace if lower
                        LowCandle[lowIdx] = x;
                    return;
                }
            }
            LowCandle.Add(x);
        }

        private void AddHighCandle(int x, List<Candle> candles)
        {
            var lowIdx = LowCandle.Count - 1;
            var highIdx = HighCandle.Count - 1;
            if (lowIdx >= 0 && highIdx >= 0)
            {
                if (HighCandle[highIdx] > LowCandle[lowIdx]) // last was high, don't want two in a row
                {
                    if (candles[x].High > candles[HighCandle[highIdx]].High) // replace if higher
                        HighCandle[highIdx] = x;
                    return;
                }
            }
            HighCandle.Add(x);
        }

        public override void Draw(PriceChart chart)
        {
            UiElements.Clear();
            if (chart.NbrCandles > 0)
            {
                int startCandle = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index

                foreach (var lb in LowCandle)
                {
                    if (lb >= startCandle && lb < startCandle + chart.NbrCandles)
                    {
                        DrawMark(chart, startCandle, lb, false);
                        /*
                        var b = lb - startCandle;
                        var ln = chart.ChartLine(Color, 3);
                        ln.X1 = chart.ChartCandleLeft(b);
                        ln.X2 = chart.ChartCandleLeft(b + 1) - 1;

                        ln.Y1 = chart.CalcTop(candles[lb].Low) + 4;
                        ln.Y2 = ln.Y1;
                        UiElements.Add(ln);
                        */
                    }
                }
                if (TentativeLowCandle > 0)
                    DrawMark(chart, startCandle, TentativeLowCandle, false);

                foreach (var hb in HighCandle)
                {
                    if (hb >= startCandle && hb < startCandle + chart.NbrCandles)
                    {
                        DrawMark(chart, startCandle, hb, true);
                        /*
                        var b = hb - startCandle;
                        var ln = chart.ChartLine(Color, 3);
                        ln.X1 = chart.ChartCandleLeft(b);
                        ln.X2 = chart.ChartCandleLeft(b + 1) - 1;

                        ln.Y1 = chart.CalcTop(candles[hb].High) - 4;
                        ln.Y2 = ln.Y1;
                        UiElements.Add(ln);
                        */
                    }
                }
                if (TentativeHighCandle > 0)
                    DrawMark(chart, startCandle, TentativeHighCandle, true);
            }
        }

        private void DrawMark(PriceChart chart, int startCandle, int candle, bool high)
        {
            var b = candle - startCandle;
            var ln = chart.ChartLine(Color, 3);
            ln.X1 = chart.ChartCandleLeft(b);
            ln.X2 = chart.ChartCandleLeft(b + 1) - 1;

            ln.Y1 = high ? chart.CalcTop(candles[candle].High) - 4 : chart.CalcTop(candles[candle].Low) + 4;
            ln.Y2 = ln.Y1;
            UiElements.Add(ln);
        }
    }
}
