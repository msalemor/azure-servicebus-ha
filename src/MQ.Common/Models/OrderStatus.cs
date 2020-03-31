using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MQ.Common.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatus
    {
        Pending,
        Processing,
        Processed
    }
}
