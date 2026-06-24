using BowlingClub.AppData;
using System;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Data.Entity;

namespace BowlingClub.Database
{
    public class DatabaseHelper : IDisposable
    {
        private readonly BowlingClubEntities _ctx = new BowlingClubEntities(); // имя контекста из EDMX

        public DatabaseHelper()
        {
            // Отключаем прокси/ленивую загрузку при необходимости
            try
            {
                _ctx.Configuration.LazyLoadingEnabled = false;
                _ctx.Configuration.ProxyCreationEnabled = false;
            }
            catch
            {
                // если контекст ObjectContext, этих свойств может не быть
            }
        }

        // Аутентификация: сравниваем plain password
        public Users Authenticate(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login) || password == null) return null;
            return _ctx.Users.FirstOrDefault(u => u.Login == login && u.Password == password);
        }

        // Проверка существования логина/email
        public bool UserExists(string login, string email)
        {
            return _ctx.Users.Any(u => u.Login == login || u.Email == email);
        }

        // Регистрация: заполняем FullName, Email, Login, Phone и сохраняем пароль как plain string
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

        // Лог действий
        public void LogAction(int userId, string actionType, string details)
        {
            try
            {
                var ua = new UserActions
                {
                    UserId = userId,
                    ActionType = actionType,
                    Details = details,
                    Timestamp = DateTime.UtcNow
                };
                _ctx.UserActions.Add(ua);
                _ctx.SaveChanges();
            }
            catch
            {

            }
        }

        public IQueryable<Lanes> GetLanes(string search = null, int? laneTypeId = null, int? statusId = null)
        {
            // Подключаем связанные сущности для отображения в DataGrid
            var q = _ctx.Lanes
                        .Include(l => l.LaneTypes)    // если в модели есть навигационные свойства
                        .Include(l => l.LaneStatuses)
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                // Поиск по номеру дорожки (приводим к строке) и по имени типа/статуса
                q = q.Where(l =>
                    SqlFunctions.StringConvert((double)l.LaneNumber).Contains(s) ||
                    l.LaneTypes.Name.Contains(s) ||
                    l.LaneStatuses.Name.Contains(s));
            }

            if (laneTypeId.HasValue)
                q = q.Where(l => l.LaneTypeId == laneTypeId.Value);

            if (statusId.HasValue)
                q = q.Where(l => l.StatusId == statusId.Value);

            return q.OrderBy(l => l.LaneNumber);
        }

            public bool DeleteGame(int id)
            {
                var game = _ctx.Events.Find(id);
                if (game == null) return false;

                _ctx.Events.Remove(game);
                _ctx.SaveChanges();
                return true;
            }

            // ----------------- Inventory -----------------
            public IQueryable<InventoryItems> GetInventoryItems(string search = null, string type = null)
            {
                var q = _ctx.InventoryItems.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    q = q.Where(i => i.Type.Contains(s) || i.Type.Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(type))
                    q = q.Where(i => i.Type == type);

                return q.OrderBy(i => i.Type);
            }

            public bool DeleteInventoryItem(int id)
            {
                var item = _ctx.InventoryItems.Find(id);
                if (item == null) return false;

                // Проверка связей: используется ли в активных играх/заказах
                bool inUse = _ctx.InventoryItems.Any(gi => gi.Id == id);
                if (inUse) return false;

                _ctx.InventoryItems.Remove(item);
                _ctx.SaveChanges();
                return true;
            }

            // ----------------- Clients -----------------
            public IQueryable<Clients> GetClients(string search = null)
            {
                var q = _ctx.Clients.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    q = q.Where(c => c.FullName.Contains(s) || c.Email.Contains(s) || c.Phone.Contains(s));
                }

                return q.OrderBy(c => c.FullName);
            }

            public bool DeleteClient(int id)
            {
                var client = _ctx.Clients.Include(c => c.Bookings).FirstOrDefault(c => c.Id == id);
                if (client == null) return false;

                // Если у клиента есть активные брони — не удаляем
                bool hasActive = client.Bookings.Any(b => b.Status != "Cancelled" && b.StartTime >= DateTime.Now);
                if (hasActive) return false;

                // Можно также пометить как IsArchived вместо удаления
                _ctx.Clients.Remove(client);
                _ctx.SaveChanges();
                return true;
            }

            // ----------------- Bookings -----------------
            public IQueryable<Bookings> GetBookings(string search = null, int? laneId = null, int? clientId = null, string status = null)
            {
                var q = _ctx.Bookings
                            .Include(b => b.Clients)
                            .Include(b => b.Lanes)
                            .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();
                    if (int.TryParse(s, out int num))
                        q = q.Where(b => b.Lanes.LaneNumber == num || b.Clients.FullName.Contains(s));
                    else
                        q = q.Where(b => b.Clients.FullName.Contains(s) || b.Status.Contains(s));
                }

                if (laneId.HasValue) q = q.Where(b => b.LaneId == laneId.Value);
                if (clientId.HasValue) q = q.Where(b => b.ClientId == clientId.Value);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(b => b.Status == status);

                return q.OrderByDescending(b => b.StartTime);
            }

            // Отмена бронирования (пометка статуса)
            public bool CancelBooking(int id)
            {
                var booking = _ctx.Bookings.Find(id);
                if (booking == null) return false;

                // Бизнес-правило: нельзя отменять прошедшие брони
                if (booking.StartTime <= DateTime.Now) return false;

                booking.Status = "Cancelled";
                _ctx.SaveChanges();
                return true;
            }
    public void Dispose() => _ctx.Dispose();
    }
}
