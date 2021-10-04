using System;
using System.Collections.Generic;
using System.Text;

namespace Seeder.Model
{
    public class OrchestratorParameter
    {
        public int NbrDocuments {  get; }

        public OrchestratorParameter(int nbrDocuments) => NbrDocuments = nbrDocuments;
    }
}
