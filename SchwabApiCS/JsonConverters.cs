using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchwabApiCS
{
    public class JsonDateTimeConverter : DateTimeConverterBase
    {
        private readonly long _timeZoneAdjust; // Adjustment in milliseconds

        public JsonDateTimeConverter(long timeZoneAdjust)
        {
            _timeZoneAdjust = timeZoneAdjust;
        }

        public override bool CanConvert(Type objectType)
        {
            // Only handle DateTime, DateTimeOffset, and their nullable versions
            return objectType == typeof(DateTime) ||
                   objectType == typeof(DateTimeOffset) ||
                   objectType == typeof(DateTime?) ||
                   objectType == typeof(DateTimeOffset ?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var dateString = reader.Value.ToString();
            var dateTimeOffset = DateTimeOffset.Parse(dateString);
            var adjustedDateTimeOffset = dateTimeOffset.AddMilliseconds(_timeZoneAdjust);

            // Return as DateTime or DateTimeOffset based on the target type
            if (objectType == typeof(DateTime))
            {
                return adjustedDateTimeOffset.DateTime; // Returns DateTime, offset is discarded
            }
            return adjustedDateTimeOffset; // Returns DateTimeOffset, preserves offset
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is DateTime dateTime)
            {
                // When serializing, apply the adjustment for consistency
                //var dateTimeOffset = new DateTimeOffset(dateTime).ToOffset(TimeSpan.FromMilliseconds(_timeZoneAdjust));
                //writer.WriteValue(dateTimeOffset.ToString("o")); // ISO 8601 format
                writer.WriteValue(dateTime.ToString("o")); // ISO 8601 format
            }
            else if (value is DateTimeOffset dateTimeOffset)
            {
                // Apply adjustment to DateTimeOffset
                //var adjustedDateTimeOffset = dateTimeOffset + TimeSpan.FromMilliseconds(_timeZoneAdjust);
                //writer.WriteValue(adjustedDateTimeOffset.ToString("o"));
                writer.WriteValue(dateTimeOffset.ToString("o"));
            }
        }
    }
}
