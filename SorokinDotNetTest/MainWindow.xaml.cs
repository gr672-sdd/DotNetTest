using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;

namespace SorokinDotNetTest
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }
        private async void butAutorization_Click(object sender, RoutedEventArgs e)
        {
            // cancel previous task
            _cancellationTokenSource?.Cancel();

            _cancellationTokenSource = new CancellationTokenSource();

            string clientId = tboxIdClient.Text;
            string clientSecret = tboxSecretClient.Text;

            try
            {
                // autorization
                var credential = await AutorizationAsync(_cancellationTokenSource.Token, clientId, clientSecret);

                if (credential != null)
                {
                    OpenContactsWindow(credential);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Авторизация была отменена.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // autorization function
        public async Task<UserCredential> AutorizationAsync(CancellationToken token, string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                MessageBox.Show("Основные поля не заполнены.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            try
            {
                var clientSecrets = new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                };

                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    new[] { "profile", "https://www.googleapis.com/auth/contacts" },
                    "me",
                    token);

                return credential;
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException("Авторизация была отменена пользователем или системой.");
            }
            catch (Exception ex)
            {
                // Логируем ошибку и пробрасываем ее выше
                throw new Exception($"Ошибка во время авторизации: {ex.Message}");
            }
        }

        // Открытие окна контактов
        private void OpenContactsWindow(UserCredential credential)
        {
            // Здесь можно передать данные авторизации для дальнейшей работы
            var window = new ContactsWindow(credential);
            window.Show();
        }
    }
}
