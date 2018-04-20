using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Reflection;

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
        List<Item> itemsToAdd = new List<Item>();
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
            this.c = new OleDbConnection();
            this.c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            this.command = new OleDbCommand();
            this.command.Connection = this.c;
            this.reader = null;
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
            this.c.Open();
            this.command.CommandText = sqlText;
            this.command.CommandType = System.Data.CommandType.Text;
            this.reader = this.command.ExecuteReader();
            if (loadAccounts)
            {
                this.dataGridAccounts.Items.Clear();
                LoadAccountsDataGrid(this.reader);
            }
            else
            {
                this.dataGridItems.Items.Clear();
                LoadItemsDataGrid(this.reader);
            }
            this.reader.Close();
            this.c.Close();
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
                this.dataGridAccounts.Items.Add(newUser);
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
                if (authorName.Length > 3)
                {

                    newItem.authorName = authorName;
                }
                else // it should work now - double check for bugs
                {
                    newItem.authorName = "";
                }
                newItem.currentlyCheckedOutBy = reader["currentlyCheckedOutBy"].ToString();
                this.itemsToAdd.Add(newItem);
            }

            for (int i = 0; i < this.itemsToAdd.Count; i++)
            {
                reader.Close();
                string currentlyCheckedOutBy = this.itemsToAdd[i].currentlyCheckedOutBy;
                this.command.CommandText = $"SELECT [firstName], [lastName] FROM [accounts] WHERE [userID] = '{currentlyCheckedOutBy}'";
                reader = this.command.ExecuteReader();
                reader.Read();
                try // more efficient way to deal with this issue? (If an item isn't registered to anyone yet)
                {
                    string firstName = reader["firstName"].ToString();
                    string lastName = reader["lastName"].ToString();
                    this.itemsToAdd[i].currentlyCheckedOutBy = currentlyCheckedOutBy + $"~({lastName}, {firstName})";
                    // '~' character to be used as check character when program needs to read currentlyCheckedOutBy (ID only)
                    // Prevent operator from registering userID with '~'
                }
                catch // specify catch?
                {
                    this.itemsToAdd[i].currentlyCheckedOutBy = currentlyCheckedOutBy;
                }
                this.dataGridItems.Items.Add(this.itemsToAdd[i]);
            }
            this.itemsToAdd.Clear();
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
            this.c.Open();
            this.command.CommandText = "SELECT [userID], [finePerDay] FROM accounts";
            this.reader = this.command.ExecuteReader();
            while (this.reader.Read())
            {
                string[] toAdd = new string[] { this.reader[0].ToString(), this.reader[1].ToString() };
                userIDs.Add(toAdd);
            }
            this.reader.Close();
            for (int i = 0; i < userIDs.Count; i++)
            {
                int overDue = 0;
                double fines = 0;
                string currentUserID = userIDs[i][0];
                string finePerDay = userIDs[i][1];
                this.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{currentUserID}'";
                this.reader = this.command.ExecuteReader();
                while (this.reader.Read())
                {
                    DateTime dueDate = Convert.ToDateTime(this.reader[0].ToString());
                    if (DateTime.Now >= dueDate)
                    {
                        overDue++;
                        fines = fines + (DateTime.Today - dueDate.AddSeconds(1)).TotalDays * double.Parse(finePerDay);
                        // Add one second because books are due at 11:59:59 of the due date, so charge fines day after.
                    }
                }
                this.reader.Close();
                this.command.CommandText = $"UPDATE accounts SET " +
                    $"[overDueItems] = {overDue}, " +
                    $"[fines] = {fines} " +
                    $"WHERE [userID] = '{currentUserID}'";
                this.command.ExecuteNonQuery();
            }
            this.c.Close();
            this.reader.Close();
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
                this.selectedUser = (User)this.dataGridAccounts.SelectedItem;
                string selectedUserInfo = $"({this.selectedUser.userID}) " +
                    $"{this.selectedUser.firstName} {this.selectedUser.lastName}";
                this.labelCheckoutSelectedUser.Content = selectedUserInfo;
                this.labelSelectedUser.Content = selectedUserInfo;
                this.userSelected = true;

                if (this.checkBoxShowItems.IsChecked == true) // had to compare to true; .IsChecked is type bool? (nullable)
                {
                    this.checkBoxShowUser.IsEnabled = false;
                    this.comboBoxItemsSearchByOptions.SelectedIndex = 5;
                    this.textBoxItemsSearchBy.Text = this.selectedUser.userID;
                }
            }
            catch // specify catch?
            {
                MessageBox.Show("Please double-click a row to select a user.");
                this.checkBoxShowItems.IsChecked = false;
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
                this.selectedItem = (Item)this.dataGridItems.SelectedItem;
                string selectedItemInfo = $"{this.selectedItem.title} ({this.selectedItem.itemID})";
                this.labelCheckoutSelectedItemTitle.Content = selectedItemInfo;
                this.labelSelectedItem.Content = selectedItemInfo;
                this.itemSelected = true;

                if (this.checkBoxShowUser.IsChecked == true)
                {
                    this.checkBoxShowItems.IsEnabled = false;
                    this.comboBoxAccountsSearchByOptions.SelectedIndex = 2;
                    string selectedItemUserID = this.selectedItem.currentlyCheckedOutBy;
                    if (selectedItemUserID.Length > 0)
                    {
                        selectedItemUserID = selectedItemUserID.Substring(0, selectedItemUserID.IndexOf('~'));
                        this.textBoxAccountsSearchBy.Text = selectedItemUserID;
                    }
                }
            }
            catch
            {
                MessageBox.Show("Please double-click a row to select an item.");
                this.checkBoxShowUser.IsChecked = false;
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
            if (this.comboBoxAccountsSearchByOptions.SelectedIndex != -1)
            {
                string setTextBoxTo = this.comboBoxAccountsSearchByOptions.SelectedValue.ToString().Substring(37);
                if (setTextBoxTo.Count() > 0)
                {
                    this.textBoxAccountsSearchBy.Text = $"Enter a {setTextBoxTo}...";
                    LoadDataGrid("SELECT * FROM accounts", true);
                }
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
            if (this.comboBoxItemsSearchByOptions.SelectedIndex != -1) // If check box is unchecked
            {
                string setTextBoxTo = this.comboBoxItemsSearchByOptions.SelectedValue.ToString().Substring(37);
                if (setTextBoxTo.Count() > 0)
                {
                    this.textBoxItemsSearchBy.Text = $"Enter a(n) {setTextBoxTo}...";
                    LoadDataGrid("SELECT * FROM items", false); // Factor out?
                }
                if (setTextBoxTo == "Lent To")
                {
                    this.textBoxItemsSearchBy.Text = $"Enter who {setTextBoxTo}...";
                    LoadDataGrid("SELECT * FROM items", false); // Factor out?
                }
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
            string currentText = this.textBoxAccountsSearchBy.Text;
            if (currentText == "")
            {
                LoadDataGrid("SELECT * FROM accounts", true);
            }
            else
            {
                int searchType = this.comboBoxAccountsSearchByOptions.SelectedIndex;
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
            string currentText = this.textBoxItemsSearchBy.Text;
            if (currentText == "")
            {
                LoadDataGrid("SELECT * FROM items", false);
            }
            else
            {
                int searchType = this.comboBoxItemsSearchByOptions.SelectedIndex;
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
            this.textBoxAccountsSearchBy.Text = "";
        }

        /// <summary>
        /// Delete hint text when user enters query textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxItemsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            this.textBoxItemsSearchBy.Text = "";
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
            if (this.userSelected && this.itemSelected)
            {
                if (this.selectedItem.currentlyCheckedOutBy != this.selectedUser.userID)
                {
                    if (this.selectedItem.currentlyCheckedOutBy == "")
                    {
                        if (int.Parse(this.selectedUser.checkedOut) < int.Parse(this.selectedUser.itemLimit))
                        {
                            DateTime dueDate = (DateTime.Today.AddDays(double.Parse(this.selectedUser.dateLimit)).AddHours(23.9999));
                            if (MessageBox.Show(
                                $"Confirm Checkout -\n" +
                                $"Check out item: {this.selectedItem.title}\n" +
                                $"To user: ({this.selectedUser.userID}) {this.selectedUser.lastName}, {this.selectedUser.firstName}\n" +
                                $"For {this.selectedUser.dateLimit} day(s). Due on {dueDate.ToString()}"
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
                        MessageBox.Show($"This item is already checked out to user {this.selectedItem.currentlyCheckedOutBy}!");
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
            string userID = this.selectedUser.userID.ToString();
            string stringDueDate = dueDate.ToString();
            string itemID = this.selectedItem.itemID.ToString();
            this.c.Open();
            this.command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '{userID}', [dueDate] = '{stringDueDate}' WHERE itemID = '{itemID}'";
            this.command.ExecuteNonQuery();
            int checkedOut = (int.Parse(this.selectedUser.checkedOut.ToString())) + 1;
            this.command.CommandText = $"UPDATE accounts SET [numberOfCheckedoutItems] = {checkedOut} WHERE userID = '{userID}'";
            this.command.ExecuteNonQuery();
            this.c.Close();

            LoadDataGrid("SELECT * FROM accounts", true);
            LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
            "FROM [items] ORDER BY [authorLastName], [ISXX], [copyID]", false);
        }
        #endregion

        #region Editing Objects (Users and Items)
        private void buttonEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected)
            {
                ItemRegistrationWindow w = new ItemRegistrationWindow(this.selectedItem);
                w.Owner = this;
                bool? receive = w.ShowDialog();
                if (receive == true)
                {
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                        "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                        "FROM [items] ORDER BY [ISXX], [copyID]", false);
                    LoadDataGrid("SELECT * FROM accounts", true);
                    Item check = (Item)this.dataGridItems.Items[0];
                    this.selectedItem = (Item)this.dataGridItems.Items[0];
                    this.labelCheckoutSelectedItemTitle.Content = this.selectedItem.title;
                }
            }
            else
            {
                MessageBox.Show("Please double-click an item to select it for editing.");
            }
        }

        private void BtnToUserEditWindow_Click(object sender, RoutedEventArgs e)
        {
            if (this.userSelected)
            {
                UserRegistrationWindow w = new UserRegistrationWindow(this.selectedUser);
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
            if (this.itemSelected)
            {
                if (this.selectedItem.currentlyCheckedOutBy != "")
                {
                    this.c.Open();
                    this.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{this.selectedItem.currentlyCheckedOutBy}'";
                    this.reader = this.command.ExecuteReader();
                    this.reader.Read();
                    DateTime dueDate = Convert.ToDateTime(this.reader[0].ToString());
                    double overdueBy = (DateTime.Today - dueDate.AddSeconds(1)).TotalDays;
                    // Add one second because book is due at 11:59:59 - count overdue days starting the next day
                    if (overdueBy < 0)
                    {
                        overdueBy = 0;
                    }
                    this.reader.Close();
                    this.command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE [userID] = '{this.selectedItem.currentlyCheckedOutBy}'";
                    this.reader = this.command.ExecuteReader();
                    this.reader.Read();
                    double totalFinesForItem = ((double)this.reader[0]) * overdueBy;
                    this.reader.Close();

                    if (MessageBox.Show($"Confirm Return of '{this.selectedItem.title}' - \n" +
                        $"Lent to {this.selectedItem.currentlyCheckedOutBy}\n" +
                        $"Overdue by {overdueBy} days.\n" +
                        $"Fines owed for this item = USD ${totalFinesForItem}",
                        "Confirm Return",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        this.command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();
                        this.command.CommandText = $"UPDATE items SET [previousCheckedOutBy] = '{this.selectedItem.currentlyCheckedOutBy}' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();
                        this.command.CommandText = $"UPDATE items SET [dueDate] = '' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();
                        this.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                            $"WHERE [userID] = '{this.selectedItem.currentlyCheckedOutBy}'";
                        this.command.ExecuteNonQuery();
                        this.c.Close(); // Needs to close before LoadDataGrid on account of reopening in CheckoutDatabaseUpdate

                        LoadDataGrid("SELECT * FROM accounts", true);
                        LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                                "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                                "FROM [items] ORDER BY [ISXX], [copyID]", false);
                    }
                    this.c.Close(); // Close in case if statement is false 
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
            if (this.itemSelected)
            {
                if (MessageBox.Show("Delete Selected Item?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    this.c.Open();
                    this.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                        $"WHERE [userID] = '{this.selectedItem.currentlyCheckedOutBy}'";
                    this.command.ExecuteNonQuery();

                    this.command.CommandText = $"DELETE * FROM items WHERE [itemID] = '{this.selectedItem.itemID}'";
                    this.command.ExecuteNonQuery();
                    this.c.Close();

                    LoadDataGrid("SELECT * FROM accounts", true);
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                            "FROM [items] ORDER BY [ISXX], [copyID]", false);

                    this.itemSelected = false;
                    this.selectedItem = new Item();
                    this.labelSelectedItem.Content = "(Select an Item)";
                    this.labelCheckoutSelectedItemTitle.Content = "(Select an Item)";
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
            if (this.userSelected)
            {
                if (MessageBox.Show("Delete Selected User?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    this.c.Open();
                    this.command.CommandText = "UPDATE items " +
                        "SET [currentlyCheckedOutBy] = '', " +
                        $"[previousCheckedOutBy] = '{this.selectedUser.userID}', " +
                        "[dueDate] = '' " +
                        $"WHERE [currentlyCheckedOutBy] = '{this.selectedUser.userID}'";
                    this.command.ExecuteNonQuery();

                    this.command.CommandText = $"DELETE * FROM accounts WHERE [userID] = '{this.selectedUser.userID}'";
                    this.command.ExecuteNonQuery();
                    this.c.Close();

                    LoadDataGrid("SELECT * FROM accounts", true);
                    LoadDataGrid("SELECT [itemID], [copyID], [ISXX], [deweyDecimal], [format], [genreClassOne], [title], " +
                            "[authorLastName], [authorFirstName], [authorMiddleName], [currentlyCheckedOutBy] " +
                            "FROM [items] ORDER BY [ISXX], [copyID]", false);

                    this.userSelected = false;
                    this.selectedUser = new User();
                    this.labelSelectedUser.Content = "(Select a User)";
                    this.labelCheckoutSelectedUser.Content = "(Select a User)";
                }
            }
            else
            {
                MessageBox.Show("Please double-click a user to select it for deletion.");
            }
        }
        #endregion

        #region CheckBoxes
        private void checkBoxShowItems_Checked(object sender, RoutedEventArgs e) // Can I factor out code? (Look in select user/items)
        {
            if (this.userSelected)
            {
                this.checkBoxShowUser.IsEnabled = false;
                this.comboBoxItemsSearchByOptions.SelectedIndex = 5;
                this.textBoxItemsSearchBy.Text = this.selectedUser.userID;
            }
            else
            {
                MessageBox.Show("Please double click to select a user."); // double check text format - does it follow the rest of the program?
                this.checkBoxShowItems.IsChecked = false;
            }
        }

        private void checkBoxShowItems_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.userSelected)
            {
                this.checkBoxShowUser.IsEnabled = true;
                this.comboBoxItemsSearchByOptions.SelectedIndex = -1;
                this.textBoxItemsSearchBy.Text = "";
            }
        }

        private void checkBoxShowUser_Checked(object sender, RoutedEventArgs e) // Can I factor out code? (Look in select user/items)
        {
            if (this.itemSelected)
            {
                this.checkBoxShowItems.IsEnabled = false;
                this.comboBoxAccountsSearchByOptions.SelectedIndex = 2;
                string selectedItemUserID = this.selectedItem.currentlyCheckedOutBy;
                if (selectedItemUserID.Length > 0)
                {
                    selectedItemUserID = selectedItemUserID.Substring(0, selectedItemUserID.IndexOf('~'));
                    this.textBoxAccountsSearchBy.Text = selectedItemUserID;
                }
            }
            else
            {
                MessageBox.Show("Please double click to select an item."); // double check text format - does it follow the rest of the program?
                this.checkBoxShowUser.IsChecked = false;
            }
        }

        private void checkBoxShowUser_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected)
            {
                this.checkBoxShowItems.IsEnabled = true;
                this.comboBoxAccountsSearchByOptions.SelectedIndex = -1;
                this.textBoxAccountsSearchBy.Text = "";
            }
        }
        #endregion

        #region Menu
        #region Backup
        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            File.Copy("LibraryDatabase.mdb", "LibraryDatabaseBackup.mdb", true);
            MessageBox.Show("Database created in program folder.\nBackup database file named 'LibraryDatabaseBackup.mdb'");
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString() + "\\LibraryDatabaseBackup.mdb"))
            {
                string corruptFilePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString() + "\\LibraryDatabaseCorrupt.mdb";
                if (File.Exists(corruptFilePath))
                {
                    File.Delete(corruptFilePath);
                }
                File.Move("LibraryDatabase.mdb", "LibraryDatabaseCorrupt.mdb");
                File.Move("LibraryDatabaseBackup.mdb", "LibraryDatabase.mdb");
                File.Copy("LibraryDatabase.mdb", "LibraryDatabaseBackup.mdb");
                MessageBox.Show("Database restored from backup.\nCreated new backup, old database file named as 'LibraryDatabaseCorrupt.mdb'.");
            }
            else
            {
                MessageBox.Show("A backup does not exist.\n(Did you rename the file from 'LibraryDatabaseBackup.mdb'?");
            }
        }
        #endregion
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
    public class Item
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
