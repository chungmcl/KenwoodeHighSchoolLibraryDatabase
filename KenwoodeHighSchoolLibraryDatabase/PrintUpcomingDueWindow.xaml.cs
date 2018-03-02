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
    /// Interaction logic for PrintOutputReports.xaml
    /// </summary>
    public partial class PrintUpcomingDueWindow : Window
    {
        private OleDbConnection c;
        private OleDbCommand command;
        private OleDbDataReader reader;
        List<ItemDueThisWeek> itemsDueThisWeek;
        int pageNumber;
        int pageMax;
        public PrintUpcomingDueWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            itemsDueThisWeek = new List<ItemDueThisWeek>();
            pageNumber = 1;
            pageMax = 1;
            buttonPreviousPage.IsEnabled = false;
            LoadItemsToDisplay();
            LoadDataGrid(pageNumber);
            labelPageNumber.Content = pageNumber;
        }

        private void InitializeDatabaseConnection()
        {
            this.c = new OleDbConnection();
            this.c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            this.command = new OleDbCommand();
            this.command.Connection = this.c;
            this.reader = null;
        }

        private void LoadItemsToDisplay()
        {
            c.Open();
            DateTime aWeekFromToday = DateTime.Today.AddDays(7).AddHours(23.99999);
            command.CommandText = $"SELECT * FROM items";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                string stringDueDate = reader["dueDate"].ToString();
                DateTime dueDate;
                try
                {

                    dueDate = Convert.ToDateTime(stringDueDate);
                }
                catch
                {
                    dueDate = DateTime.Now.AddDays(8);
                }
                if (dueDate <= aWeekFromToday
                    && dueDate >= DateTime.Now)
                {
                    ItemDueThisWeek item = new ItemDueThisWeek();
                    item.itemID = reader["itemID"].ToString();
                    item.lentTo = reader["currentlyCheckedOutBy"].ToString();
                    item.title = reader["title"].ToString();
                    item.dueDate = stringDueDate.Substring(0, 8);
                    item.daysUntilDueDate = ((dueDate.Date - DateTime.Today)).TotalDays;
                    itemsDueThisWeek.Add(item);
                }
            }

            for (int i = 0; i < itemsDueThisWeek.Count; i++)
            {
                reader.Close();
                string userID = itemsDueThisWeek[i].lentTo;
                command.CommandText = "SELECT [firstName], [lastName] FROM accounts " +
                    $"WHERE [userID] = '{userID}'";
                reader = command.ExecuteReader();
                reader.Read();
                string name;
                try
                {

                    name = $" ({reader[1].ToString()}, {reader[0].ToString()})";
                }
                catch
                {
                    name = "";
                }
                this.itemsDueThisWeek[i].lentTo = userID + name;
            }
            reader.Close();

            this.pageMax = (int)Math.Ceiling(((double)itemsDueThisWeek.Count) / 37); ;
        }

        private void LoadDataGrid(int pageNumber)
        {
            dataGridIssuedBooks.Items.Clear();
            if (itemsDueThisWeek.Count > 0)
            {
                int startIndex = 0;
                if (pageNumber != 1)
                {
                    startIndex = ((pageNumber * 37) - 37);
                }
                for (int i = startIndex; i < itemsDueThisWeek.Count && i < (pageNumber * 37); i++)
                {
                    dataGridIssuedBooks.Items.Add(itemsDueThisWeek[i]);
                }
            }
        }

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
        
        private void buttonPrintThisPage_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintVisual(dataGridIssuedBooks, "Upcoming Due Dates");
            printDlg.ShowDialog();
        }

        public class ItemDueThisWeek
        {
            public string itemID { get; set; }
            public string lentTo { get; set; }
            public string title { get; set; }
            public string dueDate { get; set; }
            public double daysUntilDueDate { get; set; }

        }
    }
}
