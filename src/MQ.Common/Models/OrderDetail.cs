using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQ.Common.Models
{
    public class OrderDetail
    {
        [JsonProperty(PropertyName = "sku")]
        public string Sku { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "qty")]
        public int Qty { get; set; }
        [JsonProperty(PropertyName = "price")]
        public double Price { get; set; }
    }
}
