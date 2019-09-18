using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoZ.Import
{
    public class ImportException : Exception
    {
        public ImportException(string message) : base(message) { }
    }
}
