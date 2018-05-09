using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for PrintOutputReports.xaml
    /// </summary>
    public partial class PrintUpcomingDueWindow : Window
    {
        private List<ItemDueThisWeek> itemsDueThisWeek;
        private int pageNumber;
        private int pageMax;
        private const int itemsPerPage = 37;
        private const double twentyThreeHoursFiftyNineMins = 23.99999;
        public PrintUpcomingDueWindow()
        {
            InitializeComponent();
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
        /// Load all the items due this week from the database to use in the data grids.
        /// Set the page max so the next page button will not go into infinity.
        /// </summary>
        private void LoadItemsToDisplay()
        {
            DBConnectionHandler.c.Open();
            // Add 23.99999 hours as items are due at 11:59 PM of the due date
            DateTime aWeekFromToday = DateTime.Today.AddDays(7).AddHours(twentyThreeHoursFiftyNineMins);
            DBConnectionHandler.command.CommandText = $"SELECT * FROM items";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            while (DBConnectionHandler.reader.Read()) // Loop through all items in the database
            {
                string stringDueDate = DBConnectionHandler.reader["dueDate"].ToString();
                try 
                {
                    // Try to get duedate of item
                    // error thrown if current item being read has no due date (not checked out)
                    DateTime dueDate = Convert.ToDateTime(stringDueDate);

                    // If item has a due date, check if it is due this upcoming week
                    // If it is, add it to the list of items due this upcoming week 
                    if (dueDate <= aWeekFromToday
                    && dueDate >= DateTime.Now)
                    {
                        ItemDueThisWeek item = new ItemDueThisWeek
                        {
                            ItemID = DBConnectionHandler.reader["itemID"].ToString(),
                            LentTo = DBConnectionHandler.reader["currentlyCheckedOutBy"].ToString(),
                            Title = DBConnectionHandler.reader["title"].ToString(),
                            DueDate = stringDueDate.Substring(0, stringDueDate.IndexOf(' ')),
                            DaysUntilDueDate = ((dueDate.Date - DateTime.Today)).TotalDays,
                            DeweyDecimal = DBConnectionHandler.reader["deweyDecimal"].ToString(),
                            ISBNTen = DBConnectionHandler.reader["ISBN10"].ToString(),
                            ISXX = DBConnectionHandler.reader["ISXX"].ToString(),
                            Genre = $"{DBConnectionHandler.reader["genreClassOne"].ToString()}, " +
                            $"{DBConnectionHandler.reader["genreClassTwo"].ToString()}, " +
                            $"{DBConnectionHandler.reader["genreClassThree"].ToString()}",
                            Edition = DBConnectionHandler.reader["edition"].ToString(),
                            Author = $"{DBConnectionHandler.reader["authorLastName"]}, " +
                            $"{DBConnectionHandler.reader["authorMiddleName"]} " +
                            $"{DBConnectionHandler.reader["authorFirstName"]}",
                            Format = DBConnectionHandler.reader["format"].ToString()
                        };
                        this.itemsDueThisWeek.Add(item);
                    }
                }
                catch
                {
                    // Don't do anything if the item isn't due this upcoming week
                }
            }

            for (int i = 0; i < this.itemsDueThisWeek.Count; i++)
            {
                DBConnectionHandler.reader.Close();
                string userID = this.itemsDueThisWeek[i].LentTo;
                DBConnectionHandler.command.CommandText = "SELECT [firstName], [lastName] FROM accounts " +
                    $"WHERE [userID] = '{userID}'";
                DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
                DBConnectionHandler.reader.Read();
                string name;
                try // Try to get name of user that overdue book is checked out to
                {
                    name = $" ({DBConnectionHandler.reader[1].ToString()}, {DBConnectionHandler.reader[0].ToString()})";
                }
                catch // If user has no name logged in database, enter empty string
                {
                    name = "";
                }
                this.itemsDueThisWeek[i].LentTo = userID + name;
            }
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.c.Close();

            this.pageMax = (int)Math.Ceiling(((double)this.itemsDueThisWeek.Count) / itemsPerPage);

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
                    startIndex = ((pageNumber * itemsPerPage) - itemsPerPage);
                }
                for (int i = startIndex; i < this.itemsDueThisWeek.Count && i < (pageNumber * itemsPerPage); i++)
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
        private void ButtonNextPage_Click(object sender, RoutedEventArgs e)
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
        private void ButtonPreviousPage_Click(object sender, RoutedEventArgs e)
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
        private void ButtonPrintThisPage_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDlg = new PrintDialog();
            printDlg.PrintVisual(this.dataGridIssuedBooks, "Upcoming Due Dates");
            printDlg.ShowDialog();
        }

        /// <summary>
        /// Event handlers for checking and unchecking the column customization checkboxes.
        /// (Displays and hides the columns according to check and uncheck)
        /// </summary>
        #region CheckBox Event Handlers
        private void CheckBoxUserLentTo_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[1].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxUserLentTo_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[1].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxTitle_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[2].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxTitle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[2].Visibility = System.Windows.Visibility.Hidden;
        }
        private void CheckBoxAuthor_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[3].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxAuthor_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[3].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxDeweyDecimal_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[4].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDeweyDecimal_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[4].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxISBNTen_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[5].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxISBNTen_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[5].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxISXX_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[6].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxISXX_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[6].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxGenre_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[7].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxGenre_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[7].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxEdition_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[8].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxEdition_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[8].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxFormat_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[9].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxFormat_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[9].Visibility = System.Windows.Visibility.Hidden;
        }

        private void CheckBoxDueDate_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[10].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDueDate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[10].Visibility = System.Windows.Visibility.Hidden;
        }
        private void CheckBoxDaysUntilDueDate_Checked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[11].Visibility = System.Windows.Visibility.Visible;
        }

        private void CheckBoxDaysUntilDueDate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.dataGridIssuedBooks.Columns[11].Visibility = System.Windows.Visibility.Hidden;
        }
        #endregion

        /// <summary>
        /// An item that is due this week.
        /// Used to load into the Data Grid.
        /// </summary>
        public class ItemDueThisWeek
        {
            public string ItemID { get; set; }
            public string LentTo { get; set; }
            public string Title { get; set; }
            public string DueDate { get; set; }
            public double DaysUntilDueDate { get; set; }
            public string DeweyDecimal { get; set; }
            public string ISBNTen { get; set; }
            public string ISXX { get; set; }
            public string Genre { get; set; }
            public string Publisher { get; set; }
            public string Edition { get; set; }
            public string Author { get; set; }
            public string Format { get; set; }
        }
    }
}
