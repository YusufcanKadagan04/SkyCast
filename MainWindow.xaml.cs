using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SkyCast
{
    public partial class MainWindow : Window
    {
        private const string API_KEY = ApiConfig.API_KEY;
        private string _lastCity = "Istanbul";
        private bool _isMetric = true;
        private List<string> _favorites = new List<string>();
        private string _favFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favorites.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadFavorites();
            GetWeatherData(_lastCity);
        }

        private void LoadFavorites()
        {
            if (File.Exists(_favFile))
            {
                try
                {
                    string json = File.ReadAllText(_favFile);
                    _favorites = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
                catch { _favorites = new List<string>(); }
            }
        }

        private void SaveFavorites()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_favorites);
                File.WriteAllText(_favFile, json);
            }
            catch (Exception ex) { MessageBox.Show("Kaydetme hatası: " + ex.Message); }
        }

        private void BtnWeather_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WeatherView.Visibility = Visibility.Visible;
            CitiesView.Visibility = Visibility.Collapsed;
        }

        private async void BtnCities_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WeatherView.Visibility = Visibility.Collapsed;
            CitiesView.Visibility = Visibility.Visible;
            await UpdateFavoritesView();
        }

        private async Task UpdateFavoritesView()
        {
            var favDataList = new List<FavoriteCityModel>();
            using (HttpClient client = new HttpClient())
            {
                foreach (var city in _favorites)
                {
                    try
                    {
                        string units = _isMetric ? "metric" : "imperial";
                        string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&units={units}&appid={API_KEY}";
                        string response = await client.GetStringAsync(url);
                        var data = JObject.Parse(response);

                        string condition = data["weather"][0]["main"].ToString();
                        string iconCode = data["weather"][0]["icon"].ToString();
                        string bg = iconCode.Contains("n") ? "/Images/night.jpg" : GetBgByCondition(condition);

                        favDataList.Add(new FavoriteCityModel
                        {
                            CityName = data["name"].ToString(),
                            Temperature = $"{(int)Convert.ToDouble(data["main"]["temp"])}°",
                            Status = condition,
                            BackgroundPath = bg
                        });
                    }
                    catch { }
                }
            }
            listFavorites.ItemsSource = favDataList;
        }

        private string GetBgByCondition(string cond)
        {
            switch (cond)
            {
                case "Clouds": case "Mist": case "Fog": return "/Images/cloudy.jpg";
                case "Rain": case "Drizzle": case "Thunderstorm": return "/Images/rainy.jpg";
                case "Snow": return "/Images/snow.png";
                default: return "/Images/sunny.jpg";
            }
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

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetWeatherData(_lastCity);
        }

        private void UnitToggle_Click(object sender, RoutedEventArgs e)
        {
            _isMetric = !(unitToggle.IsChecked ?? false);
            GetWeatherData(_lastCity);
        }

        private void btnFavorite_Click(object sender, RoutedEventArgs e)
        {
            string cleanCityName = txtCity.Text.Split(',')[0].Trim();
            if (_favorites.Contains(cleanCityName))
            {
                _favorites.Remove(cleanCityName);
                txtStar.Text = "☆";
            }
            else
            {
                _favorites.Add(cleanCityName);
                txtStar.Text = "★";
            }
            SaveFavorites();
        }

        private async void btnRemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            var cityName = (sender as Button)?.Tag.ToString();
            if (cityName != null)
            {
                _favorites.Remove(cityName);
                SaveFavorites();
                await UpdateFavoritesView();
                string currentClean = txtCity.Text.Split(',')[0].Trim();
                if (currentClean == cityName) txtStar.Text = "☆";
            }
        }

        private async void GetWeatherData(string cityName)
        {
            _lastCity = cityName;
            string units = _isMetric ? "metric" : "imperial";
            string finalUrl = $"https://api.openweathermap.org/data/2.5/forecast?units={units}&appid={API_KEY}&q={cityName}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string jsonResponse = await client.GetStringAsync(finalUrl);
                    var data = JObject.Parse(jsonResponse);

                    string tempUnit = _isMetric ? "°C" : "°F";
                    string speedUnit = _isMetric ? "km/h" : "mph";

                    string nameOnly = data["city"]["name"].ToString();
                    txtCity.Text = $"{nameOnly}, {data["city"]["country"]}";
                    txtDate.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");
                    txtStar.Text = _favorites.Contains(nameOnly) ? "★" : "☆";

                    var firstItem = data["list"][0];
                    double rawTemp = Convert.ToDouble(firstItem["main"]["temp"]);
                    txtTemperature.Text = $"{(int)rawTemp}{tempUnit}";

                    string weatherCondition = firstItem["weather"][0]["main"].ToString();
                    string iconCode = firstItem["weather"][0]["icon"].ToString();
                    SetDynamicBackground(weatherCondition, iconCode);

                    txtHumidity.Text = $"{firstItem["main"]["humidity"]}%";
                    txtWindSpeed.Text = $"{firstItem["wind"]["speed"]} {speedUnit}";
                    txtRealFeel.Text = $"{(int)Convert.ToDouble(firstItem["main"]["feels_like"])}°";
                    txtUVIndex.Text = "Low";

                    imgMainWeather.Source = new BitmapImage(new Uri(GetIconPath(weatherCondition), UriKind.Relative));

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
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
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
                bgImage.ImageSource = new BitmapImage(new Uri($"pack://application:,,,/Images/{imgName}"));
            }
            catch { }
        }
    }

    public class FavoriteCityModel
    {
        public string CityName { get; set; }
        public string Temperature { get; set; }
        public string Status { get; set; }
        public string BackgroundPath { get; set; }
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