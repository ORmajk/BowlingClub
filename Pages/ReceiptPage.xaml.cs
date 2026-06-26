using BowlingClub.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using ZXing;

namespace BowlingClub.Pages
{
    public partial class ReceiptPage : Page
    {
        private Bookings _currentBooking;
        private List<BookingItems> _itemsList;
        private decimal _totalAmount;

        public ReceiptPage(Bookings booking)
        {
            InitializeComponent();

            if (booking == null)
            {
                MessageBox.Show("Данные бронирования не найдены!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.GoBack();
                return;
            }

            _currentBooking = booking;

            LoadBookingItems();
            LoadRelatedData();

            this.DataContext = _currentBooking;
            dgReceiptItems.ItemsSource = _itemsList;
            UpdateTotalAmount();

            LoadQR();
        }

        private void LoadBookingItems()
        {
            try
            {
                _itemsList = AppConnect.model.BookingItems
                    .Where(bi => bi.BookingId == _currentBooking.Id)
                    .ToList();

                if (!_itemsList.Any())
                {
                    _itemsList = new List<BookingItems>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRelatedData()
        {
            try
            {
                if (_currentBooking.Clients == null)
                {
                    AppConnect.model.Entry(_currentBooking)
                        .Reference(b => b.Clients)
                        .Load();
                }

                if (_currentBooking.Lanes == null)
                {
                    AppConnect.model.Entry(_currentBooking)
                        .Reference(b => b.Lanes)
                        .Load();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки связанных данных: {ex.Message}");
            }
        }

        private void UpdateTotalAmount()
        {
            _totalAmount = _itemsList.Sum(item => item.Total);

            if (_currentBooking.TotalAmount != _totalAmount)
            {
                _currentBooking.TotalAmount = _totalAmount;
            }
        }

        private void LoadQR()
        {
            try
            {
                string qrData = GenerateQRData();

                var writer = new ZXing.BarcodeWriter<WriteableBitmap>
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 10
                    },
                    Renderer = new ZXing.Rendering.WriteableBitmapRenderer()
                };

                var result = writer.Write(qrData);

                var bitmap = ConvertWriteableBitmapToBitmapImage(result);

                imgQr.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка генерации QR-кода: {ex.Message}");
                CreateErrorQR();
            }
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap writeableBitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();

            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }

            return bitmapImage;
        }

        private void CreateErrorQR()
        {
            try
            {
                var writer = new ZXing.BarcodeWriter<WriteableBitmap>
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 10
                    },
                    Renderer = new ZXing.Rendering.WriteableBitmapRenderer()
                };

                var result = writer.Write("Ошибка генерации QR-кода");
                var bitmap = ConvertWriteableBitmapToBitmapImage(result);
                imgQr.Source = bitmap;
            }
            catch
            {
                imgQr.Source = null;
            }
        }

