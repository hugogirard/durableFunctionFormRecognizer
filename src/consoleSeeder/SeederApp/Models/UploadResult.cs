using System;
using System.Collections.Generic;
using System.Text;

namespace SeederApp.Models
{
    public class UploadResult
    {
        public int TotalSuccessDocument { get; set; }

        public int TotalFailureDocument { get; set; }

        public int TotalDocumentProcess { get; set; }
    }
}
