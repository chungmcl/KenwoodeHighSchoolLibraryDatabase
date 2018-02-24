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
        OleDbConnection c;
        OleDbDataReader reader;
        OleDbCommand command;
        List<string> selectedColumnValues;
        public ItemRegistrationWindow()
        {
            InitializeComponent();

            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin";
            command = new OleDbCommand();
            command.Connection = c;
            reader = null;

            selectedColumnValues = new List<String>();
        }

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

        private void buttonConvertToISBN13_Click(object sender, RoutedEventArgs e)
        {
            textBoxISBNThirteen.Text = ConvertToISBNThirteen(textBoxISBNTen.Text);
        }

        private void comboBoxGenreHundreds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxGenreTens.Items.Clear();
            selectedColumnValues.Clear();
            int column = comboBoxGenreHundreds.SelectedIndex;
            c.Open();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = $"SELECT [{column}] FROM deweyDecimal";
            reader = command.ExecuteReader();
            int count = 0;
            selectedColumnValues.Add("[General]");
            while (reader.Read())
            {
                string toAdd = reader[$"{column}"].ToString();
                selectedColumnValues.Add(toAdd);
                if ((count % 10) == 0)
                {
                    comboBoxGenreTens.Items.Add(selectedColumnValues[count]);
                }
                count = count + 1;
            }
            c.Close();
            reader.Close();
        }

        private void comboBoxGenreTens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxGenreOnes.Items.Clear();
            if (comboBoxGenreTens.SelectedValue != null)
            {
                comboBoxGenreOnes.Items.Clear();
                comboBoxGenreOnes.Items.Add("[General]");
                int sectionStart = (comboBoxGenreTens.SelectedIndex * 10) + 1;
                for (int i = sectionStart; i <= sectionStart + 8; i++)
                {
                    string toAdd = selectedColumnValues[i];
                    comboBoxGenreOnes.Items.Add(toAdd);
                }
            }
        }

        private void buttonGenerateDeweyDecimal_Click(object sender, RoutedEventArgs e)
        {
            int hundreds = comboBoxGenreHundreds.SelectedIndex * 100;
            int tens = comboBoxGenreTens.SelectedIndex * 10;
            int ones = comboBoxGenreOnes.SelectedIndex;
            string deweyDecimal = (hundreds + tens + ones).ToString();
            textBoxDeweyDecimal.Text = deweyDecimal;
        }

        private void buttonRegisterItem_Click(object sender, RoutedEventArgs e)
        {
            int copyID = GenerateCopyID(textBoxISBNThirteen.Text);
            string itemID = textBoxISBNThirteen.Text + $"-{copyID}";
            c.Open();
            command.CommandText = "INSERT INTO items ([itemID], [copyID], [title], [genreClassOne], [genreClassTwo], [genreClassThree], " +
                "[format], [authorFirstName], [authorMiddleName], [authorLastName], [deweyDecimal], [ISBN10], [ISBN13], [publisher], " +
                "[publicationYear], [edition], [description]) " +
                $"VALUES ('{itemID}', {copyID}, '{textBoxTitle.Text}', '{comboBoxGenreHundreds.SelectedValue.ToString().Substring(37)}', " +
                $"'{comboBoxGenreTens.SelectedValue.ToString()}', '{comboBoxGenreOnes.SelectedValue.ToString()}', '{comboBoxFormat.SelectedValue.ToString().Substring(37)}', " +
                $"'{textBoxAuthorFName.Text}', '{textBoxAuthorMName.Text}', '{textBoxAuthorLName.Text}', " +
                $"'{textBoxDeweyDecimal.Text}', '{textBoxISBNTen.Text}', '{textBoxISBNThirteen.Text}', " +
                $"'{textBoxPublisher.Text}', '{textBoxPublicationYear.Text}', '{textBoxEdition.Text}', '{textBoxDescription.Text}')";
            command.ExecuteNonQuery();
            c.Close();
            this.DialogResult = true;
        }

        private int GenerateCopyID(string isbnThirteen)
        {
            isbnThirteen = textBoxISBNThirteen.Text;
            c.Open();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = $"SELECT [itemID], [copyID] FROM items WHERE itemID LIKE '%{isbnThirteen}-%' ORDER BY [copyID]";
            reader = command.ExecuteReader();
            int previous;
            int copyID = 1;
            try
            {
                reader.Read();
                previous = int.Parse(reader[1].ToString());
            }
            catch (InvalidOperationException) // If no IDs contain specified ISBN13 (isbnThirteen)
            {
                c.Close();
                return 1;
            }
            
            while (reader.Read()) // loop through ItemIDs and fill in gaps in suffixes if needed
            {
                int current = int.Parse(reader[1].ToString());
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
            c.Close();
            return copyID; // Also suffix of ItemID
        }
    }
}
