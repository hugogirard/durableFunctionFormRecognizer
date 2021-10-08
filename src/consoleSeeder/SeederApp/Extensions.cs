using System;
using System.Collections.Generic;
using System.Text;

namespace SeederApp
{
    public static class Extension
    {
        public static bool IsSuccessStatusCode(this int statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 299);
        }
    }
}
