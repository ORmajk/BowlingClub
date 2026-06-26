using BowlingClub.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace BowlingClub.Database
{

    public class DatabaseHelper : IDisposable
    {
        private readonly BowlingClubEntities _ctx = new BowlingClubEntities(); // имя контекста из EDMX

        public DatabaseHelper()
        {
            try
            {
                _ctx.Configuration.LazyLoadingEnabled = false;
                _ctx.Configuration.ProxyCreationEnabled = false;
            }
            catch
            {
            }
        }

        public static void RefreshTable<T>(DataGrid grid) where T : class
        {
            if (grid == null) return;

            try
            {
                // 1. Сбрасываем локальный кэш Entity Framework только для сущностей типа T
                foreach (var entry in AppConnect.model.ChangeTracker.Entries<T>())
                {
                    entry.Reload();
                }

                // 2. Жадная загрузка (Eager Loading) в зависимости от типа таблицы
                if (typeof(T) == typeof(Events))
                {
                    // Для Игр принудительно подтягиваем регистрации, клиентов и дорожки
                    grid.ItemsSource = AppConnect.model.Events
                        .Include(e => e.EventRegistrations.Select(er => er.Clients))
                        .Include(e => e.Lanes)
                        .ToList();
                }
                else if (typeof(T) == typeof(Bookings))
                {
                    // Для Бронирований принудительно подтягиваем клиентов, дорожки и доп. услуги (чековые позиции)
                    grid.ItemsSource = AppConnect.model.Bookings
                        .Include(b => b.Clients)
                        .Include(b => b.Lanes)
                        .Include(b => b.BookingItems)
                        .ToList();
                }
                else
                {
                    // Для всех остальных стандартных таблиц (Клиенты, Инвентарь и т.д.)
                    grid.ItemsSource = AppConnect.model.Set<T>().ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении данных таблицы {typeof(T).Name}: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void Export<T>(IEnumerable<T> items, string defaultFileName)
        {
            if (items == null) return;

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = defaultFileName
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    var props = typeof(T).GetProperties();

                    // 1. Заголовки (названия свойств вашей модели)
                    List<string> headers = new List<string>();
                    foreach (var p in props) headers.Add($"\"{p.Name}\"");
                    sb.AppendLine(string.Join(";", headers));

                    // 2. Строки данных
                    foreach (var item in items)
                    {
                        List<string> cells = new List<string>();
                        foreach (var p in props)
                        {
                            var val = p.GetValue(item, null);
                            string str = val != null ? val.ToString() : "";

                            if (val is DateTime dt)
                            {
                                str = dt.ToString("dd.MM.yyyy HH:mm");
                            }

                            // Вот эта строка без сложных интерполяций, ломающих компилятор:
                            string cleanStr = str.Replace("\"", "\"\"");
                            cells.Add("\"" + cleanStr + "\"");
                        }
                        sb.AppendLine(string.Join(";", cells));
                    }


                    // Запись с BOM-маркером (гарантирует, что русский текст в Excel не превратится в кракозябры)
                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));

                        MessageBox.Show("Данные успешно сохранены!", "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
            catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public static string GetClientsList(Events ev)
        {
            if (ev == null || ev.EventRegistrations == null || !ev.EventRegistrations.Any())
                return "Нет участников";

            // Собираем FullName клиентов через запятую
            var names = ev.EventRegistrations
                .Where(er => er.Clients != null)
                .Select(er => er.Clients.FullName);

            return string.Join(", ", names);
        }

        private void ClientsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Events currentEvent)
            {
                textBlock.Text = DatabaseHelper.GetClientsList(currentEvent);
            }
        }


        public Users Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || password == null) return null;
            return _ctx.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
        }

        public bool UserExists(string login, string email)
        {
            return _ctx.Users.Any(u => u.Login == login || u.Email == email);
        }

        public Users RegisterUserSimple(string fullName, string email, string login, string phone, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || password == null)
                throw new ArgumentException("Логин и пароль обязательны");

            if (UserExists(login, email))
                return null;

            var user = new Users
            {
                FullName = fullName,
                Email = email,
                Login = login,
                Phone = phone,
                Password = password,
            };

            _ctx.Users.Add(user);
            _ctx.SaveChanges();

            LogAction(user.Id, "Регистрация", $"Зарегистрирован пользователь {login}");
            return user;
        }

        public IQueryable<Lanes> GetLanes(string search = null)
        {
            var q = _ctx.Lanes
                .Include(l => l.LaneTypes)
                .Include(l => l.LaneStatuses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                q = q.Where(l =>
                    l.LaneNumber.ToString().Contains(s) ||
                    l.LaneTypes.Name.Contains(s) ||
                    l.LaneStatuses.Name.Contains(s));
            }

            return q.OrderBy(l => l.LaneNumber);
        }

        public bool DeleteLane(int laneId)
        {
            var lane = _ctx.Lanes.Find(laneId);
            if (lane == null) return false;

            bool hasBookings = _ctx.Bookings.Any(b => b.LaneId == laneId && b.Status != "Cancelled");
            if (hasBookings) return false;

            _ctx.Lanes.Remove(lane);
            _ctx.SaveChanges();
            return true;
        }

        // -------------------- EVENTS --------------------
        public IQueryable<Events> GetEvents(string search = null)
        {
            var q = _ctx.Events
                .Include(e => e.Lanes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();

                q = q.Where(e =>
                    e.Name.Contains(s) ||
                    e.Description.Contains(s));

                if (int.TryParse(s, out int laneNum))
                {
                    q = q.Where(e => e.Lanes.LaneNumber == laneNum);
                }
            }

            return q.OrderBy(e => e.EventDate);
        }

        public bool DeleteEvent(int eventId)
        {
            var ev = _ctx.Events
                .Include(e => e.EventRegistrations)
                .FirstOrDefault(e => e.Id == eventId);

            if (ev == null) return false;

            if (ev.EventRegistrations.Any())
                return false;

            _ctx.Events.Remove(ev);
            _ctx.SaveChanges();
            return true;
        }

        // -------------------- INVENTORY --------------------
        // В DatabaseHelper.cs
        public IQueryable<InventoryItems> GetInventoryItems(string searchTerm = "")
        {
            var query = AppConnect.model.InventoryItems
                .Include(i => i.InventoryStatuses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i => i.Name.Contains(searchTerm) ||
                                          i.Type.Contains(searchTerm) ||
                                          i.InventoryStatuses.Name.Contains(searchTerm));
            }

            return query.OrderBy(i => i.Name);
        }

        public bool DeleteInventoryItem(int id)
        {
            var item = _ctx.InventoryItems.Find(id);
            if (item == null) return false;

            _ctx.InventoryItems.Remove(item);
            _ctx.SaveChanges();
            return true;
        }

        // -------------------- CLIENTS --------------------
        public IQueryable<Clients> GetClients(string search = null)
        {
            var q = _ctx.Clients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                q = q.Where(c =>
                    c.FullName.Contains(s) ||
                    c.Email.Contains(s) ||
                    c.Phone.Contains(s));
            }

            return q.OrderBy(c => c.FullName);
        }

        public bool DeleteClient(int id)
        {
            var client = _ctx.Clients
                .Include(c => c.Bookings)
                .Include(c => c.EventRegistrations)
                .FirstOrDefault(c => c.Id == id);

            if (client == null) return false;

            if (client.Bookings.Any(b => b.Status != "Cancelled"))
                return false;

            if (client.EventRegistrations.Any())
                return false;

            _ctx.Clients.Remove(client);
            _ctx.SaveChanges();
            return true;
        }

        // -------------------- BOOKINGS --------------------
        public IQueryable<Bookings> GetBookings(string search = null)
        {
            var q = _ctx.Bookings
                .Include(b => b.Clients)
                .Include(b => b.Lanes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();

                q = q.Where(b =>
                    b.Clients.FullName.Contains(s) ||
                    b.Status.Contains(s));

                if (int.TryParse(s, out int laneNum))
                {
                    q = q.Where(b => b.Lanes.LaneNumber == laneNum);
                }
            }

            return q.OrderByDescending(b => b.StartTime);
        }

        public bool CancelBooking(int id)
        {
            var booking = _ctx.Bookings.Find(id);
            if (booking == null) return false;

            booking.Status = "Cancelled";
            _ctx.SaveChanges();
            return true;
        }
        public static string GetBookingItemsList(Bookings booking)
        {
            if (booking == null || booking.BookingItems == null || !booking.BookingItems.Any())
                return "Нет услуг";

            // Собираем название услуги и её количество
            var items = booking.BookingItems
                .Select(bi => $"{bi.ItemName} (x{bi.Quantity})");

            return string.Join(", ", items);
        }


        // -------------------- LOGGING --------------------
        public void LogAction(int userId, string actionType, string details)
        {
            var log = new UserActions
            {
                UserId = userId,
                ActionType = actionType,
                Details = details,
                Timestamp = DateTime.Now
            };

            _ctx.UserActions.Add(log);
            _ctx.SaveChanges();
        } 

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
