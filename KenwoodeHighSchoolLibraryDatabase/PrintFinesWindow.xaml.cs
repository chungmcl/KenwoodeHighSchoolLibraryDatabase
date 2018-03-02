﻿using System;
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
    /// Interaction logic for PrintFinesWindow.xaml
    /// </summary>
    public partial class PrintFinesWindow : Window
    {
        private OleDbConnection c;
        private OleDbCommand command;
        private OleDbDataReader reader;
        List<AccountWithFine> accountsWithFines;
        private int pageNumber;
        private int pageMax;
        public PrintFinesWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            accountsWithFines = new List<AccountWithFine>();
            pageNumber = 1;
            LoadAccountsWithFines();
            LoadDataGrid(1);
            buttonPreviousPage.IsEnabled = false;
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

        private void LoadAccountsWithFines()
        {
            c.Open();
            command.CommandText = "SELECT " +
                "[userID], [firstName], [lastName], [userType], [overdueItems], [fines] " +
                "FROM accounts " +
                "WHERE [fines] > 0";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                AccountWithFine awf = new AccountWithFine();
                awf.userID = reader[0].ToString();
                awf.name = $"{reader[1].ToString()}, {reader[2].ToString()}";
                awf.userType = reader[3].ToString();
                awf.overdue = (int)reader[4];
                awf.fines = (double)reader[5];
                accountsWithFines.Add(awf);
            }
            this.pageMax = (int)Math.Ceiling(((double)accountsWithFines.Count) / 37);
        }

        private void LoadDataGrid(int pageNumber)
        {
            dataGridFinedUsers.Items.Clear();
            if (accountsWithFines.Count > 0)
            {
                int startIndex = 0;
                if (pageNumber != 1)
                {
                    startIndex = ((pageNumber * 37) - 37);
                }
                for (int i = startIndex; i < accountsWithFines.Count && i < (pageNumber * 37); i++)
                {
                    dataGridFinedUsers.Items.Add(accountsWithFines[i]);
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
            printDlg.PrintVisual(dataGridFinedUsers, "Fined Users");
            printDlg.ShowDialog();
        }

        public class AccountWithFine
        {
            public double fines { get; set; }
            public int overdue { get; set; }
            public string userID { get; set; }
            public string name { get; set; }
            public string userType { get; set; }
        }
    }
}
