using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPM.ApiResults
{
    class RPMApiError : Exception
    {
        public Dictionary<String, String> Error { get; set; }
        public override string Message {
            get {
				return this.Error["Message"];
            }
        }
    }
}
