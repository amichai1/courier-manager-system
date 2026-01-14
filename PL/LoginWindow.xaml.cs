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
        private static Dictionary<int, WeakReference<Courier.CourierWindow>> s_courierWindows = new();
        
        // Lock for thread-safe access to the static dictionary
        private static readonly object s_dictionaryLock = new object();

        public LoginWindow()
        {
            InitializeComponent();
        }

        #region Window Management

        /// <summary>
        /// Brings the Login window to front when user wants to login again
        /// </summary>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            
            // When window is activated (brought to front), ensure it's restored and visible
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            
            // Bring to front
            Topmost = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Topmost = false;
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        #endregion

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
            
            // Bring MainWindow to front and minimize Login
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

                // Check if this courier already has a window open (thread-safe)
                lock (s_dictionaryLock)
                {
                    if (s_courierWindows.TryGetValue(courierId, out var weakRef))
                    {
                        if (weakRef.TryGetTarget(out var existingWindow) && !existingWindow.IsClosed)
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
                            // Remove dead reference from dictionary
                            s_courierWindows.Remove(courierId);
                        }
                    }
                }

                // Create the courier window (no longer in try block for cleanup safety)
                Courier.CourierWindow courierWindow = null;
                EventHandler closedHandler = null;

                try
                {
                    courierWindow = new Courier.CourierWindow(courierId, isReadOnly: false);
                    
                    // Register the closed event BEFORE any operation that might throw
                    closedHandler = (sender, e) =>
                    {
                        lock (s_dictionaryLock)
                        {
                            s_courierWindows.Remove(courierId);
                        }
                    };
                    courierWindow.Closed += closedHandler;

                    // Add to dictionary before any operations
                    lock (s_dictionaryLock)
                    {
                        s_courierWindows[courierId] = new WeakReference<Courier.CourierWindow>(courierWindow);
                    }

                    // Now perform setup operations that might throw
                    courierWindow.SetCourierMode(true);
                    courierWindow.Show();
                    
                    // Bring CourierWindow to front and minimize Login
                    courierWindow.Activate();
                    this.WindowState = WindowState.Minimized;
                    
                    // Clear login form for next user
                    ClearLoginForm();
                }
                catch (Exception setupEx)
                {
                    // If anything fails, properly clean up
                    if (courierWindow != null)
                    {
                        if (closedHandler != null)
                        {
                            courierWindow.Closed -= closedHandler;
                        }

                        lock (s_dictionaryLock)
                        {
                            s_courierWindows.Remove(courierId);
                        }

                        courierWindow.Close();
                    }

                    lblError.Text = $"Failed to open courier window: {setupEx.Message}";
                }
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
