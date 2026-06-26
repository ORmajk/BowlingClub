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
    public partial class InventoryEditPage : Page
    {
        private InventoryItems _currentItem;
        private bool _isNew = false;

        public InventoryEditPage(InventoryItems selectedItem)
        {
            InitializeComponent();

            
            try
            {
                lblError.Text = "";
                cbStatus.ItemsSource = AppConnect.model.InventoryStatuses.ToList();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки статусов: {ex.Message}";
            }

            if (selectedItem == null)
            {
                _currentItem = new InventoryItems();
                _currentItem.CreatedAt = DateTime.Now;
                _isNew = true;
                tbTitle.Text = "Добавление инвентаря";
            }
            else
            {
                _currentItem = selectedItem;
                _isNew = false;
                tbTitle.Text = "Редактирование инвентаря";
            }

            txtItemName.Text = _currentItem.Name;
            txtType.Text = _currentItem.Type;
            txtSizeOrWeight.Text = _currentItem.SizeOrWeight;
            txtQuantity.Text = _currentItem.Quantity.ToString();
            cbStatus.SelectedValue = _currentItem.StatusId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

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

            _currentItem.Name = txtItemName.Text;
            _currentItem.Type = txtType.Text;
            _currentItem.SizeOrWeight = txtSizeOrWeight.Text;
            _currentItem.Quantity = quantity;
            _currentItem.StatusId = (int)cbStatus.SelectedValue;

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
