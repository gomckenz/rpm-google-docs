using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPM.ApiResults
{
    class ProcResult
    {
        public string Process { get; set; }
        public int ProcessID { get; set; }
        public bool Enabled { get; set; }
        public DateTime Added { get; set; }
        public DateTime Modified { get; set; }
        public int Forms { get; set; }
        public int Archived { get; set; }
        public int Fields { get; set; }
    }
}
