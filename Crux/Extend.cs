using System;
using System.Collections.Generic;
using System.Linq;

namespace Crux
{
    public static class Extend
    {
        public static double Std(this IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        public static long UnixTimestamp(this DateTime time)
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }
}
