using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeederApp.Models
{
    public class ProgramConfiguration
    {
        public string SECTION_NAME => "ProgramConfiguration";

        public int MaxConcurrency { get; set; }

        public int Factor {  get; set; }

        public int NbrDocuments { get; set; }

        public string StorageAccountName {  get; set; }

        public string StorageContainerName { get; set; }

        public string StorageCnxString { get; set; }
    }
}
