#  SkyCast: Modern Weather Visualization System

SkyCast is a high-performance desktop application built with **.NET 8** and **WPF**, designed to provide real-time weather data with a focus on interactive data visualization and secure user management.

<img src="https://github.com/YusufcanKadagan04/SkyCast/blob/ft_Sprint3/Screenshots/MainPage.png"/>

---

##  Core Features

* **Real-Time Data Integration:** Fetches global weather data using the **OpenWeatherMap REST API**.
* **Hybrid Persistence Architecture:** * **Guest Mode:** Uses JSON-based local storage for session preferences.
    * **User Mode:** Implements **SQLite** with **Entity Framework Core** for persistent, user-specific data management.
* **Advanced Visualization:** Interactive 24-hour temperature trends rendered via **LiveCharts**.
* **Secure Authentication:** Integrated login/register system featuring **SHA256 password hashing**.
* **Dynamic UI/UX:** Glassmorphism-inspired dark theme with dynamic background switching based on weather conditions.
* **System Notifications:** Windows Toast Notifications for weather alerts and status updates.

---

##  Tech Stack

* **Language:** C#
* **Framework:** .NET 8 (WPF)
* **Database:** SQLite
* **ORM:** Entity Framework Core
* **API Communication:** HttpClient & Newtonsoft.Json
* **Data Visualization:** LiveCharts.Wpf
* **Architecture:** Event-Driven with Hybrid Persistence

---

##  Screenshots

| Home Screen | Cities Screen | Settings Screen |
| :---: | :---: | :---: |
| <img src="https://github.com/YusufcanKadagan04/SkyCast/blob/ft_Sprint3/Screenshots/MainPage.png"/> | <img src="https://github.com/YusufcanKadagan04/SkyCast/blob/ft_Sprint3/Screenshots/CitiesPage.png"/> | <img src="https://github.com/YusufcanKadagan04/SkyCast/blob/ft_Sprint3/Screenshots/SettingsPage.png"/> |

---

##  Installation & Setup

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/yusufcankadagan/SkyCast.git](https://github.com/yusufcankadagan/SkyCast.git)
    ```
2.  **API Configuration:**
    Create an `ApiConfig.cs` file in the root directory:
    ```csharp
    public static class ApiConfig {
        public const string API_KEY = "YOUR_OPENWEATHERMAP_KEY_HERE";
    }
    ```
3.  **Restore NuGet Packages:**
    * `Microsoft.EntityFrameworkCore.Sqlite`
    * `LiveCharts.Wpf`
    * `Newtonsoft.Json`
    * `Microsoft.Toolkit.Uwp.Notifications`
4.  **Build & Run:** Press `F5` in Visual Studio.

---

## Technical Challenges & Learnings

* **Migrating from JSON to SQLite:** Transitioned from a simple file-based system to a relational database model to support multi-user isolation.
* **UI Thread Management:** Handled asynchronous API calls to ensure the interface remains responsive during data fetching.
* **Data Integrity:** Implemented hashing to ensure user passwords are never stored in plain text.

---

## Author

**Yusufcan Kadagan**
*  Information Systems and Technologies Student @ Atatürk University
*  [LinkedIn](https://www.linkedin.com/in/yusufcan-kada%C4%9Fan-680b7a287/) | [GitHub](https://github.com/YusufcanKadagan04)

---
*This project was developed as a showcase of C# desktop development and modern software architecture principles.*
