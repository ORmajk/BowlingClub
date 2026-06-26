using BowlingClub.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BowlingClub.Pages
{
    public partial class BookingEditPage : Page
    {
        private Bookings _currentBooking;
        private bool _isNew = false;

        private List<BookingItems> _itemsList = new List<BookingItems>();

        private List<BookingItems> _availableServices = new List<BookingItems>
        {
            new BookingItems { ItemName = "Аренда дорожки", Price = 1200.00m },
            new BookingItems { ItemName = "Аренда дорожки VIP", Price = 2000.00m },
            new BookingItems { ItemName = "Аренда обуви", Price = 250.00m },
            new BookingItems { ItemName = "Аренда дорожки детской", Price = 900.00m }
        };

        public BookingEditPage(Bookings selectedBooking)
        {
            InitializeComponent();
            lblError.Text = "";

            try
            {
                LoadReferenceData();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки справочников: {ex.Message}";
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (selectedBooking == null)
            {
                _currentBooking = new Bookings();
                _currentBooking.StartTime = DateTime.Now;
                _isNew = true;
                tbTitle.Text = "Добавление брони";
                txtBookingNumber.Text = "BR-" + DateTime.Now.ToString("yyMMddHHmmss");

                cbStatus.SelectedItem = "Создан";
                cbPaymentMethod.SelectedIndex = 0;
            }
            else
            {
                _currentBooking = selectedBooking;
                _isNew = false;
                tbTitle.Text = "Редактирование брони";
                txtBookingNumber.Text = _currentBooking.BookingNumber;

                _itemsList = AppConnect.model.BookingItems.Where(bi => bi.BookingId == _currentBooking.Id).ToList();
            }

            if (cbClient.ItemsSource != null && cbClient.ItemsSource.Cast<object>().Any())
            {
                cbClient.SelectedValue = _currentBooking.ClientId;
            }

            if (cbLane.ItemsSource != null && cbLane.ItemsSource.Cast<object>().Any())
            {
                cbLane.SelectedValue = _currentBooking.LaneId;
            }

            dpStartDate.SelectedDate = _currentBooking.StartTime;
            txtDuration.Text = _currentBooking.DurationMinutes == 0 ? "60" : _currentBooking.DurationMinutes.ToString();

            if (!_isNew)
            {
                if (!string.IsNullOrEmpty(_currentBooking.PaymentMethod))
                {
                    SelectComboBoxItem(cbPaymentMethod, _currentBooking.PaymentMethod);
                }
                if (!string.IsNullOrEmpty(_currentBooking.Status))
                {
                    SelectComboBoxItem(cbStatus, _currentBooking.Status);
                }
            }

            UpdateItemsTable();
        }

        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            if (comboBox.ItemsSource == null) return;

            foreach (var item in comboBox.ItemsSource)
            {
                if (item.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.Text = value;
        }

        private void LoadReferenceData()
        {
            var clients = AppConnect.model.Clients.ToList();
            cbClient.ItemsSource = clients;
            cbClient.DisplayMemberPath = "FullName";
            cbClient.SelectedValuePath = "Id";

            System.Diagnostics.Debug.WriteLine($"Загружено клиентов: {clients.Count}");

            var lanes = AppConnect.model.Lanes.ToList();
            cbLane.ItemsSource = lanes;
            cbLane.DisplayMemberPath = "LaneNumber";
            cbLane.SelectedValuePath = "Id";

            System.Diagnostics.Debug.WriteLine($"Загружено дорожек: {lanes.Count}");

            var allStatuses = new List<string>
            {
                "Создан",
                "Оплачен",
                "Отменен"
            };

            var dbStatuses = AppConnect.model.Bookings
                .Where(b => b.Status != null && b.Status != "")
                .Select(b => b.Status)
                .Distinct()
                .ToList();

            foreach (var status in dbStatuses)
            {
                if (!allStatuses.Contains(status))
                {
                    allStatuses.Add(status);
                }
            }

            cbStatus.ItemsSource = allStatuses.OrderBy(s => s).ToList();
            System.Diagnostics.Debug.WriteLine($"Загружено статусов: {allStatuses.Count}");
            cbStatus.SelectedIndex = 0;

            var allPaymentMethods = new List<string>
            {
                "Наличные",
                "Карта",
                "Онлайн"
            };

            var dbPaymentMethods = AppConnect.model.Bookings
                .Where(b => b.PaymentMethod != null && b.PaymentMethod != "")
                .Select(b => b.PaymentMethod)
                .Distinct()
                .ToList();

            foreach (var method in dbPaymentMethods)
            {
                if (!allPaymentMethods.Contains(method))
                {
                    allPaymentMethods.Add(method);
                }
            }

            cbPaymentMethod.ItemsSource = allPaymentMethods.OrderBy(m => m).ToList();
            System.Diagnostics.Debug.WriteLine($"Загружено способов оплаты: {allPaymentMethods.Count}");
            cbPaymentMethod.SelectedIndex = 0;

            // Загрузка услуг (прейскурант)
            cbItemName.ItemsSource = _availableServices;
            cbItemName.DisplayMemberPath = "ItemName";

            System.Diagnostics.Debug.WriteLine($"Загружено услуг: {_availableServices.Count}");
        }

        private void cbItemName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbItemName.SelectedItem is BookingItems selectedService)
            {
                txtItemPrice.Text = selectedService.Price.ToString("F2");

                if (selectedService.ItemName.ToLower().Contains("дорожки"))
                {
                    txtItemQty.Text = "1";
                    txtItemQty.IsReadOnly = true;
                    txtItemQty.Background = Brushes.LightGray;
                }
                else
                {
                    txtItemQty.Text = "1";
                    txtItemQty.IsReadOnly = false;
                    txtItemQty.Background = Brushes.White;
                }
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            if (cbItemName.SelectedItem == null)
            {
                lblError.Text = "Ошибка: Выберите услугу из списка.";
                return;
            }
            if (!int.TryParse(txtItemQty.Text, out int qty) || qty <= 0)
            {
                lblError.Text = "Ошибка: Количество должно быть целым числом больше нуля.";
                return;
            }
            if (!decimal.TryParse(txtItemPrice.Text, out decimal price) || price < 0)
            {
                lblError.Text = "Ошибка: Некорректная цена.";
                return;
            }

            var selectedService = (BookingItems)cbItemName.SelectedItem;

            var newItem = new BookingItems
            {
                ItemName = selectedService.ItemName,
                Quantity = qty,
                Price = price,
                Total = qty * price
            };

            _itemsList.Add(newItem);
            UpdateItemsTable();

            cbItemName.SelectedIndex = -1;
            txtItemQty.Text = "1";
            txtItemQty.IsReadOnly = false;
            txtItemQty.Background = Brushes.White;
            txtItemPrice.Clear();
        }

        private void UpdateItemsTable()
        {
            dgBookingItems.ItemsSource = null;
            dgBookingItems.ItemsSource = _itemsList;

            decimal sum = _itemsList.Sum(item => item.Total);
            _currentBooking.TotalAmount = sum;
            lblTotalAmount.Text = sum.ToString("F2") + " руб.";
        }

        private void RemoveSelectedGridItem_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            if (dgBookingItems.SelectedItem is BookingItems selectedItem)
            {
                _itemsList.Remove(selectedItem);
                UpdateItemsTable();
            }
            else
            {
                lblError.Text = "Ошибка: Выберите строку в таблице услуг для удаления.";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            if (!ValidateForm()) return;
            _currentBooking.BookingNumber = txtBookingNumber.Text;
            _currentBooking.ClientId = (int)cbClient.SelectedValue;
            _currentBooking.LaneId = (int)cbLane.SelectedValue;
            _currentBooking.StartTime = dpStartDate.SelectedDate.Value;
            _currentBooking.DurationMinutes = int.Parse(txtDuration.Text);
            _currentBooking.PaymentMethod = cbPaymentMethod.SelectedItem?.ToString() ?? cbPaymentMethod.Text;
            _currentBooking.Status = cbStatus.SelectedItem?.ToString() ?? cbStatus.Text;

            SaveToDatabase();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtBookingNumber.Text))
            {
                lblError.Text = "Ошибка: Заполните номер бронирования.";
                return false;
            }
            if (cbClient.SelectedValue == null)
            {
                lblError.Text = "Ошибка: Выберите клиента из списка.";
                return false;
            }
            if (cbLane.SelectedValue == null)
            {
                lblError.Text = "Ошибка: Выберите номер дорожки.";
                return false;
            }
            if (dpStartDate.SelectedDate == null)
            {
                lblError.Text = "Ошибка: Укажите дату начала бронирования.";
                return false;
            }
            if (!int.TryParse(txtDuration.Text, out int duration) || duration <= 0)
            {
                lblError.Text = "Ошибка: Укажите корректную длительность сеанса (в минутах).";
                return false;
            }

            string paymentMethod = cbPaymentMethod.SelectedItem?.ToString() ?? cbPaymentMethod.Text;
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                lblError.Text = "Ошибка: Выберите способ оплаты.";
                return false;
            }

            string status = cbStatus.SelectedItem?.ToString() ?? cbStatus.Text;
            if (string.IsNullOrWhiteSpace(status))
            {
                lblError.Text = "Ошибка: Выберите статус бронирования.";
                return false;
            }

            if (!_itemsList.Any())
            {
                lblError.Text = "Ошибка: Добавьте хотя бы одну позицию услуги в чек бронирования.";
                return false;
            }

            return true;
        }

        private void SaveToDatabase()
        {
            try
            {
                if (_isNew)
                {
                    AppConnect.model.Bookings.Add(_currentBooking);
                    AppConnect.model.SaveChanges();
                }
                else
                {
                    var existingBooking = AppConnect.model.Bookings.Find(_currentBooking.Id);
                    if (existingBooking != null)
                    {
                        AppConnect.model.Entry(existingBooking).CurrentValues.SetValues(_currentBooking);
                        AppConnect.model.SaveChanges();
                    }

                    var itemsToRemove = AppConnect.model.BookingItems
                        .Where(bi => bi.BookingId == _currentBooking.Id)
                        .ToList();

                    foreach (var oldItem in itemsToRemove)
                    {
                        AppConnect.model.BookingItems.Remove(oldItem);
                    }
                    AppConnect.model.SaveChanges();
                }

                foreach (var item in _itemsList)
                {
                    var dbItem = new BookingItems
                    {
                        BookingId = _currentBooking.Id,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Total = item.Total
                    };
                    AppConnect.model.BookingItems.Add(dbItem);
                }

                AppConnect.model.SaveChanges();

                MessageBox.Show("Бронирование успешно сохранено!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка сохранения: {ex.InnerException?.Message ?? ex.Message}";
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}