using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using DataEntities;
using KentInterface;

namespace TestSQLite
{
    public class SQLiteDatabaseService : IDatabaseService
    {
        private SQLiteConnection _conn;
        private Guid _currentUserId;

        public SQLiteDatabaseService(string connectionString, Guid currentUserId)
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
                System.Diagnostics.Debug.Assert(false, e.Message);
            }
        }

        private DateTime ParseDicomDateToDateTime(string dicomDate)
        {
            if (string.IsNullOrWhiteSpace(dicomDate)) throw new ArgumentNullException(nameof(dicomDate));
            dicomDate = dicomDate.Trim();

            string strYear  = dicomDate.Substring(0, 4);
            string strMonth = dicomDate.Substring(4, 2);
            string strDay   = dicomDate.Substring(6, 2);

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

        private T GetColumnValue<T>(IDataReader reader, string columnName)
        {
            System.Diagnostics.Debug.Assert(reader != null);
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(columnName));

            int columnIndex = reader.GetOrdinal(columnName);
            if (columnIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columnName));
            }

            return reader.IsDBNull(columnIndex) ? default(T) : (T) reader.GetValue(columnIndex);
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

            try
            {
                string strNow = DatabaseDateString(DateTime.Now);

                var cmd = _conn.CreateCommand();

                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE ID=@ID";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(user.ID));

                var exists = (long) cmd.ExecuteScalar();

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


                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }
        }

        public PatientInformation CreatePatient(string ID, string firstName, string middleName, string lastName, string dateOfBirth,
            string createdOn, string updatedOn, string createdBy, string updatedBy)
        {
            DateTime birthDate;
            DateTime.TryParse(dateOfBirth, out birthDate);
            return new PatientInformation()
            {
                PatientKey = ID,
                Firstname = firstName,
                Middlename = middleName,
                Lastname = lastName,
                DOB_Year = birthDate.Year.ToString(),
                DOB_Month = birthDate.Month.ToString(),
                DOB_Day = birthDate.Day.ToString(),
            };
        }

        public List<PatientInformation> RetrievePatients()
        {
            List<PatientInformation> list = new List<PatientInformation>();

            try
            {
                var cmd = _conn.CreateCommand();

                cmd.CommandText =
                    "SELECT ID, FirstName, MiddleName, LastName, DateOfBirth, CreatedOn, UpdatedOn, CreatedBy, UpdatedBy FROM Patients";
                cmd.Prepare();

                var reader = cmd.ExecuteReader();

                string ID;
                string firstName;
                string middleName;
                string lastName;
                string dateOfBirth;
                string createdOn;
                string updatedOn;
                string createdBy;
                string updatedBy;

                while (reader.Read())
                {
                    ID = GetColumnValue<string>(reader, "ID");
                    firstName = GetColumnValue<string>(reader, "FirstName");
                    middleName = GetColumnValue<string>(reader, "MiddleName");
                    lastName = GetColumnValue<string>(reader, "LastName");
                    dateOfBirth = GetColumnValue<string>(reader, "DateOfBirth");
                    createdOn = GetColumnValue<string>(reader, "CreatedOn");
                    updatedOn = GetColumnValue<string>(reader, "UpdatedOn");
                    createdBy = GetColumnValue<string>(reader, "CreatedBy");
                    updatedBy = GetColumnValue<string>(reader, "UpdatedBy");

                    var patient = CreatePatient(ID, firstName, middleName, lastName, dateOfBirth, createdOn, updatedOn,
                        createdBy, updatedBy);

                    list.Add(patient);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }

            return list;
        }

        public void SavePatient(PatientInformation patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            try
            {
                string strNow = DatabaseDateString(DateTime.Now);

                var cmd = _conn.CreateCommand();

                cmd.CommandText =
                    @"INSERT INTO Patients(ID, FirstName, MiddleName, LastName, DateOfBirth, CreatedOn, UpdatedOn, CreatedBy, UpdatedBy)
                  VALUES(@ID, @FirstName, @MiddleName, @LastName, @DateOfBirth, @CreatedOn, @UpdatedOn, @CreatedBy, @UpdatedBy)";

                DateTime dateOfBirth;
                DateTime.TryParse(patient.DateOfBirth, out dateOfBirth);

                cmd.Prepare();
                cmd.Parameters.AddWithValue("@ID", patient.PatientKey);
                cmd.Parameters.AddWithValue("@FirstName", patient.Firstname);
                cmd.Parameters.AddWithValue("@MiddleName", patient.Middlename);
                cmd.Parameters.AddWithValue("@LastName", patient.Lastname);
                cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.ToShortDateString());
                cmd.Parameters.AddWithValue("@CreatedOn", strNow);
                cmd.Parameters.AddWithValue("@UpdatedOn", strNow);
                cmd.Parameters.AddWithValue("@CreatedBy", DatabaseGuidString(_currentUserId));
                cmd.Parameters.AddWithValue("@UpdatedBy", DatabaseGuidString(_currentUserId));

                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }
        }

        public DicomStudy CreateStudy(string ID, string studyDate, string studyDescription, string patientId)
        {
            DateTime studyDateTime;
            DateTime.TryParse(studyDate, out studyDateTime);
            return new DicomStudy()
            {
                ID = Guid.Parse(ID),
                StudyDate = studyDateTime.ToShortDateString(),
                StudyDescription = studyDescription,
                PatientID = Guid.Parse(patientId),
            };
        }

        public List<DicomStudy> RetrievePatientStudyDetails(PatientInformation patient)
        {
            if (patient == null) throw new ArgumentNullException(nameof(patient));

            List<DicomStudy> list = new List<DicomStudy>();
            try
            {
                var cmd = _conn.CreateCommand();

                cmd.CommandText =
                    "SELECT ID, StudyDate, StudyDescription, PatientId FROM Study WHERE PatientId=@patientID";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@patientID", patient.PatientKey);

                var reader = cmd.ExecuteReader();

                string ID;
                string studyDate;
                string studyDescription;
                string patientId;

                while (reader.Read())
                {
                    ID = GetColumnValue<string>(reader, "ID");
                    studyDate = GetColumnValue<string>(reader, "StudyDate");
                    studyDescription = GetColumnValue<string>(reader, "StudyDescription");
                    patientId = GetColumnValue<string>(reader, "PatientId");

                    var study = CreateStudy(ID, studyDate, studyDescription, patientId);

                    list.Add(study);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }

            return list;
        }

        public DicomSeries CreateSeries(string ID, string seriesDate, string seriesDescription, DicomStudy study, byte[] thumbnail)
        {
            DateTime seriesDateTime;
            DateTime.TryParse(seriesDate, out seriesDateTime);
            var series = new DicomSeries(study.PatientID.ToString(), study, null)
            {
                ID = Guid.Parse(ID),
                SeriesDate = seriesDateTime.ToShortDateString(),
                SeriesTime = seriesDateTime.ToShortTimeString(),
                SeriesDescription = seriesDescription,
            };

            series.DicomSeriesDescription.Thumbnail = Convert.ToBase64String(thumbnail);

            return series;
        }

        public List<DicomSeries> RetrieveStudySeriesDetails(DicomStudy study)
        {
            if (study == null) throw new ArgumentNullException(nameof(study));

            List<DicomSeries> list = new List<DicomSeries>();
            try
            {
                var cmd = _conn.CreateCommand();

                cmd.CommandText =
                    @"SELECT ID, SeriesDate, SeriesDescription, StudyID, Thumbnail FROM Series WHERE StudyID=@studyID";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@studyID", study.ID.ToString());

                var reader = cmd.ExecuteReader();

                string ID;
                string seriesDate;
                string seriesDescription;
                string studyID;
                byte[] thumbnail;

                while (reader.Read())
                {
                    ID = GetColumnValue<string>(reader, "ID");
                    seriesDate = GetColumnValue<string>(reader, "SeriesDate");
                    seriesDescription = GetColumnValue<string>(reader, "SeriesDescription");
                    studyID = GetColumnValue<string>(reader, "StudyID");
                    thumbnail = GetColumnValue<byte[]>(reader, "Thumbnail");

                    var series = CreateSeries(ID, seriesDate, seriesDescription, study, thumbnail);

                    list.Add(series);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }

            return list;
        }

        public string RetrieveSeriesThumbnail(DicomSeries series)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));

            string thumbnail = "";
            try
            {
                var cmd = _conn.CreateCommand();

                cmd.CommandText =
                    @"SELECT ThumbnailBase64 FROM SeriesImage WHERE SeriesID=@seriesID AND ThumbnailBase64 IS NOT NULL";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@seriesID", series.ID.ToString());

                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    thumbnail = GetColumnValue<string>(reader, "ThumbnailBase64");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.Assert(false, e.Message);
            }

            return thumbnail;
        }

        public void SaveStudy(DicomStudy study, PatientInformation patient)
        {
            if (study == null) throw new ArgumentNullException(nameof(study));
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            string strNow = DatabaseDateString(DateTime.Now);

            var cmd = _conn.CreateCommand();

            cmd.CommandText = "INSERT INTO Study(ID, StudyDate, StudyDescription, PatientID) VALUES (@ID, @studydate, @studydescription, @patientID)";
            cmd.Prepare();

            Guid studyID = Guid.NewGuid();

            study.ID = studyID;

            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(studyID));
            cmd.Parameters.AddWithValue("@studydate", DatabaseDateString(GetDicomStudyDate(study)));
            cmd.Parameters.AddWithValue("@studydescription", study.StudyDescription);
            cmd.Parameters.AddWithValue("@patientID", patient.PatientKey);

            cmd.ExecuteNonQuery();
        }

        public void SaveSeries(DicomSeries series, DicomStudy study, PatientInformation patient, string studyFolderPath)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            if (study == null) throw new ArgumentNullException(nameof(study));
            if (patient == null) throw new ArgumentNullException(nameof(patient));
            if (string.IsNullOrWhiteSpace(patient.PatientKey)) throw new ArgumentNullException("patient.PatientKey");

            string strNow = DatabaseDateString(DateTime.Now);

            var pngFileList =
                System.IO.Directory.EnumerateFiles(studyFolderPath, series.SeriesFileName + "*.png").ToList();

            string pngFile = pngFileList.Count == 1 ? pngFileList[0] : "";

            byte[] thumbnail = Utilities.CreateThumbnailFromFile(pngFile, 64, 64);

            var cmd = _conn.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Series(ID, SeriesDate, SeriesDescription, StudyID, DrapeUsedAtCapture, Thumbnail) VALUES (@ID, @seriesdate, @seriesdescription, @studyID, @drapeusedatcapture, @thumbnail)";
            cmd.Prepare();

            Guid seriesID = Guid.NewGuid();

            series.ID = seriesID;

            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(series.ID));
            cmd.Parameters.AddWithValue("@seriesdate", DatabaseDateString(ParseDicomDateToDateTime(series.SeriesDate)));
            cmd.Parameters.AddWithValue("@seriesdescription", series.SeriesDescription);
            cmd.Parameters.AddWithValue("@studyID", DatabaseGuidString(study.ID));
            cmd.Parameters.AddWithValue("@drapeusedatcapture", series.DrapeUsedAtCapture ? 1 : 0);
            cmd.Parameters.AddWithValue("@drapeusedatcapture", series.DrapeUsedAtCapture ? 1 : 0);
            cmd.Parameters.Add("@thumbnail", DbType.Binary, 20).Value = thumbnail;

            cmd.ExecuteNonQuery();
        }

        public void SaveSeriesImageFile(DicomSeries series, string studyFolderPath, string imageFilename)
        {
            if (series == null) throw new ArgumentNullException(nameof(series));
            if (string.IsNullOrWhiteSpace(studyFolderPath)) throw new ArgumentNullException(nameof(studyFolderPath));
            if (string.IsNullOrWhiteSpace(imageFilename)) throw new ArgumentNullException(nameof(imageFilename));

            string imageFilePath = System.IO.Path.Combine(studyFolderPath, imageFilename);
            byte[] imageBytes = File.ReadAllBytes(imageFilePath);

            //string strBase64EncodedContents = System.Convert.ToBase64String(imageBytes);

            //string strBase64EncodedThumbnail = "";

            //if (imageFilename.EndsWith("png"))
            //{
            //    strBase64EncodedThumbnail = Utilities.CreateBase64EncodedThumbnailFromFile(imageFilePath, 64, 64);
            //}

            string strNow = DatabaseDateString(DateTime.Now);

            var cmd = _conn.CreateCommand();

            cmd.CommandText =
                @"INSERT INTO SeriesImage(ID, ImageType, Image, OriginalFilename, SeriesID)
                  VALUES (@ID, @imagetype, @image, @originalfilename, @seriesID)";
            cmd.Prepare();

            Guid imageID = Guid.NewGuid();

            string imageType = TranslateImageFilenameToImageType(imageFilename);

            cmd.Parameters.AddWithValue("@ID", DatabaseGuidString(imageID));
            cmd.Parameters.AddWithValue("@imagetype", imageType);
            cmd.Parameters.Add("@image", DbType.Binary, 20).Value = imageBytes;
            cmd.Parameters.AddWithValue("@drapeusedatcapture", series.DrapeUsedAtCapture ? 1 : 0);
            cmd.Parameters.AddWithValue("@originalfilename", imageFilename);
            cmd.Parameters.AddWithValue("@seriesID", DatabaseGuidString(series.ID));

            cmd.ExecuteNonQuery();
        }

        private string TranslateImageFilenameToImageType(string imageFilename)
        {
            if (string.IsNullOrWhiteSpace(imageFilename)) throw new ArgumentNullException(nameof(imageFilename));

            if (imageFilename.EndsWith(".png")) return "png";

            return imageFilename[imageFilename.Length - 1].ToString();
        }
    }
}
