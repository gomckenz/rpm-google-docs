using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleDocs_JobList
{

    public class AppSetupChangedEventArgs
    {
        public string key;
        public string value;

        public AppSetupChangedEventArgs(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
