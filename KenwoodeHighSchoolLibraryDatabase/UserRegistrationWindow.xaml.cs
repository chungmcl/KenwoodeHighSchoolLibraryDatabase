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
using System.Windows.Shapes;
using System.Data.OleDb;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class UserRegistrationWindow : Window
    {
        private OleDbConnection c;
        private OleDbCommand command;
        private OleDbDataReader reader;
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
            InitializeOthers();
            LoadUserIDs();
            toRegister = true;
        }

        User toEditUser;
        private int toEditUserFinePerDay;
        public UserRegistrationWindow(User user)
        {
            InitializeComponent();
            InitializeOthers();
            toRegister = false;
            labelTitle.Content = "Edit Account";
            buttonRegister.Content = "Save Changes";
            this.toEditUser = user;
            c.Open();
            command.CommandText = $"SELECT [finePerDay] FROM accounts WHERE userID = '{user.userID}'";
            reader = command.ExecuteReader();
            reader.Read();
            this.toEditUserFinePerDay = int.Parse(reader[0].ToString());
            c.Close();

            textBoxFirstNameRegister.Text = toEditUser.firstName;
            textBoxSurnameRegister.Text = toEditUser.lastName;
            textBoxUserIDRegister.Text = toEditUser.userID;
            comboBoxUserTypeRegister.SelectedValue = toEditUser.userType;
            textBoxItemLimit.Text = toEditUser.itemLimit;
            textBoxDateLimit.Text = toEditUser.dateLimit;
            textBoxFinePerDay.Text = this.toEditUserFinePerDay.ToString();
        }

        private void InitializeOthers()
        {
            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            command = new OleDbCommand();
            command.Connection = c;
            this.reader = null;

            comboBoxUserTypeRegister.Items.Add("Student");
            comboBoxUserTypeRegister.Items.Add("Teacher");
        }

        private void LoadUserIDs()
        {
            List<string[]> userIDs = new List<string[]>();
            c.Open();
            command.CommandText = "SELECT [userID], [firstName], [lastName] FROM accounts";
            command.CommandType = System.Data.CommandType.Text;
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                // Had to set to string and then add to array because
                // the reader.Read returns a reference type variable
                string[] userIDAndName = new string[3];
                string userID = (string)reader[0];
                string firstName = (string)reader[1];
                string lastName = (string)reader[2];
                userIDAndName[0] = userID;
                userIDAndName[1] = firstName;
                userIDAndName[2] = lastName;
                userIDs.Add(userIDAndName);
            }
            reader.Close();
            c.Close();
            this.userIDs =  userIDs;
        }

        private void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            if (toRegister)
            {
                LoadUserIDs();
                string errorMessage = CheckRequiredValues();
                if (errorMessage == "")
                {
                    c.Open();
                    this.fName = this.textBoxFirstNameRegister.Text.Trim();
                    this.lName = this.textBoxSurnameRegister.Text.Trim();
                    this.uID = this.textBoxUserIDRegister.Text.Trim();
                    this.uType = this.comboBoxUserTypeRegister.SelectedValue.ToString();

                    command.CommandText = "INSERT INTO accounts ([firstName], [lastName], [userID], [userType], [itemLimit], [dateLimit], [finePerDay]) " +
                        $"VALUES ('{this.fName}', '{this.lName}', '{this.uID}', '{this.uType}', {this.itemLimit}, {this.dateLimit}, {this.finePerDay})";
                    command.ExecuteNonQuery();

                    c.Close(); // close first
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show(errorMessage);
                }
            }
            else
            {
                string errorMessage = CheckRequiredValues();
                if (errorMessage == "")
                {
                    if (MessageBox.Show("Save changes?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        c.Open();
                        command.CommandText = $"UPDATE accounts SET " +
                            $"[firstName] = '{this.textBoxFirstNameRegister.Text}', " +
                            $"[lastName] = '{this.textBoxSurnameRegister.Text}', " +
                            $"[userID] = '{this.textBoxUserIDRegister.Text}', " +
                            $"[userType] = '{this.comboBoxUserTypeRegister.SelectedValue}', " +
                            $"[itemLimit] = {this.textBoxItemLimit.Text}, " +
                            $"[dateLimit] = {this.textBoxDateLimit.Text}, " +
                            $"[finePerDay] = {this.textBoxFinePerDay.Text} " +
                            $"WHERE [userID] = '{this.toEditUser.userID}'";
                        command.ExecuteNonQuery();

                        command.CommandText = $"UPDATE items SET " +
                            $"[currentlyCheckedOutBy] = {this.textBoxUserIDRegister.Text} " +
                            $"WHERE [currentlyCheckedOutBy] = '{this.toEditUser.userID}'";
                        command.ExecuteNonQuery();

                        command.CommandText = $"UPDATE items SET " +
                            $"[previousCheckedOutBy] = '{this.textBoxUserIDRegister}' " +
                            $"WHERE [previousCheckedOutBy] = '{this.toEditUser.userID}'";
                        command.ExecuteNonQuery();

                        c.Close();
                    }
                }
                else
                {

                    MessageBox.Show(errorMessage);
                }
            }

        }

        public string CheckRequiredValues()
        {
            if (textBoxFirstNameRegister.Text == "")
            {
                return "A first name is required.";
            }

            if (textBoxSurnameRegister.Text == "")
            {
                return "A surname is required.";
            }

            if (textBoxUserIDRegister.Text == "")
            {
                
                return "A User ID is required. (School/Employee ID)";
            }
            else
            {
                LoadUserIDs();
                int checkUserID = ContainsUserID(textBoxUserIDRegister.Text);
                if (checkUserID != -1)
                {
                    return $"Another student ({userIDs[checkUserID][1]} {userIDs[checkUserID][2]}) already " +
                                $"holds this Student/Teacher ID ({userIDs[checkUserID][0]}). Did you enter the wrong ID?";
                }
            }

            if (comboBoxUserTypeRegister.SelectedIndex == -1)
            {
                return "A usertype must be selected.";
            }

            if (textBoxItemLimit.Text == "")
            {
                return "A book limit is required in integer format.";
            }

            if (textBoxDateLimit.Text == "")
            {
                return "A date limit is required in integer format.\n" +
                    "(Number of days a user can checkout a book.)";
            }

            if (!(int.TryParse(textBoxItemLimit.Text.Trim(), out this.itemLimit)))
            {
                return "User Book Limit must be in integer format."; // need to check for negative integers
            }
            else if (this.itemLimit < 0)
            {
                return "User Book Limit must be a positive integer.";
            }

            if (!(int.TryParse(textBoxDateLimit.Text.Trim(), out this.dateLimit)))
            {
                return "User Date Limit must be in integer format."; // need to check for negative integers
            }
            else if (this.dateLimit < 0)
            {
                return "User Date Limit must be a positive integer.";
            }

            if (!(double.TryParse(textBoxFinePerDay.Text.Trim(), out this.finePerDay)))
            {
                return "User Fine Per (overdue) Day must be a number (integer or irrational)."; // need to check for negative numbers
            }
            else if (this.finePerDay < 0)
            {
                return "User Fine Per (overdue) day must be a positive number (integer or irrational).";
            }

            return "";
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
            for (int i = 0; i <= userIDs.Count - 1; i++)
            {
                if (userIDs[i].Contains(userID))
                {
                    return i;
                }
            }
            return -1;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //this.MainWindow.LoadDataGrid("SELECT * FROM accounts");
        }
    }
}