        private string GenerateQRData()
        {
            var data = new System.Text.StringBuilder();

            data.Append($"ЧЕК #{_currentBooking.BookingNumber}|");
            data.Append($"Клиент:{_currentBooking.Clients?.FullName ?? "Не указан"}|");
            data.Append($"Дорожка:{_currentBooking.Lanes?.LaneNumber ?? 0}|");
            data.Append($"Дата:{_currentBooking.StartTime:dd.MM.yyyy HH:mm}|");
            data.Append($"Длит:{_currentBooking.DurationMinutes}мин|");
            data.Append($"Сумма:{_totalAmount:F2}руб|");
            data.Append($"Оплата:{_currentBooking.PaymentMethod ?? "Не указан"}|");
            data.Append($"Статус:{_currentBooking.Status ?? "Не указан"}|");

            data.Append("Услуги:");
            for (int i = 0; i < _itemsList.Count; i++)
            {
                var item = _itemsList[i];
                data.Append($"{item.ItemName}x{item.Quantity}={item.Total:F2}");
                if (i < _itemsList.Count - 1)
                    data.Append(",");
            }

            return data.ToString();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    var printGrid = CreatePrintVersion();
                    printDialog.PrintVisual(printGrid, "Чек бронирования #" + _currentBooking.BookingNumber);

                    MessageBox.Show("Чек отправлен на печать!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка печати: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "PDF файлы (*.pdf)|*.pdf";
                saveFileDialog.DefaultExt = ".pdf";
                saveFileDialog.FileName = $"Чек_{_currentBooking.BookingNumber}_{DateTime.Now:ddMMyyyy}";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    MessageBox.Show($"Чек будет сохранен в файл: {filePath}", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (File.Exists(filePath))
                    {
                        MessageBox.Show($"Чек успешно сохранен!\nПуть: {filePath}", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        var result = MessageBox.Show("Открыть сохраненный файл?", "Открыть файл",
                            MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(filePath);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Файл не был создан. Проверьте права доступа.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Grid CreatePrintVersion()
        {
            Grid printGrid = new Grid
            {
                Background = Brushes.White,
                Width = 400,
                Margin = new Thickness(20)
            };

            StackPanel panel = new StackPanel
            {
                Margin = new Thickness(20),
                Background = Brushes.White
            };

            panel.Children.Add(new TextBlock
            {
                Text = "БОУЛИНГ КЛУБ",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "ЧЕК ОПЛАТЫ",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            });

            panel.Children.Add(new TextBlock
            {
                Text = new string('═', 40),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Номер брони: {_currentBooking.BookingNumber}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Клиент: {_currentBooking.Clients?.FullName ?? "Не указан"}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Дорожка: {_currentBooking.Lanes?.LaneNumber ?? 0}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Дата: {_currentBooking.StartTime:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Длительность: {_currentBooking.DurationMinutes} мин.",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = new string('─', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            headerGrid.Children.Add(new TextBlock { Text = "Услуга", FontWeight = FontWeights.Bold, Margin = new Thickness(2) });
            headerGrid.Children.Add(new TextBlock { Text = "Кол-во", FontWeight = FontWeights.Bold, Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Center });
            headerGrid.Children.Add(new TextBlock { Text = "Цена", FontWeight = FontWeights.Bold, Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Right });
            headerGrid.Children.Add(new TextBlock { Text = "Сумма", FontWeight = FontWeights.Bold, Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Right });

            Grid.SetColumn(headerGrid.Children[0], 0);
            Grid.SetColumn(headerGrid.Children[1], 1);
            Grid.SetColumn(headerGrid.Children[2], 2);
            Grid.SetColumn(headerGrid.Children[3], 3);

            panel.Children.Add(headerGrid);
            =
            foreach (var item in _itemsList)
            {
                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                itemGrid.Children.Add(new TextBlock { Text = item.ItemName, Margin = new Thickness(2) });
                itemGrid.Children.Add(new TextBlock { Text = item.Quantity.ToString(), Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Center });
                itemGrid.Children.Add(new TextBlock { Text = $"{item.Price:F2}", Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Right });
                itemGrid.Children.Add(new TextBlock { Text = $"{item.Total:F2}", Margin = new Thickness(2), HorizontalAlignment = HorizontalAlignment.Right });

                Grid.SetColumn(itemGrid.Children[0], 0);
                Grid.SetColumn(itemGrid.Children[1], 1);
                Grid.SetColumn(itemGrid.Children[2], 2);
                Grid.SetColumn(itemGrid.Children[3], 3);

                panel.Children.Add(itemGrid);
            }

            panel.Children.Add(new TextBlock
            {
                Text = new string('─', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });
            
            Grid totalGrid = new Grid();
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });
            totalGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength() });

            totalGrid.Children.Add(new TextBlock
            {
                Text = "ИТОГО:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2)
            });

            totalGrid.Children.Add(new TextBlock
            {
                Text = $"{_totalAmount:F2}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Green,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right
            });

            totalGrid.Children.Add(new TextBlock
            {
                Text = "руб.",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Green,
                Margin = new Thickness(2, 2, 0, 2)
            });

            Grid.SetColumn(totalGrid.Children[1], 1);
            Grid.SetColumn(totalGrid.Children[2], 2);

            panel.Children.Add(totalGrid);

            panel.Children.Add(new TextBlock
            {
                Text = new string('─', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Способ оплаты: {_currentBooking.PaymentMethod ?? "Не указан"}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Статус: {_currentBooking.Status ?? "Не указан"}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Дата печати: {DateTime.Now:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            panel.Children.Add(new TextBlock
            {
                Text = new string('═', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Спасибо за посещение!",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = Brushes.DarkBlue
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Ждем вас снова!",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
                Foreground = Brushes.Gray
            });

            printGrid.Children.Add(panel);
            return printGrid;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void RefreshReceipt_Click(object sender, RoutedEventArgs e)
        {
            LoadBookingItems();
            UpdateTotalAmount();
            dgReceiptItems.ItemsSource = null;
            dgReceiptItems.ItemsSource = _itemsList;

            this.DataContext = null;
            this.DataContext = _currentBooking;

            LoadQR();

            MessageBox.Show("Чек обновлен!", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == System.Windows.Input.Key.P &&
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                Print_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close_Click(null, null);
                e.Handled = true;
            }
        }
    }
}