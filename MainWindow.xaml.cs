using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace BookShop
{
    public class TableRow
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CategoryID { get; set; }
        public string ManufacturerID { get; set; }
        public string ImagePath { get; set; }

        public string DisplayGenres
        {
            get
            {
                if (string.IsNullOrEmpty(CategoryID)) return "";

                var genreIds = CategoryID.Split(',').Select(int.Parse).ToList();
                var genreNames = new List<string>();

                foreach (var genreId in genreIds)
                {
                    var genre = ShopDbContext.Instance.Categories.FirstOrDefault(c => c.CategoryID == genreId);
                    if (genre != null)
                    {
                        genreNames.Add(genre.Name);
                    }
                }

                return string.Join(", ", genreNames);
            }
        }
    }

    public partial class MainWindow : Window
    {
        public ObservableCollection<TableRow> Items { get; set; }

        public static MainWindow instance;

        public MainWindow()
        {
            InitializeComponent();
            instance = this;
            LoadData();
        }

        public void LoadData()
        {
            Items = new ObservableCollection<TableRow>();
            foreach (var item in ShopDbContext.Instance.GetProducts().Where(p => !p.IsDeleted))
            {
                Items.Add(new TableRow()
                {
                    ProductID = item.ProductID,
                    Name = item.Name,
                    Description = item.Description,
                    CategoryID = item.CategoryID,
                    ManufacturerID = item.ManufacturerID,
                    Price = item.Price,
                    ImagePath = item.ImagePath
                });
            }
            dataGrid.ItemsSource = Items;
        }

        public void AddProduct(object sender, EventArgs e)
        {
            if (!ShopDbContext.Instance.CanGetAccess(AccessLevels.Admin))
            {
                MessageBox.Show("Добавлять книги может только администратор");
                return;
            }
            AddWindow addWindow = new AddWindow();
            addWindow.ShowDialog();
            LoadData();
        }

        public void AddToCart(object sender, EventArgs e)
        {
            if (dataGrid.SelectedItem == null) return;
            var item = dataGrid.SelectedItem as TableRow;

            var cart = ShopDbContext.Instance.Cart.FirstOrDefault(x => x.UserID == ShopDbContext.Instance.activeUserID);

            if (cart == null)
            {
                cart = new Cart()
                {
                    UserID = ShopDbContext.Instance.activeUserID,
                    ProductID = new List<int>()
                };

                cart.ProductID.Add(item.ProductID);
                ShopDbContext.Instance.Cart.Add(cart);
                ShopDbContext.Instance.SaveChanges();
                MessageBox.Show("Книга добавлена в корзину");
                return;
            }
            if (cart.ProductID.Contains(item.ProductID))
            {
                MessageBox.Show("Эта книга уже добавлена в корзину");
            }
            else
            {
                cart.ProductID.Add(item.ProductID);
                ShopDbContext.Instance.SaveChanges();
                MessageBox.Show("Книга добавлена в корзину");
            }
        }

        public void OpenCart(object sender, EventArgs e)
        {
            CartWindow cart = new CartWindow();
            cart.Show();
            Close();
        }

        public void EditProduct(object sender, EventArgs e)
        {
            if (!ShopDbContext.Instance.CanGetAccess(AccessLevels.Editor))
            {
                MessageBox.Show("Редактировать может только редактор");
                return;
            }

            if (dataGrid.SelectedItem == null) return;

            var item = dataGrid.SelectedItem as TableRow;

            Product selectedProduct = new Product()
            {
                ProductID = item.ProductID,
                Name = item.Name,
                Description = item.Description,
                ManufacturerID = item.ManufacturerID,
                Price = item.Price,
                CategoryID = item.CategoryID,
                ImagePath = item.ImagePath
            };
            AddWindow addWindow = new AddWindow(selectedProduct, dataGrid.SelectedIndex);
            addWindow.ShowDialog();
        }

        public void DeleteProduct(object sender, EventArgs e)
        {
            if (!ShopDbContext.Instance.CanGetAccess(AccessLevels.Admin))
            {
                MessageBox.Show("Удалять книгу может только администратор");
                return;
            }
            if (dataGrid.SelectedItem == null) return;
            var item = dataGrid.SelectedItem as TableRow;

            Product selectedProduct = ShopDbContext.Instance.Products.FirstOrDefault(x => x.ProductID == item.ProductID);
            if (selectedProduct != null)
            {
                selectedProduct.IsDeleted = true;
                ShopDbContext.Instance.SaveChanges();
                LoadData();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchBox.Text.ToLower();

            var filteredItems = Items.Where(item =>
                item.Name.ToLower().Contains(searchText) ||
                item.Description.ToLower().Contains(searchText) ||
                item.DisplayGenres.ToLower().Contains(searchText) ||
                item.ManufacturerID.ToLower().Contains(searchText)
            ).ToList();

            dataGrid.ItemsSource = filteredItems;
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

        private void LogoutButtonClick(object sender, RoutedEventArgs e)
        {
            ShopDbContext.Instance.SetActiveUser(0);
            AutorizationWindow autorizationWindow = new AutorizationWindow();
            autorizationWindow.Show();
            Close();
        }
        private void printClick(object sender, RoutedEventArgs e)
        {
            string desktopPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "Таблица магазина.pdf");
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

            iTextSharp.text.Paragraph title = new iTextSharp.text.Paragraph("Таблица магазина", titleFont)
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
    }
}