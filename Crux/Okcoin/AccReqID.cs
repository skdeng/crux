using System;
namespace Crux.Okcoin
{
    class AccReqID : QuickFix.Fields.StringField
    {
        public AccReqID() : base(8000)
        {

        }

        public AccReqID(String data)
            : base(8000, data)
        {

        }
    }
}