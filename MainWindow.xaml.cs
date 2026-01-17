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

                    
                    txtCity.Text = $"{data["city"]["name"]}, {data["city"]["country"]}";
                    txtDate.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");

                    
                    var firstItem = data["list"][0];
                    double rawTemp = Convert.ToDouble(firstItem["main"]["temp"]);
                    txtTemperature.Text = $"{(int)rawTemp}°C";

                    string weatherCondition = firstItem["weather"][0]["main"].ToString();
                    string iconCode = firstItem["weather"][0]["icon"].ToString();
                    SetDynamicBackground(weatherCondition, iconCode);

                    
                    txtHumidity.Text = $"{firstItem["main"]["humidity"]}%";
                    txtWindSpeed.Text = $"{firstItem["wind"]["speed"]} km/h";
                    txtRealFeel.Text = $"{(int)Convert.ToDouble(firstItem["main"]["feels_like"])}°";
                    txtUVIndex.Text = "Low";

                    
                    string iconPath = GetIconPath(weatherCondition);
                    imgMainWeather.Source = new BitmapImage(new Uri(iconPath, UriKind.Relative));

                    
                    var hourlyList = new List<HourlyForecastModel>();
                    for (int i = 0; i < 8; i++)
                    {
                        var item = data["list"][i];
                        hourlyList.Add(new HourlyForecastModel
                        {
                            Time = item["dt_txt"].ToString().Split(' ')[1].Substring(0, 5),
                            Temperature = $"{(int)Convert.ToDouble(item["main"]["temp"])}°",
                            Icon = GetIconPath(item["weather"][0]["main"].ToString())
                        });
                    }
                    listHourlyForecast.ItemsSource = hourlyList;

                    
                    var dailyList = new List<DailyForecastModel>();
                    var groups = data["list"]
                        .GroupBy(x => x["dt_txt"].ToString().Substring(0, 10))
                        .Skip(1)
                        .Take(7);

                    foreach (var group in groups)
                    {
                        var midday = group.ElementAt(group.Count() / 2);
                        dailyList.Add(new DailyForecastModel
                        {
                            Day = DateTime.Parse(group.Key).ToString("ddd"),
                            Status = midday["weather"][0]["main"].ToString(),
                            IconPath = GetIconPath(midday["weather"][0]["main"].ToString()),
                            MaxTemp = $"{(int)group.Max(x => Convert.ToDouble(x["main"]["temp_max"]))}°",
                            MinTemp = $"{(int)group.Min(x => Convert.ToDouble(x["main"]["temp_min"]))}°"
                        });
                    }
                    listDaysForecast.ItemsSource = dailyList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Veri yüklenirken hata oluştu: " + ex.Message);
            }
        }

        private string GetIconPath(string condition)
        {
            if (condition == "Clouds" || condition == "Mist" || condition == "Fog") return "/Images/cloudy.png";
            if (condition == "Rain" || condition == "Drizzle" || condition == "Thunderstorm") return "/Images/rain.png";
            if (condition == "Snow") return "/Images/snow.png";
            return "/Images/sun.png";
        }

        private void SetDynamicBackground(string weatherCondition, string iconCode)
        {
            try
            {
                string imgName = "sunny.jpg";
                if (iconCode.Contains("n")) imgName = "night.jpg";
                else
                {
                    switch (weatherCondition)
                    {
                        case "Clouds": case "Mist": case "Fog": imgName = "cloudy.jpg"; break;
                        case "Rain": case "Drizzle": case "Thunderstorm": imgName = "rainy.jpg"; break;
                        case "Snow": imgName = "snowy.jpg"; break;
                    }
                }

                string packUri = $"pack://application:,,,/Images/{imgName}";
                bgImage.ImageSource = new BitmapImage(new Uri(packUri));
            }
            catch (Exception)
            {
                
            }
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