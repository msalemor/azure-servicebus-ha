using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQ.Common.Models
{
    public class JobInformation
    {
        public Guid JobId { get; set; }
        public string Message { get; set; }
        public bool Process { get; set; }
    }
}
