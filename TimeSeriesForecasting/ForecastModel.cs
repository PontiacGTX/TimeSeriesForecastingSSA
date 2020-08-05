using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeSeriesForecasting
{
    static class ForecastModel
    {
        public class PriceForecast
        {

            public float[] Forecast;
        }



        public class PricePrediction
        {
            [ColumnName("Score")]
            public float Forecast;
        }


    }
}
