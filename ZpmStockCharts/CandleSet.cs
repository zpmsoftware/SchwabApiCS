// <copyright file="CandleSet.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

namespace ZpmPriceCharts
{
    public class CandleSet
    {

        public CandleSet() { 
        }

        public CandleSet(string symbol, string descriptiom, TimeFrames timeFrame,
                         DateTime startTime, DateTime endTime, int prependCandles, List<Candle> candles) 
        {
            Symbol = symbol;
            Description = descriptiom;
            TimeFrame = TimeFrameClass.GetTimeFrameClass(timeFrame);
            StartTime = startTime;
            EndTime = endTime;
            Candles = candles;
        }

        public string Symbol { get; set; }
        public string Description { get; set; }
        public int Decimals { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public List<Candle> Candles;
        public TimeFrameClass TimeFrame;
        //public DateTime LoadTime; // time data was loaded

        /// <summary>
        /// Number of candles prepended from requested start date.  To allow for looking back, for studies to calculate
        /// </summary>
        public int PrependCandles { get; set; } = 0;

        public enum TimeFrames
        {
            Zero = 0,
            Minute = 1,
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

        public abstract class TimeFrameClass
        {
            public TimeFrames TimeFrameId;

            public static TimeFrameClass GetTimeFrameClass(TimeFrames timeFrame)
            {
                switch (timeFrame)
                {
                    case TimeFrames.Day: return new TimeFrameDay();
                    case TimeFrames.Week: return new TimeFrameWeek();
                    case TimeFrames.Month: return new TimeFrameMonth();
                    case TimeFrames.Minute15: return new TimeFrame15Min();
                    case TimeFrames.Minute5: return new TimeFrame5Min();
                    default: throw new Exception("CandleSet must set TimeFrame");
                }
            }

            public abstract DateTime PrependStartTime(CandleSet cs);

            public virtual string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy");
            }

            public virtual bool IsAfterHours(DateTime dt)
            {
                if ((int)TimeFrameId >= 1000)
                    return false; //  daily +
                if (dt.Hour < 15 && dt.Hour >= 9) // between 9am - 3pm
                    return false;
                if (dt.Hour == 8 && dt.Minute >= 30)
                    return false;
                return true;
            }

            public abstract string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp);
        }

        // ====================================================================
        public class TimeFrameDay : TimeFrameClass
        {
            public TimeFrameDay()
            {
                TimeFrameId = TimeFrames.Day;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-(cs.PrependCandles * 7 / 5));  // 5 market days in a week
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp)
            {
                if (candleIdx > 1 && (timestamp.DayOfWeek == DayOfWeek.Monday || (candleIdx - lastCandleIdx > 1 && timestamp.DayOfWeek == DayOfWeek.Tuesday)))
                    return timestamp.ToString("M/d");
                return "";
            }
        }

        // ====================================================================
        public class TimeFrame15Min : TimeFrameClass
        {
            public TimeFrame15Min()
            {
                TimeFrameId = TimeFrames.Minute15;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-2);  // hard to calculate exactly. 2days should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp)
            {
                if (timestamp.Minute == 0)
                    return timestamp.ToString("h t");
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mm t");
            }
        }

        public class TimeFrame5Min : TimeFrameClass
        {
            public TimeFrame5Min()
            {
                TimeFrameId = TimeFrames.Minute5;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-1);  // hard to calculate exactly. 1 day should be enough
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp)
            {
                if (timestamp.Minute == 0)
                    return timestamp.ToString("h t");
                return "";
            }

            public override string DateText(DateTime date)
            {
                return date.ToString("MM/dd/yyyy hh:mm t");
            }
        }

        // ====================================================================
        public class TimeFrameWeek : TimeFrameClass
        {
            public TimeFrameWeek()
            {
                TimeFrameId = TimeFrames.Week;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddDays(-(cs.PrependCandles * 7));
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp)
            {
                if (timestamp.Day <= 7)
                    return timestamp.ToString("M/d");
                return "";
            }
        }

        // ====================================================================
        public class TimeFrameMonth : TimeFrameClass
        {
            public TimeFrameMonth()
            {
                TimeFrameId = TimeFrames.Month;
            }

            public override DateTime PrependStartTime(CandleSet cs)
            {
                return cs.StartTime.AddMonths(-cs.PrependCandles);
            }

            public override string ChartXaxisDateText(int candleIdx, int lastCandleIdx, DateTime timestamp)
            {
                if (timestamp.Month == 1)
                    return timestamp.ToString("yyyy");
                return "";
            }
        }
    }
}
