using Newtonsoft.Json;
using System;

namespace MQ.Common.Models
{
    public class Order
    {
        [JsonProperty(PropertyName = "orderID")]
        public Guid OrderId { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        [JsonProperty(PropertyName = "orderStatus")]
        public OrderStatus Status { get; set; }
        [JsonProperty(PropertyName = "OrderDetails")]
        public OrderDetail[] OrderDetails { get; set; }
    }
}
