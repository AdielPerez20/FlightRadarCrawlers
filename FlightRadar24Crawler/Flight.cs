using System;
using System.Collections.Generic;
using System.Text;

namespace FlightRadar24Crawler
{
    public class Flight
    {
        public string From { get; set; }

        public string To { get; set; }

        public double Date { get; set; }

        public double FlightTime { get; set; }

        public double ScheduledTimeDeparture { get; set; }

        public double ActualTimeDeparture { get; set; }

        public double ScheduledTimeArrival { get; set; }
    }
}
