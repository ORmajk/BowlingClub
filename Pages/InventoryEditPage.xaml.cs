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
    /// Логика взаимодействия для InventoryEditPage.xaml
    /// </summary>
    public partial class InventoryEditPage : Page
    {
        private InventoryItems _currentItem;
        private bool _isNew = false;

        public InventoryEditPage(InventoryItems selectedItem)
        {
            InitializeComponent();

            // Загрузка статусов инвентаря в ComboBox из БД
            try
            {
                lblError.Text = "";
                cbStatus.ItemsSource = AppConnect.model.InventoryStatuses.ToList();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки статусов: {ex.Message}";
            }

            // Определение режима: Добавление или Редактирование
            if (selectedItem == null)
            {
                _currentItem = new InventoryItems();
                _currentItem.CreatedAt = DateTime.Now; // Заполняем обязательную дату создания
                _isNew = true;
                tbTitle.Text = "Добавление инвентаря";
            }
            else
            {
                _currentItem = selectedItem;
                _isNew = false;
                tbTitle.Text = "Редактирование инвентаря";
            }

            // Заполнение полей данными
            txtItemName.Text = _currentItem.Name;
            txtType.Text = _currentItem.Type;
            txtSizeOrWeight.Text = _currentItem.SizeOrWeight;
            txtQuantity.Text = _currentItem.Quantity.ToString();
            cbStatus.SelectedValue = _currentItem.StatusId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = ""; // Сброс ошибок перед валидацией

            // 1. Валидация полей через текстовый блок ошибок
            if (string.IsNullOrWhiteSpace(txtItemName.Text))
            {
                lblError.Text = "Ошибка: Введите название инвентаря.";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtType.Text))
            {
                lblError.Text = "Ошибка: Укажите тип инвентаря.";
                return;
            }
            if (!int.TryParse(txtQuantity.Text, out int quantity) || quantity < 0)
            {
                lblError.Text = "Ошибка: Количество должно быть целым положительным числом.";
                return;
            }
            if (cbStatus.SelectedValue == null)
            {
                lblError.Text = "Ошибка: Выберите статус инвентаря.";
                return;
            }

            // 2. Перенос данных с формы в сущность
            _currentItem.Name = txtItemName.Text;
            _currentItem.Type = txtType.Text;
            _currentItem.SizeOrWeight = txtSizeOrWeight.Text;
            _currentItem.Quantity = quantity;
            _currentItem.StatusId = (int)cbStatus.SelectedValue;

            // 3. Сохранение изменений в базу данных через ADO.NET
            try
            {
                if (_isNew)
                {
                    AppConnect.model.InventoryItems.Add(_currentItem);
                }

                AppConnect.model.SaveChanges();

                MessageBox.Show("Инвентарь успешно сохранен!");
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
