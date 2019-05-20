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
            var databaseService = new DatabaseService(@"C:\\ProgramData\Kent Imaging\\KentImaging.db", "kmontgomery");

            AppSingleton.DatabaseService = databaseService;
        }
    }
}
