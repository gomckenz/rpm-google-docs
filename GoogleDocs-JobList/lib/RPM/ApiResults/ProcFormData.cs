﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPM.ApiResults
{
    class ProcFormData
    {
        public string Process { get; set; }
        public int ProcessID { get; set; }
        public string View { get; set; }
        public List<string> Columns { get; set; }    
        public ProcForm Form { get; set; }
    }
}
