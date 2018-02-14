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
    public partial class RegistrationWindow : Window
    {
        OleDbConnection c;
        OleDbCommand command;
        OleDbDataReader reader;
        List<string[]> userIDs;
        public RegistrationWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin";
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
            c.Open();
            string fName = this.textBoxFirstNameRegister.Text;
            string lName = this.textBoxSurnameRegister.Text;
            string uID = this.textBoxUserIDRegister.Text;
            string uType = this.comboBoxUserTypeRegister.SelectedValue.ToString().Substring(37);
            int bookLimit = Int32.Parse(textBoxBookLimit.Text);
            int dateLimit = Int32.Parse(textBoxDateLimit.Text);

            // Save the location of the userID in the "userIDs" list
            // so that we can display information about the account holding this userID
            // if ContainsUserID could not find an account holding this userID, it would have returned -1
            int checkUserID = ContainsUserID(uID);
            if (checkUserID == -1)
            {
                command.CommandText = "INSERT INTO accounts ([firstName], [lastName], [userID], [userType], [bookLimit], [dateLimit]) " +
                $"VALUES ('{fName}', '{lName}', '{uID} ', '{uType}', {bookLimit}, {dateLimit})";
                command.ExecuteNonQuery();
                labelDisplayMessage.Content = $"Successfully Registered Student {fName} {lName}";
            }
            else
            {
                MessageBox.Show($"Another student ({userIDs[checkUserID][1]} {userIDs[checkUserID][2]}) already " +
                    $"holds this Student/Teacher ID ({userIDs[checkUserID][0]}). Did you enter the wrong ID?");
            }
            
            c.Close();
            
            this.textBoxFirstNameRegister.Clear();
            this.textBoxSurnameRegister.Clear();
            this.textBoxUserIDRegister.Clear();
            this.textBoxBookLimit.Clear();
            this.textBoxDateLimit.Clear();
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
            
        }
    }
}
