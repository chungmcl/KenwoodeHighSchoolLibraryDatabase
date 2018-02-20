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

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Interaction logic for BookRegistrationWindow.xaml
    /// </summary>
    public partial class BookRegistrationWindow : Window
    {
        public BookRegistrationWindow()
        {
            InitializeComponent();
        }

        private string ConvertToISBNThirteen(string isbnTen)
        {
            // Append 978 as prefix and calculate ISBN 13 Checksum to append as suffix
            string isbnThirteen = "978" + isbnTen;
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
            isbnThirteen = isbnThirteen + checkSum;
            return isbnThirteen;
        }
    }
}
