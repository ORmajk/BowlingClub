using BowlingClub.AppData;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
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
using Path = System.IO.Path;

namespace BowlingClub.Pages
{
    public partial class LaneEditPage : Page
    {
        private Lanes _currentLane;
        private bool _isNew = false;
        private string _selectedImagePath = null;

        public LaneEditPage(Lanes lane)
        {
            InitializeComponent();

            try
            {
                lblError.Text = ""; 
                cbLaneType.ItemsSource = AppData.AppConnect.model.LaneTypes.ToList();
                cbLaneStatus.ItemsSource = AppData.AppConnect.model.LaneStatuses.ToList();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Ошибка загрузки справочников: {ex.Message}";
            }

            if (lane == null)
            {
                _currentLane = new Lanes();
                _isNew = true;
                tbTitle.Text = "Добавление дорожки";
                _currentLane.Capacity = 6;
                _currentLane.PricePerHour = 1000;
            }
            else
            {
                _currentLane = lane;
                _isNew = false;
                tbTitle.Text = "Редактирование дорожки";
            }

            UpdateFields();
        }

        private void UpdateFields()
        {
            txtLaneNumber.Text = _currentLane.LaneNumber.ToString();
            txtCapacity.Text = _currentLane.Capacity.ToString();
            txtPrice.Text = _currentLane.PricePerHour.ToString();

            if (!_isNew)
            {
                cbLaneType.SelectedValue = _currentLane.LaneTypeId;
                cbLaneStatus.SelectedValue = _currentLane.StatusId;

                if (!string.IsNullOrEmpty(_currentLane.Photo))
                {
                    try
                    {
                        string cleanPath = _currentLane.Photo.TrimStart('/');
                        string fullPath = "";

                        string pathInDebug = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cleanPath);

                        string projectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.FullName ?? "";
                        string pathInProject = Path.Combine(projectRoot, cleanPath);

                        if (File.Exists(pathInDebug))
                        {
                            fullPath = pathInDebug;
                        }
                        else if (File.Exists(pathInProject))
                        {
                            fullPath = pathInProject;
                        }

                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(fullPath);
                            bitmap.EndInit();

                            imgLane.Source = bitmap;
                        }
                        else
                        {
                            lblError.Text = $"Файл не найден на диске. Путь в БД: {_currentLane.Photo}";
                        }
                    }
                    catch (Exception ex)
                    {
                        lblError.Text = $"Ошибка отображения фото: {ex.Message}";
                    }
                }
            }
        }


        private void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;

                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(_selectedImagePath);
                    bitmap.EndInit();

                    imgLane.Source = bitmap;
                }
                catch (Exception ex)
                {
                    lblError.Text = $"Ошибка предпросмотра изображения: {ex.Message}";
                }
            }
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            lblError.Text = "";

            if (!int.TryParse(txtLaneNumber.Text, out int laneNumber))
            {
                lblError.Text = "Номер дорожки должен быть целым числом.";
                return;
            }
            if (!int.TryParse(txtCapacity.Text, out int capacity) || capacity <= 0)
            {
                lblError.Text = "Вместимость должна быть числом больше нуля.";
                return;
            }
            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                lblError.Text = "Укажите корректную стоимость за час.";
                return;
            }
            if (cbLaneType.SelectedValue == null)
            {
                lblError.Text = "Не выбран тип дорожки.";
                return;
            }
            if (cbLaneStatus.SelectedValue == null)
            {
                lblError.Text = "Не выбран текущий статус дорожки.";
                return;
            }

            if (_selectedImagePath != null)
            {
                try
                {
                    string targetFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    string extension = Path.GetExtension(_selectedImagePath);
                    string newFileName = $"lane_{laneNumber}_{Guid.NewGuid()}{extension}";
                    string targetPath = Path.Combine(targetFolder, newFileName);

                    File.Copy(_selectedImagePath, targetPath, true);
                    _currentLane.Photo = $"/Images/{newFileName}";
                }
                catch (Exception ex)
                {
                    lblError.Text = $"Не удалось сохранить изображение: {ex.Message}";
                    return;
                }
            }

            _currentLane.LaneNumber = laneNumber;
            _currentLane.Capacity = capacity;
            _currentLane.PricePerHour = price;
            _currentLane.LaneTypeId = (int)cbLaneType.SelectedValue;
            _currentLane.StatusId = (int)cbLaneStatus.SelectedValue;

            try
            {
                if (_isNew)
                {
                    AppData.AppConnect.model.Lanes.Add(_currentLane);
                }

                AppData.AppConnect.model.SaveChanges();
                MessageBox.Show("Данные успешно сохранены!");
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
}
