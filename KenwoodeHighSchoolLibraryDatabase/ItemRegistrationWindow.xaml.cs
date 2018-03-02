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
    /// Interaction logic for ItemRegistrationWindow.xaml
    /// </summary>
    public partial class ItemRegistrationWindow : Window
    {
        private OleDbConnection c;
        private OleDbDataReader reader;
        private OleDbCommand command;
        private List<string> selectedColumnValues;
        private bool toRegister;
        public ItemRegistrationWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            InitializeComboBoxes();

            this.textBoxPreviousCheckedOutBy.IsEnabled = false;
            this.textBoxCurrentlyCheckedOutBy.IsEnabled = false;
            this.labelPreviousCheckedOutBy.IsEnabled = false;
            this.labelCurrentlyCheckedOutBy.IsEnabled = false;
            this.buttonCheckout.IsEnabled = false;
            this.labelDueDate.IsEnabled = false;
            this.datePickerDueDate.IsEnabled = false;

            this.selectedColumnValues = new List<String>();

            this.labelWindowTitle.Content = "Register Item";

            toRegister = true;
        }

        
        Item toEditItem;
        private string isbnTen;
        private string isxx;
        private string authorLastName;
        private string authorMiddleName;
        private string authorFirstName;
        private string genreClassTwo;
        private string genreClassThree;
        private string publisher;
        private string edition;
        private string description;
        private string publicationYear;
        private string currentlyCheckedOutBy;
        private string previousCheckedOutBy;
        private DateTime dueDate;
        /// <summary>
        /// Overload constructor for if the user chooses to edit an item.
        /// Initialize the contents of objects in the window to the item to be edited.
        /// </summary>
        /// <param name="toEdit">The Item that the user would like to edit.</param>
        public ItemRegistrationWindow(Item toEdit)
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            InitializeComboBoxes();
            this.selectedColumnValues = new List<String>();

            this.toEditItem = toEdit;
            this.textBoxDeweyDecimal.Text = this.toEditItem.deweyDecimal;
            this.textBoxTitle.Text = this.toEditItem.title;
            this.comboBoxGenreHundreds.SelectedValue = this.toEditItem.genre;
            this.comboBoxFormat.SelectedValue = this.toEditItem.format;
            this.textBoxCurrentlyCheckedOutBy.Text = this.toEditItem.currentlyCheckedOutBy;
            this.labelWindowTitle.Content = "View, Modify, or Checkout Item";
            if (toEditItem.currentlyCheckedOutBy != "")
            {
                this.datePickerDueDate.IsEnabled = true;
            }
            this.buttonRegisterItem.Content = "Save Changes - Edit Item";
            this.LoadRemainingFields();
            if (toEditItem.currentlyCheckedOutBy != "")
            {
                this.datePickerDueDate.IsEnabled = true;
            }

            toRegister = false;
        }

        #region Extra Intialization
        /// <summary>
        /// Initialize connection to database.
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
        /// If the user is editing an item, load the rest of the items that cannot be loaded
        /// through the passed Item (struct) from the MainWindow.
        /// </summary>
        private void LoadRemainingFields()
        {
            this.c.Open();
            this.command.CommandText = "SELECT [authorLastName], [authorMiddleName], [authorFirstName], [ISBN10], [ISXX], " +
                "[genreClassTwo], [genreClassThree], [publisher], [publicationYear], [edition], [description], " +
                "[currentlyCheckedOUtBy], [previousCheckedOutBy], [dueDate]" +
                $"FROM items WHERE [itemID] = '{this.toEditItem.itemID}'";
            this.command.CommandType = System.Data.CommandType.Text;
            this.reader = this.command.ExecuteReader();
            this.reader.Read();

            this.isbnTen = this.reader["ISBN10"].ToString();
            this.textBoxISBNTen.Text = this.isbnTen;
            this.isxx = this.reader["ISXX"].ToString();
            this.textBoxISXX.Text = this.isxx;
            this.authorLastName = this.reader["authorLastName"].ToString();
            this.textBoxAuthorLName.Text = this.authorLastName;
            this.authorMiddleName = this.reader["authorMiddleName"].ToString();
            this.textBoxAuthorMName.Text = this.authorMiddleName;
            this.authorFirstName = this.reader["authorFirstName"].ToString();
            this.textBoxAuthorFName.Text = this.authorFirstName;
            this.genreClassTwo = this.reader["genreClassTwo"].ToString();
            this.comboBoxGenreTens.SelectedValue = this.genreClassTwo;
            this.genreClassThree = this.reader["genreClassThree"].ToString();
            this.comboBoxGenreOnes.SelectedValue = this.genreClassThree;
            this.publisher = this.reader["publisher"].ToString();
            this.textBoxPublisher.Text = this.publisher;
            this.publicationYear = this.reader["publicationYear"].ToString();
            this.textBoxPublicationYear.Text = this.publicationYear;
            this.edition = this.reader["edition"].ToString();
            this.textBoxEdition.Text = this.edition;
            this.description = this.reader["description"].ToString();
            this.textBoxDescription.Text = this.description;
            this.currentlyCheckedOutBy = this.reader["currentlyCheckedOutBy"].ToString();
            this.textBoxCurrentlyCheckedOutBy.Text = currentlyCheckedOutBy;
            this.previousCheckedOutBy = this.reader["previousCheckedOutBy"].ToString();
            this.textBoxPreviousCheckedOutBy.Text = this.previousCheckedOutBy;
            string dueDateString = this.reader["dueDate"].ToString();
            if (dueDateString.Length > 0)
            {

                this.dueDate = Convert.ToDateTime(this.reader["dueDate"].ToString());
                this.datePickerDueDate.SelectedDate = dueDate;
            }
            this.reader.Close();
            this.c.Close();
        }

        /// <summary>
        /// Initialize the genreHundreds (genreClassOne) comboBox with strings.
        /// Initialize the format comboBox with strings.
        /// (This allows the code to set the selected value when editing.)
        /// </summary>
        private void InitializeComboBoxes()
        {
            this.comboBoxGenreHundreds.Items.Add("Computer Science, Information and General Works");
            this.comboBoxGenreHundreds.Items.Add("Philosophy and Psychology");
            this.comboBoxGenreHundreds.Items.Add("Religion");
            this.comboBoxGenreHundreds.Items.Add("Social Sciences");
            this.comboBoxGenreHundreds.Items.Add("Language");
            this.comboBoxGenreHundreds.Items.Add("Science");
            this.comboBoxGenreHundreds.Items.Add("Technology");
            this.comboBoxGenreHundreds.Items.Add("Arts and Recreation");
            this.comboBoxGenreHundreds.Items.Add("Literature and Rhetoric");
            this.comboBoxGenreHundreds.Items.Add("History and Geography");
            this.comboBoxGenreHundreds.Items.Add("Fiction");

            this.comboBoxFormat.Items.Add("Book");
            this.comboBoxFormat.Items.Add("Other");
        }
        #endregion

        #region Register Item
        /// <summary>
        /// Converts older books' ISBN10 numbers to the more modern ISBN13 format. 
        /// Can also function for related standards such as ISMN 
        /// According to the International ISBN Agency's 2012 manual,
        /// the method of conversion is to:
        /// -Append 978 to the ISBN10 number
        /// -Replace the old ISBN10 checksum (last digit in the sequence) with a newly calculated ISBN13 checksum
        /// The ISBN13 checksum can be calculated by taking the ISBN10 number with the 978 prefix
        /// and inputting it into this equation: checkSum = (10 - (x1 + 3x2 + x3 + 3x4 + ... + x11 + 3x12) mod 10)
        /// (See the ISBN users manual for more information)
        /// Manual: https://www.isbn-international.org/sites/default/files/ISBN%20Manual%202012%20-corr.pdf
        /// </summary>
        /// <param name="isbnTen">The ISBN10 number to be converted</param>
        /// <returns>The ISBN13 number equivalent to the ISBN10 number input</returns>
        private string ConvertToISBNThirteen(string isbnTen)
        {
            // Append 978 as prefix and calculate ISBN13 Checksum to append as suffix
            string isbnThirteen = "978" + isbnTen; // initialize with 978 to calculate new checksum
            int evenSum = 0;
            int oddSum = 0;
            int totalSum;
            for (int i = 0; i < 12; i++) // Run through all 12 ints (13 is the checksum)
            {
                if ((i % 2) == 0)
                {
                    evenSum = evenSum + int.Parse(isbnThirteen[i].ToString());
                }
                if ((i % 2) == 1)
                {
                    oddSum = oddSum + (3 * int.Parse(isbnThirteen[i].ToString()));
                }
            }
            totalSum = evenSum + oddSum;
            // checkSum = (10 - (x1 + 3x2 + x3 + 3x4 + ... + x11 + 3x12) mod 10)
            int checkSum = 10 - (totalSum % 10);
            isbnThirteen = isbnThirteen.Substring(0, 12) + checkSum;
            return isbnThirteen;
        }

        // Strictly trims all chracters except for numerical values
        private string AgressiveTrim(string check)
        {
            List<char> newStringCharList = new List<char>();
            int length = check.ToArray().Count();
            for (int i = 0; i <= length - 1; i++)
            {
                char current = check[i];
                if (current >= 48 && current <= 57)
                {
                    newStringCharList.Add(current);
                }
            }
            string newString = new string(newStringCharList.ToArray());
            return newString;
        }

        /// <summary>
        /// Call the ConvertToISBNThirteen method to convert
        /// the ISBN10 code entered to ISBN13.
        /// Check that the ISBN10 code is in correct format first.
        /// Trim the ISBN10 code of dashes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConvertToISBN13_Click(object sender, RoutedEventArgs e)
        {
            string isbnTen = this.textBoxISBNTen.Text.Trim();
            isbnTen = AgressiveTrim(isbnTen);
            if (isbnTen != "" && isbnTen.ToArray().Count() == 10)
            {

                this.textBoxISXX.Text = ConvertToISBNThirteen(isbnTen);
            }
            else
            {
                MessageBox.Show("ISBN10 must be ten DIGITS long.");
            }
        }

        /// <summary>
        /// Load the genre tens comboBox to the current hundreds class selected
        /// in the comboBoxGenreHundreds comboBox. Data loaded from deweyDecimal table in the database.
        /// Classes defined by https://www.oclc.org/en/dewey/features/summaries.html
        /// (OCLC Online Computer Library Center, governing body of Dewey Decimal)
        /// Selected indexes can be used to generate Dewey Decimal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxGenreHundreds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxGenreHundreds.SelectedIndex < 10)
            {
                this.comboBoxGenreTens.IsEnabled = true;
                this.comboBoxGenreOnes.IsEnabled = false;
                this.comboBoxGenreTens.Items.Clear();
                this.selectedColumnValues.Clear();
                int column = this.comboBoxGenreHundreds.SelectedIndex;
                this.c.Open();
                this.command.CommandType = System.Data.CommandType.Text;
                this.command.CommandText = $"SELECT [{column}] FROM deweyDecimal";
                this.reader = this.command.ExecuteReader();
                int count = 0;
                this.selectedColumnValues.Add("[General]");
                while (this.reader.Read())
                {
                    string toAdd = this.reader[$"{column}"].ToString();
                    this.selectedColumnValues.Add(toAdd);
                    if ((count % 10) == 0)
                    {
                        this.comboBoxGenreTens.Items.Add(this.selectedColumnValues[count]);
                    }
                    count = count + 1;
                }
                this.c.Close();
                this.reader.Close();
            }
            else // If user selects fiction
            {
                this.comboBoxGenreTens.IsEnabled = false;
                this.comboBoxGenreTens.SelectedValue = "[General]";
                this.comboBoxGenreOnes.IsEnabled = false;
                this.comboBoxGenreOnes.SelectedValue = "[General]";
            }
        }

        /// <summary>
        /// Load the genre ones comboBox to the current tens class selected
        /// in the comboBoxGenreTens comboBox. Data loaded from the deweyDecimal table in the database.
        /// Classes defined by https://www.oclc.org/en/dewey/features/summaries.html
        /// (OCLC Online Computer Library Center, governing body of Dewey Decimal)
        /// Selected indexes can be used to generate Dewey Decimal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBoxGenreTens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.comboBoxGenreOnes.IsEnabled = true;
            this.comboBoxGenreOnes.Items.Clear();
            if (this.comboBoxGenreTens.SelectedItem != null) 
            {
                this.comboBoxGenreOnes.Items.Clear();
                this.comboBoxGenreOnes.Items.Add("[General]");
                int sectionStart = (this.comboBoxGenreTens.SelectedIndex * 10) + 1;
                for (int i = sectionStart; i <= sectionStart + 8; i++)
                {
                    string toAdd = this.selectedColumnValues[i];
                    this.comboBoxGenreOnes.Items.Add(toAdd);
                }
            }
        }

        /// <summary>
        /// Generate deweyDecimal number (or letters) based on 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonGenerateDeweyDecimal_Click(object sender, RoutedEventArgs e)
        {
            if (this.comboBoxGenreHundreds.SelectedIndex == 10)
            {
                if (this.textBoxAuthorFName.Text == "" || this.textBoxAuthorLName.Text == "")
                {
                    MessageBox.Show("Author first and last name boxes must be filled out to generate a Dewey Decimal");
                }
                else if (this.textBoxAuthorLName.Text.Length < 4)
                {
                    MessageBox.Show("Fiction Dewey Decimal cannot be generated. Please enter manually.");
                }
                else
                {
                    this.textBoxDeweyDecimal.Text = $"{this.textBoxAuthorFName.Text.Substring(0, 1)} {this.textBoxAuthorLName.Text.Substring(0, 4)}";
                }
            }
            else
            {
                if ((this.comboBoxGenreHundreds.SelectedIndex != -1) && (this.comboBoxGenreTens.SelectedIndex != -1)
                && (this.comboBoxGenreOnes.SelectedIndex != -1))
                {
                    int hundreds = this.comboBoxGenreHundreds.SelectedIndex * 100;
                    int tens = this.comboBoxGenreTens.SelectedIndex * 10;
                    int ones = this.comboBoxGenreOnes.SelectedIndex;
                    string deweyDecimal = (hundreds + tens + ones).ToString();
                    this.textBoxDeweyDecimal.Text = deweyDecimal;
                }
                else
                {
                    MessageBox.Show("All genre input boxes must be filled out to " +
                        "generate a Dewey Decimal.");
                }
            }
        }

        private string CheckRequiredItemsFilledOut()
        {

            if (this.comboBoxGenreHundreds.SelectedIndex != 1)
            {
                if (this.comboBoxGenreHundreds.SelectedIndex <= 9 && (this.comboBoxGenreTens.SelectedIndex == -1
                || this.comboBoxGenreOnes.SelectedIndex == -1))
                {
                    return "A full genre is required. " +
                        "Genre boxes must be filled out. Please select values for all three Genre boxes.";
                }
            }
            if (this.comboBoxFormat.SelectedIndex == -1)
            {
                return "A format is required. " +
                    "Format box must be filled out. Please select values for the Format box.";
            }
            if (this.textBoxISXX.Text == "")
            {
                return "An ISBN13 number is required. Please enter a value for an ISBN13 number " +
                    "or generate one from an ISBN10 number.";
            }
            if (this.textBoxDeweyDecimal.Text == "")
            {
                return "A Dewey Decimal number is required. Please enter a value for a Dewey Decimal number " +
                    "or generate one from the genre or author.";
            }
            return "";
        }

        private int GenerateCopyID(string isxx)
        {
            isxx = this.textBoxISXX.Text;
            this.c.Open();
            this.command.CommandType = System.Data.CommandType.Text;
            this.command.CommandText = $"SELECT [itemID], [copyID] FROM items WHERE [itemID] LIKE '%{isxx}-%' ORDER BY [copyID]";
            this.reader = this.command.ExecuteReader();
            int previous;
            int copyID = 1;
            try
            {
                this.reader.Read();
                previous = int.Parse(this.reader[1].ToString());
                if (previous >= 1)
                {
                    this.c.Close();
                    return 0;
                }
            }
            catch (InvalidOperationException) // If no IDs contain specified ISBN13 (isbnThirteen)
            {
                this.c.Close();
                return 0;
            }
            while (this.reader.Read()) // loop through ItemIDs and fill in gaps in suffixes if needed
            {
                int current = int.Parse(this.reader[1].ToString());
                if (current == previous + 1)
                {
                    copyID = current + 1;
                    previous = current;
                }
                else
                {
                    copyID = previous + 1;
                    break;
                }
            }
            this.c.Close();
            return copyID; // Also suffix of ItemID
        }

        private void comboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxFormat.SelectedIndex == 1)
            {
                this.labelISXX.Content = "ISXX";
                this.labelISBNTen.IsEnabled = false;
                this.textBoxISBNTen.IsEnabled = false;
                this.textBoxISBNTen.Clear();
                this.buttonConvertToISBN13.IsEnabled = false;
            }
            else
            {
                this.labelISXX.Content = "ISBN 13";
                this.labelISBNTen.IsEnabled = true;
                this.textBoxISBNTen.IsEnabled = true;
                this.buttonConvertToISBN13.IsEnabled = true;
            }
        }
        #endregion Register

        #region Save Changes (Register AND Edit/Update)
        private void buttonRegisterItem_Click(object sender, RoutedEventArgs e)
        {
            if (toRegister)
            {
                Register();
            }
            else
            {
                EditAndUpdate();
            }
        }

        private void Register()
        {
            string message = CheckRequiredItemsFilledOut();
            string isxx = this.textBoxISXX.Text;
            if (message == "")
            {
                int copyID = GenerateCopyID(this.textBoxISXX.Text);
                string itemID = this.textBoxISXX.Text + $"-{copyID}";
                this.c.Open();
                this.command.CommandText = "INSERT INTO items ([itemID], [copyID], [title], [genreClassOne], [genreClassTwo], [genreClassThree], " +
                    "[format], [authorFirstName], [authorMiddleName], [authorLastName], [deweyDecimal], [ISBN10], [ISXX], [publisher], " +
                    "[publicationYear], [edition], [description]) " +
                    $"VALUES ('{itemID}', {copyID}, '{this.textBoxTitle.Text}', '{this.comboBoxGenreHundreds.SelectedValue}', " +
                    $"'{this.comboBoxGenreTens.SelectedValue}', '{this.comboBoxGenreOnes.SelectedValue}', '{this.comboBoxFormat.SelectedValue}', " +
                    $"'{this.textBoxAuthorFName.Text}', '{this.textBoxAuthorMName.Text}', '{this.textBoxAuthorLName.Text}', " +
                    $"'{this.textBoxDeweyDecimal.Text}', '{this.textBoxISBNTen.Text}', '{this.textBoxISXX.Text}', " +
                    $"'{this.textBoxPublisher.Text}', '{this.textBoxPublicationYear.Text}', '{this.textBoxEdition.Text}', '{this.textBoxDescription.Text}')";
                this.command.ExecuteNonQuery();
                this.c.Close();
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        private void EditAndUpdate()
        {
            if (MessageBox.Show("Save Changes?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string errorMessage = CheckRequiredItemsFilledOut();
                if (errorMessage.Length == 0)
                {
                    UpdateItemTable();
                    MessageBox.Show("Item data updated.");
                    this.c.Close();
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show(errorMessage);
                }
            }
        }
        #endregion

        #region Editing and Viewing Item
        private void buttonCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxCurrentlyCheckedOutBy.Text != "")
            {
                textBoxCurrentlyCheckedOutBy.Text = "";
                this.textBoxPreviousCheckedOutBy.Text = this.currentlyCheckedOutBy;
            }
            else
            {
                MessageBox.Show("This item is not checked out to any user.");
            }
        }

        private void UpdateItemTable()
        {
            if (this.textBoxTitle.Text != this.toEditItem.title)
            {
                UpdateColumn("title", this.textBoxTitle.Text);
            }

            if (this.textBoxAuthorFName.Text != this.authorFirstName)
            {
                UpdateColumn("authorFirstName", this.textBoxAuthorFName.Text);
            }

            if (this.textBoxAuthorMName.Text != this.authorMiddleName)
            {
                UpdateColumn("authorMiddleName", this.textBoxAuthorMName.Text);
            }

            if (this.textBoxAuthorLName.Text != this.authorLastName)
            {
                UpdateColumn("authorLastName", this.textBoxAuthorLName.Text);
            }

            if (this.textBoxISBNTen.Text != this.isbnTen)
            {
                UpdateColumn("ISBN10", this.textBoxISBNTen.Text);
            }

            if (this.textBoxDeweyDecimal.Text != this.toEditItem.deweyDecimal)
            {
                UpdateColumn("deweyDecimal", this.textBoxDeweyDecimal.Text);
            }

            if (this.textBoxPublisher.Text != this.publisher)
            {
                UpdateColumn("publisher", this.textBoxPublisher.Text);
            }

            if (this.textBoxPublicationYear.Text != this.publicationYear)
            {
                UpdateColumn("publicationYear", this.textBoxPublicationYear.Text);
            }

            if (this.textBoxEdition.Text != this.edition)
            {
                UpdateColumn("edition", this.textBoxEdition.Text);
            }

            if (this.textBoxDescription.Text != this.description)
            {
                UpdateColumn("description", this.textBoxDescription.Text);
            }

            if (this.comboBoxGenreHundreds.SelectedValue.ToString() != this.toEditItem.genre)
            {
                UpdateColumn("genreClassOne", this.comboBoxGenreHundreds.SelectedValue.ToString());
            }

            if (this.comboBoxGenreTens.SelectedValue.ToString() != this.genreClassTwo)
            {
                UpdateColumn("genreClassTwo", this.comboBoxGenreTens.SelectedValue.ToString());
            }

            if (this.comboBoxGenreOnes.SelectedValue.ToString() != this.toEditItem.genre)
            {
                UpdateColumn("genreClassThree", this.comboBoxGenreOnes.SelectedValue.ToString());
            }

            if (this.comboBoxFormat.SelectedValue.ToString() != this.toEditItem.format)
            {
                UpdateColumn("format", this.comboBoxFormat.SelectedValue.ToString());
            }

            if (this.textBoxCurrentlyCheckedOutBy.Text != this.currentlyCheckedOutBy)
            {
                UpdateColumn("currentlyCheckedOutBy", this.textBoxCurrentlyCheckedOutBy.Text);
                UpdateColumn("previousCheckedOutBy", this.currentlyCheckedOutBy);
                UpdateColumn("dueDate", "");
                this.datePickerDueDate.SelectedDate = null;
                c.Open();
                //command.CommandText = $"SELECT [numberofCheckedoutItems] FROM accounts WHERE [userID] = '{this.currentlyCheckedOutBy}'";
                //reader = command.ExecuteReader();
                //reader.Read();
                //int numberOfCheckedOutItems = int.Parse(reader[0].ToString());
                //reader.Close();
                //if (numberOfCheckedOutItems != 0)
                command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " + //lowercase o second
                    $"WHERE [userID] = '{this.currentlyCheckedOutBy}'";
                command.ExecuteNonQuery();
                c.Close();
            }

            if (this.datePickerDueDate.SelectedDate != this.dueDate)
            {
                if (this.datePickerDueDate.SelectedDate != null)
                {

                    DateTime newDueDate = ((DateTime)this.datePickerDueDate.SelectedDate).AddHours(23.9999);
                    UpdateColumn("dueDate", newDueDate.ToString());
                }
            }

            if (this.textBoxISXX.Text != this.isxx)
            {
                UpdateColumn("ISXX", this.textBoxISXX.Text);
                int newCopyID = GenerateCopyID(this.textBoxISXX.Text);
                this.c.Open();
                this.command.CommandText = $"UPDATE items SET [itemID] = '{this.textBoxISXX.Text}-{newCopyID}', [copyID] = {newCopyID} WHERE [itemID] = '{this.toEditItem.itemID}'";
                this.command.ExecuteNonQuery();
                this.c.Close();
            }
        }

        private void UpdateColumn(string column, string newValue)
        {
            this.c.Open();
            this.command.CommandText = $"UPDATE items SET [{column}] = '{newValue}' WHERE itemID = '{this.toEditItem.itemID}'";
            this.command.ExecuteNonQuery();
            this.c.Close();
        }
        #endregion
    }
}
