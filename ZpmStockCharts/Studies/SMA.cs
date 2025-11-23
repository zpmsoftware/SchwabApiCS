// <copyright file="SMA.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class SMA : Study
    {
        // Simple Moving Average

        public int Periods { get; set; }
        private CandleSet candleSet;
        private List<Candle> candlesLastUsed; // Added to track last used candles
        private int PeriodsLastCalculated = 0;

        /// <summary>
        /// SmaType: C=Close (default), H=High, L=Low, O=Open, V=Volume, HL=High Low mid point, OC=Open Close mid point
        /// </summary>
        public string SmaType { get; init; }

        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the first chart candle

        /// <summary>
        /// SMA - Simple Moving Average on closing price
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="color"></param>
        public SMA(int periods, Brush color)
            : base(color)
        {
            Periods = periods;
            SmaType = "C";
            UseRightAxis = true;
        }

        /// <summary>
        /// SMA - Simple Moving Average
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="smaType">SmaType: C=Close (default), H=High, L=Low, O=Open, V=Volume, HL=High Low mid point, OC=Open Close mid point</param>
        /// <param name="color"></param>
        public SMA(int periods, string smaType, Brush color)
            : base(color)
        {
            Periods = periods;
            SmaType = smaType.ToUpper();
            UseRightAxis = true;
        }

        public override string StudyDescription()
        {
            return $"SMA({Periods},{SmaType})";
        }

        public override string StudyToolTip() // longer description
        {
            string typeDesc = SmaType switch
            {
                "C" => "Close",
                "H" => "High",
                "L" => "Low",
                "O" => "Open",
                "V" => "Volume",
                "HL" => "High-Low Midpoint",
                "OC" => "Open-Close Midpoint",
                _ => "Close"
            };
            return $"{StudyDescription()} - Simple Moving Average on {typeDesc}";
        }

        public override void Calculate(CandleSet candleSet_)
        {
            Values = Calculate(candleSet_, Periods, SmaType);
            TimeLastCalculated = DateTime.Now;
            PeriodsLastCalculated = Periods;
            candleSet = candleSet_;
        }

        public static double[] Calculate(CandleSet candleSet, int periods, string smaType)
        {
            List<Candle> candles = candleSet.HeikinAshiCandles != null ? candleSet.HeikinAshiCandles : candleSet.Candles; // Use Heikin-Ashi if available
            var values = new double[candles.Count];

            if (periods <= 0) return values;

            smaType = smaType.ToUpper();

            // calculate average
            values[0] = GetCandleValue(candles[0], smaType);
            for (int x = 1; x < candles.Count; x++)
            { // build totals
                if (x >= periods)
                    values[x] = values[x - 1] + GetCandleValue(candles[x], smaType) - GetCandleValue(candles[x - periods], smaType);
                else
                    values[x] = values[x - 1] + GetCandleValue(candles[x], smaType);
            }
            for (int x = 1; x < candles.Count; x++)
            { // get average
                if (x >= periods)
                    values[x] = Math.Round(values[x] / periods, 2);
                else
                    values[x] = Math.Round(values[x] / (x + 1), 2);
            }

            return values;
        }

        public override void CalculateLast2(CandleSet candleSet_)
        {
            List<Candle> candles;
            if (candleSet_.HeikinAshiCandles != null)
            {
                candleSet_.PopulateHeikinAshiCandlesLast2();
                candles = candleSet_.HeikinAshiCandles;
            }
            else
            {
                candles = candleSet_.Candles;
            }

            // Check if we need to recalculate everything
            if (candleSet_ != candleSet || candles != candlesLastUsed || Periods != PeriodsLastCalculated || Values == null)
            {
                Calculate(candleSet_);
                return;
            }

            // Handle case with insufficient candles
            if (candles.Count < Periods)
            {
                if (Values.Length != candles.Count)
                {
                    Array_Resize(ref Values, candles.Count);
                }
                TimeLastCalculated = DateTime.Now;
                candlesLastUsed = candles;
                return;
            }

            // Resize Values array if candle count changed
            if (Values.Length != candles.Count)
            {
                Array_Resize(ref Values, candles.Count);
            }

            // Calculate SMA for the last two candles
            int lastIdx = candles.Count - 1;
            int secondLastIdx = lastIdx - 1;

            for (int i = Math.Max(Periods - 1, secondLastIdx); i <= lastIdx; i++)
            {
                double sum = 0;
                int count = Math.Min(Periods, i + 1);
                for (int j = i; j > i - count; j--)
                {
                    sum += GetCandleValue(candles[j], SmaType);
                }
                Values[i] = Math.Round(sum / count, 2);
            }

            // Ensure earlier values are zero if necessary
            for (int i = secondLastIdx; i <= lastIdx; i++)
            {
                if (i < Periods - 1)
                {
                    Values[i] = 0;
                }
            }

            TimeLastCalculated = DateTime.Now;
            candlesLastUsed = candles;
        }

        private static double GetCandleValue(Candle candle, string smaType)
        {
            return smaType switch
            {
                "H" => candle.High,
                "L" => candle.Low,
                "O" => candle.Open,
                "V" => candle.Volume,
                "HL" => (candle.High + candle.Low) / 2.0,
                "OC" => (candle.Open + candle.Close) / 2.0,
                _ => candle.Close // Default to Close
            };
        }

        /// <summary>
        /// Calculate SMA on an array of data, not a candle set.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="periods"></param>
        /// <returns></returns>
        public static double[] Calculate(double[] data, int periods)
        {
            var values = new double[data.Length];

            if (periods > 0)
            {
                // calculate average
                values[0] = data[0];
                for (int x = 1; x < data.Length; x++)
                { // build totals
                    if (x >= periods)
                        values[x] = values[x - 1] + data[x] - data[x - periods];  // add one in, take one out
                    else
                        values[x] = values[x - 1] + data[x];
                }
                for (int x = 1; x < data.Length; x++)
                { // get average
                    if (x >= periods)
                        values[x] = Math.Round(values[x] / periods, 2);
                    else
                        values[x] = Math.Round(values[x] / (x + 1), 2);
                }
            }
            return values;
        }

        public override void Draw(ZpmPriceCharts.PriceChart chart)
        {
            if (SmaType == "V")
            {
                UiElements.Clear();
            }
            else
            {
                base.Draw(chart);
            }
        }
    }
}
