using System;
using System.Collections.Generic;
using System.Text;

namespace Seeder.Model
{
    public class UploadResult
    {
        public int TotalSuccessDocument { get; set; }

        public int TotalFailureDocument {  get; set; }

        public int TotalDocumentProcess { get; set; }

        public DateTime StartedTime { get; set; }

        public DateTime EndedTime { get; set; }
    }
}
