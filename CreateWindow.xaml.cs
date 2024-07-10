using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;

namespace BookShop
{
    /// <summary>
    /// Логика взаимодействия для CreatePage.xaml
    /// </summary>
    public partial class CreateWindow : Window
    {
        private DispatcherTimer timer = new DispatcherTimer();
        public CreateWindow()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(0.05);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            string message = "";

            if (string.IsNullOrWhiteSpace(Login.Text)) message += "Неверное форматирование логина\n";
            if (string.IsNullOrWhiteSpace(Password.Password)) message += "Неверное форматирование пароля\n";
            if (!string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show(message);
                return;
            }

            ShopDbContext.Instance.Users.Add(new User()
            {
                Name = Login.Text,
                Password = Password.Password,
                Access = Access.SelectedIndex
            });
            ShopDbContext.Instance.SaveChanges();
            AutorizationWindow auth = new AutorizationWindow();
            auth.Show();
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                CreateMovingStar();
            }
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            AnimateClose();
            OpenWindowWithAnimation(new AutorizationWindow());
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void StartTransitionButton_Click(object sender, RoutedEventArgs e)
        {
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.2))
            };

            fadeOutAnimation.Completed += (s, a) =>
            {
                this.Close();
                ShowSecondWindow();
            };

            this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
        }

        private void ShowSecondWindow()
        {
            var autorizationWindow = new AutorizationWindow();
            autorizationWindow.Show();
        }

        public void Exit_Click(object sender, RoutedEventArgs e)
        {
            AutorizationWindow auth = new AutorizationWindow();
            auth.Show();
            Close();
        }
    }
}
