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

        private void LoadGenreDeweyDecimal()
        {
            string column = comboBoxGenreHundreds.Text;
            c.Open();
            command.CommandText = $"SELECT {column} FROM deweyDecimals";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                selectedColumnValues.Add(reader.ToString());
            }

            //comboBoxGenreTens.Items.Add();
        }
    }
}
