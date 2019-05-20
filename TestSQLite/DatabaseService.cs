using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEntities;

namespace TestSQLite
{
    public class DatabaseService
    {
        private SQLiteConnection _conn;
        private string _currentUserId;

        public DatabaseService(string connectionString, string currentUserId)
        {
            if (string.IsNullOrWhiteSpace(currentUserId)) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(currentUserId)) throw new ArgumentNullException(nameof(currentUserId));

            _currentUserId = currentUserId;

            try
            {
                SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
                builder.DataSource = connectionString;

                _conn = new SQLiteConnection(builder.ToString());

                _conn.Open();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void CreatePatient(PatientInformation patient)
        {
            if (patient == null ) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            var cmd = _conn.CreateCommand();

            cmd.CommandText =
                @"INSERT INTO Patients(ID, FirstName, MiddleName, LastName, CreatedOn, UpdatedOn, CreatedBy, UpdatedBy)
                  VALUES(@ID, @FirstName, @MiddleName, @LastName, @CreatedOn, @UpdatedOn, @CreatedBy, @UpdatedBy)";

            int index = cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@ID"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@FirstName"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@MiddleName"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@LastName"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@CreatedOn"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@UpdatedOn"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@CreatedBy"));
            cmd.Parameters.Add(new SQLiteParameter(DbType.String, "@UpdatedBy"));

            DateTime now = DateTime.Now;

            string strNow = now.ToShortDateString() + " " + now.ToShortTimeString();

            cmd.Parameters["@ID"].Value = patient.PatientKey;
            cmd.Parameters["@FirstName"].Value = patient.Firstname;
            cmd.Parameters["@MiddleName"].Value = patient.Middlename;
            cmd.Parameters["@LastName"].Value = patient.Lastname;
            cmd.Parameters["@CreatedOn"].Value = strNow;
            cmd.Parameters["@UpdatedOn"].Value = strNow;
            cmd.Parameters["@CreatedBy"].Value = _currentUserId;
            cmd.Parameters["@UpdatedBy"].Value = _currentUserId;

            cmd.ExecuteNonQuery();
        }
    }
}
