using System;
using System.Windows;

namespace PL
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = string.Empty;

            string userId = txtUserId.Text?.Trim() ?? string.Empty;
            string password = txtPassword.Password ?? string.Empty;

            // Admin credentials (hardcoded for stage 6 - can be extended to BL authentication)
            const int adminId = 999;
            const string adminPassword = "admin123";

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Please enter both ID and Password.";
                return;
            }

            if (!int.TryParse(userId, out int parsedId))
            {
                lblError.Text = "User ID must be a numeric value.";
                return;
            }

            // Validate admin credentials
            if (parsedId == adminId && password == adminPassword)
            {
                MainWindow mainWindow = new();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                lblError.Text = "Invalid ID or Password. Please try again.";
                txtPassword.Clear();
                txtUserId.Focus();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
