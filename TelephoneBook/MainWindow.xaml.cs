using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml.Linq;


namespace TelephoneBook
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Contact> contacts;
        private Contact selectedContact;
        private SqlConnection connection;
        private SqlDataAdapter adapter;
        private DataSet dataSet;
        private DataTable contactsTable;


        public MainWindow()
        {
            InitializeComponent();
            LoadContacts();
            string connectionString = "Data Source=DESKTOP-GNNUFQH;Initial Catalog=TelephoneBookDb;Integrated Security=True";
            connection = new SqlConnection(connectionString);

            List<string> styles = new List<string> { "light", "dark" };
            styleBox.SelectionChanged += ThemeChange;
            styleBox.ItemsSource = styles;
            styleBox.SelectedItem = "dark";

        }
        private int selectedContactId;

        private void LoadContacts()
        {
            // Создаем подключение к базе данных
            using (SqlConnection connection = new SqlConnection("Data Source=DESKTOP-GNNUFQH;Initial Catalog=TelephoneBookDb;Integrated Security=True"))
            {
                connection.Open();

                // Создаем SQL-запрос для получения списка контактов
                string query = "SELECT * FROM Contacts";

                // Создаем команду
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Создаем адаптер данных
                    SqlDataAdapter adapter = new SqlDataAdapter(command);

                    // Создаем DataSet для хранения данных
                    DataSet dataSet = new DataSet();

                    // Заполняем DataSet данными из базы данных
                    adapter.Fill(dataSet);

                    // Создаем коллекцию контактов и заполняем ее данными из DataSet
                    contacts = new ObservableCollection<Contact>();
                    foreach (DataRow row in dataSet.Tables[0].Rows)
                    {
                        Contact contact = new Contact
                        {
                            ID = Convert.ToInt32(row["ID"]),
                            Name = row["Name"].ToString(),
                            Phone = row["Phone"].ToString(),
                            Category = row["Category"].ToString()
                        };
                        contacts.Add(contact);
                    }
                    dgContacts.ItemsSource = dataSet.Tables[0].DefaultView;
                }
            }
        }

        private void txtPhoneNumber_PreviewTextInput(object sender, TextCompositionEventArgs e) // мнтод на проверку, если это не буквы, то добавляем символ в текстбокс
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text) // метод на проверку, является ли символ числом
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string firstName = txtFirstName.Text;
            string category = txtCategory.Text;
            string phoneNumber = txtPhoneNumber.Text;

            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(category) &&
                !string.IsNullOrEmpty(phoneNumber))
            {
                using (SqlConnection connection = new SqlConnection("Data Source=DESKTOP-GNNUFQH;Initial Catalog=TelephoneBookDb;Integrated Security=True"))
                {
                    connection.Open();

                    // Создаем SQL-запрос для добавления контакта
                    string query = "INSERT INTO Contacts (Name, Phone, Category) VALUES (@Name, @Phone, @Category)";

                    // Создаем команду с параметрами
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", firstName);
                        command.Parameters.AddWithValue("@Phone", phoneNumber);
                        command.Parameters.AddWithValue("@Category", category);

                        // Выполняем команду
                        command.ExecuteNonQuery();
                    }
                }
            }
            LoadContacts();
        }

        private void DgContacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Получаем выбранный контакт
            DataRowView selectedRow = dgContacts.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                // Получаем идентификатор выбранного контакта
                selectedContactId = Convert.ToInt32(selectedRow["ID"]);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e) // обновление данных
        {
            
            try
            {
                DataRowView selectedRow = dgContacts.SelectedItem as DataRowView;
                // Откройте соединение с базой данных
                connection.Open();

                // Создайте команду SQL для обновления записи в базе данных
                string updateQuery = "UPDATE Contacts SET Name = @name, Phone = @phone, Category = @category WHERE ID = @id";
                SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@name", selectedRow["Name"].ToString());
                updateCommand.Parameters.AddWithValue("@phone", selectedRow["Phone"].ToString());
                updateCommand.Parameters.AddWithValue("@category", selectedRow["Category"].ToString());
                updateCommand.Parameters.AddWithValue("@id", selectedRow["ID"]);

                // Выполните команду обновления
                updateCommand.ExecuteNonQuery();

                
                

                MessageBox.Show("Изменения успешно сохранены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении данных: " + ex.Message);
            }
            finally
            {
                // Закройте соединение с базой данных
                connection.Close();
            }
            LoadContacts();
        }

        private void Delete_Click(object sender, RoutedEventArgs e) // удаление
        {
            // Создаем подключение к базе данных
            using (SqlConnection connection = new SqlConnection("Data Source=DESKTOP-GNNUFQH;Initial Catalog=TelephoneBookDb;Integrated Security=True"))
            {
                connection.Open();

                // Создаем SQL-запрос для удаления контакта
                string query = "DELETE FROM Contacts WHERE ID = @ContactId";

                // Создаем команду с параметром
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ContactId", selectedContactId);

                    // Выполняем команду
                    command.ExecuteNonQuery();
                }
            }

            // Обновляем список контактов в DataGrid
            LoadContacts();
        }

        private void Search_Click(object sender, RoutedEventArgs e) // поиск
        {
            try
            {
                // Откройте соединение с базой данных
                connection.Open();

                // Создайте команду SQL для выполнения поиска контактов по имени
                string searchQuery = "SELECT * FROM Contacts WHERE Name LIKE @nameParam";
                SqlCommand command = new SqlCommand(searchQuery, connection);
                command.Parameters.AddWithValue("@nameParam", "%" + txtSearch.Text + "%");

                // Создайте экземпляр класса SqlDataAdapter и выполните команду SQL для заполнения DataSet
                adapter = new SqlDataAdapter(command);
                dataSet = new DataSet();
                adapter.Fill(dataSet, "Contacts");

                // Привяжите данные из DataSet к вашему DataGrid
                dgContacts.ItemsSource = dataSet.Tables["Contacts"].DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении поиска: " + ex.Message);
            }
            finally
            {
                // Закройте соединение с базой данных
                connection.Close();
            }
        }
        private void ThemeChange(object sender, SelectionChangedEventArgs e)
        {
            string style = styleBox.SelectedItem as string;
            // определяем путь к файлу ресурсов
            var uri = new Uri(style + ".xaml", UriKind.Relative);
            // загружаем словарь ресурсов
            ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
            // очищаем коллекцию ресурсов приложения
            Application.Current.Resources.Clear();
            // добавляем загруженный словарь ресурсов
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }
    }
}
