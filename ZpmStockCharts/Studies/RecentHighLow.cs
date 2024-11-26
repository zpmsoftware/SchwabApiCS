// <copyright file="RecentHighLow.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace Studies
{
    public class RecentHighLow : Study
    {
        private List<Candle> candles;
        private List<int> highCandle;
        private List<int> lowCandle;

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

        public override void Caclulate(CandleSet candleSet)
        {
            candles = candleSet.Candles;
            highCandle = new List<int>();
            lowCandle = new List<int>();

            DateTime breakPt = new DateTime(2023, 3, 21);

            // calculate
            int minCandles = 3;
            for (int x = minCandles; x < candles.Count-minCandles; x++)
            {
                if (candles[x].DateTime == breakPt) {
                    var xx = 1;
                }

                if (candles[x].Low < candles[x-1].Low && candles[x].Low < candles[x - 2].Low && candles[x].Low < candles[x - 3].Low
                    && candles[x].Low < candles[x + 1].Low && candles[x].Low < candles[x + 2].Low && candles[x].Low < candles[x + 3].Low) 
                    lowCandle.Add(x);
                if (candles[x].High > candles[x - 1].High && candles[x].High > candles[x - 2].High && candles[x].High > candles[x - 3].High
                    && candles[x].High > candles[x + 1].High && candles[x].High > candles[x + 2].High && candles[x].High > candles[x + 3].High)
                    highCandle.Add(x);

            }
        }

        public override void Draw(PriceChart chart)
        {
            UiElements.Clear();
            if (chart.NbrCandles > 0)
            {
                int startCandle = chart.StartCandle + chart.Cset.StartTimeIndex;  // starting study index

                foreach (var lb in lowCandle)
                {
                    if (lb >= startCandle && lb < startCandle + chart.NbrCandles)
                    {
                        var b = lb - startCandle;
                        var ln = chart.ChartLine(Color, 3);
                        ln.X1 = chart.ChartCandleLeft(b);
                        ln.X2 = chart.ChartCandleLeft(b + 1) - 1;

                        ln.Y1 = chart.CalcTop(candles[lb].Low) + 4;
                        ln.Y2 = ln.Y1;
                        UiElements.Add(ln);
                    }
                }

                foreach (var hb in highCandle)
                {
                    if (hb >= startCandle && hb < startCandle + chart.NbrCandles)
                    {
                        var b = hb - startCandle;
                        var ln = chart.ChartLine(Color, 3);
                        ln.X1 = chart.ChartCandleLeft(b);
                        ln.X2 = chart.ChartCandleLeft(b + 1) - 1;

                        ln.Y1 = chart.CalcTop(candles[hb].High) - 4;
                        ln.Y2 = ln.Y1;
                        UiElements.Add(ln);
                    }
                }
            }
        }
    }
}
