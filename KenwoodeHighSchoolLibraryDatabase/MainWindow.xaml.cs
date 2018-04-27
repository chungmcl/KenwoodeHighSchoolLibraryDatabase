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
        private OleDbConnection c;
        private OleDbDataReader reader;
        private OleDbCommand command;
        private User selectedUser;
        private Item selectedItem;
        private bool userSelected;
        private bool itemSelected;
        private string currentFolderPath;
        private List<Item> itemsToAdd = new List<Item>();
        public MainWindow()
        {
            //
            //
            try // Attempt to load database, if anything fails, it's likely fault of missing required files
            {
                InitializeDatabaseConnection();
                InitializeComponent();
                LoadDataGrid();

                this.currentFolderPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
                Directory.CreateDirectory(this.currentFolderPath + "\\Backups");
            }
            catch // Display error message and close program
            {
                MessageBox.Show("ERROR: Database could not be loaded." +
                    "\nPlease ensure the following files are in the same folder as this program and named exactly the same:" +
                    "\nLibraryDatabase.mdb" +
                    "\n\nKenwoodeHighSchoolLibraryDatabase.pdb" +
                    "\nKenwoodeHighSchoolLibraryDatabase.exe.config");
                System.Windows.Application.Current.Shutdown();
            }
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
        /// Reload both datagrids with default queries. (Specfic queries can be used by calling each method individually)
        /// </summary>
        private void LoadDataGrid()
        {
            // Calculate overdue items and fines everytime full datagrids are loaded - ensures dynamic loading of data
            CalculateOverdueAndFines();
            LoadAccountsDataGrid("SELECT * FROM accounts");
            LoadItemsDataGrid("SELECT * FROM items ORDER BY [authorLastName], [ISXX], [copyID]");
    }

        /// <summary>
        /// Load every called entry from the accounts table within the database.
        /// </summary>
        /// <param name="sqlCommand">The SQL Query to perform to load datagrid.</param>
        private void LoadAccountsDataGrid(string sqlCommand)
        {
            this.c.Open();
            this.command.CommandText = sqlCommand;
            this.command.CommandType = System.Data.CommandType.Text;
            this.reader = this.command.ExecuteReader();
            this.dataGridAccounts.Items.Clear();
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
            this.reader.Close();
            this.c.Close();
        }

        /// <summary>
        /// Load every called entry from the items table within the database.
        /// </summary>
        /// <param name="sqlCommand">The SQL Query to perform to load datagrid.</param>
        private void LoadItemsDataGrid(string sqlCommand)
        {
            this.c.Open();
            this.command.CommandText = sqlCommand;
            this.command.CommandType = System.Data.CommandType.Text;
            this.reader = this.command.ExecuteReader();
            this.dataGridItems.Items.Clear();
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
                if (authorName.Length > 3) // If author name is not empty (3 because of comma and empty space) 
                {
                    newItem.authorName = authorName;
                }
                else // else, author name is empty
                {
                    newItem.authorName = ""; // Remove comma and empty space
                }
                newItem.currentlyCheckedOutBy = reader["currentlyCheckedOutBy"].ToString();
                this.itemsToAdd.Add(newItem); // Add to list of items to be loaded to datagrid
            }

            for (int i = 0; i < this.itemsToAdd.Count; i++) // For every item to be added to datagrid
            {
                reader.Close(); // Close reader from previous use
                string currentlyCheckedOutBy = this.itemsToAdd[i].currentlyCheckedOutBy;
                this.command.CommandText = $"SELECT [firstName], [lastName] FROM [accounts] WHERE [userID] = '{currentlyCheckedOutBy}'";
                reader = this.command.ExecuteReader();
                reader.Read();
                try // add first and last name to currently checked out by column of datagrid - throws error if firstName or lastName is empty
                {
                    string firstName = reader["firstName"].ToString();
                    string lastName = reader["lastName"].ToString();
                    this.itemsToAdd[i].currentlyCheckedOutBy = currentlyCheckedOutBy + $" ({lastName}, {firstName})";
                }
                catch // try requires catch - otherwise set itself to what it was before (just empty)
                {
                    this.itemsToAdd[i].currentlyCheckedOutBy = "";
                }
                this.dataGridItems.Items.Add(this.itemsToAdd[i]);
            }
            this.itemsToAdd.Clear();
            this.reader.Close();
            this.c.Close();
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
            while (this.reader.Read()) // Load all the users and their fine rate (fine per day) into a list
            {
                string[] toAdd = new string[] { this.reader[0].ToString(), this.reader[1].ToString() };
                userIDs.Add(toAdd);
            }
            this.reader.Close();
            for (int i = 0; i < userIDs.Count; i++) // For each user, calculate number of overdue and the amount of fines
            {
                int overDue = 0;
                double fines = 0;
                string currentUserID = userIDs[i][0];
                string finePerDay = userIDs[i][1];
                this.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{currentUserID}'";
                // Select all items that are currently checked out by this user
                this.reader = this.command.ExecuteReader();
                while (this.reader.Read())
                {
                    // Every time an item that is checked out by the user is calculated to be overdue, add one
                    // Also calculate the fines - multiply overdue days by the user's fine rate (fine per day)
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
                // Save data to secondary storage - database
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
        private void DataGridAccounts_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try // attempt to set selected user double clicked to this.selectedUser
            {
                this.selectedUser = (User)this.dataGridAccounts.SelectedItem;
                string selectedUserInfo = $"({this.selectedUser.userID}) " +
                    $"{this.selectedUser.firstName} {this.selectedUser.lastName}";
                this.labelCheckoutSelectedUser.Content = selectedUserInfo;
                this.labelSelectedUser.Content = selectedUserInfo;
                this.userSelected = true;

                if (this.checkBoxShowItems.IsChecked == true) // had to compare to true; .IsChecked is type bool? (nullable)
                {
                    // If check box to show items for selected user is checked,
                    // show items checked out to user when user is double clicked
                    this.checkBoxShowUser.IsEnabled = false;
                    this.comboBoxItemsSearchByOptions.SelectedIndex = 5;
                    this.textBoxItemsSearchBy.Text = this.selectedUser.userID;
                }
            }
            catch // if thing double-clicked is not a row that represents user, show error message
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
        private void DataGridItems_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            try // attempt to set selected user double clicked to this.selectedItem
            {
                this.selectedItem = (Item)this.dataGridItems.SelectedItem;
                string selectedItemInfo = $"{this.selectedItem.title} ({this.selectedItem.itemID})";
                this.labelCheckoutSelectedItemTitle.Content = selectedItemInfo;
                this.labelSelectedItem.Content = selectedItemInfo;
                this.itemSelected = true;

                if (this.checkBoxShowUser.IsChecked == true)
                {
                    // If check box to show user currently checked out by for selected item is checked,
                    // show user checked out to when item is double clicked
                    this.checkBoxShowItems.IsEnabled = false;
                    this.comboBoxAccountsSearchByOptions.SelectedIndex = 2;
                    string selectedItemUserID = this.selectedItem.currentlyCheckedOutBy;
                    if (selectedItemUserID.Length > 0) // if the item is checked out to someone (selectedItemUserID will be empty if not checked out)
                    {
                        selectedItemUserID = selectedItemUserID.Substring(0, selectedItemUserID.IndexOf(' ')); // remove name from selectedItemUserID
                        this.textBoxAccountsSearchBy.Text = selectedItemUserID;
                    }
                    else // If this.selectedItem.currentlyCheckedOutBy is empty, the toolbar will not be changed - need to set to empty space
                    {
                        this.textBoxAccountsSearchBy.Text = " ";
                    }
                }
            }
            catch // if thing double-clicked is not a row that represents user, show error message
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
        private void ComboBoxAccountsSearchByOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxAccountsSearchByOptions.SelectedIndex != -1) // if combo box to set filter is not empty (.SelectedIndex !=-1)
            {
                string setTextBoxTo = this.comboBoxAccountsSearchByOptions.SelectedValue.ToString().Substring(37); // Substring because returns with ListBox tag
                this.textBoxAccountsSearchBy.Text = $"Enter a(n) {setTextBoxTo}...";
                LoadAccountsDataGrid("SELECT * FROM accounts");
            }
        }

        /// <summary>
        /// Save the colum that the user would like to query from.
        /// Display hint text to show the purpose of the textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxItemsSearchByOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxItemsSearchByOptions.SelectedIndex != -1) // If comboBox value is selected
            {
                string setTextBoxTo = this.comboBoxItemsSearchByOptions.SelectedValue.ToString().Substring(37); // Substring because returns with ListBox tag
                if (setTextBoxTo == "Lent To") // different case due to difference in use of English grammar for this specific case
                {
                    this.textBoxItemsSearchBy.Text = $"Enter who {setTextBoxTo}...";
                }
                else
                {
                    this.textBoxItemsSearchBy.Text = $"Enter a(n) {setTextBoxTo}...";
                }
                LoadItemsDataGrid("SELECT * FROM items ORDER BY[authorLastName], [ISXX], [copyID]");
            }
        }

        /// <summary>
        /// Query all items with similar text in the textBox in the column that user
        /// specified with the comboBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxAccountsSearchBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentText = this.textBoxAccountsSearchBy.Text;
            if (currentText == "") // If user sets textbox to be empty, then load all values to both datagrids
            {
                LoadAccountsDataGrid("SELECT * FROM accounts");
            }
            else // else, load datagrid according to user specfied query
            {
                int searchType = this.comboBoxAccountsSearchByOptions.SelectedIndex;
                string queryColumn = "";
                switch (searchType) // set query column according to the comboBox item the user selects
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

                if (queryColumn != "") // If a comboBox value is selected (filter)
                {
                    LoadAccountsDataGrid($"SELECT * FROM accounts WHERE [{queryColumn}] LIKE '%{currentText}%'");
                    // Load account datagrid with values that are similar or are same to the values the user specifies
                }
            }
        }

        /// <summary>
        /// Query all items with similar text in the textBox in the column that user
        /// specified with the comboBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxItemsSearchBy_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentText = this.textBoxItemsSearchBy.Text;
            if (currentText == "")
            {
                LoadItemsDataGrid("SELECT * FROM items ORDER BY[authorLastName], [ISXX], [copyID]");
            }
            else // set query column according to the comboBox item the user selects
            {
                int searchType = this.comboBoxItemsSearchByOptions.SelectedIndex;
                string queryColumn = "";
                switch (searchType) // set query column according to the comboBox item the user selects
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

                if (queryColumn != "") // If a comboBox value is selected (filter)
                {
                    LoadItemsDataGrid($"SELECT * FROM [items] WHERE [{queryColumn}] LIKE '%{currentText}%'");
                    // Load account datagrid with values that are similar or are same to the values the user specifies
                }
            }
        }

        /// <summary>
        /// Delete hint text when user enters query textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxAccountsSearchBy_GotFocus(object sender, RoutedEventArgs e)
        {
            this.textBoxAccountsSearchBy.Text = "";
        }

        /// <summary>
        /// Delete hint text when user enters query textBox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBoxItemsSearchBy_GotFocus(object sender, RoutedEventArgs e)
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
        private void ButtonToItemRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            ItemRegistrationWindow x = new ItemRegistrationWindow();
            x.Owner = this;
            bool? receive = x.ShowDialog(); // bool returned when ItemRegistrationWindow is closed
            if (receive == true)
            {
                LoadDataGrid();
            }
        }

        /// <summary>
        /// Open UserRegistrationWindow.
        /// If new user is registered, reload accounts DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonToUserRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            UserRegistrationWindow w = new UserRegistrationWindow();
            w.Owner = this;
            bool? receive = w.ShowDialog(); // bool returned when ItemRegistrationWindow is closed
            if (receive == true)
            {
                LoadDataGrid();
            }
        }

        /// <summary>
        /// Open the PrintUpcomingDueWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrintUpcomingItems_Click(object sender, RoutedEventArgs e)
        {
            PrintUpcomingDueWindow w = new PrintUpcomingDueWindow();
            w.Show();
        }

        /// <summary>
        /// Open the PrintFinesWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonPrintFinedUsers_Click(object sender, RoutedEventArgs e)
        {
            PrintFinesWindow w = new PrintFinesWindow();
            w.Show();
        }
        #endregion

        #region Checkout Item to Selected User
        /// <summary>
        /// Checkout selected book to select user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnToCheckout_Click(object sender, RoutedEventArgs e)
        {
            /// Display an error message with MessageBox if:
            /// Either an item or a user is not selected.
            /// The selected item is already checked out to the selected user.
            /// The selected item is already checked out to another user.
            /// The selected user is at his/her item limit.
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
                                , "Confirm Checkout", MessageBoxButton.OKCancel) == MessageBoxResult.OK) // If user clicks OK to confirm checkout
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
        /// Update database after checking out item to user.
        /// </summary>
        /// <param name="dueDate">Due date of the item for the user</param>
        private void CheckoutDatabaseUpdate(DateTime dueDate)
        {
            // Take necessary information from selectedItem and selectedUser to put into SQL UPDATE command
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

            LoadDataGrid(); // Reload datagrids after item is checked out
        }
        #endregion

        #region Editing Objects (Users and Items)
        private void ButtonEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected) // if an item is selected
            {
                ItemRegistrationWindow w = new ItemRegistrationWindow(this.selectedItem);
                w.Owner = this;
                bool? receive = w.ShowDialog(); // Load datagrids if specified by window when edit/view window is closed
                if (receive == true) // nullable bool - needs to be compared directly
                {
                    LoadDataGrid();
                    Item check = (Item)this.dataGridItems.Items[0];
                    this.selectedItem = (Item)this.dataGridItems.Items[0];
                    this.labelCheckoutSelectedItemTitle.Content = this.selectedItem.title;
                }
            }
            else // else, notify user that an item has not been selected
            {
                MessageBox.Show("Please double-click an item to select it for editing.");
            }
        }

        /// <summary>
        /// Open the edit window and pass the selectedItem to be edited,
        /// or for the item to be viewed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnToUserEditWindow_Click(object sender, RoutedEventArgs e)
        {
            if (this.userSelected) // if the a user is selected
            {
                // Registration window has an overload constructor - if passed an item,
                // it becomes an edit/view page
                UserRegistrationWindow w = new UserRegistrationWindow(this.selectedUser);
                bool? receive = w.ShowDialog(); // Load datagrids if specified by window when edit/view window is closed
                if (receive == true) // nullable bool - needs to be compared directly
                {
                    LoadDataGrid();
                }
            }
            else // else, notify user that an item has not been selected
            {
                MessageBox.Show("Please double-click a user to select it for editing.");
            }
        }

        /// <summary>
        /// Return the selected item to the library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonReturnSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected)
            {
                // get the ID AND name of the user that selectedItem is currently checked to
                string userCheckedOutTo = this.selectedItem.currentlyCheckedOutBy;
                if (userCheckedOutTo != "")
                {
                    // get ONLY the ID of the user that selectedItem is currently checked out to
                    // (ID comes before a space and before the name) 
                    string userCheckedOutToID = userCheckedOutTo.Substring(0, userCheckedOutTo.IndexOf(' '));

                    this.c.Open();
                    // 
                    this.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{userCheckedOutToID}'";
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
                    this.command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE [userID] = '{userCheckedOutToID}'";
                    this.reader = this.command.ExecuteReader();
                    this.reader.Read();
                    double totalFinesForItem = ((double)this.reader[0]) * overdueBy;
                    this.reader.Close();

                    if (MessageBox.Show($"Confirm Return of '{this.selectedItem.title}' - \n" +
                        $"Lent to {this.selectedItem.currentlyCheckedOutBy}\n" +
                        $"Overdue by {overdueBy} days.\n" +
                        $"Fines owed for this item = USD ${totalFinesForItem}",
                        "Confirm Return",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user clicks OK to confirm return of item to library from user
                    {
                        // Set the item's log of user currently checked out by to empty to indicate checked out by no one
                        this.command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();

                        // Delete the item's log of currentlyCheckedOutBy and set previousCheckedOutBy to the user who the item was checked out to.
                        this.command.CommandText = $"UPDATE items SET [previousCheckedOutBy] = '{userCheckedOutToID}' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();

                        // Remove the dueDate from the item.
                        this.command.CommandText = $"UPDATE items SET [dueDate] = '' WHERE [itemID] = '{this.selectedItem.itemID}'";
                        this.command.ExecuteNonQuery();

                        // Subtract the user's number of checked out items by one.
                        this.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                            $"WHERE [userID] = '{userCheckedOutToID}'";

                        this.command.ExecuteNonQuery();
                        this.c.Close(); // Needs to close before LoadDataGrid on account of reopening in CheckoutDatabaseUpdate

                        LoadDataGrid(); // (Fines and number of overdue will be recalculated for users when the DataGrid is reloaded).
                    }
                    this.c.Close(); // Close in case if statement is false 
                }
                else
                {
                    MessageBox.Show("This item is not checked out to any user.");// Display error message if the selected item is checked out to no one 
                }
            }
            else
            {
                MessageBox.Show("Please double-click an item to select for returning.");// Display error message an item is not selected.
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
        private void ButtonDeleteSelectedItem_Click(object sender, RoutedEventArgs e)
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

                    LoadDataGrid();

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

                    LoadDataGrid();

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
        private void CheckBoxShowItems_Checked(object sender, RoutedEventArgs e)
        {
            if (this.userSelected)
            {
                this.checkBoxShowUser.IsEnabled = false;
                this.comboBoxItemsSearchByOptions.SelectedIndex = 5;
                this.textBoxItemsSearchBy.Text = this.selectedUser.userID;
            }
            else
            {
                MessageBox.Show("Please double-click to select a user.");
                this.checkBoxShowItems.IsChecked = false;
            }
        }

        private void CheckBoxShowItems_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.userSelected)
            {
                this.checkBoxShowUser.IsEnabled = true;
                this.comboBoxItemsSearchByOptions.SelectedIndex = -1;
                this.textBoxItemsSearchBy.Text = "";
            }
        }

        private void CheckBoxShowUser_Checked(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected)
            {
                this.checkBoxShowItems.IsEnabled = false;
                this.comboBoxAccountsSearchByOptions.SelectedIndex = 2;
                string selectedItemUserID = this.selectedItem.currentlyCheckedOutBy;
                if (selectedItemUserID.Length > 0)
                {
                    selectedItemUserID = selectedItemUserID.Substring(0, selectedItemUserID.IndexOf(' '));
                    this.textBoxAccountsSearchBy.Text = selectedItemUserID;
                }
                else // If this.selectedItem.currentlyCheckedOutBy is empty, the toolbar will not be changed - need to set to empty space
                {
                    this.textBoxAccountsSearchBy.Text = " ";
                }
            }
            else
            {
                MessageBox.Show("Please double-click to select an item.");
                this.checkBoxShowUser.IsChecked = false;
            }
        }

        private void CheckBoxShowUser_Unchecked(object sender, RoutedEventArgs e)
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
            string dateTime = DateTime.Now.ToString();
            dateTime = dateTime.Replace("/", "_"); // Set markers in DateTime to be underscores, '/' and ':' are not permitted in file names
            dateTime = dateTime.Replace(":", "_");
            string backupFileName = "Backup-" + dateTime;

            File.Copy("LibraryDatabase.mdb", backupFileName + ".mdb", true);
            
            string parentFolderPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
            File.Move(backupFileName + ".mdb", this.currentFolderPath + "\\Backups\\" + backupFileName + ".mdb");

            MessageBox.Show($"Backup database file created in:\n\n{this.currentFolderPath}\\Backups.\n\nBackup database file named '{backupFileName}.mdb'");
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            
            
            System.Windows.Forms.OpenFileDialog browserDialog = new System.Windows.Forms.OpenFileDialog();
            browserDialog.Title = "Select Backup File to Restore From";
            if (Directory.Exists(this.currentFolderPath + "\\Backups")) // Start select file window in backups folder if it exists
            {
                browserDialog.InitialDirectory = this.currentFolderPath + "\\Backups";
            }
            bool selectedDBFunctions = false;
            while (!selectedDBFunctions) // While the user has not selected a functional database (user can break out of loop and return to pre-restore database if needed)
            {
                string corruptDatabasePath = ""; // The path of the current database the user wishes to switch out of
                if (browserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedFilePath = browserDialog.FileName;
                    string selectedFileName = browserDialog.SafeFileName;
                    if (selectedFileName.Substring(selectedFileName.IndexOf('.')) == ".mdb")
                    {
                        try
                        {
                            Directory.CreateDirectory(this.currentFolderPath + "\\Corrupt"); // Create a folder for 'throw away' databases if one does not exist
                            string dateTime = DateTime.Now.ToString();
                            dateTime = dateTime.Replace("/", "_"); // Set markers in DateTime to be underscores, '/' and ':' are not permitted in file names
                            dateTime = dateTime.Replace(":", "_");
                            string corruptFileName = "CorruptDB-" + dateTime; // Rename database to 'CorruptDB-{Current date and time}'

                            corruptDatabasePath = this.currentFolderPath + "\\Corrupt\\" + corruptFileName + ".mdb";
                            File.Move(this.currentFolderPath + "\\LibraryDatabase.mdb", this.currentFolderPath + "\\Corrupt\\" + corruptFileName + ".mdb"); // Renamte and move current database to 'corrupt' folder'
                            File.Copy(selectedFilePath, this.currentFolderPath + "\\LibraryDatabase.mdb");

                            LoadDataGrid();

                            MessageBox.Show($"Restored database file from selected file:\n'{selectedFileName}'");

                            selectedDBFunctions = true;
                        }
                        catch (Exception exception)
                        {
                            if (this.c.State == System.Data.ConnectionState.Open) // In case exception is thrown outside of SQL query
                            {
                                this.c.Close();
                            }
                            MessageBox.Show("Database could not be restored with the selected file." +
                                "\nPlease select a new file and try again." +
                                $"\n\nERROR MESSAGE: \"{exception.Message}\"");

                            File.Delete(this.currentFolderPath + "\\LibraryDatabase.mdb");
                            File.Move(corruptDatabasePath, this.currentFolderPath + "\\LibraryDatabase.mdb");

                            selectedDBFunctions = false;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select a .mdb database file for restoration.");
                    }
                }
                else // Otherwise, if Backups folder does not exist
                {
                    try // In case moving LibraryDatabase.mdb to 'Corrupt' folder fails
                    {
                        if (corruptDatabasePath != "")
                        {
                            File.Delete(this.currentFolderPath + "\\LibraryDatabase.mdb");
                            File.Move(corruptDatabasePath, this.currentFolderPath + "\\LibraryDatabase.mdb");
                        }
                        MessageBox.Show("Restore aborted. Running on previous save of database file.");
                    }
                    catch
                    {
                        MessageBox.Show("Restore aborted. Previous database cannot be found." +
                            "\nProgram is incapable of functioning." +
                            "\nPlease move functional database file into the folder that contains this program, and rename it" +
                            "to 'LibraryDatabase.mdb'." +
                            $"\nProgram folder is located at {this.currentFolderPath}");
                    }

                    selectedDBFunctions = true;
                }
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
