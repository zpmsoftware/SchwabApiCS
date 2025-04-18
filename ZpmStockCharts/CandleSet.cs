// <copyright file="CandleSet.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using Windows.ApplicationModel.Background;

namespace ZpmPriceCharts
{
    public class CandleSet
    {

        public CandleSet() { 
        }

        public CandleSet(string symbol, string description, int decimals, FrequencyTypes frequencyType,
                         bool extendedHours, int prependCandles, DateTime loadTime, List<Candle> candles) 
        {
            Symbol = symbol;
            Description = description;
            Decimals = decimals;
            FrequencyType = FrequencyTypeClass.GetFrequencyTypeClass(frequencyType);
            ExtendedHours = extendedHours;
            if (candles.Count > 0)
            {
                StartTime = candles[0].DateTime;
                EndTime = candles.Last().DateTime;
            }
            Candles = candles;
            LoadTime = loadTime;
        }

        public string Symbol { get; set; }
        public string Description { get; set; }
        public int Decimals { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime LoadTime { get; set; }  // used to test when studies need to be recalulated.

        public List<Candle> Candles;
        public FrequencyTypeClass FrequencyType;
        public bool ExtendedHours;

        /// <summary>
        /// Number of candles prepended from requested start date.  To allow for looking back, for studies to calculate
        /// </summary>
        public int PrependCandles { get; set; } = 0;

        public enum FrequencyTypes // was "TimeFrames"
        {
            Zero = 0,
            Minute1 = 1,
            Minute5 = 5,
            Minute15 = 15,
            Minute30 = 30,
            Hour = 60,
            Hour2 = 120,   // 120 minutes
            Hour4 = 240,  // 240 minutes
            Day = 1000,  // 1000 = one day.
            Day2 = 2000,
            Day3 = 3000,
            Week = 7000,
            Month = 30000
        }
        
        /// <summary>
        /// Index of requested start date
        /// </summary>
        public int StartTimeIndex { get; set; } = 0;
        public static int tdaRequests = 0;
        public static string EmptySymbols = "";


        public int Count { get { return Candles.Count; } }

        public abstract class FrequencyTypeClass
        {
            public FrequencyTypes FrequencyTypeId;

            public static FrequencyTypeClass GetFrequencyTypeClass(FrequencyTypes frequencyType)
            {
                switch (frequencyType)
                {
                    case FrequencyTypes.Day: return new FrequencyTypeDay();
                    case FrequencyTypes.Week: return new FrequencyTypeWeek();
                    case FrequencyTypes.Month: return new FrequencyTypeMonth();
                    case FrequencyTypes.Hour: return new FrequencyTypeHour();
                    case FrequencyTypes.Minute30: return new FrequencyType30Min();
                    case FrequencyTypes.Minute15: return new FrequencyType15Min();
                    case FrequencyTypes.Minute5: return new FrequencyType5Min();
                    case FrequencyTypes.Minute1: return new FrequencyType1Min();
                    default: throw new Exception("CandleSet must set FrequencyType");
                }
            }

            public abstract DateTime PrependStartTime(CandleSet cs);

            public virtual string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy");
            }

            public virtual bool IsAfterHours(DateTime dt)
            {
                if ((int)FrequencyTypeId >= 1000)
                    return false; //  daily +
                if (dt.Hour < 15 && dt.Hour >= 9) // between 9am - 3pm
                    return false;
                if (dt.Hour == 8 && dt.Minute >= 30)
                    return false;
                return true;
            }

            public abstract string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth);
        }

        // ====================================================================
        public class FrequencyTypeDay : FrequencyTypeClass
        {
            public FrequencyTypeDay()
            {
                FrequencyTypeId = FrequencyTypes.Day;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-(cs.PrependCandles * 7 / 5));  // 5 market days in a week
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (candleIdx > 1 && (timestamp.DayOfWeek == DayOfWeek.Monday || (candleIdx - lastCandleIdx > 1 && timestamp.DayOfWeek == DayOfWeek.Tuesday)))
                    return timestamp.ToString("M/d");
                return "";
            }
        }

        // ====================================================================
        public class FrequencyTypeHour : FrequencyTypeClass
        {
            public FrequencyTypeHour()
            {
                FrequencyTypeId = FrequencyTypes.Hour;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-3);  // hard to calculate exactly. 2days should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                var mod = 2;
                if (candleWidth <= 5)
                    mod = 4;
                else if (candleWidth <= 10)
                    mod = 3;

                if (timestamp.Minute == 0 && timestamp.Hour % mod == 0)
                    return timestamp.ToString("ht").ToLower(); ;
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mmt").ToLower();
            }
        }

        public class FrequencyType30Min : FrequencyTypeClass
        {
            public FrequencyType30Min()
            {
                FrequencyTypeId = FrequencyTypes.Minute30;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-3);  // hard to calculate exactly. 2days should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Minute == 0 && timestamp.Hour % (candleWidth <=11 ? 2 : 1) == 0)
                    return timestamp.ToString("ht").ToLower(); ;
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mmt").ToLower();
            }
        }

        public class FrequencyType15Min : FrequencyTypeClass
        {
            public FrequencyType15Min()
            {
                FrequencyTypeId = FrequencyTypes.Minute15;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-2);  // hard to calculate exactly. 2days should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Minute == 0)
                    return timestamp.ToString("ht").ToLower();
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mmt").ToLower();
            }
        }

        public class FrequencyType5Min : FrequencyTypeClass
        {
            public FrequencyType5Min()
            {
                FrequencyTypeId = FrequencyTypes.Minute5;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-1);  // hard to calculate exactly. 1 day should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Minute == 0)
                    return timestamp.ToString("ht").ToLower();
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mmt").ToLower();
            }
        }

        public class FrequencyType1Min : FrequencyTypeClass
        {
            public FrequencyType1Min()
            {
                FrequencyTypeId = FrequencyTypes.Minute1;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-1);  // hard to calculate exactly. 1 day should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Minute % (candleWidth <= 6 ? 10 : 5) == 0)
                    return timestamp.ToString("h:mmt").ToLower();
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mmt").ToLower();
            }
        }
        // ====================================================================
        public class FrequencyTypeWeek : FrequencyTypeClass
        {
            public FrequencyTypeWeek()
            {
                FrequencyTypeId = FrequencyTypes.Week;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-(cs.PrependCandles * 7));
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Day <= 7)
                    return timestamp.ToString("M/d");
                return "";
            }
        }

        // ====================================================================
        public class FrequencyTypeMonth : FrequencyTypeClass
        {
            public FrequencyTypeMonth()
            {
                FrequencyTypeId = FrequencyTypes.Month;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddMonths(-cs.PrependCandles);
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp, double candleWidth)
            {
                if (timestamp.Month == 1)
                    return timestamp.ToString("yyyy");
                return "";
            }
        }
    }
}
