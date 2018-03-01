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
    /// Interaction logic for PrintOutputReports.xaml
    /// </summary>
    public partial class PrintUpcomingDueWindow : Window
    {
        private OleDbConnection c;
        private OleDbCommand command;
        private OleDbDataReader reader;
        List<ItemDueThisWeek> itemsDueThisWeek;
        public PrintUpcomingDueWindow()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            itemsDueThisWeek = new List<ItemDueThisWeek>();
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

        private void LoadUpcomingDueDataGrid()
        {
            c.Open();
            DateTime aWeekFromToday = DateTime.Today.AddDays(7).AddHours(23.99999);
            command.CommandText = $"SELECT * FROM items";
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                DateTime read = Convert.ToDateTime(reader["dueDate"].ToString());
                if (read <= aWeekFromToday
                    && read >= DateTime.Now)
                {
                    // Add to itemsDueThisWeek
                }
            }
        }

        private struct ItemDueThisWeek
        {

        }
    }
}
