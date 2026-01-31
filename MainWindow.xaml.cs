using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Toolkit.Uwp.Notifications;
using LiveCharts;
using LiveCharts.Wpf;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using SkyCast.Data;

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
        private AppSettings _guestSettings = new AppSettings();
        private string _settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyCast_Config.json");
        private string _favFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkyCast_Favorites.json");
        private string _currentCity;
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
            LoadUserData();
            txtDefaultCity.Text = GetDefaultCity();
            UpdateUnitToggles();
            UpdateLoginStatusUI();
            GetWeatherData(_currentCity);
        }

        private void LoadUserData()
        {
            _favorites.Clear();
            if (AppSession.IsLoggedIn)
            {
                var user = AppSession.CurrentUser;
                _currentCity = user.DefaultCity;
                _isMetric = user.IsMetric;
                using (var db = new AppDbContext())
                {
                    var dbFavs = db.FavoriteCities
                                   .Where(f => f.UserId == user.Id)
                                   .Select(f => f.CityName)
                                   .ToList();
                    _favorites.AddRange(dbFavs);
                }
            }
            else
            {
                LoadGuestSettings();
                LoadGuestFavorites();
                _currentCity = _guestSettings.DefaultCity;
                _isMetric = _guestSettings.IsMetric;
            }
        }

        private void SaveSettings()
        {
            string newDefaultCity = txtDefaultCity.Text.Trim();
            if (string.IsNullOrEmpty(newDefaultCity)) newDefaultCity = "Istanbul";
            bool newIsMetric = rbMetric.IsChecked == true;

            if (AppSession.IsLoggedIn)
            {
                using (var db = new AppDbContext())
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == AppSession.CurrentUser.Id);
                    if (user != null)
                    {
                        user.DefaultCity = newDefaultCity;
                        user.IsMetric = newIsMetric;
                        db.SaveChanges();
                        AppSession.CurrentUser = user;
                    }
                }
            }
            else
            {
                _guestSettings.DefaultCity = newDefaultCity;
                _guestSettings.IsMetric = newIsMetric;
                SaveGuestSettings();
            }

            _isMetric = newIsMetric;
            if (_currentCity != newDefaultCity) GetWeatherData(newDefaultCity);
            else GetWeatherData(_currentCity);
        }

        private void AddFavorite(string cityName)
        {
            if (!_favorites.Contains(cityName))
            {
                _favorites.Add(cityName);
                if (AppSession.IsLoggedIn)
                {
                    using (var db = new AppDbContext())
                    {
                        db.FavoriteCities.Add(new FavoriteCity { CityName = cityName, UserId = AppSession.CurrentUser.Id });
                        db.SaveChanges();
                    }
                }
                else
                {
                    SaveGuestFavorites();
                }
                txtStar.Text = "★";
                ShowWeatherAlert("Added", $"{cityName} added to favorites.");
            }
        }

        private void RemoveFavorite(string cityName)
        {
            if (_favorites.Contains(cityName))
            {
                _favorites.Remove(cityName);
                if (AppSession.IsLoggedIn)
                {
                    using (var db = new AppDbContext())
                    {
                        var fav = db.FavoriteCities.FirstOrDefault(f => f.UserId == AppSession.CurrentUser.Id && f.CityName == cityName);
                        if (fav != null)
                        {
                            db.FavoriteCities.Remove(fav);
                            db.SaveChanges();
                        }
                    }
                }
                else
                {
                    SaveGuestFavorites();
                }
                string currentClean = txtCity.Text.Split(',')[0].Trim();
                if (currentClean == cityName) txtStar.Text = "☆";
            }
        }

        private void LoadGuestSettings()
        {
            if (File.Exists(_settingsFile))
            {
                try { _guestSettings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_settingsFile)) ?? new AppSettings(); }
                catch { _guestSettings = new AppSettings(); }
            }
        }

        private void SaveGuestSettings()
        {
            try { File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(_guestSettings)); } catch { }
        }

        private void LoadGuestFavorites()
        {
            if (File.Exists(_favFile))
            {
                try { _favorites = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_favFile)) ?? new List<string>(); }
                catch { _favorites = new List<string>(); }
            }
        }

        private void SaveGuestFavorites()
        {
            try { File.WriteAllText(_favFile, JsonConvert.SerializeObject(_favorites)); } catch { }
        }

        private void BtnUserIcon_Click(object sender, MouseButtonEventArgs e)
        {
            AuthenticationView authView = new AuthenticationView();
            authView.Owner = this;
            if (authView.ShowDialog() == true && AppSession.IsLoggedIn)
            {
                HandleLoginSuccess();
            }
        }

        private void HandleLoginSuccess()
        {
            LoadUserData();
            txtDefaultCity.Text = GetDefaultCity();
            UpdateUnitToggles();
            UpdateLoginStatusUI();
            GetWeatherData(_currentCity);
            ShowWeatherAlert("Login Success", $"Switched to {AppSession.CurrentUser.Username}'s profile.");
        }

        private void UpdateLoginStatusUI()
        {
            if (AppSession.IsLoggedIn)
            {
                txtLoginStatus.Text = AppSession.CurrentUser.Username;
                txtLoginStatus.Foreground = new SolidColorBrush(Colors.Cyan);
            }
            else
            {
                txtLoginStatus.Text = "Login";
                txtLoginStatus.Foreground = Brushes.Gray;
            }
        }

        private string GetDefaultCity()
        {
            return AppSession.IsLoggedIn ? AppSession.CurrentUser.DefaultCity : _guestSettings.DefaultCity;
        }

        private void UpdateUnitToggles()
        {
            if (_isMetric) rbMetric.IsChecked = true; else rbImperial.IsChecked = true;
            unitToggle.IsChecked = !_isMetric;
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            SettingsView.Visibility = Visibility.Collapsed;
            WeatherView.Visibility = Visibility.Visible;
            UpdateNavColors("Home");
        }

        private void btnFavorite_Click(object sender, RoutedEventArgs e)
        {
            string cleanCityName = txtCity.Text.Split(',')[0].Trim();
            if (_favorites.Contains(cleanCityName)) RemoveFavorite(cleanCityName);
            else AddFavorite(cleanCityName);
        }

        private async void btnRemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            var cityName = (sender as Button)?.Tag.ToString();
            if (cityName != null)
            {
                RemoveFavorite(cityName);
                await UpdateFavoritesView();
            }
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
            GetWeatherData(_currentCity);
            ShowWeatherAlert("Updated", $"{_currentCity} weather data refreshed.");
        }

        private void UnitToggle_Click(object sender, RoutedEventArgs e)
        {
            _isMetric = !(unitToggle.IsChecked ?? false);
            GetWeatherData(_currentCity);
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

        private void ShowWeatherAlert(string title, string message)
        {
            new ToastContentBuilder().AddText(title).AddText(message).Show();
        }

        private async void GetWeatherData(string cityName)
        {
            _currentCity = cityName;
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
                    txtTemperature.Text = $"{(int)Convert.ToDouble(firstItem["main"]["temp"])}{tempUnit}";
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
                        tempValues.Add(Convert.ToDouble(item["main"]["temp"]));
                        timeLabels.Add(item["dt_txt"].ToString().Split(' ')[1].Substring(0, 5));
                    }
                    HourlySeries = new SeriesCollection { new LineSeries { Values = tempValues, PointGeometrySize = 10, StrokeThickness = 3, Stroke = Brushes.White, Fill = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)) } };
                    HourlyLabels = timeLabels.ToArray();

                    var dailyList = new List<DailyForecastModel>();
                    var groups = data["list"].GroupBy(x => x["dt_txt"].ToString().Substring(0, 10)).Take(5);
                    foreach (var group in groups)
                    {
                        var midday = group.ElementAt(group.Count() / 2);
                        dailyList.Add(new DailyForecastModel { Day = DateTime.Parse(group.Key).ToString("ddd"), Status = midday["weather"][0]["main"].ToString(), IconPath = GetIconPath(midday["weather"][0]["main"].ToString()), MaxTemp = $"{(int)group.Max(x => Convert.ToDouble(x["main"]["temp_max"]))}°", MinTemp = $"{(int)group.Min(x => Convert.ToDouble(x["main"]["temp_min"]))}°" });
                    }
                    listDaysForecast.ItemsSource = dailyList;
                    _isFirstLoad = false;
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
                        var data = JObject.Parse(await client.GetStringAsync(url));
                        string iconCode = data["weather"][0]["icon"].ToString();
                        favDataList.Add(new FavoriteCityModel { CityName = data["name"].ToString(), Temperature = $"{(int)Convert.ToDouble(data["main"]["temp"])}°", Status = data["weather"][0]["main"].ToString(), BackgroundPath = iconCode.Contains("n") ? "/Images/night.jpg" : GetBgByCondition(data["weather"][0]["main"].ToString()) });
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
            if (condition == "Snow") return "/Images/snowy.png";
            return "/Images/sun.png";
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
                string imgName = iconCode.Contains("n") ? "night.jpg" : GetBgByCondition(weatherCondition).Replace("/Images/", "");
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