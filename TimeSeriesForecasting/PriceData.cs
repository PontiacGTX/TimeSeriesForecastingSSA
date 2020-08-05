using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesForecasting
{
 
    public class PriceData
    {

        [ColumnName("Time")]
        [LoadColumn(0)]
        public DateTime Time;

        [ColumnName("ClosePrice")]
        [LoadColumn(1)]
        public float ClosePrice;

    }
}
