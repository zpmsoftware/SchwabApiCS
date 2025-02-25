// <copyright file="Candle.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System.ComponentModel.DataAnnotations.Schema;

namespace ZpmPriceCharts
{
    public class Candle
    {
        public Candle() { }

        public Candle(DateTime dateTime, double open, double high, double low, double close, long volume) {
            this.DateTime = dateTime;
            this.Open = open;
            this.High = high;
            this.Low = low;
            this.Close = close;
            this.Volume = volume;
        }

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }

        [NotMapped]
        public DateTime DateTime { get; set; }

        public override string ToString()
        {
            return string.Format("{0}  O:{1}  H:{2}  L:{3}  C:{4}  V:{5}", DateTime, Open, High, Low, Close, Volume);
        }
    }
}
