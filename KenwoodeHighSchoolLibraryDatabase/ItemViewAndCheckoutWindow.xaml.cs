using System;
using System.Data.OleDb;
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
    /// Interaction logic for ItemCheckoutWindow.xaml
    /// </summary>
    public partial class ItemViewAndCheckoutWindow : Window
    {
        OleDbConnection c;
        OleDbCommand command;
        OleDbDataReader reader; 
        string itemID;
        string deweyDecimal;
        string title;
        string authorLastName;
        string authorMiddleName;
        string authorFirstName;
        string genreClassOne;
        string genreClassTwo;
        string genreClassThree;
        string format;
        string currentlyCheckedOutBy;
        string isxx;
        string isbnTen;
        string publisher;
        string publicationYear;
        string edition;
        string description;
        string previousCheckedOutBy;
        public ItemViewAndCheckoutWindow(Item item)
        {
            c = new OleDbConnection();
            c.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K";
            command = new OleDbCommand();
            command.Connection = c;
            reader = null;
            itemID = item.itemID;
            isxx = itemID.Substring(0, 13);
            deweyDecimal = item.deweyDecimal;
            title = item.title;
            // author name cannot be determined by passed item
            genreClassOne = item.genre;
            format = item.format;
            currentlyCheckedOutBy = item.currentlyCheckedOutBy;
            LoadRemainingFields();
            InitializeComponent();
        }

        private void LoadRemainingFields()
        {
            c.Open();
            command.CommandText = "SELECT [authorLastName], [authorMiddleName], [authorFirstName], [ISBN10], " +
                "[genreClassTwo], [genreClassThree], [publisher], [publicationYear], [edition], [description], " +
                "[previousCheckedOutBy] " +
                $"FROM items WHERE [itemID] = {itemID}";
            command.CommandType = System.Data.CommandType.Text;
            reader = command.ExecuteReader();
            reader.Read();
            isbnTen = reader["ISBN10"].ToString();
            authorLastName = reader["authorLastName"].ToString();
            authorMiddleName = reader["authorMiddleName"].ToString();
            authorFirstName = reader["authorFirstName"].ToString();
            genreClassTwo = reader["genreClassTwo"].ToString();
            genreClassThree = reader["genreClassThree"].ToString();
            publisher = reader["publisher"].ToString();
            publicationYear = reader["publicationYear"].ToString();
            edition = reader["edition"].ToString();
            description = reader["description"].ToString();
            previousCheckedOutBy = reader["previousCheckedOutBy"].ToString();
            reader.Close();
            c.Close();
        }
    }
}
