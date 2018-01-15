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

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnToRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow w = new RegistrationWindow();
            w.Show();
        }

        private void TstBtnDeleteFromAccounts_Click(object sender, RoutedEventArgs e)
        {
            OleDbConnection c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin";
            c.Open();

            OleDbCommand command = new OleDbCommand();
            command.Connection = c;

            command.CommandText = "DELETE * FROM accounts";
            command.ExecuteNonQuery();
            c.Close();
        }
    }
}
