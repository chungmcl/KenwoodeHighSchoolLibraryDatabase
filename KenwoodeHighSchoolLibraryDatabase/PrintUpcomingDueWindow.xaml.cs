using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
            this.itemsDueThisWeek = new List<ItemDueThisWeek>();
            this.pageNumber = 1;
            this.pageMax = 1;
            this.buttonPreviousPage.IsEnabled = false;
            this.buttonNextPage.IsEnabled = false;
            LoadItemsToDisplay();
            LoadDataGrid(this.pageNumber);
            this.labelPageNumber.Content = this.pageNumber;
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
        /// Load all the items due this week from the database to use in the data grids.
        /// Set the page max so the next page button will not go into infinity.
        /// </summary>
        private void LoadItemsToDisplay()
        {
            this.c.Open();
            DateTime aWeekFromToday = DateTime.Today.AddDays(7).AddHours(23.99999);
            this.command.CommandText = $"SELECT * FROM items";
            this.reader = this.command.ExecuteReader();
            while (this.reader.Read())
            {
                string stringDueDate = this.reader["dueDate"].ToString();
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
                    item.itemID = this.reader["itemID"].ToString();
                    item.lentTo = this.reader["currentlyCheckedOutBy"].ToString();
                    item.title = this.reader["title"].ToString();
                    item.dueDate = stringDueDate.Substring(0, 8);
                    item.daysUntilDueDate = ((dueDate.Date - DateTime.Today)).TotalDays;
                    this.itemsDueThisWeek.Add(item);
                }
            }

            for (int i = 0; i < this.itemsDueThisWeek.Count; i++)
            {
                this.reader.Close();
                string userID = this.itemsDueThisWeek[i].lentTo;
                this.command.CommandText = "SELECT [firstName], [lastName] FROM accounts " +
                    $"WHERE [userID] = '{userID}'";
                this.reader = this.command.ExecuteReader();
                this.reader.Read();
                string name;
                try
                {

                    name = $" ({this.reader[1].ToString()}, {this.reader[0].ToString()})";
                }
                catch
                {
                    name = "";
                }
                this.itemsDueThisWeek[i].lentTo = userID + name;
            }
            this.reader.Close();

            this.pageMax = (int)Math.Ceiling(((double)this.itemsDueThisWeek.Count) / 37);

            if (this.pageMax > 1)
            {
                this.buttonNextPage.IsEnabled = true;
            }
        }

        /// <summary>
        /// Load the data grid with 37 of the items due this week
        /// (so it fits on one standard 8 and (1/2) by 11 printer paper)
        /// </summary>
        /// <param name="pageNumber">The page to load.</param>
        private void LoadDataGrid(int pageNumber)
        {
            this.dataGridIssuedBooks.Items.Clear();
            if (this.itemsDueThisWeek.Count > 0)
            {
                int startIndex = 0;
                if (pageNumber != 1)
                {
                    startIndex = ((pageNumber * 37) - 37);
                }
                for (int i = startIndex; i < this.itemsDueThisWeek.Count && i < (pageNumber * 37); i++)
                {
                    this.dataGridIssuedBooks.Items.Add(this.itemsDueThisWeek[i]);
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
            this.buttonPreviousPage.IsEnabled = true;
            this.pageNumber++;
            LoadDataGrid(this.pageNumber);
            if (this.pageNumber >= this.pageMax)
            {
                this.buttonNextPage.IsEnabled = false;
            }
            this.labelPageNumber.Content = this.pageNumber;
        }

        /// <summary>
        /// Return to previous page and reload Data Grid to correct page.
        /// Disables itself at end of page range. (Min and max page number)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPreviousPage_Click(object sender, RoutedEventArgs e)
        {
            this.buttonNextPage.IsEnabled = true;
            this.pageNumber--;
            LoadDataGrid(this.pageNumber);
            if (this.pageNumber == 1)
            {
                this.buttonPreviousPage.IsEnabled = false;
            }
            this.labelPageNumber.Content = this.pageNumber;
        }

        /// <summary>
        /// Open the print dialog for printing the current page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonPrintThisPage_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintVisual(this.dataGridIssuedBooks, "Upcoming Due Dates");
            printDlg.ShowDialog();
        }

        /// <summary>
        /// An item that is due this week.
        /// Used to load into the Data Grid.
        /// </summary>
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
