using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SkyCast
{
    public partial class AuthenticationView : Window
    {
        private AuthService _authService = new AuthService();

        public AuthenticationView()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnSwitchToRegister_Click(object sender, MouseButtonEventArgs e)
        {
            pnlLogin.Visibility = Visibility.Collapsed;
            pnlRegister.Visibility = Visibility.Visible;
            txtTitle.Text = "Create Account";
            txtSubtitle.Text = "Join us to save your preferences";
        }

        private void BtnSwitchToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            pnlRegister.Visibility = Visibility.Collapsed;
            pnlLogin.Visibility = Visibility.Visible;
            txtTitle.Text = "Welcome Back";
            txtSubtitle.Text = "Please sign in to continue";
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtLoginUser.Text.Trim();
            string password = txtLoginPass.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            var user = _authService.LoginUser(username, password);
            if (user != null)
            {
                AppSession.CurrentUser = user;

                MessageBox.Show($"Welcome back, {user.Username}!");
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtRegUser.Text.Trim();
            string pass = txtRegPass.Password;
            string confirm = txtRegPassConfirm.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            if (pass != confirm)
            {
                MessageBox.Show("Passwords do not match.");
                return;
            }

            string result = _authService.RegisterUser(username, pass);
            if (result == "Success")
            {
                MessageBox.Show("Account created successfully! Please sign in.");
                BtnSwitchToLogin_Click(null, null);
            }
            else
            {
                MessageBox.Show("Error: " + result);
            }
        }
    }
}