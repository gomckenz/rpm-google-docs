using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleDocs_JobList
{
    class ProcessNotFoundException : Exception
    {
        public ProcessNotFoundException(string message) : base(message)
        {
        }
    }
}
