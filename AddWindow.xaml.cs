using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BookShop
{
    public partial class AddWindow : Window, INotifyPropertyChanged
    {
        int selectedItemIndex, dataGridIndex;
        private string _imagePath;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class GenreSelection : INotifyPropertyChanged
        {
            private Category _selectedGenre;

            public Category SelectedGenre
            {
                get { return _selectedGenre; }
                set
                {
                    if (_selectedGenre != value)
                    {
                        _selectedGenre = value;
                        OnPropertyChanged(nameof(SelectedGenre));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private List<Category> _availableGenres;
        public List<Category> AvailableGenres
        {
            get { return _availableGenres; }
            set
            {
                if (_availableGenres != value)
                {
                    _availableGenres = value;
                    OnPropertyChanged(nameof(AvailableGenres));
                }
            }
        }


        private List<GenreSelection> _genres;
        public List<GenreSelection> Genres
        {
            get { return _genres; }
            set
            {
                if (_genres != value)
                {
                    _genres = value;
                    OnPropertyChanged(nameof(Genres));
                }
            }
        }


        public AddWindow()
        {
            InitializeComponent();
            LoadCategoriesAndManufacturers();
            AddButton.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Collapsed;
            AddBlockName.Text = "Добавление новой книги";

            AvailableGenres = ShopDbContext.Instance.GetCategories();
            var defaultGenre = new Category { CategoryID = -1, Name = "Выберите жанр" };
            AvailableGenres.Insert(0, defaultGenre);

            Genres = new List<GenreSelection>();
            for (int i = 0; i < 4; i++)
            {
                Genres.Add(new GenreSelection { SelectedGenre = defaultGenre });
            }

            DataContext = this;
        }

        public AddWindow(Product selectedProduct, int index)
        {
            InitializeComponent();
            NameBox.Text = selectedProduct.Name;
            DescriptionBox.Text = selectedProduct.Description;
            PriceBox.Text = selectedProduct.Price.ToString();
            ManufacturerBox.Text = selectedProduct.ManufacturerID.ToString();
            AddButton.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Visible;
            selectedItemIndex = selectedProduct.ProductID;
            AddBlockName.Text = "Редактирование книги";
            dataGridIndex = index;

            AvailableGenres = ShopDbContext.Instance.GetCategories();
            var defaultGenre = new Category { CategoryID = -1, Name = "Выберите жанр" };
            AvailableGenres.Insert(0, defaultGenre);

            var genreIds = selectedProduct.CategoryID.Split(',').Select(int.Parse).ToList();
            Genres = new List<GenreSelection>();

            for (int i = 0; i < 4; i++)
            {
                Genres.Add(new GenreSelection());
                if (i < genreIds.Count)
                {
                    Genres[i].SelectedGenre = AvailableGenres.FirstOrDefault(g => g.CategoryID == genreIds[i]);
                }
                else
                {
                    Genres[i].SelectedGenre = defaultGenre;
                }
            }

            if (!string.IsNullOrEmpty(selectedProduct.ImagePath))
            {
                _imagePath = selectedProduct.ImagePath;
                PreviewImage.Source = new BitmapImage(new Uri(_imagePath));
            }

            DataContext = this;
        }

        private void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            if (openFileDialog.ShowDialog() == true)
            {
                _imagePath = openFileDialog.FileName;
                PreviewImage.Source = new BitmapImage(new Uri(_imagePath));
            }
        }

        public void AddProduct(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;

            var selectedGenres = Genres.Where(g => g.SelectedGenre != null && g.SelectedGenre.CategoryID != -1).ToList();
            if (selectedGenres.Count == 0)
            {
                MessageBox.Show("Жанр не добавлен, добавьте хотя бы 1 жанр");
                return;
            }

            string author = ManufacturerBox.Text;
            string name = NameBox.Text;

            if (!IsProductUnique(author, name))
            {
                MessageBox.Show("Книга с таким автором и названием уже существует");
                return;
            }

            Product item;
            try
            {
                item = new Product()
                {
                    Name = NameBox.Text,
                    Description = DescriptionBox.Text,
                    Price = decimal.Parse(PriceBox.Text),
                    ManufacturerID = ManufacturerBox.Text,
                    CategoryID = GetSelectedGenresString(),
                    ImagePath = _imagePath
                };
            }
            catch
            {
                MessageBox.Show("Неверное форматирование");
                return;
            }

            ShopDbContext.Instance.Products.Add(item);
            ShopDbContext.Instance.SaveChanges();
            MainWindow.instance.Items.Add(new TableRow()
            {
                ProductID = item.ProductID,
                Name = item.Name,
                Description = item.Description,
                CategoryID = item.CategoryID,
                ManufacturerID = item.ManufacturerID,
                Price = item.Price,
                ImagePath = item.ImagePath
            });
            this.Close();
        }

        public void EditProduct(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields()) return;

            var selectedGenres = Genres.Where(g => g.SelectedGenre != null && g.SelectedGenre.CategoryID != -1).ToList();
            if (selectedGenres.Count == 0)
            {
                MessageBox.Show("Жанр не добавлен, добавьте хотя бы 1 жанр.");
                return;
            }

            Product item;
            try
            {
                item = new Product()
                {
                    ProductID = selectedItemIndex,
                    Name = NameBox.Text,
                    Description = DescriptionBox.Text,
                    Price = decimal.Parse(PriceBox.Text),
                    ManufacturerID = ManufacturerBox.Text,
                    CategoryID = GetSelectedGenresString(),
                    ImagePath = _imagePath
                };
            }
            catch
            {
                MessageBox.Show("Неверное форматирование");
                return;
            }

            var selectedItem = ShopDbContext.Instance.Products.First(x => x.ProductID == selectedItemIndex);

            if (selectedItem != null)
            {
                selectedItem.ProductID = selectedItemIndex;
                selectedItem.Name = NameBox.Text;
                selectedItem.Description = DescriptionBox.Text;
                selectedItem.Price = decimal.Parse(PriceBox.Text);
                selectedItem.ManufacturerID = ManufacturerBox.Text;
                selectedItem.CategoryID = GetSelectedGenresString();
                selectedItem.ImagePath = _imagePath;

                ShopDbContext.Instance.SaveChanges();
                MainWindow.instance.Items[dataGridIndex].ProductID = selectedItemIndex;
                MainWindow.instance.Items[dataGridIndex].Name = NameBox.Text;
                MainWindow.instance.Items[dataGridIndex].Description = DescriptionBox.Text;
                MainWindow.instance.Items[dataGridIndex].Price = decimal.Parse(PriceBox.Text);
                MainWindow.instance.Items[dataGridIndex].ManufacturerID = ManufacturerBox.Text;
                MainWindow.instance.Items[dataGridIndex].CategoryID = GetSelectedGenresString();
                MainWindow.instance.Items[dataGridIndex].ImagePath = _imagePath;

                MainWindow.instance.LoadData();
                Close();
            }
        }

        private string GetSelectedGenresString()
        {
            return string.Join(",", Genres
                .Where(g => g.SelectedGenre != null && g.SelectedGenre.CategoryID != -1)
                .Select(g => g.SelectedGenre.CategoryID.ToString()));
        }

        public bool IsProductUnique(string author, string name)
        {
            string connectionString = "Host=localhost;Port=5432;Database=ShopDB;Username=postgres;Password=123";
            string sql = "SELECT COUNT(*) FROM \"Products\" WHERE \"ManufacturerID\" = @Manufacturer AND \"Name\" = @Name";

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Manufacturer", author);
                    command.Parameters.AddWithValue("@Name", name);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count == 0;
                }
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

        public void Cancel(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateAvailableGenres()
        {
            AvailableGenres = ShopDbContext.Instance.GetCategories();
            var defaultGenre = new Category { CategoryID = -1, Name = "Выберите жанр" };
            AvailableGenres.Insert(0, defaultGenre);
        }

        private void AddGenre(object sender, RoutedEventArgs e)
        {
            var genreName = GenreTextBox.Text;
            if (string.IsNullOrEmpty(genreName))
            {
                MessageBox.Show("Введите название жанра");
                return;
            }

            if (ShopDbContext.Instance.Categories.Any(c => c.Name == genreName))
            {
                MessageBox.Show("Такой жанр уже существует");
                return;
            }

            try
            {
                var newGenre = new Category { Name = genreName };
                ShopDbContext.Instance.Categories.Add(newGenre);
                ShopDbContext.Instance.SaveChanges();

                UpdateAvailableGenres();

                GenreTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void AddManufacturer(object sender, RoutedEventArgs e)
        {
            var manufacturerName = NewAuthorBox.Text;
            if (string.IsNullOrEmpty(manufacturerName))
            {
                MessageBox.Show("Введите название автора");
                return;
            }

            if (ShopDbContext.Instance.Manufacturers.Any(m => m.Name == manufacturerName))
            {
                MessageBox.Show("Такой автор уже существует");
                return;
            }

            try
            {
                var newManufacturer = new Manufacturer
                {
                    Name = manufacturerName,
                    Address = "Unknown",
                    ContactInfo = "Not specified"
                };
                ShopDbContext.Instance.Manufacturers.Add(newManufacturer);
                ShopDbContext.Instance.SaveChanges();
                ManufacturerBox.ItemsSource = ShopDbContext.Instance.GetManufacturers();
                NewAuthorBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void LoadCategoriesAndManufacturers()
        {
            ManufacturerBox.ItemsSource = ShopDbContext.Instance.GetManufacturers();
        }

        private bool ValidateFields()
        {
            if (string.IsNullOrEmpty(NameBox.Text))
            {
                MessageBox.Show("Введите название книги");
                return false;
            }
            if (string.IsNullOrEmpty(DescriptionBox.Text))
            {
                MessageBox.Show("Введите описание книги");
                return false;
            }
            if (string.IsNullOrEmpty(PriceBox.Text))
            {
                MessageBox.Show("Введите цену книги");
                return false;
            }
            if (string.IsNullOrEmpty(ManufacturerBox.Text))
            {
                MessageBox.Show("Введите автора книги");
                return false;
            }
            if (string.IsNullOrEmpty(_imagePath))
            {
                MessageBox.Show("Загрузите изображение книги");
                return false;
            }
            return true;
        }
    }
}