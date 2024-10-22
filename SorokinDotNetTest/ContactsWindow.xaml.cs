using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Auth.OAuth2;
namespace SorokinDotNetTest
{

    /// <summary>
    /// Логика взаимодействия для ContactsWindow.xaml
    /// </summary>
    public partial class ContactsWindow : Window
    {
        private UserCredential _credential;
        private IList<Person> _peopleList; // Список контактов

        public ContactsWindow(UserCredential credential)
        {
            InitializeComponent();
            _credential = credential;
            LoadContactsAsync(); // Загружаем контакты при открытии окна
        }

        // loading contacts
        private async Task LoadContactsAsync()
        {
            try
            {
                _peopleList = await GetPeopleListAsync();
                if (_peopleList != null)
                {
                    ContactList.ItemsSource = _peopleList.OrderBy(x => x.Names?[0]?.GivenName).ToList();
                }
                else
                {
                    ContactList.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки контактов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // getting contacts
        private async Task<IList<Person>> GetPeopleListAsync()
        {
            try
            {
                var serviceClass = new ServiceClass();
                var service = serviceClass.Credential(_credential);

                // request
                var peopleRequest = service.People.Connections.List("people/me");
                peopleRequest.PersonFields = "names,emailAddresses,phoneNumbers,organizations,photos,birthdays,biographies";

                var response = await peopleRequest.ExecuteAsync();

                // sort by date
                var sortedContacts = response.Connections?.OrderByDescending(c => c.Metadata.Sources?.FirstOrDefault()?.UpdateTime).ToList();

                return sortedContacts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении контактов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        // Transitioncreate to the window contact
        private void butCreateContact_Click(object sender, RoutedEventArgs e)
        {
            var createContact = new CreateContact(_credential, _peopleList, "", "Create");
            createContact.Show();
            this.Close();
        }
        //Transitioncreate to the window contact
        private void butEditContact_Click(object sender, RoutedEventArgs e)
        {
            var GetContact = (sender as Button).DataContext as Person;
            var editContactWindow = new CreateContact(_credential, _peopleList, GetContact.ETag, "Edit");
            editContactWindow.Show();
            this.Close();
        }
        //Close the window
        private void butClose_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        //Searth contact
        private void tbSearth_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if(_peopleList != null)
            {
                ContactList.ItemsSource = _peopleList.Where(x => x.Names[0].FamilyName.ToLower().Contains(tbSearth.Text.ToLower()) || x.Names[0].GivenName.ToLower().Contains(tbSearth.Text.ToLower())).ToList();
            }
        }
    }

}