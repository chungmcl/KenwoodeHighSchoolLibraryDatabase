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
using System.Drawing;
using System.Printing;

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
        User selectedUser;
        Item selectedItem;
        bool userSelected;
        bool itemSelected;
        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            LoadDataGrid("SELECT * FROM accounts", true);
            LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                    "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                    "FROM [items] ORDER BY [authorLastName], [ISXX], [copyID]", false);
        }


        /// <summary>
        /// Connect to Microsoft Access Database.
        /// Initialize objects for reading data from the database.
        /// </summary>
        private void InitializeDatabaseConnection()
        {
            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            command = new OleDbCommand();
            command.Connection = c;
            reader = null;
        }

        #region LoadDataGrids
        /// <summary>
        /// Update the fines and overdue books for each user by calling CalculateOverDueAndFines(),
        /// to ensure that database stays up to date.
        /// Take in the SQL command to specify what to call from database and to display in datagrids.
        /// Load specified datagrid with the information called.
        /// </summary>
        /// <param name="sqlText">The SQL command to be executed</param>
        /// <param name="loadAccounts">Load accounts datagrid or items datagrid</param>
        private void LoadDataGrid(string sqlText, bool loadAccounts)
        {
            CalculateOverdueAndFines();
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

        /// <summary>
        /// Load every called entry from the accounts table within the database.
        /// </summary>
        /// <param name="reader">The OleDbDataReader to read each entry from the databse</param>
        private void LoadAccountsDataGrid(OleDbDataReader reader)
        {
            while (reader.Read())
            {
                User newUser = new User();
                newUser.firstName = reader["firstName"].ToString();
                newUser.lastName = reader["lastName"].ToString();
                newUser.userID = reader["userID"].ToString();
                newUser.userType = reader["userType"].ToString();
                newUser.itemLimit = reader["itemLimit"].ToString();
                newUser.dateLimit = reader["dateLimit"].ToString();
                newUser.checkedOut = reader["numberOfCheckedoutItems"].ToString();
                newUser.overdueItems = reader["overdueItems"].ToString();
                newUser.fines = reader["fines"].ToString();
                dataGridAccounts.Items.Add(newUser);
            }
        }

        /// <summary>
        /// Load every called entry from the items table within the database.
        /// </summary>
        /// <param name="reader">The OleDbDataReader to read each entry from the databse</param>
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
                if (authorName.Length > 1)
                {

                    newItem.authorName = authorName;
                }
                else // this isn't working yet
                {
                    newItem.authorName = "";
                }
                string currentlyCheckedOutBy = reader["currentlyCheckedOutBy"].ToString();
                reader.Close();
                command.CommandText = $"SELECT [firstName], [lastName] FROM [accounts] WHERE [userID] = '{currentlyCheckedOutBy}'";
                reader = command.ExecuteReader();
                reader.Read();
                try // more efficient way to deal with this issue? (If an item isn't registered to anyone yet)
                {
                    string firstName = reader["firstName"].ToString();
                    string lastName = reader["lastName"].ToString();
                    newItem.currentlyCheckedOutBy = currentlyCheckedOutBy + $"({lastName}, {firstName})";
                }
                catch
                {
                    newItem.currentlyCheckedOutBy = currentlyCheckedOutBy;
                }

                dataGridItems.Items.Add(newItem);
            }
        }
        #endregion

        #region Calculations
        /// <summary>
        /// Calculate the number of overdue items and fines for each account.
        /// Save updates to database.
        /// </summary>
        private void CalculateOverdueAndFines()
        {
            List<string[]> userIDs = new List<string[]>();
            c.Open();
            command.CommandText = "SELECT [userID], [finePerDay] FROM accounts";
            reader = command.ExecuteReader();
            while(reader.Read())
            {
                string[] toAdd = new string[] { reader[0].ToString(), reader[1].ToString() };
                userIDs.Add(toAdd);
            }
            reader.Close();
            for (int i = 0; i < userIDs.Count; i++)
            {
                int overDue = 0;
                double fines = 0;
                string currentUserID = userIDs[i][0];
                string finePerDay = userIDs[i][1];
                command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{currentUserID}'";
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    DateTime dueDate = Convert.ToDateTime(reader[0].ToString());
                    if (DateTime.Now >= dueDate)
                    {
                        overDue++;
                        fines = fines + (DateTime.Today - dueDate.AddSeconds(1)).TotalDays * double.Parse(finePerDay);
                        // Add one second because books are due at 11:59:59 of the due date, so charge fines day after.
                    }
                }
                reader.Close();
                command.CommandText = $"UPDATE accounts SET " +
                    $"[overDueItems] = {overDue}, " +
                    $"[fines] = {fines} " +
                    $"WHERE [userID] = '{currentUserID}'";
                command.ExecuteNonQuery();
            }
            c.Close();
            reader.Close();
        }
        #endregion Calculations

        #region Select Item or User
        /// <summary>
        /// Set the selected user to the row that the user double-clicked.
        /// Display error message if user double-clicks something other than a user
        /// in the accounts DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridAccounts_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                selectedUser = (User)this.dataGridAccounts.SelectedItem;
                string selectedUserInfo = $"({selectedUser.userID}) " +
                    $"{selectedUser.firstName} {selectedUser.lastName}";
                labelCheckoutSelectedUser.Content = selectedUserInfo;
                labelSelectedUser.Content = selectedUserInfo;
                this.userSelected = true;
            }
            catch
            {
                MessageBox.Show("Please double-click a row to select a user.");
            }
        }

        /// <summary>
        /// Set the selected item to the row that the item double-clicked.
        /// Display error message if user double-clicks something other than an item
        /// in the items DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridItems_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                selectedItem = (Item)this.dataGridItems.SelectedItem;
                string selectedItemInfo = $"{selectedItem.title} ({selectedItem.itemID})";
                labelCheckoutSelectedItemTitle.Content = selectedItemInfo;
                labelSelectedItem.Content = selectedItemInfo;
                this.itemSelected = true;
            }
            catch
            {
                MessageBox.Show("Please double-click a row to select an item.");
            }
        }
        #endregion

        #region DataGrid Queries
        /// <summary>
        /// Save the column that user would like to query from.
        /// Display hint text to show the purpose of the textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxAccountsSearchByOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string setTextBoxTo = comboBoxAccountsSearchByOptions.SelectedValue.ToString().Substring(37);
            if (setTextBoxTo.Count() > 0)
            {
                textBoxAccountsSearchBy.Text = $"Enter a {setTextBoxTo}...";
                LoadDataGrid("SELECT * FROM accounts", true);
            }
        }

        /// <summary>
        /// Save the colum that the user would like to query from.
        /// Display hint text to show the purpose of the textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Query all items with similar text in the textBox in the column that user
        /// specified with the comboBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    LoadDataGrid($"SELECT * FROM accounts WHERE [{queryColumn}] LIKE '%{currentText}%'", true);
                }
            }
        }

        /// <summary>
        /// Query all items with similar text in the textBox in the column that user
        /// specified with the comboBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                        queryColumn = "authorLastName";
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
                    LoadDataGrid($"SELECT * FROM items WHERE [{queryColumn}] LIKE '%{currentText}%'", false);
                }
            }
        }
        
        /// <summary>
        /// Delete hint text when user enters query textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxAccountsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxAccountsSearchBy.Text = "";
        }

        /// <summary>
        /// Delete hint text when user enters query textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxItemsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxItemsSearchBy.Text = "";
        }
        #endregion

        #region Window Openers
        /// <summary>
        /// Open ItemRegistrationWindow.
        /// If new item is registered, reload items DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonToItemRegistrationWindow_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Open UserRegistrationWindow.
        /// If new user is registered, reload accounts DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonToUserRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            UserRegistrationWindow w = new UserRegistrationWindow();
            w.Owner = this;
            bool? receive = w.ShowDialog();
            if (receive == true)
            {
                LoadDataGrid("SELECT * FROM accounts", true);
            }
        }

        /// <summary>
        /// Open the PrintUpcomingDueWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrintUpcomingItems_Click(object sender, RoutedEventArgs e)
        {
            PrintUpcomingDueWindow w = new PrintUpcomingDueWindow();
            w.Show();
        }

        /// <summary>
        /// Open the PrintFinesWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrintFinedUsers_Click(object sender, RoutedEventArgs e)
        {
            PrintFinesWindow w = new PrintFinesWindow();
            w.Show();
        }
        #endregion

        #region Checkout Item to Selected User
        /// <summary>
        /// Checkout selected book to select user.
        /// Display an error message with MessageBox if:
        /// Either an item or a user is not selected.
        /// The selected item is already checked out to the selected user.
        /// The selected item is already checked out to another user.
        /// The selected user is at his/her item limit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnToCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (userSelected && itemSelected)
            {
                if (selectedItem.currentlyCheckedOutBy != selectedUser.userID)
                {
                    if (selectedItem.currentlyCheckedOutBy == "")
                    {
                        if (int.Parse(selectedUser.checkedOut) < int.Parse(selectedUser.itemLimit))
                        {
                            DateTime dueDate = (DateTime.Today.AddDays(double.Parse(selectedUser.dateLimit)).AddHours(23.9999));
                            if (MessageBox.Show(
                                $"Confirm Checkout -\n" +
                                $"Check out item: {selectedItem.title}\n" +
                                $"To user: ({selectedUser.userID}) {selectedUser.lastName}, {selectedUser.firstName}\n" +
                                $"For {selectedUser.dateLimit} day(s). Due on {dueDate.ToString()}"
                                , "Confirm Checkout", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                            {
                                CheckoutDatabaseUpdate(dueDate);
                            }
                        }
                        else
                        {
                            MessageBox.Show("This user has reached his/her item limit!");
                        }
                    }
                    else
                    {
                        MessageBox.Show($"This item is already checked out to user {selectedItem.currentlyCheckedOutBy}!");
                    }
                }
                else
                {
                    MessageBox.Show("This book is already checked out to this user!");
                }
            }
            else
            {
                MessageBox.Show("Please double-click a user and an item to select them for checkout.");
            }
        }

        /// <summary>
        /// Needs a darn comment!
        /// </summary>
        /// <param name="dueDate"></param>
        private void CheckoutDatabaseUpdate(DateTime dueDate)
        {
            string userID = selectedUser.userID.ToString();
            string stringDueDate = dueDate.ToString();
            string itemID = selectedItem.itemID.ToString();
            c.Open();
            command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '{userID}', [dueDate] = '{stringDueDate}' WHERE itemID = '{itemID}'";
            command.ExecuteNonQuery();
            int checkedOut = (int.Parse(selectedUser.checkedOut.ToString())) + 1;
            command.CommandText = $"UPDATE accounts SET [numberOfCheckedoutItems] = {checkedOut} WHERE userID = '{userID}'";
            command.ExecuteNonQuery();
            c.Close();

            LoadDataGrid("SELECT * FROM accounts", true);
            LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
            "FROM [items] ORDER BY [authorLastName], [ISXX], [copyID]", false);
        }
        #endregion

        #region Editing Objects (Users and Items)
        private void buttonEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (itemSelected)
            {
                ItemRegistrationWindow w = new ItemRegistrationWindow(selectedItem);
                w.Owner = this;
                bool? receive = w.ShowDialog();
                if (receive == true)
                {
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                        "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                        "FROM [items] ORDER BY [ISXX], [copyID]", false);
                    LoadDataGrid("SELECT * FROM accounts", true);
                    Item check = (Item)dataGridItems.Items[0];
                    this.selectedItem = (Item)dataGridItems.Items[0];
                    labelCheckoutSelectedItemTitle.Content = selectedItem.title;
                }
            }
            else
            {
                MessageBox.Show("Please double-click an item to select it for editing.");
            }
        }

        private void BtnToUserEditWindow_Click(object sender, RoutedEventArgs e)
        {
            if (userSelected)
            {
                UserRegistrationWindow w = new UserRegistrationWindow(selectedUser);
                bool? receive = w.ShowDialog();
                if (receive == true)
                {
                    LoadDataGrid("SELECT * FROM accounts", true);
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                            "FROM [items] ORDER BY [ISXX], [copyID]", false);
                }
            }
            else
            {
                MessageBox.Show("Please double-click a user to select it for editing.");
            }
        }

        /// <summary>
        /// Return the selected item to the library.
        /// Display error message if the selected item is checked out to no one or if an item is
        /// not selected.
        /// Delete the item's log of currentlyCheckedOutBy and set previousCheckedOutBy to the user who
        /// the item was checked out to.
        /// Remove the dueDate from the item.
        /// Subtract the user's number of checked out items by one.
        /// (Fines and overdue will be recalculated when the DataGrid is reloaded).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonReturnSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (itemSelected)
            {
                if (selectedItem.currentlyCheckedOutBy != "")
                {
                    c.Open();
                    command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{selectedItem.currentlyCheckedOutBy}'";
                    reader = command.ExecuteReader();
                    reader.Read();
                    DateTime dueDate = Convert.ToDateTime(reader[0].ToString());
                    double overdueBy = (DateTime.Today - dueDate.AddSeconds(1)).TotalDays;
                    // Add one second because book is due at 11:59:59 - count overdue days starting the next day
                    if (overdueBy < 0)
                    {
                        overdueBy = 0;
                    }
                    reader.Close();
                    command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE [userID] = '{selectedItem.currentlyCheckedOutBy}'";
                    reader = command.ExecuteReader();
                    reader.Read();
                    double totalFinesForItem = ((double)reader[0]) * overdueBy;
                    reader.Close();

                    if (MessageBox.Show($"Confirm Return of '{selectedItem.title}' - \n" +
                        $"Lent to {selectedItem.currentlyCheckedOutBy}\n" +
                        $"Overdue by {overdueBy} days.\n" +
                        $"Fines owed for this item = USD ${totalFinesForItem}", 
                        "Confirm Return", 
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '' WHERE [itemID] = '{selectedItem.itemID}'";
                        command.ExecuteNonQuery();
                        command.CommandText = $"UPDATE items SET [previousCheckedOutBy] = '{selectedItem.currentlyCheckedOutBy}' WHERE [itemID] = '{selectedItem.itemID}'";
                        command.ExecuteNonQuery();
                        command.CommandText = $"UPDATE items SET [dueDate] = '' WHERE [itemID] = '{selectedItem.itemID}'";
                        command.ExecuteNonQuery();
                        command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                            $"WHERE [userID] = '{selectedItem.currentlyCheckedOutBy}'";
                        command.ExecuteNonQuery();
                        c.Close(); // Needs to close before LoadDataGrid on account of reopening in CheckoutDatabaseUpdate

                      LoadDataGrid("SELECT * FROM accounts", true);
                        LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                                "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                                "FROM [items] ORDER BY [ISXX], [copyID]", false);
                    }
                    c.Close(); // Close in case if statement is false 
                }
                else
                {
                    MessageBox.Show("This item is not checked out to any user.");
                }
            }
            else
            {
                MessageBox.Show("Please double-click an item to select for returning.");
            }
        }

        #endregion

        #region Deletion
        /// <summary>
        /// Delete the selected Item from the Data Base.
        /// Display error message if an item is not selected.
        /// Lower the user's number of checked out items by one (if item is checked out to a user)
        /// (User's number of overdue items and fines will be recalculated when dataGrid is loaded.)
        /// Set the selected item to an empty item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDeleteSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (itemSelected)
            {
                if (MessageBox.Show("Delete Selected Item?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    c.Open();
                    command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                        $"WHERE [userID] = '{selectedItem.currentlyCheckedOutBy}'";
                    command.ExecuteNonQuery();

                    command.CommandText = $"DELETE * FROM items WHERE [itemID] = '{selectedItem.itemID}'";
                    command.ExecuteNonQuery();
                    c.Close();

                    LoadDataGrid("SELECT * FROM accounts", true);
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                            "FROM [items] ORDER BY [ISXX], [copyID]", false);

                    itemSelected = false;
                    selectedItem = new Item();
                    labelSelectedItem.Content = "(Select an Item)";
                    labelCheckoutSelectedItemTitle.Content = "(Select an Item)";
                }
            }
            else
            {
                MessageBox.Show("Please double-click an item to select it for deletion.");
            }
        }

        /// <summary>
        /// Delete the selected user from the Database.
        /// Display error message if user is not selected.
        /// Clear checkedOutBy, dueDate, and set previousCheckedOutBy to the user being deleted.
        /// Set the currently selected user to an empty user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDeleteSelectedUser_Click(object sender, RoutedEventArgs e)
        {
            if (userSelected)
            {
                if (MessageBox.Show("Delete Selected User?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    c.Open();
                    command.CommandText = "UPDATE items " +
                        "SET [currentlyCheckedOutBy] = '', " +
                        $"[previousCheckedOutBy] = '{selectedUser.userID}', " +
                        "[dueDate] = '' " +
                        $"WHERE [currentlyCheckedOutBy] = '{selectedUser.userID}'";
                    command.ExecuteNonQuery();

                    command.CommandText = $"DELETE * FROM accounts WHERE [userID] = '{selectedUser.userID}'";
                    command.ExecuteNonQuery();
                    c.Close();

                    LoadDataGrid("SELECT * FROM accounts", true);
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                            "FROM [items] ORDER BY [ISXX], [copyID]", false);

                    userSelected = false;
                    selectedUser = new User();
                    labelSelectedUser.Content = "(Select a User)";
                    labelCheckoutSelectedUser.Content = "(Select a User)";
                }
            }
            else
            {
                MessageBox.Show("Please double-click a user to select it for deletion.");
            }
        }
        #endregion
    }

    /// <summary>
    /// User to be displayed within the accounts dataGrid.
    /// Can be used to load information about the user in the database.
    /// </summary>
    public struct User
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string userID { get; set; }
        public string userType { get; set; }
        public string itemLimit { get; set; }
        public string dateLimit { get; set; }
        public string checkedOut { get; set; }
        public string overdueItems { get; set; }
        public string fines { get; set; }
    }

    /// <summary>
    /// Item to be displayed within the items dataGrid.
    /// Can be used to load information about the item in the database.
    /// </summary>
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
