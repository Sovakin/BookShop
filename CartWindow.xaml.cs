using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using System;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Text;

namespace BookShop
{
    /// <summary>
    /// Логика взаимодействия для CartWindow.xaml
    /// </summary>
    public partial class CartWindow : Window
    {
        public decimal finalCostOfCart;
        public ObservableCollection<TableRow> Items { get; set; } = new ObservableCollection<TableRow>();
        private User currentUser;

        public CartWindow()
        {
            InitializeComponent();
            LoadCart();
            LoadPurchaseHistory();
            currentUser = ShopDbContext.Instance.Users.First(x => x.ClientID == ShopDbContext.Instance.activeUserID);
        }

        public int UserBalance
        {
            get
            {
                var user = ShopDbContext.Instance.Users.Find(ShopDbContext.Instance.activeUserID);
                return user.Balance;
            }
            set
            {
                var user = ShopDbContext.Instance.Users.Find(ShopDbContext.Instance.activeUserID);
                user.Balance = value;
                ShopDbContext.Instance.SaveChanges();
            }
        }

        public void LoadCart()
        {
            try
            {
                Items.Clear();
                finalCostOfCart = 0;
                var cart = ShopDbContext.Instance.Cart.FirstOrDefault(x => x.UserID == ShopDbContext.Instance.activeUserID);

                if (cart == null)
                {
                    MessageBox.Show("Корзина пуста или не найдена.");
                    return;
                }

                Dictionary<int, TableRow> itemsDict = new Dictionary<int, TableRow>();

                foreach (int product in cart.ProductID)
                {
                    var item = ShopDbContext.Instance.Products.FirstOrDefault(x => x.ProductID == product && !x.IsDeleted);
                    if (item == null)
                    {
                        MessageBox.Show($"Товар с ID {product} не найден или удален.");
                        continue;
                    }
                    if (itemsDict.TryGetValue(item.ProductID, out TableRow existingItem))
                    {
                        finalCostOfCart += item.Price;
                    }
                    else
                    {
                        var newItem = new TableRow()
                        {
                            ProductID = item.ProductID,
                            Name = item.Name,
                            Description = item.Description,
                            CategoryID = item.CategoryID,
                            ManufacturerID = item.ManufacturerID,
                            Price = item.Price,
                            ImagePath = item.ImagePath
                        };
                        itemsDict.Add(item.ProductID, newItem);
                        finalCostOfCart += item.Price;
                    }
                }
                foreach (var item in itemsDict.Values)
                    Items.Add(item);

                dataGrid.ItemsSource = Items;
                TotalCostText.Text = $"Итого к оплате: {finalCostOfCart} (Баланс: {UserBalance})";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке корзины: {ex.Message}");
            }
        }

        public void RemoveFromCarts(object sender, EventArgs e)
        {
            if (dataGrid.SelectedItem == null) return;
            var item = dataGrid.SelectedItem as TableRow;
            var cart = ShopDbContext.Instance.Cart.Where(x => x.UserID == ShopDbContext.Instance.activeUserID).FirstOrDefault();
            cart.ProductID.Remove(item.ProductID);
            ShopDbContext.Instance.SaveChanges();
            LoadCart();
        }

        public void CloseCart(object sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        public void BuyProducts(object sender, EventArgs e)
        {
            if (dataGrid.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите книгу для оплаты");
                return;
            }

            var selectedItem = dataGrid.SelectedItem as TableRow;
            if (UserBalance >= selectedItem.Price)
            {
                MessageBox.Show($"Покупка книги \"{selectedItem.Name}\" прошла успешно");
                UserBalance -= (int)selectedItem.Price;

                var purchase = new PurchaseHistory
                {
                    UserID = ShopDbContext.Instance.activeUserID,
                    ProductID = selectedItem.ProductID,
                    PurchaseDate = DateTime.UtcNow 
                };
                ShopDbContext.Instance.PurchaseHistories.Add(purchase);
                var cart = ShopDbContext.Instance.Cart.Where(x => x.UserID == ShopDbContext.Instance.activeUserID).FirstOrDefault();
                cart.ProductID.Remove(selectedItem.ProductID);
                ShopDbContext.Instance.SaveChanges();

                LoadCart();
                LoadPurchaseHistory();
            }
            else
            {
                MessageBox.Show("Недостаточно средств на балансе");
            }
        }

        public void LoadPurchaseHistory()
        {
            try
            {
                var purchaseHistory = ShopDbContext.Instance.PurchaseHistories
                    .Where(ph => ph.UserID == ShopDbContext.Instance.activeUserID)
                    .Join(ShopDbContext.Instance.Products,
                          ph => ph.ProductID,
                          p => p.ProductID,
                          (ph, p) => new
                          {
                              ph.ID,
                              p.ProductID,
                              p.Name,
                              p.Description,
                              p.Price,
                              p.ImagePath,
                              PurchaseDate = ph.PurchaseDate
                          })
                    .ToList();

                historyDataGrid.ItemsSource = purchaseHistory;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке истории покупок: {ex.Message}");
            }
        }


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void printClick_two(object sender, RoutedEventArgs e)
        {
            string desktopPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "История покупок.pdf");
            PrintDataGridToPdf(historyDataGrid, desktopPath, "История покупок");
        }

