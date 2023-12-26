using Rhino.PlugIns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacadeAnalysis
{
    public class FacadeAnalysis : PlugIn
    {
        public FacadeAnalysis Instance
        {
            get; private set;
        }

        public FacadeAnalysis()
        {
            Instance = this;
        }

        // Eklenti yüklenirken çalışacak kod
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // İhtiyacınız olan başlangıç kodları
            return LoadReturnCode.Success;
        }
    }
}
