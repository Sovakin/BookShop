using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BookShop
{
    public partial class AutorizationWindow : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();
        public AutorizationWindow()
        {
            InitializeComponent();
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(0.05);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void signin_Click(object sender, RoutedEventArgs e)
        {

            string login_ = login.Text;
            string password_ = password.Password;
            login.Text = "";
            password.Password = "";


            if (string.IsNullOrWhiteSpace(login_) || string.IsNullOrWhiteSpace(password_))
            {
                MessageBox.Show("Пустой логин или пароль");
                return;
            }

            var user = ShopDbContext.Instance.Users.Where(x => x.Name == login_).FirstOrDefault();

            if (user == null)
            {
                MessageBox.Show("Пользователя не существует");
                return;
            }

            if (user.Password != password_)
            {
                MessageBox.Show("Неправильные данные");
                return;
            }

            ShopDbContext.Instance.SetActiveUser(user.ClientID);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private async void AnimateClose()
        {
            var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.2));
            anim.Completed += (s, a) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
            await Task.Delay(500);
        }
        public static void OpenWindowWithAnimation(Window windowToOpen)
        {
            windowToOpen.Opacity = 0;
            windowToOpen.Show();
            var anim = new DoubleAnimation(1, (Duration)TimeSpan.FromSeconds(0.2));
            windowToOpen.BeginAnimation(UIElement.OpacityProperty, anim);
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

        private void CreateMovingStar()
        {
            Random rand = new Random();
            var starSize = rand.Next(2, 5);
            var star = new Ellipse
            {
                Fill = new SolidColorBrush(Color.FromArgb((byte)rand.Next(100, 255), 255, 255, 255)),
                Width = starSize,
                Height = starSize
            };

            Canvas.SetLeft(star, rand.Next((int)StarsCanvas.ActualWidth));
            Canvas.SetTop(star, StarsCanvas.ActualHeight + 100);
            StarsCanvas.Children.Add(star);

            DoubleAnimation animation = new DoubleAnimation
            {
                To = -100,
                Duration = TimeSpan.FromSeconds(rand.Next(2, 10)),
            };

            animation.Completed += (s, a) => StarsCanvas.Children.Remove(star);
            star.BeginAnimation(Canvas.TopProperty, animation);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                CreateMovingStar();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AnimateClose();
            OpenWindowWithAnimation(new CreateWindow());
        }

        private void signup_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow createWindow = new CreateWindow();
            createWindow.ShowDialog();

            Close();
        }
    }
}
