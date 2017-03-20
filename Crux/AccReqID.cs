using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickFix;
namespace Crux
{
    class AccReqID : QuickFix.Fields.StringField
    {
        public AccReqID():base(8000)
        {
           
        }

        public AccReqID(String data)
            : base(8000, data)
        {
            
        }
    }
}