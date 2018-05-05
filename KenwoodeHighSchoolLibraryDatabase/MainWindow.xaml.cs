using System;
using System.Collections.Generic;
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
        private User selectedUser;
        private Item selectedItem;
        private bool? userSelected;
        private bool? itemSelected;
        private string currentFolderPath;
        List<User> selectedUsers;
        List<Item> selectedItems;
        private List<Item> itemsToAdd = new List<Item>();
        public MainWindow()
        {
            try // Attempt to load database, if anything fails, it's likely fault of missing required files
            {
                DBConnectionHandler.InitializeConnection(); // Initialize the database connection for the whole program at startup
                InitializeComponent();
                LoadDataGrid();

                this.currentFolderPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
                Directory.CreateDirectory(this.currentFolderPath + "\\Backups");

                // this.userSelected and this.itemSelected use a null state to represent multiple selected items.
                // Set to false to accurately represent data.
                this.userSelected = false;
                this.itemSelected = false;

                this.selectedUsers = new List<User>();
                this.selectedItems = new List<Item>();
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
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = sqlCommand;
            DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            this.dataGridAccounts.Items.Clear();
            while (DBConnectionHandler.reader.Read())
            {
                User newUser = new User
                {
                    FirstName = DBConnectionHandler.reader["firstName"].ToString(),
                    LastName = DBConnectionHandler.reader["lastName"].ToString(),
                    UserID = DBConnectionHandler.reader["userID"].ToString(),
                    UserType = DBConnectionHandler.reader["userType"].ToString(),
                    ItemLimit = DBConnectionHandler.reader["itemLimit"].ToString(),
                    DateLimit = DBConnectionHandler.reader["dateLimit"].ToString(),
                    CheckedOut = DBConnectionHandler.reader["numberOfCheckedoutItems"].ToString(),
                    OverdueItems = DBConnectionHandler.reader["overdueItems"].ToString(),
                    Fines = DBConnectionHandler.reader["fines"].ToString()
                };
                this.dataGridAccounts.Items.Add(newUser);
            }
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.c.Close();
        }

        /// <summary>
        /// Load every called entry from the items table within the database.
        /// </summary>
        /// <param name="sqlCommand">The SQL Query to perform to load datagrid.</param>
        private void LoadItemsDataGrid(string sqlCommand)
        {
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = sqlCommand;
            DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            this.dataGridItems.Items.Clear();
            while (DBConnectionHandler.reader.Read())
            {
                Item newItem = new Item
                {
                    ItemID = DBConnectionHandler.reader["itemID"].ToString(),
                    DeweyDecimal = DBConnectionHandler.reader["deweyDecimal"].ToString(),
                    Format = DBConnectionHandler.reader["format"].ToString(),
                    Genre = DBConnectionHandler.reader["genreClassOne"].ToString(),
                    Title = DBConnectionHandler.reader["title"].ToString()
                };
                string authorName = $"{DBConnectionHandler.reader["authorLastName"].ToString()}, {DBConnectionHandler.reader["authorFirstName"].ToString()} " +
                    $"{DBConnectionHandler.reader["authorMiddleName"].ToString()}";
                if (authorName.Length > 3) // If author name is not empty (3 because of comma and empty space) 
                {
                    newItem.AuthorName = authorName;
                }
                else // else, author name is empty
                {
                    newItem.AuthorName = ""; // Remove comma and empty space
                }
                newItem.CurrentlyCheckedOutBy = DBConnectionHandler.reader["currentlyCheckedOutBy"].ToString();
                this.itemsToAdd.Add(newItem); // Add to list of items to be loaded to datagrid
            }

            for (int i = 0; i < this.itemsToAdd.Count; i++) // For every item to be added to datagrid
            {
                DBConnectionHandler.reader.Close(); // Close reader from previous use
                string currentlyCheckedOutBy = this.itemsToAdd[i].CurrentlyCheckedOutBy;
                DBConnectionHandler.command.CommandText = $"SELECT [firstName], [lastName] FROM [accounts] WHERE [userID] = '{currentlyCheckedOutBy}'";
                DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
                DBConnectionHandler.reader.Read();
                try // add first and last name to currently checked out by column of datagrid - throws error if firstName or lastName is empty
                {
                    string firstName = DBConnectionHandler.reader["firstName"].ToString();
                    string lastName = DBConnectionHandler.reader["lastName"].ToString();
                    this.itemsToAdd[i].CurrentlyCheckedOutBy = currentlyCheckedOutBy + $" ({lastName}, {firstName})";
                }
                catch // try requires catch - otherwise set itself to what it was before (just empty)
                {
                    this.itemsToAdd[i].CurrentlyCheckedOutBy = "";
                }
                this.dataGridItems.Items.Add(this.itemsToAdd[i]);
            }
            this.itemsToAdd.Clear();
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.c.Close();
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
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = "SELECT [userID], [finePerDay] FROM accounts";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            while (DBConnectionHandler.reader.Read()) // Load all the users and their fine rate (fine per day) into a list
            {
                string[] toAdd = new string[] { DBConnectionHandler.reader[0].ToString(), DBConnectionHandler.reader[1].ToString() };
                userIDs.Add(toAdd);
            }
            DBConnectionHandler.reader.Close();
            for (int i = 0; i < userIDs.Count; i++) // For each user, calculate number of overdue and the amount of fines
            {
                int overDue = 0;
                double fines = 0;
                string currentUserID = userIDs[i][0];
                string finePerDay = userIDs[i][1];
                DBConnectionHandler.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{currentUserID}'";
                // Select all items that are currently checked out by this user
                DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
                while (DBConnectionHandler.reader.Read())
                {
                    // Every time an item that is checked out by the user is calculated to be overdue, add one
                    // Also calculate the fines - multiply overdue days by the user's fine rate (fine per day)
                    DateTime dueDate = Convert.ToDateTime(DBConnectionHandler.reader[0].ToString());
                    if (DateTime.Now >= dueDate)
                    {
                        overDue++;
                        fines = fines + (DateTime.Today - dueDate.AddSeconds(1)).TotalDays * double.Parse(finePerDay);
                        // Add one second because books are due at 11:59:59 of the due date, so charge fines day after.
                    }
                }
                DBConnectionHandler.reader.Close();
                DBConnectionHandler.command.CommandText = $"UPDATE accounts SET " +
                    $"[overDueItems] = {overDue}, " +
                    $"[fines] = {fines} " +
                    $"WHERE [userID] = '{currentUserID}'";
                // Save data to secondary storage - database
                DBConnectionHandler.command.ExecuteNonQuery();
            }
            DBConnectionHandler.c.Close();
            DBConnectionHandler.reader.Close();
        }
        #endregion Calculations

        #region Select Item or User
        /// <summary>
        /// Set 'selected user' to the row(s) that the user clicks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridAccounts_SelectionChanged(object sender, EventArgs e)
        {
            this.selectedUsers = new List<User>();
            // Load all the users that the user selects
            for (int i = 0; i < this.dataGridAccounts.SelectedItems.Count; i++)
            {
                this.selectedUsers.Add((User)this.dataGridAccounts.SelectedItems[i]);
            }

            if (this.selectedUsers.Count == 1)
            {
                try // attempt to set selected user clicked to this.selectedUser
                {
                    this.selectedUser = this.selectedUsers[0];
                    string selectedUserInfo = $"({this.selectedUser.UserID}) " +
                        $"{this.selectedUser.FirstName} {this.selectedUser.LastName}";
                    this.labelCheckoutSelectedUser.Content = selectedUserInfo;
                    this.labelSelectedUser.Content = selectedUserInfo;
                    this.userSelected = true;

                    if (this.checkBoxShowItems.IsChecked == true) // had to compare to true; .IsChecked is type bool? (nullable)
                    {
                        // If check box to show items for selected user is checked,
                        // show items checked out to user when user is clicked
                        this.checkBoxShowUser.IsEnabled = false;
                        this.comboBoxItemsSearchByOptions.SelectedIndex = 5;
                        this.textBoxItemsSearchBy.Text = this.selectedUser.UserID;
                    }
                }
                catch // if thing clicked is not a row that represents user, show error message
                {
                    MessageBox.Show("Please click a single row to select a user.");
                    this.checkBoxShowItems.IsChecked = false;
                }
            }
            else if (this.selectedUsers.Count > 1)
            {
                string displayMessage = "[Multiple Users Selected]";
                this.labelCheckoutSelectedUser.Content = displayMessage;
                this.labelSelectedUser.Content = displayMessage;
                this.userSelected = null;
                this.selectedUser = new User();
            }
            else
            {
                string displayMessage = "(Select a User)";
                this.labelCheckoutSelectedUser.Content = displayMessage;
                this.labelSelectedUser.Content = displayMessage;
                this.userSelected = false;
                this.selectedUser = new User();
            }
        }

        /// <summary>
        /// Set the selected item to the row that the item clicked.
        /// Display error message if user clicks something other than an item
        /// in the items DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridItems_SelectionChanged(object sender, EventArgs e)
        {
            this.selectedItems = new List<Item>();
            // Load all the items that the user selects
            for (int i = 0; i < this.dataGridItems.SelectedItems.Count; i++)
            {
                this.selectedItems.Add((Item)this.dataGridItems.SelectedItems[i]);
            }

            if (this.selectedItems.Count == 1)
            {
                try // attempt to set selected user double clicked to this.selectedItem
                {
                    this.selectedItem = this.selectedItems[0];
                    string selectedItemInfo = $"{this.selectedItem.Title} ({this.selectedItem.ItemID})";
                    this.labelCheckoutSelectedItemTitle.Content = selectedItemInfo;
                    this.labelSelectedItem.Content = selectedItemInfo;
                    this.itemSelected = true;

                    if (this.checkBoxShowUser.IsChecked == true)
                    {
                        // If check box to show user currently checked out by for selected item is checked,
                        // show user checked out to when item is double clicked
                        this.checkBoxShowItems.IsEnabled = false;
                        this.comboBoxAccountsSearchByOptions.SelectedIndex = 2;
                        string selectedItemUserID = this.selectedItem.CurrentlyCheckedOutBy;
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
                catch // if thing clicked is not a row that represents user, show error message
                {
                    MessageBox.Show("Please click a single row to select an item.");
                    this.checkBoxShowUser.IsChecked = false;
                }
            }
            else if (this.selectedItems.Count > 1)
            {
                string displayMessage = "[Multiple Items Selected]";
                this.labelCheckoutSelectedItemTitle.Content = displayMessage;
                this.labelSelectedItem.Content = displayMessage;
                this.itemSelected = null;
                this.selectedItem = new Item();
            }
            else
            {
                string displayMessage = "(Select an Item)";
                this.labelCheckoutSelectedItemTitle.Content = displayMessage;
                this.labelSelectedItem.Content = displayMessage;
                this.itemSelected = false;
                this.selectedItem = new Item();
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

        // Other window opening methods exist in #region Menu and #region Editing Objects (Users and Items)
        #region Window Openers
        /// <summary>
        /// Open ItemRegistrationWindow.
        /// If new item is registered, reload items DataGrid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonToItemRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            ItemRegistrationWindow x = new ItemRegistrationWindow
            {
                Owner = this
            };
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
            UserRegistrationWindow w = new UserRegistrationWindow
            {
                Owner = this
            };
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
            // Display an error message with MessageBox if...

            // ...either an item or a user is not selected.
            if (this.userSelected == true && this.itemSelected == true) // had to use == true because this.userSelected and this.itemSelected are bool?
            {
                CheckoutDatabaseUpdate(this.selectedUser, this.selectedItem);
                LoadDataGrid(); // Reload datagrids after item is checked out
            }
            else if (this.userSelected == true && this.itemSelected == null)
            {
                for (int i = 0; i < this.selectedItems.Count; i++)
                {
                    CheckoutDatabaseUpdate(this.selectedUser, this.selectedItems[i]);
                }
                LoadDataGrid(); // Reload datagrids after item is checked out
            }
            else
            {
                MessageBox.Show("Please click a single user and (an) item(s) to select them for checkout.");
            }
        }

        /// <summary>
        /// Update database after checking out item to user.
        /// </summary>
        /// <param name="dueDate">Due date of the item for the user</param>
        private void CheckoutDatabaseUpdate(User user, Item item)
        {
            // ...the selected item is already checked out to the selected user.
            if (item.CurrentlyCheckedOutBy != user.UserID)
            {
                // ...the selected item is already checked out to another user.
                if (item.CurrentlyCheckedOutBy == "")
                {
                    // ...the selected user is at his/her item limit.
                    if (int.Parse(user.CheckedOut) < int.Parse(user.ItemLimit))
                    {
                        DateTime dueDate = (DateTime.Today.AddDays(double.Parse(user.DateLimit)).AddHours(23.9999));
                        // Due at end of day so add 23.9999 hours
                        if (MessageBox.Show(
                            $"Confirm Checkout -\n" +
                            $"Check out item: {this.selectedItem.Title}\n" +
                            $"To user: ({user.UserID}) {user.LastName}, {user.FirstName}\n" +
                            $"For {user.DateLimit} day(s). Due on {dueDate.ToString()}"
                            , "Confirm Checkout", MessageBoxButton.OKCancel) == MessageBoxResult.OK) // If user clicks OK to confirm checkout...
                        {
                            // Update database after checking item out to user.
                            // Take necessary information from selectedItem and selectedUser to put into SQL UPDATE command
                            string userID = user.UserID;
                            string stringDueDate = dueDate.ToString();
                            string itemID = item.ItemID;
                            DBConnectionHandler.c.Open();
                            DBConnectionHandler.command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '{userID}', [dueDate] = '{stringDueDate}' WHERE itemID = '{itemID}'";
                            DBConnectionHandler.command.ExecuteNonQuery();

                            DBConnectionHandler.command.CommandText = $"SELECT [numberOfCheckedoutItems] FROM accounts WHERE [userID] = '{userID}'";
                            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
                            DBConnectionHandler.reader.Read();
                            int checkedOut = ((int)(DBConnectionHandler.reader[0])) + 1;
                            DBConnectionHandler.reader.Close();

                            DBConnectionHandler.command.CommandText = $"UPDATE accounts SET [numberOfCheckedoutItems] = {checkedOut} WHERE userID = '{userID}'";
                            DBConnectionHandler.command.ExecuteNonQuery();
                            DBConnectionHandler.c.Close();
                        }
                    }
                    else
                    {
                        MessageBox.Show("This user has reached his/her item limit!");
                    }
                }
                else
                {
                    MessageBox.Show($"This item is already checked out to user {item.CurrentlyCheckedOutBy}!");
                }
            }
            else
            {
                MessageBox.Show("This book is already checked out to this user!");
            }
        }
        #endregion

        #region Editing Objects (Users and Items)
        private void ButtonEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected == true) // if an item is selected (this.itemSelected is of type 'bool?')
            {
                ItemRegistrationWindow w = new ItemRegistrationWindow(this.selectedItem)
                {
                    Owner = this
                };
                bool? receive = w.ShowDialog(); // Load datagrids if specified by window when edit/view window is closed
                if (receive == true) // nullable bool - needs to be compared directly
                {
                    LoadDataGrid();
                    Item check = (Item)this.dataGridItems.Items[0];
                    this.selectedItem = (Item)this.dataGridItems.Items[0];
                    this.labelCheckoutSelectedItemTitle.Content = this.selectedItem.Title;
                }
            }
            else // else, notify user that an item has not been selected
            {
                MessageBox.Show("Please click an item to select it for editing.");
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
            if (this.userSelected == true) // if the a user is selected (this.userSelected is of type 'bool?')
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
                MessageBox.Show("Please click a user to select it for editing.");
            }
        }

        /// <summary>
        /// Return the selected item to the library.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonReturnSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected == true) // (this.itemSelected is 'bool?')
            {
                // get the ID AND name of the user that selectedItem is currently checked to
                string userCheckedOutTo = this.selectedItem.CurrentlyCheckedOutBy;
                if (userCheckedOutTo != "")
                {
                    UpdateDatabaseForReturningItem(userCheckedOutTo, this.selectedItem);
                    LoadDataGrid();
                }
                else
                {
                    MessageBox.Show("This item is not checked out to any user."); // Display error message if the selected item is checked out to no one 
                }

            }
            else if (this.itemSelected == null)
            {
                for (int i = 0; i < this.selectedItems.Count; i++)
                {
                    // get the ID AND name of the user that selectedItem is currently checked to
                    string userCheckedOutTo = this.selectedItems[i].CurrentlyCheckedOutBy;
                    if (userCheckedOutTo != "")
                    {
                        UpdateDatabaseForReturningItem(userCheckedOutTo, this.selectedItems[i]);
                    }
                    else
                    {
                        MessageBox.Show($"This book [{this.selectedItems[i].Title} - ({this.selectedItems[i].ItemID})]" +
                            $"is not checked out to any user.");
                    }
                }
                LoadDataGrid();
            }
            else
            {
                MessageBox.Show("Please click an item to select for returning."); // Display error message an item is not selected.
            }
        }

        private void UpdateDatabaseForReturningItem(string userCheckedOutTo, Item selectedItem)
        {
            // get ONLY the ID of the user that selectedItem is currently checked out to
            // (ID comes before a space and before the name) 
            string userCheckedOutToID = userCheckedOutTo.Substring(0, userCheckedOutTo.IndexOf(' '));

            DBConnectionHandler.c.Open();

            DBConnectionHandler.command.CommandText = $"SELECT [dueDate] FROM items WHERE [currentlyCheckedOutBy] = '{userCheckedOutToID}'";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            DBConnectionHandler.reader.Read();
            DateTime dueDate = Convert.ToDateTime(DBConnectionHandler.reader[0].ToString());
            double overdueBy = (DateTime.Today - dueDate.AddSeconds(1)).TotalDays;
            // Add one second because book is due at 11:59:59 - count overdue days starting the next day
            if (overdueBy < 0)
            {
                overdueBy = 0;
            }
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE [userID] = '{userCheckedOutToID}'";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            DBConnectionHandler.reader.Read();
            double totalFinesForItem = ((double)DBConnectionHandler.reader[0]) * overdueBy;
            DBConnectionHandler.reader.Close();

            if (MessageBox.Show($"Confirm Return of '{selectedItem.Title}' - \n" +
                $"Lent to {userCheckedOutTo}\n" +
                $"Overdue by {overdueBy} days.\n" +
                $"Fines owed for this item = USD ${totalFinesForItem}",
                "Confirm Return",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user clicks OK to confirm return of item to library from user
            {
                // Set the item's log of user currently checked out by to empty to indicate checked out by no one
                DBConnectionHandler.command.CommandText = $"UPDATE items SET [currentlyCheckedOutBy] = '' WHERE [itemID] = '{selectedItem.ItemID}'";
                DBConnectionHandler.command.ExecuteNonQuery();

                // Delete the item's log of currentlyCheckedOutBy and set previousCheckedOutBy to the user who the item was checked out to.
                DBConnectionHandler.command.CommandText = $"UPDATE items SET [previousCheckedOutBy] = '{userCheckedOutToID}' WHERE [itemID] = '{selectedItem.ItemID}'";
                DBConnectionHandler.command.ExecuteNonQuery();

                // Remove the dueDate from the item.
                DBConnectionHandler.command.CommandText = $"UPDATE items SET [dueDate] = '' WHERE [itemID] = '{selectedItem.ItemID}'";
                DBConnectionHandler.command.ExecuteNonQuery();

                // Subtract the user's number of checked out items by one.
                DBConnectionHandler.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                    $"WHERE [userID] = '{userCheckedOutToID}'";

                DBConnectionHandler.command.ExecuteNonQuery();
                DBConnectionHandler.c.Close(); // Needs to close before LoadDataGrid on account of reopening in CheckoutDatabaseUpdate
            }
            DBConnectionHandler.c.Close(); // Close in case if statement is false
        }

        #endregion

        #region Deletion
        /// <summary>
        /// Delete the selected Item from the database and datagrid.
        /// Display error message if an item is not selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDeleteSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected == true) // (this.itemSelected is of type 'bool?')
            {
                if (MessageBox.Show("Delete Selected Item?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user confirms deletion
                {
                    UpdateDatabaseForItemDeletion(this.selectedItem.ItemID, this.selectedItem.CurrentlyCheckedOutBy);

                    LoadDataGrid();
                }
            }
            else if (this.itemSelected == null)
            {
                if (MessageBox.Show("Delete Selected Items?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user confirms deletions
                {
                    int selectedItemsCountBeforeDeletion = this.selectedItems.Count;
                    for (int i = 0; i < selectedItemsCountBeforeDeletion; i++)
                    {
                        UpdateDatabaseForItemDeletion(this.selectedItems[0].ItemID, this.selectedItems[0].CurrentlyCheckedOutBy);
                        this.selectedItems.RemoveAt(0); // Slide the next selectedItem into the 0 index
                    }
                    LoadDataGrid();
                }
            }
            else // else, user has not selected any items - display error message
            {
                MessageBox.Show("Please click to select a single item to select it for deletion, " +
                    "or CTRL + Click to select multiple items for deletion.");
            }
        }

        private void UpdateDatabaseForItemDeletion(string itemID, string currentlyCheckedOutBy)
        {
            DBConnectionHandler.c.Open();

            // Lower the user's number of checked out items by one (if item is checked out to a user)
            // (User's number of overdue items and fines will be recalculated when dataGrid is loaded.)
            DBConnectionHandler.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                $"WHERE [userID] = '{currentlyCheckedOutBy}'";
            DBConnectionHandler.command.ExecuteNonQuery();

            // Delete the selected item from the database file
            DBConnectionHandler.command.CommandText = $"DELETE * FROM items WHERE [itemID] = '{itemID}'";
            DBConnectionHandler.command.ExecuteNonQuery();
            DBConnectionHandler.c.Close();
        }

        /// <summary>
        /// Delete the selected user from the database and datagrid.
        /// Display error message if user is not selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonDeleteSelectedUser_Click(object sender, RoutedEventArgs e)
        {
            /// Clear checkedOutBy, dueDate, and set previousCheckedOutBy to the user being deleted.
            /// Set the currently selected user to an empty user.
            if (this.userSelected == true) // (this.userSelected is of type 'bool?')
            {
                if (MessageBox.Show("Delete Selected User?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user confirms deletion
                {
                    UpdateDatabaseForUserDeletion(this.selectedUser.UserID);
                    LoadDataGrid();
                }
            }
            else if (this.userSelected == null)
            {
                if (MessageBox.Show("Delete Selected Users?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes) // If user confirms deletions
                {
                    int selectedUsersCountBeforeDeletion = this.selectedUsers.Count;
                    for (int i = 0; i < selectedUsersCountBeforeDeletion; i++)
                    {
                        UpdateDatabaseForUserDeletion(this.selectedUsers[0].UserID);
                        this.selectedUsers.RemoveAt(0); // Slide the next selectedUser into the 0 index
                    }
                    LoadDataGrid();
                }
            }
            else // else, user has not selected a user - display error message
            {
                MessageBox.Show("Please click to select a user to select it for deletion, " +
                    "or CTRL + Click to select multiple users for deletion.");
            }
        }

        private void UpdateDatabaseForUserDeletion(string userID)
        {
            DBConnectionHandler.c.Open();

            // Clear currentlyCheckedOutBy and set previouslyCheckedOutBy to user deleted
            // Set dueDate of items checked out by the user being deleted to be empty
            DBConnectionHandler.command.CommandText = "UPDATE items " +
                "SET [currentlyCheckedOutBy] = '', " +
                $"[previousCheckedOutBy] = '{userID}', " +
                "[dueDate] = '' " +
                $"WHERE [currentlyCheckedOutBy] = '{userID}'"; // All items checked out by the user being deleted
            DBConnectionHandler.command.ExecuteNonQuery();

            // Delete all values from accounts table in database of the user being deleted
            DBConnectionHandler.command.CommandText = $"DELETE * FROM accounts WHERE [userID] = '{userID}'";
            DBConnectionHandler.command.ExecuteNonQuery();
            DBConnectionHandler.c.Close();
        }
        #endregion

        #region CheckBoxes
        /// <summary>
        /// If the checkBoxShowItems (of selected user) is checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBoxShowItems_Checked(object sender, RoutedEventArgs e)
        {
            if (this.userSelected == true) // (this.userSelected is of type 'bool?')
            {
                // Disable checkBoxShowUser - can't use both at the same time
                this.checkBoxShowUser.IsEnabled = false;
                this.comboBoxItemsSearchByOptions.SelectedIndex = 5; // 5 is index of search by userID
                this.textBoxItemsSearchBy.Text = this.selectedUser.UserID;
            }
            else
            {
                MessageBox.Show("Please click a single row to select a user.");
                this.checkBoxShowItems.IsChecked = false;
            }
        }

        private void CheckBoxShowItems_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.userSelected == true) // (this.userSelected is of type 'bool?')
            {
                this.checkBoxShowUser.IsEnabled = true;
                this.comboBoxItemsSearchByOptions.SelectedIndex = -1; //-1 is index for null
                this.textBoxItemsSearchBy.Text = "";
            }
        }

        private void CheckBoxShowUser_Checked(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected == true) // (this.itemSelected is of type 'bool?')
            {
                // Disable checkBoxShowItems - can't use both at the same time
                this.checkBoxShowItems.IsEnabled = false;
                this.comboBoxAccountsSearchByOptions.SelectedIndex = 2; // 2 is index of search by userID (currentlyCheckedOutBy)
                string selectedItemUserID = this.selectedItem.CurrentlyCheckedOutBy;
                if (selectedItemUserID.Length > 0) // If the item is checked out to someone
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
                MessageBox.Show("Please click a single row to select an item.");
                this.checkBoxShowUser.IsChecked = false;
            }
        }

        private void CheckBoxShowUser_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.itemSelected == true) // (this.itemSelected is of type 'bool?')
            {
                this.checkBoxShowItems.IsEnabled = true;
                this.comboBoxAccountsSearchByOptions.SelectedIndex = -1; // -1 is index of null
                this.textBoxAccountsSearchBy.Text = "";
            }
        }
        #endregion

        #region Menu

        #region Backup
        /// <summary>
        /// Backup the current database file into the "Backup" folder (if it already exists; if not, create folder)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            string dateTime = DateTime.Now.ToString();
            dateTime = dateTime.Replace("/", "_"); // Set markers in DateTime to be underscores, '/' and ':' are not permitted in file names
            dateTime = dateTime.Replace(":", "_");
            string backupFileName = "Backup-" + dateTime; // Name the backup file after the current date and time

            File.Copy("LibraryDatabase.mdb", backupFileName + ".mdb", true);
            
            string parentFolderPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
            File.Move(backupFileName + ".mdb", this.currentFolderPath + "\\Backups\\" + backupFileName + ".mdb");

            MessageBox.Show($"Backup database file created in:\n\n{this.currentFolderPath}\\Backups.\n\nBackup database file named '{backupFileName}.mdb'");
        }

        /// <summary>
        /// Open file explorer to select file to restore the database from.
        /// Restore from the selected file and put old database into the 'Corrupt' folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer to select file to restore from
            System.Windows.Forms.OpenFileDialog browserDialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select Backup File to Restore From"
            };
            if (Directory.Exists(this.currentFolderPath + "\\Backups")) // Start select file window in backups folder if it exists
            {
                browserDialog.InitialDirectory = this.currentFolderPath + "\\Backups";
            }
            bool selectedDBFunctions = false;
            while (!selectedDBFunctions) // While the user has not selected a functional database (user can exit loop and return to pre-restore database if needed)
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
                            // Rename and move current database to 'corrupt' folder'
                            File.Move(this.currentFolderPath + "\\LibraryDatabase.mdb", this.currentFolderPath + "\\Corrupt\\" + corruptFileName + ".mdb"); 
                            File.Copy(selectedFilePath, this.currentFolderPath + "\\LibraryDatabase.mdb");

                            LoadDataGrid();

                            MessageBox.Show($"Restored database file from selected file:\n'{selectedFileName}'");

                            selectedDBFunctions = true;
                        }
                        catch (Exception exception)
                        {
                            if (DBConnectionHandler.c.State == System.Data.ConnectionState.Open) // In case exception is thrown outside of SQL query
                            {
                                DBConnectionHandler.c.Close();
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

        #region Help Window
        /// <summary>
        /// Opens the Help Window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow w = new HelpWindow();
            w.Show();
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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserID { get; set; }
        public string UserType { get; set; }
        public string ItemLimit { get; set; }
        public string DateLimit { get; set; }
        public string CheckedOut { get; set; }
        public string OverdueItems { get; set; }
        public string Fines { get; set; }
    }

    /// <summary>
    /// Item to be displayed within the items dataGrid.
    /// Can be used to load information about the item in the database.
    /// </summary>
    public class Item
    {
        public string ItemID { get; set; }
        public string DeweyDecimal { get; set; }
        public string Title { get; set; }
        public string AuthorName { get; set; }
        public string Genre  { get; set; }
        public string Format { get; set; }
        public string CurrentlyCheckedOutBy { get; set; }
    }
}
