using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class UserRegistrationWindow : Window
    {
        private List<string[]> userIDs;
        private string fName;
        private string lName;
        private string uID;
        private string uType;
        private int itemLimit;
        private int dateLimit;
        private double finePerDay;
        bool toRegister;
        public UserRegistrationWindow()
        {
            InitializeComponent();
            this.comboBoxUserTypeRegister.Items.Add("Student");
            this.comboBoxUserTypeRegister.Items.Add("Teacher");
            LoadUserIDs();
            this.toRegister = true;
        }

        User toEditUser;
        private double toEditUserFinePerDay;
        /// <summary>
        /// Overload constructor for editing a user.
        /// </summary>
        /// <param name="user">User to edit.</param>
        public UserRegistrationWindow(User user) // User as defined in MainWindow (struct)
        {
            InitializeComponent();
            this.toRegister = false;
            this.labelTitle.Content = "Edit Account";
            this.buttonRegister.Content = "Save Changes";
            this.toEditUser = user;

            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE userID = '{user.UserID}'";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            DBConnectionHandler.reader.Read();
            this.toEditUserFinePerDay = double.Parse(DBConnectionHandler.reader[0].ToString());
            DBConnectionHandler.c.Close();

            this.comboBoxUserTypeRegister.Items.Add("Student");
            this.comboBoxUserTypeRegister.Items.Add("Teacher");

            this.comboBoxUserTypeRegister.Items.Add("Student");
            this.comboBoxUserTypeRegister.Items.Add("Teacher");

            this.textBoxFirstNameRegister.Text = this.toEditUser.FirstName;
            this.textBoxSurnameRegister.Text = this.toEditUser.LastName;
            this.textBoxUserIDRegister.Text = this.toEditUser.UserID;
            this.comboBoxUserTypeRegister.SelectedValue = this.toEditUser.UserType;
            this.textBoxItemLimit.Text = this.toEditUser.ItemLimit;
            this.textBoxDateLimit.Text = this.toEditUser.DateLimit;
            this.textBoxFinePerDay.Text = this.toEditUserFinePerDay.ToString();
        }

        /// <summary>
        /// Load the current list of userIDs so that the program can check
        /// if the user is choosing to register an already registered ID.
        /// (All UserIDs must be unique)
        /// </summary>
        private void LoadUserIDs()
        {
            List<string[]> userIDs = new List<string[]>();
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = "SELECT [userID], [firstName], [lastName] FROM accounts";
            DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            while (DBConnectionHandler.reader.Read())
            {
                // Had to set to string and then add to array because
                // the reader.Read returns a reference type variable
                string[] userIDAndName = new string[3];
                string userID = (string)DBConnectionHandler.reader[0];
                string firstName = (string)DBConnectionHandler.reader[1];
                string lastName = (string)DBConnectionHandler.reader[2];
                userIDAndName[0] = userID;
                userIDAndName[1] = firstName;
                userIDAndName[2] = lastName;
                userIDs.Add(userIDAndName);
            }
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.c.Close();
            this.userIDs =  userIDs;
        }

        /// <summary>
        /// Checks if paramater "userID" is in the "userIDs" list and returns location of "userID"
        /// if "userID" is found in the "userIDs" list
        /// (MS Access does not offer creation of UNIQUE columns)
        /// </summary>
        /// <param name="userID">The userID to check for</param>
        /// <returns>Returns the location in "userIDs" list where the searched for userID is
        /// Returns -1 if searched for userID is not found</returns>
        private int ContainsUserID(string userID)
        {
            for (int i = 0; i <= this.userIDs.Count - 1; i++)
            {
                if (this.userIDs[i].Contains(userID))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Register or edit item depending on constructor initialization.
        /// Takes in all fields and saves to database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRegister_Click(object sender, RoutedEventArgs e)
        {
            if (this.toRegister) // Register new user
            {
                LoadUserIDs();
                string errorMessage = CheckRequiredValues();
                if (errorMessage == "")
                {
                    DBConnectionHandler.c.Open();
                    this.fName = this.textBoxFirstNameRegister.Text.Trim();
                    this.lName = this.textBoxSurnameRegister.Text.Trim();
                    this.uID = this.textBoxUserIDRegister.Text.Trim();
                    this.uType = this.comboBoxUserTypeRegister.SelectedValue.ToString();

                    DBConnectionHandler.command.CommandText = "INSERT INTO accounts ([firstName], [lastName], [userID], [userType], [itemLimit], [dateLimit], [finePerDay]) " +
                        $"VALUES ('{this.fName}', '{this.lName}', '{this.uID}', '{this.uType}', {this.itemLimit}, {this.dateLimit}, {this.finePerDay})";
                    DBConnectionHandler.command.ExecuteNonQuery();

                    DBConnectionHandler.c.Close(); // close first
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show(errorMessage);
                }
            }
            else // Edit existing user
            {
                string errorMessage = CheckRequiredValues();
                if (errorMessage == "")
                {
                    if (MessageBox.Show("Save changes?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        DBConnectionHandler.c.Open();
                        DBConnectionHandler.command.CommandText = $"UPDATE accounts SET " +
                            $"[firstName] = '{this.textBoxFirstNameRegister.Text}', " +
                            $"[lastName] = '{this.textBoxSurnameRegister.Text}', " +
                            $"[userID] = '{this.textBoxUserIDRegister.Text}', " +
                            $"[userType] = '{this.comboBoxUserTypeRegister.SelectedValue}', " +
                            $"[itemLimit] = {this.textBoxItemLimit.Text}, " +
                            $"[dateLimit] = {this.textBoxDateLimit.Text}, " +
                            $"[finePerDay] = {this.finePerDay} " +
                            $"WHERE [userID] = '{this.toEditUser.UserID}'";
                        DBConnectionHandler.command.ExecuteNonQuery();

                        DBConnectionHandler.command.CommandText = $"UPDATE items SET " +
                            $"[currentlyCheckedOutBy] = '{this.textBoxUserIDRegister.Text}' " +
                            $"WHERE [currentlyCheckedOutBy] = '{this.toEditUser.UserID}'";
                        DBConnectionHandler.command.ExecuteNonQuery();

                        DBConnectionHandler.command.CommandText = $"UPDATE items SET " +
                            $"[previousCheckedOutBy] = '{this.textBoxUserIDRegister}' " +
                            $"WHERE [previousCheckedOutBy] = '{this.toEditUser.UserID}'";
                        DBConnectionHandler.command.ExecuteNonQuery();

                        DBConnectionHandler.c.Close();
                        this.DialogResult = true;
                    }
                }
                else
                {

                    MessageBox.Show(errorMessage);
                }
            }

        }

        /// <summary>
        /// Check that all required fields are filled out
        /// correctly in the correct format.
        /// Return error message if a field is incorrect, return empty string if all are correct
        /// </summary>
        /// <returns>The error message if a field is incorrect, empty string if all are correct</returns>
        public string CheckRequiredValues()
        {
            if (this.textBoxFirstNameRegister.Text == "")
            {
                return "A first name is required.";
            }

            if (this.textBoxSurnameRegister.Text == "")
            {
                return "A surname is required.";
            }

            if (this.textBoxUserIDRegister.Text == "")
            {

                return "A User ID is required. (School/Employee ID)";
            }
            else
            {
                LoadUserIDs();
                int checkUserID = ContainsUserID(this.textBoxUserIDRegister.Text);
                if (checkUserID != -1)
                {
                    if (this.userIDs[checkUserID][0] != this.toEditUser.UserID) // User may be editing
                    {
                        return $"Another student ({this.userIDs[checkUserID][1]} {this.userIDs[checkUserID][2]}) already " +
                                    $"holds this Student/Teacher ID ({this.userIDs[checkUserID][0]}). Did you enter the wrong ID?";
                    }
                }
            }

            if (this.comboBoxUserTypeRegister.SelectedIndex == -1)
            {
                return "A usertype must be selected.";
            }

            if (this.textBoxItemLimit.Text == "")
            {
                return "An item limit is required in integer format.";
            }

            if (this.textBoxDateLimit.Text == "")
            {
                return "A date limit is required in integer format.\n" +
                    "(Number of days a user can checkout an item.)";
            }

            if (!(int.TryParse(this.textBoxItemLimit.Text.Trim(), out this.itemLimit)))
            {
                return "User Item Limit must be in integer format.";
            }
            else if (this.itemLimit < 0)
            {
                return "User Item Limit must be a positive integer.";
            }

            if (!(int.TryParse(this.textBoxDateLimit.Text.Trim(), out this.dateLimit)))
            {
                return "User Date Limit must be in integer format.";
            }
            else if (this.dateLimit < 0)
            {
                return "User Date Limit must be a positive integer.";
            }

            if (!(double.TryParse(this.textBoxFinePerDay.Text.Trim(), out this.finePerDay)))
            {
                return "User Fine Per (overdue) Day must be a number.";
            }
            else if (this.finePerDay < 0)
            {
                return "User Fine Per (overdue) day must be a positive number (integer or irrational).";
            }

            return "";
        }
    }
}
