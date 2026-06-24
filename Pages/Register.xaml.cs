using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using BowlingClub.Database;

namespace BowlingClub.Pages
{
    public partial class Register : Page
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public Register()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";
            lblSuccess.Text = "";

            string fullName = tbFullName.Text.Trim();
            string email = tbEmail.Text.Trim();
            string login = tbLogin.Text.Trim();
            string phone = tbPhone.Text.Trim();
            string password = pbPassword.Password;
            string confirm = pbConfirm.Password;

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Заполните все обязательные поля";
                return;
            }

            if (!IsValidEmail(email))
            {
                lblError.Text = "Неверный формат email";
                return;
            }

            if (password != confirm)
            {
                lblError.Text = "Пароли не совпадают";
                return;
            }

            if (password.Length < 4)
            {
                lblError.Text = "Пароль должен содержать не менее 4 символов";
                return;
            }

            try
            {
                if (_db.UserExists(login, email))
                {
                    lblError.Text = "Пользователь с таким логином или email уже существует";
                    return;
                }

                var user = _db.RegisterUserSimple(fullName, email, login, phone, password);
                if (user == null)
                {
                    lblError.Text = "Не удалось зарегистрировать пользователя";
                    return;
                }

                lblSuccess.Text = "Регистрация прошла успешно. Перенаправление на вход...";
                _db.LogAction(user.Id, "Регистрация", $"Пользователь {login} зарегистрирован");

                // Переход на страницу входа через короткую задержку
                var nav = NavigationService;
                if (nav != null)
                {
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        nav.Navigate(new Login());
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                lblError.Text = "Ошибка при регистрации: " + ex.Message;
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Login());
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }
    }
}
