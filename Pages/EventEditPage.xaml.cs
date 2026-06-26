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
    public partial class EventEditPage : Page
    {
        private Events _currentEvent;
        private bool _isNew = false;
        private List<ClientViewModel> _clientList = new List<ClientViewModel>();

        public EventEditPage(Events selectedEvent)
        {
            InitializeComponent();

            try
            {
                lblError.Text = "";
                cbLane.ItemsSource = AppConnect.model.Lanes.ToList();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки дорожек: {ex.Message}";
            }

            if (selectedEvent == null)
            {
                _currentEvent = new Events();
                _isNew = true;
                tbTitle.Text = "Добавление игры";
                dpEventDate.SelectedDate = DateTime.Now;
            }
            else
            {
                _currentEvent = selectedEvent;
                _isNew = false;
                tbTitle.Text = "Редактирование игры";
            }

            txtEventName.Text = _currentEvent.Name;
            dpEventDate.SelectedDate = _currentEvent.EventDate;
            txtDescription.Text = _currentEvent.Description;
            cbLane.SelectedValue = _currentEvent.LaneId;

            LoadClients();
        }

        private void LoadClients()
        {
            try
            {
                var allClients = AppConnect.model.Clients.ToList();

                var registeredClientIds = _currentEvent.EventRegistrations
                    .Select(er => er.ClientId)
                    .ToList();

                _clientList = allClients.Select(c => new ClientViewModel
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    IsSelected = registeredClientIds.Contains(c.Id)
                }).ToList();

                lbClients.ItemsSource = _clientList;
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки клиентов: {ex.Message}";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtEventName.Text))
            {
                lblError.Text = "Ошибка: Введите название игры.";
                return;
            }
            if (dpEventDate.SelectedDate == null)
            {
                lblError.Text = "Ошибка: Выберите дату проведения.";
                return;
            }

            _currentEvent.Name = txtEventName.Text;
            _currentEvent.EventDate = dpEventDate.SelectedDate.Value;
            _currentEvent.Description = txtDescription.Text;
            _currentEvent.LaneId = cbLane.SelectedValue as int?;

            try
            {
                if (_isNew)
                {
                    AppConnect.model.Events.Add(_currentEvent);
                    AppConnect.model.SaveChanges();
                }

                var existingRegistrations = AppConnect.model.EventRegistrations
                    .Where(er => er.EventId == _currentEvent.Id)
                    .ToList();

                foreach (var reg in existingRegistrations)
                {
                    AppConnect.model.EventRegistrations.Remove(reg);
                }

                foreach (var client in _clientList.Where(c => c.IsSelected))
                {
                    var newRegistration = new EventRegistrations
                    {
                        EventId = _currentEvent.Id,
                        ClientId = client.Id
                    };
                    AppConnect.model.EventRegistrations.Add(newRegistration);
                }

                AppConnect.model.SaveChanges();

                MessageBox.Show("Данные игры успешно сохранены!");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка базы данных: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
    public class ClientViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public bool IsSelected { get; set; }
    }
}
