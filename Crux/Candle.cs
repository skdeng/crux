﻿namespace Crux
{
    public class Candle
    {
        public TimePeriod Timespan { get; set; }

        public double Open { get; set; }

        public double Close { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public Candle(double open, double close, double high, double low, TimePeriod timeperiod)
        {
            Open = open;
            Close = close;
            High = high;
            Low = low;
            Timespan = timeperiod;
        }
    }
}
