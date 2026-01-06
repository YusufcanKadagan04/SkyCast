using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace SkyCast
{
    public partial class MainWindow : Window
    {
        
        private const string API_KEY = "f85b20092a5a5b00883907aa16b14c04";
        private const string CITY_URL = "https://api.openweathermap.org/data/2.5/forecast?q=Istanbul&units=metric&appid=";

        public MainWindow()
        {
            InitializeComponent();
            GetWeatherData();
        }

        private async void GetWeatherData()
        {
            string finalUrl = CITY_URL + API_KEY;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string jsonVerisi = await client.GetStringAsync(finalUrl);
                    var data = JObject.Parse(jsonVerisi);

                    string şehirIsmi = data["city"]["name"].ToString();
                    string ülkeKodu = data["city"]["country"].ToString();
                    txtCity.Text = $"{şehirIsmi}, {ülkeKodu}";

                    double hamDerece = (double)data["list"][0]["main"]["temp"];
                    int derece = (int)hamDerece;
                    txtTemperature.Text = $"{derece}°C";

                    txtDate.Text = DateTime.Now.ToString("dd MMMM yyyy dddd");

                    string havaDurumu = data["list"][0]["weather"][0]["main"].ToString();

                    if (havaDurumu == "Clear")
                        imgMainWeather.Source = new BitmapImage(new Uri("/Images/sun.png", UriKind.Relative));
                    else if (havaDurumu == "Clouds")
                        imgMainWeather.Source = new BitmapImage(new Uri("/Images/cloudy.png", UriKind.Relative));
                    else if (havaDurumu == "Rain")
                        imgMainWeather.Source = new BitmapImage(new Uri("/Images/rain.png", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
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