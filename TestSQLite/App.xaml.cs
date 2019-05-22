using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TestSQLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //
            // TODO: remove hard-coding
            var databaseService = new SQLiteDatabaseService(@"C:\\ProgramData\Kent Imaging\\KentImaging.db",
                Guid.Parse("3FA88031-A392-4639-BDBF-BE2887EE31E1"));

            AppSingleton.DatabaseService = databaseService;

            // TODO: remove hard-coding
            AppSingleton.CurrentUserId = Guid.Parse("3FA88031-A392-4639-BDBF-BE2887EE31E1");
        }
    }
}
