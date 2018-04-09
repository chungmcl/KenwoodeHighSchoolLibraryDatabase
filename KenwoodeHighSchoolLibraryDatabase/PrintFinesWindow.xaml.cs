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
    /// Interaction logic for PrintFinesWindow.xaml
    /// </summary>
    public partial class PrintFinesWindow : Window
    {
        private OleDbConnection c;
        private OleDbCommand command;
        private OleDbDataReader reader;
        List<AccountWithFine> accountsWithFines;
        private int pageNumber;
        private int pageMax;
        public PrintFinesWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            accountsWithFines = new List<AccountWithFine>();
            pageNumber = 1;
            LoadAccountsWithFines();
            LoadDataGrid(1);
            buttonPreviousPage.IsEnabled = false;
            buttonNextPage.IsEnabled = false;
            labelPageNumber.Content = pageNumber;
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

        /// <summary>
        /// Load all the accounts with fines from the database to use in the data grids.
        /// Set the page max so the next page button will not go into infinity.
        /// </summary>
        private void LoadAccountsWithFines()
        {
            c.Open();
            command.CommandText = "SELECT " +
                "[userID], [firstName], [lastName], [userType], [overdueItems], [fines] " +
                "FROM accounts " +
                "WHERE [fines] > 0";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                AccountWithFine awf = new AccountWithFine();
                awf.userID = reader[0].ToString();
                awf.name = $"{reader[1].ToString()}, {reader[2].ToString()}";
                awf.userType = reader[3].ToString();
                awf.overdue = (int)reader[4];
                awf.fines = (double)reader[5];
                accountsWithFines.Add(awf);
            }
            this.pageMax = (int)Math.Ceiling(((double)accountsWithFines.Count) / 37);

            if (this.pageMax > 1)
            {
                buttonNextPage.IsEnabled = true;
            }
        }

        /// <summary>
        /// Load the data grid with 37 of the users with fines
        /// (so it fits on one standard 8 and (1/2) by 11 printer paper)
        /// </summary>
        /// <param name="pageNumber">The page to load.</param>
        private void LoadDataGrid(int pageNumber)
        {
            dataGridFinedUsers.Items.Clear();
            if (accountsWithFines.Count > 0)
            {
                int startIndex = 0;
                if (pageNumber != 1)
                {
                    startIndex = ((pageNumber * 37) - 37);
                }
                for (int i = startIndex; i < accountsWithFines.Count && i < (pageNumber * 37); i++)
                {
                    dataGridFinedUsers.Items.Add(accountsWithFines[i]);
                }
            }
        }

        /// <summary>
        /// Go to next page and reload Data Grid to correct page.
        /// Disables itself at end of page range. (Min and max page number)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonNextPage_Click(object sender, RoutedEventArgs e)
        {
            buttonPreviousPage.IsEnabled = true;
            pageNumber++;
            LoadDataGrid(pageNumber);
            if (pageNumber >= pageMax)
            {
                buttonNextPage.IsEnabled = false;
            }
            labelPageNumber.Content = pageNumber;
        }

        /// <summary>
        /// Return to previous page and reload Data Grid to correct page.
        /// Disables itself at end of page range. (Min and max page number)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            buttonNextPage.IsEnabled = true;
            pageNumber--;
            LoadDataGrid(pageNumber);
            if (pageNumber == 1)
            {
                buttonPreviousPage.IsEnabled = false;
            }
            labelPageNumber.Content = pageNumber;
        }

        /// <summary>
        /// Open the print dialog for printing the current page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrintThisPage_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintVisual(dataGridFinedUsers, "Fined Users");
            printDlg.ShowDialog();
        }

        /// <summary>
        /// The acount with fine to be displayed in the Data Grid.
        /// </summary>
        public class AccountWithFine
        {
            public double fines { get; set; }
            public int overdue { get; set; }
            public string userID { get; set; }
            public string name { get; set; }
            public string userType { get; set; }
        }
    }
}
