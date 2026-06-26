using BowlingClub.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BowlingClub.Pages
{
    /// <summary>
    /// Логика взаимодействия для ClientEditPage.xaml
    /// </summary>
    public partial class ClientEditPage : Page
    {
        private Clients _currentClient;
        private bool _isNew = false;

        public ClientEditPage(Clients selectedClient)
        {
            InitializeComponent();

            lblError.Text = "";

            // Определение режима работы страницы
            if (selectedClient == null)
            {
                _currentClient = new Clients();
                _currentClient.RegistrationDate = DateTime.Now; // Устанавливаем текущую дату для нового клиента
                _currentClient.Points = 0; // Изначально баллов 0
                _isNew = true;
                tbTitle.Text = "Добавление клиента";
            }
            else
            {
                _currentClient = selectedClient;
                _isNew = false;
                tbTitle.Text = "Редактирование клиента";
            }

            // Заполнение полей формы данными из сущности
            txtFullName.Text = _currentClient.FullName;
            txtPhone.Text = _currentClient.Phone;
            txtEmail.Text = _currentClient.Email;
            txtPoints.Text = _currentClient.Points.ToString();
            chbSubscribed.IsChecked = _currentClient.IsSubscribedToAl;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = ""; // Очищаем поле ошибок

            // 1. Валидация полей через текстовый блок lblError
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                lblError.Text = "Ошибка: Заполните ФИО клиента.";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                lblError.Text = "Ошибка: Укажите номер телефона.";
                return;
            }
            if (!int.TryParse(txtPoints.Text, out int points) || points < 0)
            {
                lblError.Text = "Ошибка: Бонусные баллы должны быть целым положительным числом.";
                return;
            }

            // 2. Перенос измененных данных с формы в объект
            _currentClient.FullName = txtFullName.Text;
            _currentClient.Phone = txtPhone.Text;
            _currentClient.Email = txtEmail.Text;
            _currentClient.Points = points;
            _currentClient.IsSubscribedToAl = chbSubscribed.IsChecked ?? false;

            // 3. Запись в БД ADO.NET
            try
            {
                if (_isNew)
                {
                    AppConnect.model.Clients.Add(_currentClient);
                }

                AppConnect.model.SaveChanges();

                MessageBox.Show("Данные клиента успешно сохранены!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка сохранения в базу данных: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
