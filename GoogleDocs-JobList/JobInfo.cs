using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleDocs_JobList
{
    public class JobInfo
    {
        public string JobId { private set; get; }
        public string Description { private set; get; }
        public string Location { private set; get; }

        public JobInfo(string JobId, string description, string location)
        {
            this.JobId = JobId;
            this.Description = description;
            this.Location = location;
        }
    }
}
