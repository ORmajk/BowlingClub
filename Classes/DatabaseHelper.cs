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
            try
            {
                _ctx.Configuration.LazyLoadingEnabled = false;
                _ctx.Configuration.ProxyCreationEnabled = false;
            }
            catch
            {
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
        public IQueryable<InventoryItems> GetInventoryItems(string search = null)
        {
            var q = _ctx.InventoryItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                q = q.Where(i =>
                    i.Name.Contains(s) ||
                    i.Type.Contains(s));
            }

            return q.OrderBy(i => i.Name);
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
