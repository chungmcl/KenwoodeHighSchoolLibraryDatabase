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
using System.Data.OleDb;
using System.Data;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OleDbConnection c;
        OleDbDataReader reader;
        OleDbCommand command;
        public MainWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            command = new OleDbCommand();
            command.Connection = c;
            reader = null;
            LoadDataGrid("SELECT * FROM accounts", true);
            LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                    "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                    "FROM [items] ORDER BY [authorLastName], [ISXX], [copyID]", false);
        }

        private void LoadDataGrid(string sqlText, bool loadAccounts)
        {
            c.Open();
            command.CommandText = sqlText;
            command.CommandType = System.Data.CommandType.Text;
            reader = command.ExecuteReader();
            if (loadAccounts)
            {
                dataGridAccounts.Items.Clear();
                LoadAccountsDataGrid(reader);
            }
            else
            {
                dataGridItems.Items.Clear();
                LoadItemsDataGrid(reader);
            }
            reader.Close();
            c.Close();
        }

        private void LoadAccountsDataGrid(OleDbDataReader reader)
        {
            while (reader.Read())
            {
                User newUser = new User();
                newUser.firstName = reader["firstName"].ToString();
                newUser.lastName = reader["lastName"].ToString();
                newUser.userID = reader["userID"].ToString();
                newUser.userType = reader["userType"].ToString();
                newUser.itemLimit = reader["bookLimit"].ToString();
                newUser.dateLimit = reader["dateLimit"].ToString();
                dataGridAccounts.Items.Add(newUser);
            }
        }

        private void LoadItemsDataGrid(OleDbDataReader reader)
        {
            while (reader.Read())
            {
                Item newItem = new Item();
                newItem.itemID = reader["itemID"].ToString();
                newItem.deweyDecimal = reader["deweyDecimal"].ToString();
                newItem.format = reader["format"].ToString();
                newItem.genre = reader["genreClassOne"].ToString();
                newItem.title = reader["title"].ToString();
                string authorName = $"{reader["authorLastName"].ToString()}, {reader["authorFirstName"].ToString()} " +
                    $"{reader["authorMiddleName"].ToString()}";
                newItem.authorName = authorName;
                newItem.currentlyCheckedOutBy = reader["currentlyCheckedOutBy"].ToString();
                dataGridItems.Items.Add(newItem);
            }
        }

        private void TstBtnDeleteFromAccounts_Click(object sender, RoutedEventArgs e)
        {
            c.Open();
            command.CommandText = "DELETE * FROM accounts";
            command.ExecuteNonQuery();
            c.Close();
            LoadDataGrid("SELECT * FROM accounts", true);
        }

        private void dataGridAccounts_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            User selectedUser = (User)this.dataGridAccounts.SelectedItem;
        }

        private void comboBoxAccountsSearchByOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string setTextBoxTo = comboBoxAccountsSearchByOptions.SelectedValue.ToString().Substring(37);
            if (setTextBoxTo.Count() > 0)
            {
                textBoxAccountsSearchBy.Text = $"Enter a {setTextBoxTo}...";
                LoadDataGrid("SELECT * FROM accounts", true);
            }
        }

        private void comboBoxItemsSearchByOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string setTextBoxTo = comboBoxItemsSearchByOptions.SelectedValue.ToString().Substring(37);
            if (setTextBoxTo.Count() > 0)
            {
                textBoxItemsSearchBy.Text = $"Enter a(n) {setTextBoxTo}...";
                LoadDataGrid("SELECT * FROM items", false);
            }
            if (setTextBoxTo == "Lent To")
            {
                textBoxItemsSearchBy.Text = $"Enter who {setTextBoxTo}...";
                LoadDataGrid("SELECT * FROM items", false);
            }
        }

        private void textBoxAccountsSearchBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentText = textBoxAccountsSearchBy.Text;
            if (currentText == "")
            {
                LoadDataGrid("SELECT * FROM accounts", true);
            }
            else
            {
                int searchType = comboBoxAccountsSearchByOptions.SelectedIndex;
                string queryColumn = "";
                switch (searchType)
                {
                    case 0:
                        queryColumn = "firstName";
                        break;
                    case 1:
                        queryColumn = "lastName";
                        break;
                    case 2:
                        queryColumn = "userID";
                        break;
                }

                if (queryColumn != "")
                {
                    LoadDataGrid($"SELECT * FROM accounts WHERE [{queryColumn}] = '{currentText}'", true);
                }
            }
        }


        private void textBoxItemsSearchBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentText = textBoxItemsSearchBy.Text;
            if (currentText == "")
            {
                LoadDataGrid("SELECT * FROM items", false);
            }
            else
            {
                int searchType = comboBoxItemsSearchByOptions.SelectedIndex;
                string queryColumn = "";
                switch (searchType)
                {
                    case 0:
                        queryColumn = "deweyDecimal";
                        break;
                    case 1:
                        queryColumn = "itemID";
                        break;
                    case 2:
                        queryColumn = "title";
                        break;
                    case 3:
                        queryColumn = "authorLastName"; // need to include full name
                        break;
                    case 4:
                        queryColumn = "genreClassOne";
                        break;
                    case 5:
                        queryColumn = "currentlyCheckedOutBy";
                        break;
                }

                if (queryColumn != "")
                {
                    LoadDataGrid($"SELECT * FROM items WHERE [{queryColumn}] = '{currentText}'", false);
                }
            }
        }

        private void textBoxAccountsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxAccountsSearchBy.Text = "";
        }

        private void textBoxItemsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxItemsSearchBy.Text = "";
        }

        private void BtnToBookRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            ItemRegistrationWindow x = new ItemRegistrationWindow();
            x.Owner = this;
            bool? receive = x.ShowDialog();
            if (receive == true)
            {
                LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                    "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                    "FROM [items] ORDER BY [ISXX], [copyID]", false);
            }
        }

        private void BtnToUserRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            UserRegistrationWindow w = new UserRegistrationWindow();
            w.Owner = this;
            bool? receive = w.ShowDialog();
            if (receive == true)
            {
                LoadDataGrid("SELECT * FROM accounts", true);
            }
        }



        private void dataGridItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Item selected = (Item)this.dataGridItems.SelectedItem;
            ItemViewAndCheckoutWindow w = new ItemViewAndCheckoutWindow(selected);
        }
    }

    public struct User
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string userID { get; set; }
        public string userType { get; set; }
        public string itemLimit { get; set; }
        public string dateLimit { get; set; }
        public string checkedOut { get; set; }
        public string overdue { get; set; }
    }

    public struct Item
    {
        public string itemID { get; set; }
        public string deweyDecimal { get; set; }
        public string title { get; set; }
        public string authorName { get; set; }
        public string genre  { get; set; }
        public string format { get; set; }
        public string currentlyCheckedOutBy { get; set; }
    }
}
