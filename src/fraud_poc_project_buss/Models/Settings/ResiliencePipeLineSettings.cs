using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fraud_poc_project_buss.Models.Settings
{
    public class ResiliencePipeLineSettings
    {
        public int RetryCount { get; set; }
        public int DelayCount { get; set; }
        public int TimeoutValue { get; set; }
    }
}
