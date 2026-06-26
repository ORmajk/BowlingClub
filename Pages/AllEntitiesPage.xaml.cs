using BowlingClub.AppData;
using BowlingClub.Database;
using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BowlingClub.Pages
{
    public partial class AllEntitiesPage : Page
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();
        private readonly Users _currentUser;

        public AllEntitiesPage(Users currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            ConfigurePermissions();

            this.Loaded += AllEntitiesPage_Loaded;
        }

        private void AllEntitiesPage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshAllData();
            txtStatus.Text = $"Данные загружены: {DateTime.Now:g}";
        }

        private void Page_Navigated(object sender, NavigationEventArgs e)
        {
            RefreshAllData();
            txtStatus.Text = $"Данные обновлены: {DateTime.Now:g}";
        }

        private void ConfigurePermissions()
        {
            SetAllCrudEnabled(false, false, false);

            if (_currentUser == null) return;

            if (_currentUser.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                SetAllCrudEnabled(true, true, true);
                return;
            }

            if (_currentUser.Role.Equals("Moderator", StringComparison.OrdinalIgnoreCase) ||
                _currentUser.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                SetAllCrudEnabled(true, true, false);
                btnCancelBooking.IsEnabled = false;
                return;
            }

            SetAllCrudEnabled(false, false, false);
        }

        private void SetAllCrudEnabled(bool canAdd, bool canEdit, bool canDelete)
        {
            btnAddLane.IsEnabled = canAdd;
            btnEditLane.IsEnabled = canEdit;
            btnDeleteLane.IsEnabled = canDelete;

            btnAddGame.IsEnabled = canAdd;
            btnEditGame.IsEnabled = canEdit;
            btnDeleteGame.IsEnabled = canDelete;

            btnAddInventory.IsEnabled = canAdd;
            btnEditInventory.IsEnabled = canEdit;
            btnDeleteInventory.IsEnabled = canDelete;

            btnAddClient.IsEnabled = canAdd;
            btnEditClient.IsEnabled = canEdit;
            btnDeleteClient.IsEnabled = canDelete;

            btnAddBooking.IsEnabled = canAdd;
            btnEditBooking.IsEnabled = canEdit;
            btnCancelBooking.IsEnabled = canDelete;
        }

        private void RefreshAllData()
        {
            try
            {
                foreach (var entry in AppConnect.model.ChangeTracker.Entries().ToList())
                {
                    if (entry.State != System.Data.Entity.EntityState.Detached)
                    {
                        entry.Reload();
                    }
                }
                LoadLanes();
                LoadGames();
                LoadInventory();
                LoadClients();
                LoadBookings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления данных: {ex.Message}");
            }
        }

        private void LoadLanes()
        {
            try
            {
                var lanes = _db.GetLanes(txtGlobalSearch.Text).ToList();
                dgLanes.ItemsSource = null;
                dgLanes.ItemsSource = lanes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки дорожек: {ex.Message}");
            }
        }

        private void LoadGames()
        {
            try
            {
                var games = _db.GetEvents(txtGlobalSearch.Text).ToList();
                dgGames.ItemsSource = null;
                dgGames.ItemsSource = games;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки событий: {ex.Message}");
            }
        }

        private void LoadInventory()
        {
            try
            {
                var inventory = _db.GetInventoryItems(txtGlobalSearch.Text).ToList();
                dgInventory.ItemsSource = null;
                dgInventory.ItemsSource = inventory;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки инвентаря: {ex.Message}");
            }
        }

        private void LoadClients()
        {
            try
            {
                var clients = _db.GetClients(txtGlobalSearch.Text).ToList();
                dgClients.ItemsSource = null;
                dgClients.ItemsSource = clients;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки клиентов: {ex.Message}");
            }
        }

        private void LoadBookings()
        {
            try
            {
                var bookings = _db.GetBookings(txtGlobalSearch.Text).ToList();
                dgBookings.ItemsSource = null;
                dgBookings.ItemsSource = bookings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки бронирований: {ex.Message}");
            }
        }

        //LANES
        private void AddLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Moderator", "Manager")) { ShowNoRights(); return; }
            NavigationService.Navigate(new LaneEditPage(null));
        }

        private void EditLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Moderator", "Manager")) { ShowNoRights(); return; }
            if (dgLanes.SelectedItem is Lanes lane)
            {
                NavigationService.Navigate(new LaneEditPage(lane));
            }
            else MessageBox.Show("Выберите дорожку.");
        }

        private void DeleteLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgLanes.SelectedItem is Lanes lane)
            {
                if (MessageBox.Show($"Удалить дорожку №{lane.LaneNumber}?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteLane(lane.Id))
                    {
                        LoadLanes();
                        _db.LogAction(_currentUser.Id, "Удаление", $"Дорожка №{lane.LaneNumber}");
                    }
                    else MessageBox.Show("Невозможно удалить дорожку (есть активные бронирования).");
                }
            }
            else MessageBox.Show("Выберите дорожку.");
        }

        //EVENTS
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Moderator", "Manager")) { ShowNoRights(); return; }
            NavigationService.Navigate(new EventEditPage(null));
        }

        private void EditGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Moderator", "Manager")) { ShowNoRights(); return; }
            if (dgGames.SelectedItem is Events ev)
            {
                NavigationService.Navigate(new EventEditPage(ev));
            }
            else MessageBox.Show("Выберите событие.");
        }

        private void DeleteGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgGames.SelectedItem is Events ev)
            {
                if (MessageBox.Show($"Удалить событие \"{ev.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteEvent(ev.Id))
                    {
                        LoadGames();
                        _db.LogAction(_currentUser.Id, "Удаление", $"Событие Id={ev.Id}");
                    }
                    else MessageBox.Show("Невозможно удалить событие (есть участники).");
                }
            }
            else MessageBox.Show("Выберите событие.");
        }

        //INVENTORY
        private void AddInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            NavigationService.Navigate(new InventoryEditPage(null));
        }

        private void EditInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgInventory.SelectedItem is InventoryItems selectedItem)
            {
                NavigationService.Navigate(new InventoryEditPage(selectedItem));
            }
            else MessageBox.Show("Выберите элемент инвентаря из таблицы.");
        }

        private void DeleteInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgInventory.SelectedItem is InventoryItems it)
            {
                if (MessageBox.Show($"Удалить \"{it.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteInventoryItem(it.Id))
                    {
                        LoadInventory();
                        _db.LogAction(_currentUser.Id, "Удаление", $"Инвентарь Id={it.Id}");
                    }
                }
            }
            else MessageBox.Show("Выберите элемент.");
        }

        //CLIENTS
        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager", "Moderator")) { ShowNoRights(); return; }
            NavigationService.Navigate(new ClientEditPage(null));
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager", "Moderator")) { ShowNoRights(); return; }
            if (dgClients.SelectedItem is Clients selectedClient)
            {
                NavigationService.Navigate(new ClientEditPage(selectedClient));
            }
            else MessageBox.Show("Выберите клиента из таблицы для редактирования.");
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgClients.SelectedItem is Clients c)
            {
                if (MessageBox.Show($"Удалить клиента \"{c.FullName}\"?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteClient(c.Id))
                    {
                        LoadClients();
                        _db.LogAction(_currentUser.Id, "Удаление", $"Клиент Id={c.Id}");
                    }
                }
            }
            else MessageBox.Show("Выберите клиента.");
        }

        //BOOKINGS
        private void AddBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager", "Moderator")) { ShowNoRights(); return; }
            NavigationService.Navigate(new BookingEditPage(null));
        }

        private void EditBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager", "Moderator")) { ShowNoRights(); return; }
            if (dgBookings.SelectedItem is Bookings selectedBooking)
            {
                NavigationService.Navigate(new BookingEditPage(selectedBooking));
            }
            else MessageBox.Show("Выберите бронирование из таблицы для редактирования.");
        }

        private void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgBookings.SelectedItem is Bookings b)
            {
                if (MessageBox.Show($"Отменить бронирование Id={b.Id}?", "Подтверждение",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.CancelBooking(b.Id))
                    {
                        LoadBookings();
                        _db.LogAction(_currentUser.Id, "Отмена", $"Бронирование Id={b.Id}");
                    }
                }
            }
            else MessageBox.Show("Выберите бронирование.");
        }

        private void ClientsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Events currentEvent)
            {
                textBlock.Text = DatabaseHelper.GetClientsList(currentEvent);
            }
        }

        private void BookingItemsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Bookings currentBooking)
            {
                textBlock.Text = DatabaseHelper.GetBookingItemsList(currentBooking);
            }
        }

        private void OpenReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Bookings selectedBooking)
            {
                string status = selectedBooking.Status?.Trim();

                if (status == "Оплачен")
                {
                    NavigationService.Navigate(new ReceiptPage(selectedBooking));
                }
                else
                {
                    MessageBox.Show($"Невозможно просмотреть чек. Данное бронирование еще не оплачено! Текущий статус: \"{status}\".",
                                    "Внимание",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                }
            }
        }

        private bool HasRole(params string[] roles)
        {
            if (_currentUser == null) return false;
            return roles.Any(r => r.Equals(_currentUser.Role, StringComparison.OrdinalIgnoreCase));
        }

        private void ShowNoRights()
        {
            MessageBox.Show("Недостаточно прав.", "Доступ запрещён",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void RefreshAll_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllData();
            txtStatus.Text = $"Данные обновлены: {DateTime.Now:g}";
        }

        private void txtGlobalSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshAllData();
        }
    }
}