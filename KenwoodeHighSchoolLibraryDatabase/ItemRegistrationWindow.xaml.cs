using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for ItemRegistrationWindow.xaml
    /// </summary>
    public partial class ItemRegistrationWindow : Window
    {
        private List<string> selectedColumnValues;
        private bool toRegister;
        private string numberOfCopiesToRegister;
        public ItemRegistrationWindow()
        {
            InitializeComponent();
            InitializeComboBoxes();

            this.textBoxPreviousCheckedOutBy.IsEnabled = false;
            this.textBoxCurrentlyCheckedOutBy.IsEnabled = false;
            this.labelPreviousCheckedOutBy.IsEnabled = false;
            this.labelCurrentlyCheckedOutBy.IsEnabled = false;
            this.buttonCheckout.IsEnabled = false;
            this.labelDueDate.IsEnabled = false;
            this.datePickerDueDate.IsEnabled = false;

            this.textBoxNumberOfCopies.Text = "1";

            this.selectedColumnValues = new List<String>();

            this.labelWindowTitle.Content = "Register Item";

            this.toRegister = true;
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
        public ItemRegistrationWindow(Item toEdit) // Item as defined in MainWindow (struct)
        {
            InitializeComponent();
            InitializeComboBoxes();
            this.selectedColumnValues = new List<String>();

            this.toEditItem = toEdit;
            this.textBoxDeweyDecimal.Text = this.toEditItem.DeweyDecimal;
            this.textBoxTitle.Text = this.toEditItem.Title;
            this.comboBoxGenreHundreds.SelectedValue = this.toEditItem.Genre;
            this.comboBoxFormat.SelectedValue = this.toEditItem.Format;
            this.textBoxCurrentlyCheckedOutBy.Text = this.toEditItem.CurrentlyCheckedOutBy;
            this.labelWindowTitle.Content = "View, Modify, or Checkout Item";
            this.textBoxNumberOfCopies.Text = "1";
            if (this.toEditItem.CurrentlyCheckedOutBy != "")
            {
                this.datePickerDueDate.IsEnabled = true;
            }
            this.buttonRegisterItem.Content = "Save Changes - Edit Item";
            LoadRemainingFields();
            if (this.toEditItem.CurrentlyCheckedOutBy != "")
            {
                this.datePickerDueDate.IsEnabled = true;
            }
            this.buttonAddOneNumberOfCopies.IsEnabled = false;
            this.buttonSubtractOneNumberOfCopies.IsEnabled = false;
            this.textBoxNumberOfCopies.IsEnabled = false;
            
            this.toRegister = false;
        }

        #region Extra Intialization
        /// <summary>
        /// If the user is editing an item, load the rest of the items that cannot be loaded
        /// through the passed Item (struct) from the MainWindow.
        /// </summary>
        private void LoadRemainingFields()
        {
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = "SELECT [authorLastName], [authorMiddleName], [authorFirstName], [ISBN10], [ISXX], " +
                "[genreClassTwo], [genreClassThree], [publisher], [publicationYear], [edition], [description], " +
                "[currentlyCheckedOUtBy], [previousCheckedOutBy], [dueDate]" +
                $"FROM items WHERE [itemID] = '{this.toEditItem.ItemID}'";
            DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            DBConnectionHandler.reader.Read();

            this.isbnTen = DBConnectionHandler.reader["ISBN10"].ToString();
            this.textBoxISBNTen.Text = this.isbnTen;
            this.isxx = DBConnectionHandler.reader["ISXX"].ToString();
            this.textBoxISXX.Text = this.isxx;
            this.authorLastName = DBConnectionHandler.reader["authorLastName"].ToString();
            this.textBoxAuthorLName.Text = this.authorLastName;
            this.authorMiddleName = DBConnectionHandler.reader["authorMiddleName"].ToString();
            this.textBoxAuthorMName.Text = this.authorMiddleName;
            this.authorFirstName = DBConnectionHandler.reader["authorFirstName"].ToString();
            this.textBoxAuthorFName.Text = this.authorFirstName;
            this.genreClassTwo = DBConnectionHandler.reader["genreClassTwo"].ToString();
            this.comboBoxGenreTens.SelectedValue = this.genreClassTwo;
            this.genreClassThree = DBConnectionHandler.reader["genreClassThree"].ToString();
            this.comboBoxGenreOnes.SelectedValue = this.genreClassThree;
            this.publisher = DBConnectionHandler.reader["publisher"].ToString();
            this.textBoxPublisher.Text = this.publisher;
            this.publicationYear = DBConnectionHandler.reader["publicationYear"].ToString();
            this.textBoxPublicationYear.Text = this.publicationYear;
            this.edition = DBConnectionHandler.reader["edition"].ToString();
            this.textBoxEdition.Text = this.edition;
            this.description = DBConnectionHandler.reader["description"].ToString();
            this.textBoxDescription.Text = this.description;
            this.currentlyCheckedOutBy = DBConnectionHandler.reader["currentlyCheckedOutBy"].ToString();
            this.textBoxCurrentlyCheckedOutBy.Text = this.currentlyCheckedOutBy;
            this.previousCheckedOutBy = DBConnectionHandler.reader["previousCheckedOutBy"].ToString();
            this.textBoxPreviousCheckedOutBy.Text = this.previousCheckedOutBy;
            string dueDateString = DBConnectionHandler.reader["dueDate"].ToString();
            if (dueDateString.Length > 0)
            {

                this.dueDate = Convert.ToDateTime(DBConnectionHandler.reader["dueDate"].ToString());
                this.datePickerDueDate.SelectedDate = this.dueDate;
            }
            DBConnectionHandler.reader.Close();
            DBConnectionHandler.c.Close();
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
        /// </summary>
        /// <param name="isbnTen">The ISBN10 number to be converted</param>
        /// <returns>The ISBN13 number equivalent to the ISBN10 number input</returns>
        private string ConvertToISBNThirteen(string isbnTen)
        {
            // According to the International ISBN Agency's 2012 manual,
            // the method of conversion is to:
            // -Append 978 to the ISBN10 number
            // -Replace the old ISBN10 checksum (last digit in the sequence) with a newly calculated ISBN13 checksum
            // The ISBN13 checksum can be calculated by taking the ISBN10 number with the 978 prefix
            // and inputting it into this equation: checkSum = (10 - (x1 + 3x2 + x3 + 3x4 + ... + x11 + 3x12) mod 10)
            // (See the ISBN users manual for more information)
            // Manual: https://www.isbn-international.org/sites/default/files/ISBN%20Manual%202012%20-corr.pdf
            // Assert isbnTen is exactly 10 ints in string form (checked in event handler)
            // Append 978 as prefix and calculate ISBN13 Checksum to append as suffix
            string isbnThirteen = "978" + isbnTen; // initialize with 978 to calculate new checksum
            int totalSum = 0;
            for (int i = 0; i < 12; i++) // Run through all 12 ints (13 is the checksum)
            {
                if ((i % 2) != 0)
                {
                    totalSum = totalSum + (3 * int.Parse(isbnThirteen[i].ToString()));
                }
                else
                {
                    totalSum = totalSum + (int.Parse(isbnThirteen[i].ToString()));
                }
            }
            // checkSum = (10 - (a1 + 3 * a2 + a3 + 3 * a4 + ... + a11 + 3 * a12) mod 10)
            int checkSum = 10 - (totalSum % 10);
            isbnThirteen = isbnThirteen.Substring(0, 12) + checkSum;
            return isbnThirteen;
        }

        /// <summary>
        /// Strictly trims all chracters except for numerical values
        /// </summary>
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
        private void ButtonConvertToISBN13_Click(object sender, RoutedEventArgs e)
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
        private void ComboBoxGenreHundreds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboBoxGenreHundreds.SelectedIndex < 10)
            {
                this.comboBoxGenreTens.IsEnabled = true;
                this.comboBoxGenreOnes.IsEnabled = false;
                this.comboBoxGenreTens.Items.Clear();
                this.selectedColumnValues.Clear();
                int column = this.comboBoxGenreHundreds.SelectedIndex;
                DBConnectionHandler.c.Open();
                DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
                DBConnectionHandler.command.CommandText = $"SELECT [{column}] FROM deweyDecimal";
                DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
                int count = 0;
                this.selectedColumnValues.Add("[General]");
                while (DBConnectionHandler.reader.Read())
                {
                    string toAdd = DBConnectionHandler.reader[$"{column}"].ToString();
                    this.selectedColumnValues.Add(toAdd);
                    if ((count % 10) == 0)
                    {
                        this.comboBoxGenreTens.Items.Add(this.selectedColumnValues[count]);
                    }
                    count = count + 1;
                }
                DBConnectionHandler.c.Close();
                DBConnectionHandler.reader.Close();
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
        private void ComboBoxGenreTens_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        /// Generate Dewey Decimal number (or letters) based on selected genre comboBox indexes.
        /// Generate author based Dewey Decimal if selected hundreds class is fiction.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonGenerateDeweyDecimal_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Check that all required fields are filled out in correct formatting.
        /// Return error message to display in MessageBox if all required fields are
        /// not filled out in correct formatting. If all required fields are correct
        /// and in correct formatting, return empty string.
        /// </summary>
        /// <returns>Error message, empty string if all filled out correctly</returns>
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
            if (String.IsNullOrWhiteSpace(this.textBoxNumberOfCopies.Text))
            {
                return "A number of copies to register (at least one) must be entered. Please enter a value" +
                    "for number of copies to register.";
            }
                return "";
        }

        /// <summary>
        /// Generates the copyID for an item (the number of copies of the same item)
        /// Unique item IDs are based on the ISXX (ISBN13, etc) number and a suffix that is equivalent
        /// to the copy number. This method generates a unique suffix so that all items have a
        /// unique ID. If an item is deleted, this method will fill in the copy number the next time
        /// a book of the same ISXX number is passed in. Returns the suffix to be used in ID.
        /// </summary>
        /// <param name="isxx">The ISXX number to generate a copyID and suffix for.</param>
        /// <returns></returns>
        private int GenerateCopyID(string isxx)
        {
            isxx = this.textBoxISXX.Text;
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandType = System.Data.CommandType.Text;
            DBConnectionHandler.command.CommandText = $"SELECT [itemID], [copyID] FROM items WHERE [itemID] LIKE '%{isxx}-%' ORDER BY [copyID]";
            DBConnectionHandler.reader = DBConnectionHandler.command.ExecuteReader();
            int previous;
            int copyID = 2;
            try
            {
                DBConnectionHandler.reader.Read();
                previous = int.Parse(DBConnectionHandler.reader[1].ToString());
                if (previous >= 2)
                {
                    DBConnectionHandler.c.Close();
                    return 1;
                }
            }
            catch (InvalidOperationException) // If no IDs contain specified ISBN13 (isbnThirteen)
            {
                DBConnectionHandler.c.Close();
                return 1;
            }

            while (DBConnectionHandler.reader.Read()) // loop through ItemIDs and fill in gaps in suffixes if needed
            {
                int current = int.Parse(DBConnectionHandler.reader[1].ToString());
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
            DBConnectionHandler.c.Close();
            return copyID; // Also suffix of ItemID
        }

        /// <summary>
        /// Display ISBN13 and leave ISBN10 enabled if the item is a book.
        /// If not, then display ISXX as the label - Other International Standard
        /// numbers exist for specific format types.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBoxFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        /// <summary>
        /// Leads to the correct save method depending on if the user is registering
        /// an item or if the user is editing an item. Boolean "toRegister" intialized in the
        /// constructor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRegisterItem_Click(object sender, RoutedEventArgs e)
        {
            if (this.toRegister)
            {
                Register();
            }
            else
            {
                EditAndUpdate();
            }
        }

        /// <summary>
        /// Save the new item to the database and close the window and load data grids.
        /// If all required items are not filled out correctly and in the correct format,
        /// display error message in a MessageBox.
        /// </summary>
        private void Register()
        {
            string message = CheckRequiredItemsFilledOut();
            string isxx = this.textBoxISXX.Text;
            if (message == "")
            {
                for (int i = 0; i < int.Parse(this.textBoxNumberOfCopies.Text); i++)
                {
                    int copyID = GenerateCopyID(this.textBoxISXX.Text);
                    string itemID = this.textBoxISXX.Text + $"-{copyID}";
                    DBConnectionHandler.c.Open();
                    DBConnectionHandler.command.CommandText = "INSERT INTO items ([itemID], [copyID], [title], [genreClassOne], [genreClassTwo], [genreClassThree], " +
                        "[format], [authorFirstName], [authorMiddleName], [authorLastName], [deweyDecimal], [ISBN10], [ISXX], [publisher], " +
                        "[publicationYear], [edition], [description]) " +
                        $"VALUES ('{itemID}', {copyID}, '{this.textBoxTitle.Text}', '{this.comboBoxGenreHundreds.SelectedValue}', " +
                        $"'{this.comboBoxGenreTens.SelectedValue}', '{this.comboBoxGenreOnes.SelectedValue}', '{this.comboBoxFormat.SelectedValue}', " +
                        $"'{this.textBoxAuthorFName.Text}', '{this.textBoxAuthorMName.Text}', '{this.textBoxAuthorLName.Text}', " +
                        $"'{this.textBoxDeweyDecimal.Text}', '{this.textBoxISBNTen.Text}', '{this.textBoxISXX.Text}', " +
                        $"'{this.textBoxPublisher.Text}', '{this.textBoxPublicationYear.Text}', '{this.textBoxEdition.Text}', '{this.textBoxDescription.Text}')";
                    DBConnectionHandler.command.ExecuteNonQuery();
                    DBConnectionHandler.c.Close();
                }
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show(message);
            }
        }
        #endregion

        #region Editing and Viewing Item
        /// <summary>
        /// Ask the user to confirm to save changes.
        /// Check that all fields are correctly input, then save to database.
        /// Display error message in MessageBox if any fields are incorrectly entered.
        /// </summary>
        private void EditAndUpdate()
        {
            if (MessageBox.Show("Save Changes?", "Update Database", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                string errorMessage = CheckRequiredItemsFilledOut();
                if (errorMessage.Length == 0)
                {
                    UpdateItemTable();
                    MessageBox.Show("Item data updated.");
                    DBConnectionHandler.c.Close();
                    this.DialogResult = true;
                }
                else
                {
                    MessageBox.Show(errorMessage);
                }
            }
        }

        /// <summary>
        /// Remove the checked out user to the current item.
        /// Place the checked out user into previousCheckedOutBy in the window and in
        /// the database.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonCheckout_Click(object sender, RoutedEventArgs e)
        {
            if (this.textBoxCurrentlyCheckedOutBy.Text != "")
            {
                this.textBoxCurrentlyCheckedOutBy.Text = "";
                this.textBoxPreviousCheckedOutBy.Text = this.currentlyCheckedOutBy;
            }
            else
            {
                MessageBox.Show("This item is not checked out to any user.");
            }
        }

        /// <summary>
        /// If a field is changed, update it in the database.
        /// Performs other calculations as necessary.
        /// (For example, a changed ISXX).
        /// </summary>
        private void UpdateItemTable()
        {
            if (this.textBoxTitle.Text != this.toEditItem.Title)
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

            if (this.textBoxDeweyDecimal.Text != this.toEditItem.DeweyDecimal)
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

            if (this.comboBoxGenreHundreds.SelectedValue.ToString() != this.toEditItem.Genre)
            {
                UpdateColumn("genreClassOne", this.comboBoxGenreHundreds.SelectedValue.ToString());
            }

            if (this.comboBoxGenreTens.SelectedValue.ToString() != this.genreClassTwo)
            {
                UpdateColumn("genreClassTwo", this.comboBoxGenreTens.SelectedValue.ToString());
            }

            if (this.comboBoxGenreOnes.SelectedValue.ToString() != this.toEditItem.Genre)
            {
                UpdateColumn("genreClassThree", this.comboBoxGenreOnes.SelectedValue.ToString());
            }

            if (this.comboBoxFormat.SelectedValue.ToString() != this.toEditItem.Format)
            {
                UpdateColumn("format", this.comboBoxFormat.SelectedValue.ToString());
            }

            if (this.textBoxCurrentlyCheckedOutBy.Text != this.currentlyCheckedOutBy)
            {
                UpdateColumn("currentlyCheckedOutBy", this.textBoxCurrentlyCheckedOutBy.Text);
                UpdateColumn("previousCheckedOutBy", this.currentlyCheckedOutBy);
                UpdateColumn("dueDate", "");
                this.datePickerDueDate.SelectedDate = null;
                DBConnectionHandler.c.Open();
                DBConnectionHandler.command.CommandText = "UPDATE accounts SET [numberOfCheckedoutItems] = [numberOfCheckedOutItems] - 1 " +
                    $"WHERE [userID] = '{this.currentlyCheckedOutBy}'";
                DBConnectionHandler.command.ExecuteNonQuery();
                DBConnectionHandler.c.Close();
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
                DBConnectionHandler.c.Open();
                DBConnectionHandler.command.CommandText = $"UPDATE items SET [itemID] = '{this.textBoxISXX.Text}-{newCopyID}', [copyID] = {newCopyID} WHERE [itemID] = '{this.toEditItem.ItemID}'";
                DBConnectionHandler.command.ExecuteNonQuery();
                DBConnectionHandler.c.Close();
            }
        }

        /// <summary>
        /// Updates the current item in the items table within the database
        /// in the specified column with the new value. 
        /// </summary>
        /// <param name="column">Column to modify in database</param>
        /// <param name="newValue">New value to update in the column</param>
        private void UpdateColumn(string column, string newValue)
        {
            DBConnectionHandler.c.Open();
            DBConnectionHandler.command.CommandText = $"UPDATE items SET [{column}] = '{newValue}' WHERE itemID = '{this.toEditItem.ItemID}'";
            DBConnectionHandler.command.ExecuteNonQuery();
            DBConnectionHandler.c.Close();
        }
        #endregion

        #region Register Multiple Copies
        private void ButtonAddOneNumberOfCopies_Click(object sender, RoutedEventArgs e)
        {
            string textBoxNumberOfCopies = this.textBoxNumberOfCopies.Text;
            if (!String.IsNullOrWhiteSpace(textBoxNumberOfCopies))
            {
                this.numberOfCopiesToRegister = (int.Parse(textBoxNumberOfCopies) + 1).ToString();
                this.textBoxNumberOfCopies.Text = this.numberOfCopiesToRegister;
            }
            else
            {
                this.textBoxNumberOfCopies.Text = "1";
            }
        }

        private void ButtonSubtractOneNumberOfCopies_Click(object sender, RoutedEventArgs e)
        {
            string textBoxNumberOfCopies = this.textBoxNumberOfCopies.Text;
            if (textBoxNumberOfCopies != "1" && !String.IsNullOrWhiteSpace(textBoxNumberOfCopies))
            {
                this.numberOfCopiesToRegister = (int.Parse(this.textBoxNumberOfCopies.Text) - 1).ToString();
                this.textBoxNumberOfCopies.Text = this.numberOfCopiesToRegister;
            }
            else
            {
                this.textBoxNumberOfCopies.Text = "1";
            }
        }

        /// <summary>
        /// Precheck what the user inputs before entering into textBoxNumberOfCopies and ensure that the textbox only contains a integer greater than or equal to 1.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The text within textBoxNumberOfCopies</param>
        private void CheckIfTextBoxContainsNumbers(object sender, TextCompositionEventArgs e)
        {
            // Will not allow any characters besides space and digits 1 through 9 to be entered for the first character.
            // Ensures textbox always contains an integer bewteen 1 and infinity
            if (String.IsNullOrWhiteSpace(this.textBoxNumberOfCopies.Text)) // If the textbox is empty
            {
                Regex regex = new Regex("^[1-9]$"); // Only allow entering of digits between one and 9
                e.Handled = !regex.IsMatch(e.Text);
            }
            else // else, textbox already contains at least one integer - allow any digit between 1 and 9
            {
                Regex regex = new Regex("^[0-9]$");
                e.Handled = !regex.IsMatch(e.Text);
            }
        }
        #endregion

        #region Closing Edit Window
        private void Registration_Edit_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.toEditItem != null)
            {
                EditAndUpdate();
            }
        }
        #endregion
    }
}
