using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace SkyCast
{
    public partial class MainWindow : Window
    {
        private const string API_KEY = ApiConfig.API_KEY;
        private const string BASE_URL = "https://api.openweathermap.org/data/2.5/forecast?units=metric&appid=" + API_KEY + "&q=";

        public MainWindow()
        {
            InitializeComponent();
            GetWeatherData("Istanbul");
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string query = txtSearch.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    GetWeatherData(query);
                    txtSearch.Text = "";
                }
            }
        }

        private async void GetWeatherData(string cityName)
        {
            string finalUrl = BASE_URL + cityName;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string jsonResponse = await client.GetStringAsync(finalUrl);
                    var data = JObject.Parse(jsonResponse);

                    string returnedCity = data["city"]["name"].ToString();
                    string countryCode = data["city"]["country"].ToString();
                    txtCity.Text = $"{returnedCity}, {countryCode}";

                    double rawTemp = (double)data["list"][0]["main"]["temp"];
                    txtTemperature.Text = $"{(int)rawTemp}°C";

                    txtDate.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");

                    string weatherCondition = data["list"][0]["weather"][0]["main"].ToString();
                    string iconPath = "/Images/sun.png";

                    if (weatherCondition == "Clouds") iconPath = "/Images/cloudy.png";
                    else if (weatherCondition == "Rain") iconPath = "/Images/rain.png";
                    else if (weatherCondition == "Snow") iconPath = "/Images/snow.png";

                    imgMainWeather.Source = new BitmapImage(new Uri(iconPath, UriKind.Relative));

                    txtHumidity.Text = $"{data["list"][0]["main"]["humidity"]}%";
                    txtWindSpeed.Text = $"{data["list"][0]["wind"]["speed"]} km/h";
                    txtRealFeel.Text = $"{(int)(double)data["list"][0]["main"]["feels_like"]}°";
                    txtUVIndex.Text = "Low";

                    List<HourlyForecastModel> hourlyForecastList = new List<HourlyForecastModel>();
                    for (int i = 0; i < 8; i++)
                    {
                        var item = data["list"][i];
                        string dateTimeStr = item["dt_txt"].ToString();
                        string timeOnly = dateTimeStr.Split(' ')[1].Substring(0, 5);

                        string condition = item["weather"][0]["main"].ToString();
                        string hourlyIcon = "/Images/sun.png";
                        if (condition == "Rain") hourlyIcon = "/Images/rain.png";
                        else if (condition == "Clouds") hourlyIcon = "/Images/cloudy.png";
                        else if (condition == "Snow") hourlyIcon = "/Images/snow.png";

                        hourlyForecastList.Add(new HourlyForecastModel
                        {
                            Time = timeOnly,
                            Temperature = $"{(int)(double)item["main"]["temp"]}°",
                            Icon = hourlyIcon
                        });
                    }
                    listHourlyForecast.ItemsSource = hourlyForecastList;

                    List<DailyForecastModel> dailyForecastList = new List<DailyForecastModel>();
                    var dailyGroups = data["list"]
                        .GroupBy(x => x["dt_txt"].ToString().Substring(0, 10))
                        .Skip(1)
                        .Take(7);

                    foreach (var group in dailyGroups)
                    {
                        var minTemp = group.Min(x => (double)x["main"]["temp_min"]);
                        var maxTemp = group.Max(x => (double)x["main"]["temp_max"]);
                        var middayData = group.ElementAt(group.Count() / 2);
                        string status = middayData["weather"][0]["main"].ToString();

                        string dayIcon = "/Images/sun.png";
                        if (status == "Rain") dayIcon = "/Images/rain.png";
                        else if (status == "Clouds") dayIcon = "/Images/cloudy.png";
                        else if (status == "Snow") dayIcon = "/Images/snow.png";

                        DateTime dateValue = DateTime.Parse(group.Key);
                        string dayName = dateValue.ToString("ddd");

                        dailyForecastList.Add(new DailyForecastModel
                        {
                            Day = dayName,
                            MinTemp = $"{(int)minTemp}°",
                            MaxTemp = $"{(int)maxTemp}°",
                            IconPath = dayIcon,
                            Status = status
                        });
                    }
                    listDaysForecast.ItemsSource = dailyForecastList;
                }
            }
            catch (HttpRequestException)
            {
                MessageBox.Show("City not found! Please check the spelling.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnWeather_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            WeatherView.Visibility = Visibility.Visible;

            CitiesView.Visibility = Visibility.Collapsed;
        }
        private void BtnCities_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            WeatherView.Visibility = Visibility.Collapsed;

            CitiesView.Visibility = Visibility.Visible;
        }



    }

    



    public class HourlyForecastModel
    {
        public string Time { get; set; }
        public string Temperature { get; set; }
        public string Icon { get; set; }
    }

    public class DailyForecastModel
    {
        public string Day { get; set; }
        public string IconPath { get; set; }
        public string Status { get; set; }
        public string MaxTemp { get; set; }
        public string MinTemp { get; set; }
    }
}