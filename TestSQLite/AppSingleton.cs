using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestSQLite
{
    public static class AppSingleton
    {
        public static DatabaseService DatabaseService { get; set; }

        public static string CurrentUserId { get; set; }
    }
}
