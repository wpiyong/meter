using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gUV.Model
{
    public class LogEntry
    {
        public LogEntry()
        {
        }

        public LogEntry(DateTime d, string m)
        {
            //DateTime = d.ToString(@"M/d/yyyy hh:mm:ss.fff tt");
            DateTime = d.ToString(@"M/d/yyyy hh:mm:ss tt");
            Message = m;
        }

        public string DateTime { get; set; }

        public string Message { get; set; }
    }

    class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }
    }
}
