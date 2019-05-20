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
        private Guid _currentUserId;

        public DatabaseService(string connectionString, Guid currentUserId)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (currentUserId == Guid.Empty) throw new ArgumentNullException(nameof(currentUserId));

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

        private static string DatabaseDateString(DateTime dateTime)
        {
            return dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString();
        }

        private static string DatabaseGuidString(Guid guid)
        {
            return guid.ToString();
        }

        public User CreateUser(string userName, string userEmail, bool immediateSave)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentNullException(nameof(userName));
            if (string.IsNullOrWhiteSpace(userEmail)) throw new ArgumentNullException(nameof(userEmail));

            string strNow = DatabaseDateString(DateTime.Now);

            var user = new User()
            {
                ID = Guid.NewGuid(), Name = userName, Email = userEmail, CreatedOn = DateTime.Now,
                CreatedBy = _currentUserId, UpdatedOn = DateTime.Now, UpdatedBy = _currentUserId
            };

            if (immediateSave)
            {
                SaveUser(user);
            }

            return user;
        }

        public void SaveUser(User user)
        {
            if (user == null) throw  new ArgumentNullException(nameof(user));
            if (user.ID == Guid.Empty) throw new ArgumentOutOfRangeException("User.ID");

            string strNow = DatabaseDateString(DateTime.Now);

            var cmd = _conn.CreateCommand();

            cmd.CommandText = "SELECT COUNT(*) FROM User WHERE ID=@ID";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(user.ID));

            int exists = (int)cmd.ExecuteScalar();

            cmd = _conn.CreateCommand();
            if (exists > 0)
            {
                cmd.CommandText =
                    "UPDATE USER SET Name=@name, Email=@email, UpdatedOn=@updatedon, UpdatedBy=@updatedby WHERE ID=@ID";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@updatedon", strNow);
                cmd.Parameters.AddWithValue("updatedby", DatabaseGuidString(_currentUserId));
                cmd.Parameters.AddWithValue("@ID", user.ID);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO User(ID, Name, Email, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
                                    VALUES(@ID, @name, @email, @createdon, @createdby, @updatedon, @updatedby)";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@ID", user.ID);
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@createdon", strNow);
                cmd.Parameters.AddWithValue("@createdby", _currentUserId);
                cmd.Parameters.AddWithValue("@updatedon", strNow);
                cmd.Parameters.AddWithValue("updatedby", DatabaseGuidString(_currentUserId));
            }

            cmd.ExecuteNonQuery();
        }

        public void CreatePatient(PatientInformation patient)
        {
            if (patient == null ) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            string strNow = DatabaseDateString(DateTime.Now);

            var cmd = _conn.CreateCommand();

            cmd.CommandText =
                @"INSERT INTO Patients(ID, FirstName, MiddleName, LastName, CreatedOn, UpdatedOn, CreatedBy, UpdatedBy)
                  VALUES(@ID, @FirstName, @MiddleName, @LastName, @CreatedOn, @UpdatedOn, @CreatedBy, @UpdatedBy)";

            cmd.Prepare();
            cmd.Parameters.AddWithValue("@ID", patient.PatientKey);
            cmd.Parameters.AddWithValue("@FirstName", patient.Firstname);
            cmd.Parameters.AddWithValue("@MiddleName", patient.Middlename);
            cmd.Parameters.AddWithValue("@LastName", patient.Lastname);
            cmd.Parameters.AddWithValue("@CreatedOn", strNow);
            cmd.Parameters.AddWithValue("@UpdatedOn", strNow);
            cmd.Parameters.AddWithValue("@CreatedBy", _currentUserId);
            cmd.Parameters.AddWithValue("@UpdatedBy", _currentUserId);

            cmd.ExecuteNonQuery();
        }
    }
}
