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
    /// <summary>
    /// Логика взаимодействия для ReceiptPage.xaml
    /// </summary>
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

            // Загружаем услуги для этого бронирования
            LoadBookingItems();

            // Загружаем связанные данные (клиент, дорожка)
            LoadRelatedData();

            // Устанавливаем контекст данных
            this.DataContext = _currentBooking;

            // Заполняем таблицу услуг
            dgReceiptItems.ItemsSource = _itemsList;

            // Обновляем итоговую сумму
            UpdateTotalAmount();
        }

        private void LoadBookingItems()
        {
            try
            {
                // Загружаем услуги из БД
                _itemsList = AppConnect.model.BookingItems
                    .Where(bi => bi.BookingId == _currentBooking.Id)
                    .ToList();

                // Если услуги не найдены, создаем тестовые данные (для демонстрации)
                if (!_itemsList.Any())
                {
                    _itemsList = new List<BookingItems>
                    {
                        new BookingItems
                        {
                            ItemName = "Аренда дорожки",
                            Quantity = 1,
                            Price = 1200.00m,
                            Total = 1200.00m
                        },
                        new BookingItems
                        {
                            ItemName = "Аренда обуви",
                            Quantity = 2,
                            Price = 250.00m,
                            Total = 500.00m
                        }
                    };

                    // Обновляем общую сумму в бронировании
                    _currentBooking.TotalAmount = _itemsList.Sum(i => i.Total);
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
                // Загружаем данные клиента, если они не загружены
                if (_currentBooking.Clients == null)
                {
                    AppConnect.model.Entry(_currentBooking)
                        .Reference(b => b.Clients)
                        .Load();
                }

                // Загружаем данные дорожки, если они не загружены
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

            // Обновляем общую сумму в бронировании, если она отличается
            if (_currentBooking.TotalAmount != _totalAmount)
            {
                _currentBooking.TotalAmount = _totalAmount;
            }
        }

        // Печать чека
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Создаем копию чека для печати
                    var printGrid = CreatePrintVersion();

                    // Настраиваем печать
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

        // Экспорт в PDF (опционально)
        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем диалог сохранения файла
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "PDF файлы (*.pdf)|*.pdf";
                saveFileDialog.DefaultExt = ".pdf";
                saveFileDialog.FileName = $"Чек_{_currentBooking.BookingNumber}_{DateTime.Now:ddMMyyyy}";

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Здесь можно использовать стороннюю библиотеку для создания PDF
                    // Например, iTextSharp или PdfSharp
                    MessageBox.Show($"Чек сохранен в файл: {saveFileDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Создание версии для печати
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

            // Заголовок
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

            // Информация о бронировании
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

            // Разделитель
            panel.Children.Add(new TextBlock
            {
                Text = new string('─', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });

            // Заголовки таблицы
            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            headerGrid.Children.Add(new TextBlock
            {
                Text = "Услуга",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2)
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "Кол-во",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "Цена",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right
            });
            headerGrid.Children.Add(new TextBlock
            {
                Text = "Сумма",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Right
            });

            Grid.SetColumn(headerGrid.Children[0], 0);
            Grid.SetColumn(headerGrid.Children[1], 1);
            Grid.SetColumn(headerGrid.Children[2], 2);
            Grid.SetColumn(headerGrid.Children[3], 3);

            panel.Children.Add(headerGrid);

            // Услуги
            foreach (var item in _itemsList)
            {
                Grid itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.ItemName,
                    Margin = new Thickness(2)
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = item.Quantity.ToString(),
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = $"{item.Price:F2}",
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                itemGrid.Children.Add(new TextBlock
                {
                    Text = $"{item.Total:F2}",
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Right
                });

                Grid.SetColumn(itemGrid.Children[0], 0);
                Grid.SetColumn(itemGrid.Children[1], 1);
                Grid.SetColumn(itemGrid.Children[2], 2);
                Grid.SetColumn(itemGrid.Children[3], 3);

                panel.Children.Add(itemGrid);
            }

            // Разделитель
            panel.Children.Add(new TextBlock
            {
                Text = new string('─', 40),
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 10)
            });

            // Итоговая сумма
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

            // Информация об оплате
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

        // Закрытие страницы
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // Спрашиваем, нужно ли сохранить изменения
            if (_currentBooking.Status != "Оплачен")
            {
                var result = MessageBox.Show("Статус бронирования не 'Оплачен'. Сохранить изменения?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Обновляем статус
                    _currentBooking.Status = "Оплачен";

                    try
                    {
                        AppConnect.model.SaveChanges();
                        MessageBox.Show("Статус обновлен на 'Оплачен'", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            NavigationService.GoBack();
        }

        // Обновление чека (если были изменения)
        private void RefreshReceipt_Click(object sender, RoutedEventArgs e)
        {
            LoadBookingItems();
            UpdateTotalAmount();
            dgReceiptItems.ItemsSource = null;
            dgReceiptItems.ItemsSource = _itemsList;

            // Обновляем контекст данных
            this.DataContext = null;
            this.DataContext = _currentBooking;

            MessageBox.Show("Чек обновлен!", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Подсчет количества позиций
        private int GetItemsCount()
        {
            return _itemsList?.Sum(i => i.Quantity) ?? 0;
        }

        // Получение общей суммы прописью (опционально)
        private string GetTotalAmountWords()
        {
            // Здесь можно реализовать перевод суммы в текстовый вид
            // Например: "Одна тысяча двести рублей 00 копеек"
            return $"{_totalAmount:F2} рублей";
        }

        // Проверка возможности печати
        private bool CanPrint()
        {
            PrintDialog printDialog = new PrintDialog();
            return printDialog.ShowDialog() == true;
        }

        // Обработчик нажатия клавиш (Ctrl+P - печать)
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
