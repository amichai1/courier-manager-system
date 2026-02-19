using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PL.Helpers
{
    /// <summary>
    /// Attached Properties for WPF controls.
    /// </summary>
    public static class AttachedProperties
    {
        #region Watermark / Placeholder Text

        /// <summary>
        /// Attached property for placeholder text in TextBox.
        /// </summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(AttachedProperties),
                new PropertyMetadata(string.Empty, OnPlaceholderChanged));

        public static string GetPlaceholder(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderProperty);
        }

        public static void SetPlaceholder(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderProperty, value);
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.GotFocus -= TextBox_GotFocus;
                textBox.LostFocus -= TextBox_LostFocus;

                if (!string.IsNullOrEmpty((string)e.NewValue))
                {
                    textBox.GotFocus += TextBox_GotFocus;
                    textBox.LostFocus += TextBox_LostFocus;

                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        ShowPlaceholder(textBox);
                    }
                }
            }
        }

        private static void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string placeholder = GetPlaceholder(textBox);
                if (textBox.Text == placeholder)
                {
                    textBox.Text = string.Empty;
                    textBox.Foreground = Brushes.Black;
                }
            }
        }

        private static void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                ShowPlaceholder(textBox);
            }
        }

        private static void ShowPlaceholder(TextBox textBox)
        {
            textBox.Text = GetPlaceholder(textBox);
            textBox.Foreground = Brushes.Gray;
        }

        #endregion

        #region Select All On Focus

        /// <summary>
        /// Attached property to select all text when TextBox gets focus.
        /// </summary>
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(AttachedProperties),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static bool GetSelectAllOnFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(SelectAllOnFocusProperty);
        }

        public static void SetSelectAllOnFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectAllOnFocusProperty, value);
        }

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotFocus += TextBox_SelectAll_GotFocus;
                    textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                }
                else
                {
                    textBox.GotFocus -= TextBox_SelectAll_GotFocus;
                    textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
                }
            }
        }

        private static void TextBox_SelectAll_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        #endregion

        #region Enter Key Command

        /// <summary>
        /// Attached property to execute command on Enter key press.
        /// </summary>
        public static readonly DependencyProperty EnterKeyCommandProperty =
            DependencyProperty.RegisterAttached(
                "EnterKeyCommand",
                typeof(ICommand),
                typeof(AttachedProperties),
                new PropertyMetadata(null, OnEnterKeyCommandChanged));

        public static ICommand GetEnterKeyCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(EnterKeyCommandProperty);
        }

        public static void SetEnterKeyCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(EnterKeyCommandProperty, value);
        }

        private static void OnEnterKeyCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.KeyDown -= Element_KeyDown_EnterCommand;

                if (e.NewValue != null)
                {
                    element.KeyDown += Element_KeyDown_EnterCommand;
                }
            }
        }

        private static void Element_KeyDown_EnterCommand(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is DependencyObject d)
            {
                ICommand command = GetEnterKeyCommand(d);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Corner Radius

        /// <summary>
        /// Attached property for corner radius on any control.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.RegisterAttached(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(AttachedProperties),
                new PropertyMetadata(new CornerRadius(0)));

        public static CornerRadius GetCornerRadius(DependencyObject obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }

        #endregion

        #region Is Loading

        /// <summary>
        /// Attached property to indicate loading state.
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.RegisterAttached(
                "IsLoading",
                typeof(bool),
                typeof(AttachedProperties),
                new PropertyMetadata(false));

        public static bool GetIsLoading(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLoadingProperty);
        }

        public static void SetIsLoading(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLoadingProperty, value);
        }

        #endregion

        #region Icon

        /// <summary>
        /// Attached property for icon on buttons/controls.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(AttachedProperties),
                new PropertyMetadata(null));

        public static object GetIcon(DependencyObject obj)
        {
            return obj.GetValue(IconProperty);
        }

        public static void SetIcon(DependencyObject obj, object value)
        {
            obj.SetValue(IconProperty, value);
        }

        #endregion
    }
}
