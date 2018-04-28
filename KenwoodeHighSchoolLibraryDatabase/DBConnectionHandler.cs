using System.Data.OleDb;

namespace KenwoodeHighSchoolLibraryDatabase
{
    /// <summary>
    /// Contains all necessary components for initializing and interacting with the database file.
    /// </summary>
    public static class DBConnectionHandler
    {
        public static OleDbConnection c;
        public static OleDbDataReader reader;
        public static OleDbCommand command;

        /// <summary>
        /// Initialize the connection of the OleDbConnection components to the database file.
        /// </summary>
        public static void InitializeConnection()
        {
            c = new OleDbConnection
            {
                ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=|DataDirectory|" +
                "\\LibraryDatabase.mdb;Persist Security Info=True;User ID=admin;Jet OLEDB:Database Password=ExKr52F317K"
            };
            command = new OleDbCommand
            {
                Connection = c
            };
            reader = null;
        }
    }
}
