using System;
using System.Collections.Generic;
using SkyCast.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SkyCast
{
    public class AppSettings
    {
        public string DefaultCity { get; set; } = "Istanbul";
        public bool IsMetric { get; set; } = true;
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private const string API_KEY = ApiConfig.API_KEY;

        private AppSettings _settings = new AppSettings();
        private string _settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyCast_Config.json");
        private string _favFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyCast_Favorites.json");

        private string _lastCity;
        private bool _isMetric;
        private List<string> _favorites = new List<string>();
        private bool _isFirstLoad = true;

        private SeriesCollection _hourlySeries;
        public SeriesCollection HourlySeries
        {
            get { return _hourlySeries; }
            set { _hourlySeries = value; OnPropertyChanged("HourlySeries"); }
        }

        private string[] _hourlyLabels;
        public string[] HourlyLabels
        {
            get { return _hourlyLabels; }
            set { _hourlyLabels = value; OnPropertyChanged("HourlyLabels"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow()
        {
            InitializeComponent();
            using (var context = new AppDbContext())
            {
                context.Database.EnsureCreated();
            }
            DataContext = this;

            LoadSettings();
            LoadFavorites();

            _lastCity = _settings.DefaultCity;
            _isMetric = _settings.IsMetric;

            txtDefaultCity.Text = _settings.DefaultCity;
            if (_settings.IsMetric) rbMetric.IsChecked = true; else rbImperial.IsChecked = true;
            unitToggle.IsChecked = !_settings.IsMetric;

            GetWeatherData(_lastCity);
        }

        private void BtnFavoriteCard_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is string cityName)
            {
                GetWeatherData(cityName);
                WeatherView.Visibility = Visibility.Visible;
                CitiesView.Visibility = Visibility.Collapsed;
                SettingsView.Visibility = Visibility.Collapsed;
                UpdateNavColors("Home");
            }
        }

        private void UpdateNavColors(string activePage)
        {
            var grayBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
            var whiteBrush = Brushes.White;

            txtNavHome.Foreground = grayBrush;
            imgNavHome.Opacity = 0.5;

            txtNavCities.Foreground = grayBrush;
            imgNavCities.Opacity = 0.5;

            txtNavSettings.Foreground = grayBrush;
            txtNavSettingsIcon.Foreground = grayBrush;

            if (activePage == "Home")
            {
                txtNavHome.Foreground = whiteBrush;
                imgNavHome.Opacity = 1.0;
            }
            else if (activePage == "Cities")
            {
                txtNavCities.Foreground = whiteBrush;
                imgNavCities.Opacity = 1.0;
            }
            else if (activePage == "Settings")
            {
                txtNavSettings.Foreground = whiteBrush;
                txtNavSettingsIcon.Foreground = whiteBrush;
            }
        }

        private void txtCitiesSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string query = txtCitiesSearch.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    GetWeatherData(query);
                    txtCitiesSearch.Text = "";

                    CitiesView.Visibility = Visibility.Collapsed;
                    SettingsView.Visibility = Visibility.Collapsed;
                    WeatherView.Visibility = Visibility.Visible;

                    UpdateNavColors("Home");
                }
            }
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.DefaultCity = txtDefaultCity.Text.Trim();
            if (string.IsNullOrEmpty(_settings.DefaultCity)) _settings.DefaultCity = "Istanbul";
            _settings.IsMetric = rbMetric.IsChecked == true;

            SaveSettings();

            _isMetric = _settings.IsMetric;
            unitToggle.IsChecked = !_settings.IsMetric;

            ShowWeatherAlert("Settings Saved", "Preferences updated.");

            if (_lastCity != _settings.DefaultCity) GetWeatherData(_settings.DefaultCity);
            else GetWeatherData(_lastCity);

            SettingsView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Visible;

            UpdateNavColors("Home");
        }

        private void BtnWeather_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WeatherView.Visibility = Visibility.Visible;
            CitiesView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Collapsed;
            UpdateNavColors("Home");
        }

        private async void BtnCities_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WeatherView.Visibility = Visibility.Collapsed;
            CitiesView.Visibility = Visibility.Visible;
            SettingsView.Visibility = Visibility.Collapsed;
            UpdateNavColors("Cities");
            await UpdateFavoritesView();
        }

        private void BtnSettings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WeatherView.Visibility = Visibility.Collapsed;
            CitiesView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Visible;
            UpdateNavColors("Settings");
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
            ShowWeatherAlert("Updated", $"{_lastCity} weather data refreshed.");
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
                ShowWeatherAlert("Added", $"{cleanCityName} added to favorites.");
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

        private void LoadSettings()
        {
            if (File.Exists(_settingsFile))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFile);
                    _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                catch { _settings = new AppSettings(); }
            }
        }

        private void SaveSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_settings);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
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
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
        }

        private void ShowWeatherAlert(string title, string message)
        {
            new ToastContentBuilder().AddText(title).AddText(message).Show();
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
                    string currentTemp = $"{(int)rawTemp}{tempUnit}";
                    txtTemperature.Text = currentTemp;

                    string weatherCondition = firstItem["weather"][0]["main"].ToString();
                    string iconCode = firstItem["weather"][0]["icon"].ToString();
                    SetDynamicBackground(weatherCondition, iconCode);

                    txtHumidity.Text = $"{firstItem["main"]["humidity"]}%";
                    txtWindSpeed.Text = $"{firstItem["wind"]["speed"]} {speedUnit}";
                    txtRealFeel.Text = $"{(int)Convert.ToDouble(firstItem["main"]["feels_like"])}°";
                    txtUVIndex.Text = "Low";

                    imgMainWeather.Source = new BitmapImage(new Uri(GetIconPath(weatherCondition), UriKind.Relative));

                    var tempValues = new ChartValues<double>();
                    var timeLabels = new List<string>();
                    for (int i = 0; i < 8; i++)
                    {
                        var item = data["list"][i];
                        double t = Convert.ToDouble(item["main"]["temp"]);
                        tempValues.Add(t);
                        string time = item["dt_txt"].ToString().Split(' ')[1].Substring(0, 5);
                        timeLabels.Add(time);
                    }

                    HourlySeries = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Temp",
                            Values = tempValues,
                            PointGeometry = DefaultGeometries.Circle,
                            PointGeometrySize = 10,
                            StrokeThickness = 3,
                            Stroke = System.Windows.Media.Brushes.White,
                            Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 255, 255))
                        }
                    };
                    HourlyLabels = timeLabels.ToArray();

                    var dailyList = new List<DailyForecastModel>();
                    var groups = data["list"]
                        .GroupBy(x => x["dt_txt"].ToString().Substring(0, 10))
                        .Take(5);

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

                    if (_isFirstLoad)
                    {
                        ShowWeatherAlert("Forecast", $"{nameOnly}: {weatherCondition}, {currentTemp}.");
                        _isFirstLoad = false;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
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
                        string temp = $"{(int)Convert.ToDouble(data["main"]["temp"])}°";

                        favDataList.Add(new FavoriteCityModel
                        {
                            CityName = data["name"].ToString(),
                            Temperature = temp,
                            Status = condition,
                            BackgroundPath = bg
                        });
                    }
                    catch { }
                }
            }
            listFavorites.ItemsSource = favDataList;
        }

        private string GetIconPath(string condition)
        {
            if (condition == "Clouds" || condition == "Mist" || condition == "Fog") return "/Images/cloudy.png";
            if (condition == "Rain" || condition == "Drizzle" || condition == "Thunderstorm") return "/Images/rainy.png";
            if (condition == "Snow") return "/Images/snow.png";
            return "/Images/sun.png";
        }
        private void BtnUserIcon_Click(object sender, MouseButtonEventArgs e)
        {
            AuthenticationView authView = new AuthenticationView();
            authView.Owner = this;
            if (authView.ShowDialog() == true)
            {
                if (AppSession.IsLoggedIn)
                {
                    txtLoginStatus.Text = AppSession.CurrentUser.Username;
                }
            }
        }
        private string GetBgByCondition(string cond)
        {
            switch (cond)
            {
                case "Clouds": case "Mist": case "Fog": return "/Images/cloudy.png";
                case "Rain": case "Drizzle": case "Thunderstorm": return "/Images/rainy.png";
                case "Snow": return "/Images/snowy.png";
                default: return "/Images/sun.png";
            }
        }

        private void SetDynamicBackground(string weatherCondition, string iconCode)
        {
            try
            {
                string imgName = "sun.png";

                if (iconCode.Contains("n")) imgName = "night.jpg";
                else
                {
                    switch (weatherCondition)
                    {
                        case "Clouds": case "Mist": case "Fog": imgName = "cloudy.png"; break;
                        case "Rain": case "Drizzle": case "Thunderstorm": imgName = "rainy.png"; break;
                        case "Snow": imgName = "snowy.png"; break;
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

    public class DailyForecastModel
    {
        public string Day { get; set; }
        public string IconPath { get; set; }
        public string Status { get; set; }
        public string MaxTemp { get; set; }
        public string MinTemp { get; set; }
    }
}