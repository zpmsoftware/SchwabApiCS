// <copyright file="EMA.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using ZpmPriceCharts;
using System.Windows.Media;

namespace ZpmPriceCharts.Studies
{
    public class EMA : Study
    {
        // Exponential Moving Average

        public int Periods { get; private set; }
        public override int PrependCandlesNeeded { get { return Periods - 1; } } // required to get a good value for the first chart candle

        /// <summary>
        /// SMA - Exponential Moving Average on closing price
        /// </summary>
        /// <param name="periods"></param>
        /// <param name="color"></param>
        public EMA(int periods, Brush color)
            : base(color)
        {
            Periods = periods;
            UseRightAxis = true;
        }

        public override string StudyDescription()
        {
            return "EMA(" + Periods.ToString() + ")";
        }

        public override string StudyToolTip() // longer description
        {
            return StudyDescription() + " - exponential moving average.";
        }

        public override void Calculate(CandleSet candleSet)
        {
            Calculate(candleSet, Periods);
        }

        public void Calculate(CandleSet candleSet, int periods)
        {
            TimeLastCalculated = DateTime.Now;
            List<Candle> candles = candleSet.Candles;

            // Edge case checks
            if (candles == null || candles.Count == 0)
            {
                Values = Array.Empty<double>();
                return;
            }
            if (periods <= 0)
            {
                throw new ArgumentException("Periods must be greater than 0.", nameof(periods));
            }
            if (candles.Count < periods)
            {
                throw new ArgumentException($"At least {periods} candles are required for EMA calculation.", nameof(candleSet));
            }

            // Initialize Values array
            if (Values == null || Values.Length != candles.Count)
            {
                Values = new double[candles.Count];
            }

            Periods = periods;
            double smoothingFactor = 2.0 / (periods + 1);
            double complementFactor = 1 - smoothingFactor;

            // Calculate initial SMA for the first 'periods' candles
            double sum = 0;
            for (int i = 0; i < periods; i++)
            {
                sum += candles[i].Close;
                Values[i] = sum / (i + 1); // Progressive SMA for early values
            }
            Values[periods - 1] = sum / periods; // True SMA at periods-1

            // Calculate EMA for remaining candles
            for (int i = periods; i < candles.Count; i++)
            {
                Values[i] = smoothingFactor * candles[i].Close + Values[i - 1] * complementFactor;
            }
        }

        public void CalculateLast2(CandleSet candleSet)
        {
            TimeLastCalculated = DateTime.Now;
            List<Candle> candles = candleSet?.Candles;

            // Validate inputs
            if (candles == null || candles.Count == 0)
            {
                throw new ArgumentException("CandleSet is null or empty.", nameof(candleSet));
            }
            if (Periods <= 0)
            {
                throw new ArgumentException("Periods must be greater than 0.", nameof(Periods));
            }
            if (candles.Count < Periods)
            {
                throw new ArgumentException($"At least {Periods} candles are required for EMA calculation.", nameof(candleSet));
            }
            if (Values == null || Values.Length < candles.Count - 1)
            {
                throw new InvalidOperationException("Values array is not initialized or too small. Run Calculate first.");
            }

            // Adjust Values array if a new candle was added
            if (Values.Length < candles.Count)
            {
                Array.Resize(ref Values, candles.Count);
            }

            // Calculate EMA for the last two candles
            double smoothingFactor = 2.0 / (Periods + 1);
            double complementFactor = 1 - smoothingFactor;
            int startIndex = candles.Count - 2; // Start at second-to-last candle

            // Ensure we don't go before the first valid EMA (at Periods-1)
            if (startIndex < Periods - 1)
            {
                startIndex = Periods - 1;
            }

            for (int i = startIndex; i < candles.Count; i++)
            {
                Values[i] = smoothingFactor * candles[i].Close + Values[i - 1] * complementFactor;
            }
        }
    }
}
