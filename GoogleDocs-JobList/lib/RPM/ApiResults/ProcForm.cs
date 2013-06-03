using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPM.ApiResults
{
    class ProcForm
    {
        public string Number { get; set; }
        public int FormID { get; set; }
        public string Owner { get; set; }
        public string Status { get; set; }
        public string ApprovalResult { get; set; }
        public DateTime Started { get; set; }
        public DateTime Modified { get; set; }
        public List<ProcFormField> Fields { get; set; }
        public List<string> Values { get; set; }
        // TODO: rest of properties for form, see: http://rpmsoftware.wordpress.com/json-structure-for-forms/


        private Dictionary<string, string> fieldsByName;
        public string valueForField(string fieldName)
        {
            if (this.fieldsByName == null)
            {
                this.fieldsByName = new Dictionary<string, string>();
                foreach (ProcFormField field in this.Fields)
                {
                    this.fieldsByName.Add(field.Field, field.Value);
                }
            }
            if (this.fieldsByName.ContainsKey(fieldName))
            {
                return this.fieldsByName[fieldName];
            }
            throw new KeyNotFoundException(fieldName + " Not Found");
        }
    }
}
