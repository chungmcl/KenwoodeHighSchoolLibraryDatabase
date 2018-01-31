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

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OleDbConnection c;
        public MainWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin";

            //OleDbDataAdapter adapter = new OleDbDataAdapter();
            //DataTable dTable = new DataTable();
            //adapter.Fill(dTable);
            //this.dataGridAccounts.DataSource = dTable;
        }

        private void BtnToRegistrationWindow_Click(object sender, RoutedEventArgs e)
        {
            RegistrationWindow w = new RegistrationWindow();
            w.Show();

            dataGridAccounts.Items.Add("hehexD");

        }

        private void TstBtnDeleteFromAccounts_Click(object sender, RoutedEventArgs e)
        {
            

            OleDbCommand command = new OleDbCommand();
            command.Connection = c;

            command.CommandText = "DELETE * FROM accounts";
            command.ExecuteNonQuery();
            c.Close();
        }
    }
}
