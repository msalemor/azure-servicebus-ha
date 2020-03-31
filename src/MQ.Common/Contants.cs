using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQ.Common
{
    public class Contants
    {
        public const string JobQueue = "job-queue";
        public const string IdProperty = "jobid";
        public const string PrimaryConnectionString = "Endpoint=sb://alemorprimarysb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=9qoSdfx4dyX13MzHD/5kyqWkVU8O3eg9HXUZi9q8W2I=";
        public const string SecondaryConnectionString = "Endpoint=sb://alemorsecondarysb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=iZ8PRntN5jxrvg5Kn1TYEDr/yGQ4Ou98pVPn5HRASiM=";
    }
}
