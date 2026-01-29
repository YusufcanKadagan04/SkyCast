using System.Windows;
using System.Windows.Input;

namespace SkyCast
{
    public partial class AuthenticationView : Window
    {
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
            MessageBox.Show("Giriş mantığı (Issue #3) burada yapılacak.");
        }
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kayıt mantığı (Issue #3) burada yapılacak.");
        }
    }
}