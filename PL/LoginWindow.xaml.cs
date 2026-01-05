using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using BlApi;

namespace PL
{
    public partial class LoginWindow : Window
    {
        static readonly IBI s_bl = BL.Factory.Get();
        
        // Static dictionary to track open Courier windows by courier ID
        private static Dictionary<int, Courier.CourierWindow> s_courierWindows = new();

        public LoginWindow()
        {
            InitializeComponent();
        }

        #region Event Handlers - Key Navigation

        /// <summary>
        /// Handles Enter key press on the Window level
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                btnCancel_Click(null, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles Enter key press in TextBox
        /// </summary>
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                // Move focus to password field if we're in ID field
                if (sender == txtUserId && string.IsNullOrEmpty(txtPassword.Password))
                {
                    txtPassword.Focus();
                }
                else
                {
                    // Otherwise, trigger login
                    btnLogin_Click(null, null);
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles Enter key press in PasswordBox
        /// </summary>
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                btnLogin_Click(null, null);
                e.Handled = true;
            }
        }

        #endregion

        #region Login Logic

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = string.Empty;

            string userId = txtUserId.Text?.Trim() ?? string.Empty;
            string password = txtPassword.Password ?? string.Empty;

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

            // Get admin credentials from configuration
            try
            {
                var config = s_bl.Admin.GetConfig();
                
                // Validate admin credentials
                if (parsedId == config.ManagerId && password == config.ManagerPassword)
                {
                    HandleAdminLogin();
                }
                else
                {
                    // Courier login attempt
                    HandleCourierLogin(parsedId, password);
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"An error occurred: {ex.Message}";
            }
        }

        private void HandleAdminLogin()
        {
            // Admin login - only allow one MainWindow
            if (MainWindow.IsAdminWindowOpen)
            {
                lblError.Text = "Admin window is already open. Please close it first.";
                return;
            }

            MainWindow mainWindow = new();
            mainWindow.Show();
            
            // Bring MainWindow to front and send Login to back
            mainWindow.Activate();
            this.WindowState = WindowState.Minimized;
            
            // Clear login form for next user
            ClearLoginForm();
        }

        private void HandleCourierLogin(int courierId, string password)
        {
            try
            {
                var courier = s_bl.Couriers.Read(courierId);
                if (courier == null || courier.Password != password)
                {
                    lblError.Text = "Invalid ID or Password. Please try again.";
                    ClearLoginForm();
                    return;
                }

                if (!courier.IsActive)
                {
                    lblError.Text = "This courier account is inactive. Contact administration.";
                    ClearLoginForm();
                    return;
                }

                // Check if this courier already has a window open
                if (s_courierWindows.TryGetValue(courierId, out var existingWindow))
                {
                    if (existingWindow != null && !existingWindow.IsClosed)
                    {
                        // Bring existing window to front
                        existingWindow.WindowState = WindowState.Normal;
                        existingWindow.Activate();
                        
                        lblError.Text = "Courier window is already open.";
                        ClearLoginForm();
                        return;
                    }
                    else
                    {
                        // Remove closed window from dictionary
                        s_courierWindows.Remove(courierId);
                    }
                }

                // Create and show new CourierWindow
                var courierWindow = new Courier.CourierWindow(courierId, isReadOnly: false);
                s_courierWindows[courierId] = courierWindow;

                courierWindow.SetCourierMode(true);

                // Register window closed event to clean up dictionary
                courierWindow.Closed += (sender, e) =>
                {
                    s_courierWindows.Remove(courierId);
                };

                courierWindow.Show();
                
                // Bring CourierWindow to front and send Login to back
                courierWindow.Activate();
                this.WindowState = WindowState.Minimized;
                
                // Clear login form for next user
                ClearLoginForm();
            }
            catch (BO.BLException ex)
            {
                lblError.Text = "Invalid ID or Password. Please try again.";
                ClearLoginForm();
            }
            catch (Exception ex)
            {
                lblError.Text = $"An error occurred: {ex.Message}";
                ClearLoginForm();
            }
        }

        private void ClearLoginForm()
        {
            txtPassword.Clear();
            txtUserId.Clear();
            txtUserId.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion
    }
}
