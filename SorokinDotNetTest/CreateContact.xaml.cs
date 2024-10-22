using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.PeopleService.v1;
using Google.Apis.Auth.OAuth2;

namespace SorokinDotNetTest
{
    /// <summary>
    /// Логика взаимодействия для CreateContact.xaml
    /// </summary>
    public partial class CreateContact : Window
    {
        private UserCredential _credential;
        private string _etagAll;
        private Person _apiContact;
        private string _actionType;
        private ServiceClass _serviceClass;
        IList<Person> existingContacts;
        public CreateContact(UserCredential credential, IList<Person> peopleList, string etag, string actionType)
        {
            InitializeComponent();
            _credential = credential;
            _etagAll = etag;
            _actionType = actionType;
            _serviceClass = new ServiceClass();
            existingContacts = peopleList;
            if (actionType == "Create")
            {
                butCreatePeople.Content = "Создать";
                butDeletePeople.Visibility = Visibility.Hidden;
            }
            else if (actionType == "Edit")
            {
                butCreatePeople.Content = "Изменить";
                _apiContact = peopleList.FirstOrDefault(x => x.ETag == etag);
                LoadContactData(_apiContact); // loading contact
            }
        }

        // Uploading contact data for editing
        private void LoadContactData(Person contact)
        {
            if (contact == null) return;

            if (contact.Names != null)
            {
                tbFirstName.Text = contact.Names[0]?.GivenName;
                tbLastName.Text = contact.Names[0]?.FamilyName;
            }

            if (contact.EmailAddresses != null)
            {
                tbEmail.Text = contact.EmailAddresses[0]?.Value;
            }

            if (contact.Organizations != null)
            {
                tbCompany.Text = contact.Organizations[0]?.Name;
                tbPosition.Text = contact.Organizations[0]?.Title;
            }

            if (contact.PhoneNumbers != null)
            {
                tbPhone.Text = contact.PhoneNumbers[0]?.Value;
            }

            if (contact.Biographies != null)
            {
                tbNote.Text = contact.Biographies[0]?.Value;
            }

            if (contact.Photos != null && contact.Photos[0]?.Url != null)
            {
                ImageContact.Source = new BitmapImage(new Uri(contact.Photos[0].Url));
            }
        }

        // data verification
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(tbFirstName.Text) || string.IsNullOrWhiteSpace(tbLastName.Text))
            {
                MessageBox.Show("Заполните имя или фамилию", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(tbEmail.Text) || string.IsNullOrWhiteSpace(tbPhone.Text))
            {
                MessageBox.Show("Email и телефон не могут быть пустыми", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(tbEmail.Text) && !IsValidEmail(tbEmail.Text))
            {
                MessageBox.Show("Введите корректный email.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(tbPhone.Text) && !IsValidPhoneNumber(tbPhone.Text))
            {
                MessageBox.Show("Введите корректный номер телефона.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            var duplicateContact = existingContacts.FirstOrDefault(contact =>
                (contact.Names != null &&
                 contact.Names[0].GivenName == tbFirstName.Text &&
                 contact.Names[0].FamilyName == tbLastName.Text) ||
                (contact.EmailAddresses != null &&
                 contact.EmailAddresses[0].Value == tbEmail.Text) ||
                (contact.PhoneNumbers != null &&
                 contact.PhoneNumbers[0].Value == tbPhone.Text));

            if (duplicateContact != null)
            {
                MessageBox.Show("Контакт с таким именем, электронной почтой или номером телефона уже существует.", "Ошибка дублирования", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return phoneNumber.All(char.IsDigit);
        }

        private async void butCreatePeople_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            var contactToCreate = new Person
            {
                Names = new List<Name>
                {
                    new Name { GivenName = tbFirstName.Text, FamilyName = tbLastName.Text }
                },
                EmailAddresses = new List<EmailAddress> { new EmailAddress { Value = tbEmail.Text } },
                PhoneNumbers = new List<PhoneNumber> { new PhoneNumber { Value = tbPhone.Text } },
                Organizations = new List<Organization>
                {
                    new Organization { Name = tbCompany.Text, Title = tbPosition.Text }
                },
                Biographies = new List<Biography> { new Biography { Value = tbNote.Text } }
            };

            if (_actionType == "Create")
            {
                await CreateContactAsync(contactToCreate);
            }
            else if (_actionType == "Edit")
            {
                await EditContactAsync(contactToCreate);
            }
        }

        // contact creation
        private async Task CreateContactAsync(Person contact)
        {
            try
            {
                var service = _serviceClass.Credential(_credential);
                var createdContact = await service.People.CreateContact(contact).ExecuteAsync();
                MessageBox.Show("Контакт успешно создан.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании контакта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // contact editing
        private async Task EditContactAsync(Person contact)
        {
            try
            {
                var service = _serviceClass.Credential(_credential);

                // Удаляем старый контакт
                await service.People.DeleteContact(_apiContact.ResourceName).ExecuteAsync();

                // Создаем новый с изменениями
                var updatedContact = await service.People.CreateContact(contact).ExecuteAsync();
                MessageBox.Show("Контакт успешно изменен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении контакта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // delete contack
        private async void butDeletePeople_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var service = _serviceClass.Credential(_credential);
                await service.People.DeleteContact(_apiContact.ResourceName).ExecuteAsync();
                MessageBox.Show("Контакт успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                CloseWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении контакта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // close window
        private void butClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWindow();
        }

        // navigation
        private void CloseWindow()
        {
            var contactsWindow = new ContactsWindow(_credential);
            contactsWindow.Show();
            this.Close();
        }
    }
}
