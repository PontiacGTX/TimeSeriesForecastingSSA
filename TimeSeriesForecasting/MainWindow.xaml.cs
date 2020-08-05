using Microsoft.ML;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.ML.Transforms.TimeSeries;
using System.Windows.Controls.DataVisualization.Charting;

namespace TimeSeriesForecasting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    using PriceForecast = ForecastModel.PriceForecast;
    public partial class MainWindow : Window
    {

        bool IsOutdated
        {
            get
            {
                var creationDate = ConvertToDateFormat(File.GetLastWriteTime(txtSource.Text));
                var today = ConvertToDateFormat(DateTime.Now);

                return creationDate < today;
            }
        }

        private static DateTime ConvertToDateFormat(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day).Date;
        }

        public void GetData(ref List<PriceData> priceData)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            string json = "";
            using (WebClient wc = new WebClient())
            {
                json = wc.DownloadString(txtSource.Text);
            }

            var deserializedJson = JsonConvert.DeserializeObject<APIResult>(json);


            foreach (var jsonObject in deserializedJson.result)
            {
                priceData.Add( new PriceData 
                                { 
                                    Time = jsonObject.T , 
                                    ClosePrice  = jsonObject.C   
                                }
                             );
            }

        }

        List<PriceData> _PriceData = new List<PriceData>();
        List<PriceData> _PriceForecast = new List<PriceData>();
        int timespan = -1;
        public MainWindow()
        {
            InitializeComponent();
            GetData(ref _PriceData);

            if(!string.IsNullOrEmpty(txtDateTime.Text))

            GetForecastPast(_PriceData, ref _PriceForecast,DateTime.Parse(txtDateTime.Text));
            else
            GetForecast(_PriceData, ref _PriceForecast);

            LoadDataToChart(_PriceData.GetRange((_PriceData.Count / 2) - 1, ((_PriceData.Count / 2) - 1)-1), _PriceForecast);
            
        }

        private void GetForecastPast(List<PriceData> priceData, ref List<PriceData> forecast, DateTime? time, bool half=false)
        {
            try
            {
                var context = new MLContext();

                int begin = (priceData.Count / 2) - 1;

                int end = begin - 1;

                List<PriceData> lastItems = new List<PriceData>();

                if (time != null)
                {
                    if ((bool)half)
                    {
                        lastItems = priceData.Where(dat => dat.Time <= time).ToList();

                        begin = (lastItems.Count / 2) - 1;
                        end = begin - 1;

                        lastItems = lastItems.GetRange(begin, end);
                    }
                    else
                        lastItems = priceData.Where(dat => dat.Time < time).ToList();


                }
                else
                {
                    if ((bool)half)
                        lastItems = priceData.GetRange(begin, end);
                    else
                        lastItems = priceData;
                }


                //var data = context.Data.LoadFromTextFile<PriceData>(CSVName,hasHeader:false,separatorChar:',');
                var data = context.Data.LoadFromEnumerable(lastItems);

                var forecastingPipeline = context.Forecasting.ForecastBySsa("Forecast", nameof(PriceData.ClosePrice), windowSize: int.Parse(txtWindowSize.Text), seriesLength: int.Parse(txtSeriesLength.Text), trainSize: int.Parse(txtTrainSize.Text), horizon: int.Parse(txtHorizon.Text));
                //nameof( PriceData.ClosePrice), windowSize: 6, seriesLength: 12, trainSize: 64, horizon: 6);

                var model = forecastingPipeline.Fit(data);


                var forecastingEngine = model.CreateTimeSeriesEngine<PriceData, PriceForecast>(context);

                var lastItem = priceData.Last();

                priceData.Reverse();

                var beforeLast = priceData.Skip(1).Take(1).Last();

                var timeDifference = lastItem.Time - beforeLast.Time;

                int addTime = 1;

                var forecastedPrice = forecastingEngine.Predict().Forecast.ToList();

                var forecastList = new List<PriceData>();

                forecastedPrice.ForEach((float price) =>
                {
                    var copy = lastItem.Time;
                    var date = lastItem.Time;

                    if (IsDay(ref timeDifference))
                    {
                        timespan = 0;
                        date = copy.AddDays(addTime);

                    }
                    else if (IsHour(ref timeDifference))
                    {
                        timespan = 1;
                        date = copy.AddHours(addTime);

                    }
                    else if (IsMinute(ref timeDifference))
                    {
                        timespan = 2;
                        date = copy.AddMinutes(addTime);
                    }

                    forecastList.Add
                    (
                        new PriceData { ClosePrice = price, Time = date }
                    );

                    addTime++;

                });

                forecast = forecastList;

                priceData.Reverse();



                return;
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.InnerException.Message);

            }
        }

        private void LoadDataToChart(List<PriceData> priceData, List<PriceData> priceForecast)
        {
            if (timespan == 0)
            {
               

                ((ColumnSeries)chrtPrice.Series[0]).ItemsSource = priceData.Select(x => new KeyValuePair<string, float>(x.Time.ToString("MMMM dd"), x.ClosePrice)).ToArray();

                ((AreaSeries)chrtForecast.Series[0]).ItemsSource = priceForecast.Select(x => new KeyValuePair<string, float>(x.Time.ToString("MMMM dd"), x.ClosePrice)).ToArray();

            }
            else if (timespan == 1 || timespan == 2)
            {
                ((AreaSeries)chrtPrice.Series[0]).ItemsSource = priceData.Select(x => new KeyValuePair<string, float>(x.Time.ToString("HH:mm:ss"), x.ClosePrice)).ToArray();

                ((AreaSeries)chrtForecast.Series[0]).ItemsSource = priceForecast.Select(x => new KeyValuePair<string, float>(x.Time.ToString("HH:mm:ss"), x.ClosePrice)).ToArray();

            }
           
        }

        private void GetForecast(List<PriceData> priceData, ref List<PriceData> forecast, bool? half = true)
        {
            var context = new MLContext();

            int begin = (priceData.Count / 2) - 1;

            int end = begin - 1;

            List<PriceData> lastItems;

            if ((bool)half)
             lastItems = priceData.GetRange(begin, end);
            else
             lastItems = priceData;

            //var data = context.Data.LoadFromTextFile<PriceData>(CSVName,hasHeader:false,separatorChar:',');
            var data = context.Data.LoadFromEnumerable(lastItems);

            var forecastingPipeline = context.Forecasting.ForecastBySsa("Forecast", nameof(PriceData.ClosePrice), windowSize: int.Parse(txtWindowSize.Text), seriesLength: int.Parse(txtSeriesLength.Text), trainSize: int.Parse(txtTrainSize.Text), horizon: int.Parse(txtHorizon.Text));
            //nameof( PriceData.ClosePrice), windowSize: 6, seriesLength: 12, trainSize: 64, horizon: 6);

            var model = forecastingPipeline.Fit(data);

           
            var forecastingEngine = model.CreateTimeSeriesEngine<PriceData, PriceForecast> (context);

            var lastItem = priceData.Last();

            priceData.Reverse();

            var beforeLast = priceData.Skip(1).Take(1).Last();

            var timeDifference = lastItem.Time - beforeLast.Time;
            
            int addTime = 1;

            var forecastedPrice = forecastingEngine.Predict().Forecast.ToList();

            var forecastList = new List<PriceData>();

            forecastedPrice.ForEach((float price) =>
            {
                var copy = lastItem.Time;
                var date = lastItem.Time;

                if (IsDay(ref timeDifference))
                {
                    timespan = 0;
                    date = copy.AddDays(addTime);

                }
                else if (IsHour(ref timeDifference))
                {
                    timespan = 1;
                    date = copy.AddHours(addTime);

                }
                else if (IsMinute(ref timeDifference))
                {
                    timespan = 2;
                    date = copy.AddMinutes(addTime);
                }

                forecastList.Add
                (
                    new PriceData { ClosePrice = price, Time = date }
                );

                addTime++;

            } );

            forecast = forecastList;

            priceData.Reverse();



            return;

        }

        private bool IsMinute(ref TimeSpan timeDifference)
        {
            return timeDifference.TotalSeconds == 60 && timeDifference.TotalMilliseconds == 60000;
        }
        private bool IsHour(ref TimeSpan timeDifference)
        {
            return timeDifference.TotalMinutes == 60 && timeDifference.TotalSeconds == 3600;
        }

        private bool IsDay(ref TimeSpan timeDifference)
        {
            return timeDifference.TotalHours == 24 && timeDifference.TotalMinutes == 1440;
        }

        private void btnGetForecast_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtDateTime.Text))
               GetForecastPast(_PriceData, ref _PriceForecast, DateTime.Parse(txtDateTime.Text), half: (bool)chckTakeLastHalf.IsChecked);
            else
               GetForecast(_PriceData, ref _PriceForecast,half: chckTakeLastHalf.IsChecked);

            LoadDataToChart(_PriceData.GetRange((_PriceData.Count / 2) - 1, ((_PriceData.Count / 2) - 1) - 1), _PriceForecast);
        }
    }
}
