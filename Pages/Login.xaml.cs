using BowlingClub.Database;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BowlingClub.Pages
{
    public partial class Login : Page
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public Login()
        {
            InitializeComponent();
        }

        // Кнопка "Войти"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            string login = tbLogin.Text.Trim();
            string password = pbPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Введите логин и пароль";
                return;
            }

            var user = _db.Authenticate(login, password);
            if (user != null)
            {
                _db.LogAction(user.Id, "Вход", $"Успешная авторизация: {login}");

                NavigationService?.Navigate(new AllEntitiesPage(user));
            }
            else
            {
                lblError.Text = "Неверный логин или пароль";
            }
        }

        // Кнопка "Регистрация" — переходим на страницу регистрации
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Register());
        }
    }
}
