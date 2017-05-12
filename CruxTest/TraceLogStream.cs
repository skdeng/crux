using Crux;
using System.Diagnostics;

namespace CruxTest
{
    class TraceLogStream : ILogStream
    {
        public void Write(string msg, int level)
        {
            Trace.WriteLine(msg);
        }
    }
}
