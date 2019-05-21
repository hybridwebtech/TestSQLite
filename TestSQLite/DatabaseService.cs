﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEntities;
using KentInterface;

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

        private DateTime ParseDicomDateToDateTime(string dicomDate)
        {
            if (string.IsNullOrWhiteSpace(dicomDate)) throw new ArgumentNullException(nameof(dicomDate));
            dicomDate = dicomDate.Trim();

            string strYear  = dicomDate.Substring(0, 4);
            string strMonth = dicomDate.Substring(4, 2);
            string strDay   = dicomDate.Substring(7, 2);

            int year;
            int month;
            int day;
            if (int.TryParse(strYear, out year) && int.TryParse(strMonth, out month) && int.TryParse(strDay, out day))
            {
                return new DateTime(year, month, day);
            }

            return DateTime.MinValue;
        }

        private DateTime GetDicomStudyDate(DicomStudy study)
        {
            if (study == null) throw new ArgumentNullException(nameof(study));

            if (!string.IsNullOrWhiteSpace(study.StudyDateLongForm))
            {
                DateTime studyDate;
                if (!DateTime.TryParse(study.StudyDateLongForm, out studyDate))
                {
                    studyDate = ParseDicomDateToDateTime(study.StudyDate);
                }

                return studyDate;
            }
            else
            {
                return ParseDicomDateToDateTime(study.StudyDate);
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

            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE ID=@ID";
            cmd.Prepare();
            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(user.ID));

            var exists = (long)cmd.ExecuteScalar();

            bool userExists = exists > 0;

            cmd = _conn.CreateCommand();
            if (userExists)
            {
                cmd.CommandText =
                    "UPDATE Users SET Name=@name, Email=@email, UpdatedOn=@updatedon, UpdatedBy=@updatedby WHERE ID=@ID";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@updatedon", strNow);
                cmd.Parameters.AddWithValue("updatedby", DatabaseGuidString(_currentUserId));
                cmd.Parameters.AddWithValue("@ID", user.ID);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO Users(ID, Name, Email, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
                                    VALUES(@ID, @name, @email, @createdon, @createdby, @updatedon, @updatedby)";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(user.ID));
                cmd.Parameters.AddWithValue("@name", user.Name);
                cmd.Parameters.AddWithValue("@email", user.Email);
                cmd.Parameters.AddWithValue("@createdon", strNow);
                cmd.Parameters.AddWithValue("@createdby", DatabaseGuidString(_currentUserId));
                cmd.Parameters.AddWithValue("@updatedon", strNow);
                cmd.Parameters.AddWithValue("updatedby", DatabaseGuidString(_currentUserId));
            }

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw;
            }
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
            cmd.Parameters.AddWithValue("@CreatedBy", DatabaseGuidString(_currentUserId));
            cmd.Parameters.AddWithValue("@UpdatedBy", DatabaseGuidString(_currentUserId));

            cmd.ExecuteNonQuery();
        }

        public void SaveStudy(DicomStudy study, PatientInformation patient)
        {
            if (study == null) throw new ArgumentNullException(nameof(study));
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            string strNow = DatabaseDateString(DateTime.Now);

            var cmd = _conn.CreateCommand();

            cmd.CommandText = "INSERT INTO Study(ID, StudyDate, StudyDescription, PatientID) VALUES ( @ID, @studydate, @studydescription, @patientID)";
            cmd.Prepare();

            Guid studyID = Guid.NewGuid();

            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(studyID));
            cmd.Parameters.AddWithValue("@studydate", DatabaseDateString(GetDicomStudyDate(study)));
            cmd.Parameters.AddWithValue("@studydescription", study.StudyDescription);
            cmd.Parameters.AddWithValue("@patientID", patient.PatientKey);

            cmd.ExecuteNonQuery();
        }
    }
}
