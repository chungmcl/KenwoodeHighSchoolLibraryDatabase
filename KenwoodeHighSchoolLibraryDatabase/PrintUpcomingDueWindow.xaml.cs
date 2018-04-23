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
                    item.dueDate = stringDueDate.Substring(0, stringDueDate.IndexOf(' '));
                    item.daysUntilDueDate = ((dueDate.Date - DateTime.Today)).TotalDays;
                    item.deweyDecimal = this.reader["deweyDecimal"].ToString();
                    item.isbnTen = this.reader["ISBN10"].ToString();
                    item.isxx = this.reader["ISXX"].ToString();
                    item.genre = $"{this.reader["genreClassOne"].ToString()}, " +
                        $"{this.reader["genreClassTwo"].ToString()}, " +
                        $"{this.reader["genreClassThree"].ToString()}";
                    item.edition = this.reader["edition"].ToString();
                    item.author = $"{this.reader["authorLastName"]}, " +
                        $"{this.reader["authorMiddleName"]} " +
                        $"{this.reader["authorFirstName"]}";
                    item.format = this.reader["format"].ToString();
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

        #region CheckBoxEventHandlers
        private void CheckBoxUserLentTo_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[1].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxUserLentTo_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[1].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxTitle_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[2].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxTitle_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[2].Visibility = System.Windows.Visibility.Hidden;
        }
        private void CheckBoxAuthor_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[3].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxAuthor_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[3].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxDeweyDecimal_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[4].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDeweyDecimal_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[4].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxISBNTen_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[5].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxISBNTen_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[5].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxISXX_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[6].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxISXX_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[6].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxGenre_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[7].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxGenre_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[7].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxEdition_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[8].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxEdition_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[8].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxFormat_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[9].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxFormat_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[9].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxDueDate_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[10].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDueDate_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[10].Visibility = System.Windows.Visibility.Hidden;
        }
        private void CheckBoxDaysUntilDueDate_Checked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[11].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDaysUntilDueDate_Unchecked(object sender, RoutedEventArgs e)
        {
            dataGridIssuedBooks.Columns[11].Visibility = System.Windows.Visibility.Hidden;
        }
        #endregion

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
            public string deweyDecimal { get; set; }
            public string isbnTen { get; set; }
            public string isxx { get; set; }
            public string genre { get; set; }
            public string publisher { get; set; }
            public string edition { get; set; }
            public string author { get; set; }
            public string format { get; set; }
        }
    }
}
