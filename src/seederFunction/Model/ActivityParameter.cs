using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Seeder.Model
{
    public class ActivityParameter
    {
        public string Filename { get; set; }

        public Stream Content { get; set; }
    }
}
