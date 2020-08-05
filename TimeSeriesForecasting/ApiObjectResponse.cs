using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TimeSeriesForecasting
{
    public class APIResult
    {
        public bool success { get; set; }
        public string message { get; set; }
        public Price[] result { get; set; }
        public object explanation { get; set; }
    }

    public class Price
    {
        public float O { get; set; }
        public float H { get; set; }
        public float L { get; set; }
        public float C { get; set; }
        public float V { get; set; }
        public DateTime T { get; set; }
        public float BV { get; set; }
    }
}
