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
        // Singleton instance of this PlugIn
        // Bu PlugIn'in tekil örneği
        public FacadeAnalysis Instance
        {
            get; private set;
        }

        public FacadeAnalysis()
        {
            // Assign the singleton instance
            // Tekil örneği ata
            Instance = this;
        }

        // Code to be executed when the plugin is loaded
        // Eklenti yüklenirken çalışacak kod
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Initialization code needed
            // İhtiyacınız olan başlangıç kodları
            return LoadReturnCode.Success;
        }
    }
}
