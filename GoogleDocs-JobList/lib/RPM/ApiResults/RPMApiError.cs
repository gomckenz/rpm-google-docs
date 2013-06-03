using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPM.ApiResults
{
    class RPMApiError : Exception
    {
        public string Error { get; set; }
        public override string Message {
            get {
                return this.Error;
            }
        }
    }
}
