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
        OleDbConnection c;
        OleDbCommand command;
        OleDbDataReader reader;
        List<string[]> userIDs;
        string fName;
        string lName;
        string uID;
        string uType;
        int bookLimit;
        int dateLimit;
        public UserRegistrationWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            command = new OleDbCommand();
            command.Connection = c;
            reader = null;
            userIDs = LoadUserIDs();
        }

        private List<string[]> LoadUserIDs()
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
            return userIDs;
        }

        private void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            LoadUserIDs();
            string errorMessage = CheckRequiredValues();
            if (errorMessage == "")
            {
                c.Open();
                this.fName = this.textBoxFirstNameRegister.Text.Trim();
                this.lName = this.textBoxSurnameRegister.Text.Trim();
                this.uID = this.textBoxUserIDRegister.Text.Trim();
                this.uType = this.comboBoxUserTypeRegister.SelectedValue.ToString().Substring(37);
                // book limit and date limit already set in CheckRequiredValues through int.TryParse (out)

                // Save the location of the userID in the "userIDs" list
                // so that we can display information about the account holding this userID
                // if ContainsUserID could not find an account holding this userID, it would have returned -1
                int checkUserID = ContainsUserID(uID);
                if (checkUserID == -1)
                {
                    command.CommandText = "INSERT INTO accounts ([firstName], [lastName], [userID], [userType], [bookLimit], [dateLimit]) " +
                    $"VALUES ('{fName}', '{lName}', '{uID}', '{uType}', {bookLimit}, {dateLimit})";
                    command.ExecuteNonQuery();
                }
                else
                {
                    MessageBox.Show($"Another student ({userIDs[checkUserID][1]} {userIDs[checkUserID][2]}) already " +
                        $"holds this Student/Teacher ID ({userIDs[checkUserID][0]}). Did you enter the wrong ID?");
                }
                c.Close(); // close first
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show(errorMessage);
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
            if (comboBoxUserTypeRegister.SelectedIndex == -1)
            {
                return "A usertype must be selected.";
            }
            if (textBoxBookLimit.Text == "")
            {
                return "A book limit is required in integer format.";
            }
            if (textBoxDateLimit.Text == "")
            {
                return "A date limit is required in integer format.\n" +
                    "(Number of days a user can checkout a book.)";
            }
            if (!(int.TryParse(textBoxBookLimit.Text.Trim(), out this.bookLimit)))
            {
                return "User Book Limit must be in integer format."; // need to check for negative integers
            }
            if (!(int.TryParse(textBoxDateLimit.Text.Trim(), out this.dateLimit)))
            {
                return "User Date Limit must be in integer format."; // need to check for negative integers
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