        private void printClick(object sender, RoutedEventArgs e)
        {
            string desktopPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "Таблица корзины.pdf");
            PrintDataGridToPdf(dataGrid, desktopPath);
        }

        private void PrintDataGridToPdf(DataGrid dataGrid, string fileName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Document doc = new Document(PageSize.A4.Rotate());
            PdfWriter.GetInstance(doc, new FileStream(fileName, FileMode.Create));
            doc.Open();
            string fontPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "arial.ttf");

            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font font = new Font(bf, 12, Font.NORMAL, BaseColor.BLACK);
            Font headerFont = new Font(bf, 12, Font.BOLD, BaseColor.WHITE);
            Font titleFont = new Font(bf, 16, Font.BOLD, BaseColor.BLACK);

            iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph("Ваша корзина", titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };
            doc.Add(title);

            PdfPTable pdfTable = new PdfPTable(dataGrid.Columns.Count);
            pdfTable.WidthPercentage = 100;

            foreach (var column in dataGrid.Columns)
            {
                string header = column.Header.ToString();
                PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = BaseColor.DARK_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                pdfTable.AddCell(cell);
            }
            foreach (var item in dataGrid.Items)
            {
                if (item is TableRow row)
                {
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.ProductID.ToString(), font)));
                    pdfTable.AddCell(GetImageCell(row.ImagePath));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Name, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Description, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.DisplayGenres, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.ManufacturerID, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Price.ToString(), font)));
                }
            }

            doc.Add(pdfTable);
            doc.Close();

            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }

        private PdfPCell GetImageCell(string imagePath)
        {
            iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imagePath);
            image.ScaleToFit(100f, 100f);
            image.Alignment = Element.ALIGN_CENTER;
            PdfPCell cell = new PdfPCell(image)
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Border = iTextSharp.text.Rectangle.BOX,
                FixedHeight = 100f
            };
            return cell;
        }

        private void PrintDataGridToPdf(DataGrid dataGrid, string fileName, string titleText)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Document doc = new Document(PageSize.A4.Rotate());
            PdfWriter.GetInstance(doc, new FileStream(fileName, FileMode.Create));
            doc.Open();
            string fontPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "arial.ttf");

            BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            Font font = new Font(bf, 12, Font.NORMAL, BaseColor.BLACK);
            Font headerFont = new Font(bf, 12, Font.BOLD, BaseColor.WHITE);
            Font titleFont = new Font(bf, 16, Font.BOLD, BaseColor.BLACK);

            iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph(titleText, titleFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20f
            };
            doc.Add(title);

            PdfPTable pdfTable = new PdfPTable(dataGrid.Columns.Count);
            pdfTable.WidthPercentage = 100;

            foreach (var column in dataGrid.Columns)
            {
                string header = column.Header.ToString();
                PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = BaseColor.DARK_GRAY,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE
                };
                pdfTable.AddCell(cell);
            }

            foreach (var item in dataGrid.Items)
            {
                dynamic row = item;
                if (row != null)
                {
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.ProductID.ToString(), font)));
                    pdfTable.AddCell(GetImageCell(row.ImagePath));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Name, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Description, font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.Price.ToString(), font)));
                    pdfTable.AddCell(new PdfPCell(new Phrase(row.PurchaseDate.ToString(), font)));
                }
            }

            doc.Add(pdfTable);
            doc.Close();

            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }
    }
}