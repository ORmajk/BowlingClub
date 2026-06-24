using BowlingClub.AppData;
using BowlingClub.Database;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;


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
            LoadAll();
        }

        private void ConfigurePermissions()
        {
            // По умолчанию — все отключены
            SetAllCrudEnabled(false, false, false);

            if (_currentUser == null) return;

            if (string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                SetAllCrudEnabled(canAdd: true, canEdit: true, canDelete: true);
            }
            else if (string.Equals(_currentUser.Role, "Manager", StringComparison.OrdinalIgnoreCase))
            {
                SetAllCrudEnabled(canAdd: true, canEdit: true, canDelete: false);
                // Для бронирований менеджеру можно разрешить отмену, если нужно:
                btnCancelBooking.IsEnabled = false; // оставить false по требованию; можно поставить true
            }
            else
            {
                SetAllCrudEnabled(canAdd: false, canEdit: false, canDelete: false);
            }
        }

        private void SetAllCrudEnabled(bool canAdd, bool canEdit, bool canDelete)
        {
            // Lanes
            btnAddLane.IsEnabled = canAdd;
            btnEditLane.IsEnabled = canEdit;
            btnDeleteLane.IsEnabled = canDelete;

            // Games
            btnAddGame.IsEnabled = canAdd;
            btnEditGame.IsEnabled = canEdit;
            btnDeleteGame.IsEnabled = canDelete;

            // Inventory
            btnAddInventory.IsEnabled = canAdd;
            btnEditInventory.IsEnabled = canEdit;
            btnDeleteInventory.IsEnabled = canDelete;

            // Clients
            btnAddClient.IsEnabled = canAdd;
            btnEditClient.IsEnabled = canEdit;
            btnDeleteClient.IsEnabled = canDelete;

            // Bookings
            btnAddBooking.IsEnabled = canAdd;
            btnEditBooking.IsEnabled = canEdit;
            btnCancelBooking.IsEnabled = canDelete;
        }

        private void LoadAll()
        {
            LoadLanes();
            LoadGames();
            LoadInventory();
            LoadClients();
            LoadBookings();
            txtStatus.Text = $"Данные загружены: {DateTime.Now:g}";
        }

        private void RefreshAll_Click(object sender, RoutedEventArgs e) => LoadAll();

        #region Load methods
        private void LoadLanes()
        {
            dgLanes.ItemsSource = _db.GetLanes(txtGlobalSearch.Text).ToList();
        }

        private void LoadGames()
        {
            //dgGames.ItemsSource = _db.GetGames(txtGlobalSearch.Text).ToList();
        }

        private void LoadInventory()
        {
            dgInventory.ItemsSource = _db.GetInventoryItems(txtGlobalSearch.Text).ToList();
        }

        private void LoadClients()
        {
            dgClients.ItemsSource = _db.GetClients(txtGlobalSearch.Text).ToList();
        }

        private void LoadBookings()
        {
            dgBookings.ItemsSource = _db.GetBookings(txtGlobalSearch.Text).ToList();
        }
        #endregion

        #region Lanes handlers
        private void AddLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            //var dlg = new LaneDialog(null) { Owner = Window.GetWindow(this) };
           //if (dlg.ShowDialog() == true)
            {
                LoadLanes();
                _db.LogAction(_currentUser?.Id ?? 0, "Добавление", "Добавлена дорожка");
            }
        }

        private void EditLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgLanes.SelectedItem is Lanes lane)
            {
                //var dlg = new LaneDialog(lane) { Owner = Window.GetWindow(this) };
             //   if (dlg.ShowDialog() == true)
                {
                    LoadLanes();
                    _db.LogAction(_currentUser?.Id ?? 0, "Редактирование", $"Отредактирована дорожка №{lane.LaneNumber}");
                }
            }
            else MessageBox.Show("Выберите дорожку.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteLane_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgLanes.SelectedItem is Lanes lane)
            {
                if (MessageBox.Show($"Удалить дорожку №{lane.LaneNumber}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    //if (_db.DeleteLane(lane.Id))
                    {
                        LoadLanes();
                        _db.LogAction(_currentUser?.Id ?? 0, "Удаление", $"Удалена дорожка №{lane.LaneNumber}");
                    }
                 //   else MessageBox.Show("Невозможно удалить дорожку (возможно есть активные бронирования).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("Выберите дорожку.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Games handlers (скелет)
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
           //var dlg = new GameDialog(null) { Owner = Window.GetWindow(this) };
            //if (dlg.ShowDialog() == true)
            {
                LoadGames();
                _db.LogAction(_currentUser?.Id ?? 0, "Добавление", "Добавлена игра");
            }
        }

        private void EditGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgGames.SelectedItem is Events g)
            {
               // var dlg = new GameDialog(g) { Owner = Window.GetWindow(this) };
               // if (dlg.ShowDialog() == true)
                {
                    LoadGames();
                    _db.LogAction(_currentUser?.Id ?? 0, "Редактирование", $"Отредактирована игра Id={g.Id}");
                }
            }
            else MessageBox.Show("Выберите игру.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteGame_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgGames.SelectedItem is Events g)
            {
                if (MessageBox.Show($"Удалить игру \"{g.Name}\"?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteGame(g.Id))
                    {
                        LoadGames();
                        _db.LogAction(_currentUser?.Id ?? 0, "Удаление", $"Удалена игра Id={g.Id}");
                    }
                    else MessageBox.Show("Невозможно удалить игру.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("Выберите игру.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Inventory handlers
        private void AddInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            //var dlg = new InventoryDialog(null) { Owner = Window.GetWindow(this) };
            //if (dlg.ShowDialog() == true)
            {
                LoadInventory();
                _db.LogAction(_currentUser?.Id ?? 0, "Добавление", "Добавлен элемент инвентаря");
            }
        }

        private void EditInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgInventory.SelectedItem is InventoryItems it)
            {
                ////var dlg = new InventoryDialog(it) { Owner = Window.GetWindow(this) };
                ///if (dlg.ShowDialog() == true)
                {
                    LoadInventory();
                    _db.LogAction(_currentUser?.Id ?? 0, "Редактирование", $"Отредактирован инвентарь Id={it.Id}");
                }
            }
            else MessageBox.Show("Выберите элемент инвентаря.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteInventory_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgInventory.SelectedItem is InventoryItems it)
            {
                if (MessageBox.Show($"Удалить \"{it.Type}\"?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteInventoryItem(it.Id))
                    {
                        LoadInventory();
                        _db.LogAction(_currentUser?.Id ?? 0, "Удаление", $"Удалён инвентарь Id={it.Id}");
                    }
                    else MessageBox.Show("Невозможно удалить элемент инвентаря.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("Выберите элемент инвентаря.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Clients handlers
        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            ////var dlg = new ClientDialog(null) { Owner = Window.GetWindow(this) };
          //  if (dlg.ShowDialog() == true)
            {
                LoadClients();
                _db.LogAction(_currentUser?.Id ?? 0, "Добавление", "Добавлен клиент");
            }
        }

        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgClients.SelectedItem is Clients c)
            {
            //   var dlg = new ClientDialog(c) { Owner = Window.GetWindow(this) };
            //    if (dlg.ShowDialog() == true)
                {
                    LoadClients();
                    _db.LogAction(_currentUser?.Id ?? 0, "Редактирование", $"Отредактирован клиент Id={c.Id}");
                }
            }
            else MessageBox.Show("Выберите клиента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgClients.SelectedItem is Clients c)
            {
                if (MessageBox.Show($"Удалить клиента \"{c.FullName}\"?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.DeleteClient(c.Id))
                    {
                        LoadClients();
                        _db.LogAction(_currentUser?.Id ?? 0, "Удаление", $"Удалён клиент Id={c.Id}");
                    }
                    else MessageBox.Show("Невозможно удалить клиента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("Выберите клиента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Bookings handlers
        private void AddBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            //var win = new BookingWindow(_currentUser, null) { Owner = Window.GetWindow(this) };
           // if (win.ShowDialog() == true)
            {
                LoadBookings();
                _db.LogAction(_currentUser?.Id ?? 0, "Добавление", "Добавлено бронирование");
            }
        }

        private void EditBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin", "Manager")) { ShowNoRights(); return; }
            if (dgBookings.SelectedItem is Bookings b)
            {
          //     var win = new BookingWindow(_currentUser, b) { Owner = Window.GetWindow(this) };
           //     if (win.ShowDialog() == true)
                {
                    LoadBookings();
                    _db.LogAction(_currentUser?.Id ?? 0, "Редактирование", $"Отредактировано бронирование Id={b.Id}");
                }
            }
            else MessageBox.Show("Выберите бронирование.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelBooking_Click(object sender, RoutedEventArgs e)
        {
            if (!HasRole("Admin")) { ShowNoRights(); return; }
            if (dgBookings.SelectedItem is Bookings b)
            {
                if (MessageBox.Show($"Отменить бронирование Id={b.Id}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (_db.CancelBooking(b.Id))
                    {
                        LoadBookings();
                        _db.LogAction(_currentUser?.Id ?? 0, "Отмена", $"Отменено бронирование Id={b.Id}");
                    }
                    else MessageBox.Show("Невозможно отменить бронирование.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else MessageBox.Show("Выберите бронирование.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region Helpers
        private bool HasRole(params string[] roles)
        {
            if (_currentUser == null) return false;
            return roles.Any(r => string.Equals(_currentUser.Role, r, StringComparison.OrdinalIgnoreCase));
        }

        private void ShowNoRights()
        {
            MessageBox.Show("У вас нет прав для выполнения этой операции.", "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        #endregion
    }
}
