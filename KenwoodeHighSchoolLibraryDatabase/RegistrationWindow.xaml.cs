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
        public RegistrationWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin";
            c.Open();

            command = new OleDbCommand();
            command.Connection = c;
            //command.CommandText = "CREATE TABLE accounts([firstName] TEXT, [lastName] TEXT, " +
            //    "[userID] TEXT, [userType] TEXT)";
            //command.ExecuteNonQuery();
            c.Close();
        }

        private void buttonRegister_Click(object sender, RoutedEventArgs e)
        {
            c.Open();
            string fName = this.textBoxFirstNameRegister.Text;
            string lName = this.textBoxSurnameRegister.Text;
            string uID = this.textBoxUserIDRegister.Text;
            string uType = comboBoxUserTypeRegister.SelectedValue.ToString().Substring(37);

            command.CommandText = "INSERT INTO accounts ([firstName], [lastName], [userID], [userType]) " +
                "VALUES ('" + fName +"', '" + lName + "', '" + uID + "', '" + uType + "')";
            command.ExecuteNonQuery();
            c.Close();
        }
    }
}
