using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataEntities;
using KentInterface;
using Newtonsoft.Json;

namespace TestSQLite
{
    public class MainViewModel
    {
        private string _kentImagingDir = @"C:\ProgramData\Kent Imaging";

        private List<PatientInformation> _items = null;

        public MainViewModel()
        {
        }

        public void Process()
        {
            CreateUsers();

            ReadPatientListJSON();

            WritePatientObjectsToDb();

            ProcessPatientStudies();
        }

        private void CreateUsers()
        {
            //var user = AppSingleton.DatabaseService.CreateUser("keith", "keith@kentimaging.com", true);
            //user = AppSingleton.DatabaseService.CreateUser("pierre", "pierre@kentimaging.com", true);
        }

        private void ReadPatientListJSON()
        {
            using (StreamReader r = new StreamReader(Path.Combine(_kentImagingDir, "patientlist_KentDbver1.1.0.12.db")))
            {
                string json = r.ReadToEnd();
                _items = JsonConvert.DeserializeObject<List<PatientInformation>>(json);
            }
        }

        private void WritePatientObjectsToDb()
        {
            if (_items != null)
            {
                foreach (var patient in _items)
                {
                    if (string.IsNullOrWhiteSpace(patient.PatientKey))
                    {
                        patient.PatientKey = Guid.NewGuid().ToString();
                    }

                    AppSingleton.DatabaseService.CreatePatient(patient);
                }
            }
        }

        private void ProcessPatientStudies()
        {
            foreach (var patient in _items)
            {
                string studyPath = Path.Combine(_kentImagingDir, "Images", patient.FileDirectory);

                using (StreamReader r = new StreamReader(Path.Combine(studyPath, "studylist.txt")))
                {
                    string json = r.ReadToEnd();

                    json = json.Replace("KentDbver1.1.0.12\r\n", "");

                    bool isList = Regex.Matches(json, "SeriesInStudy").Count > 1;

                    if (isList)
                    {
                        json = "[" + json + "]";
                    }

                    List<DicomStudy> studyList;// = new List<DicomStudy>();

                    try
                    {
                        var study = JsonConvert.DeserializeObject<DicomStudy>(json);

                        studyList = new List<DicomStudy>();

                        studyList.Add(study);
                    }
                    catch (Exception e)
                    {
                        studyList = JsonConvert.DeserializeObject<List<DicomStudy>>(json);
                    }

                    foreach (var study in studyList)
                    {
                        if (study == null) continue;

                        WriteStudyObjectToDb(study, patient);

                        foreach (var series in study.SeriesInStudy)
                        {
                            WriteSeriesObjectToDb(series, study, patient);
                        }
                    }
                }
            }
        }

        private void WriteStudyObjectToDb(DicomStudy study, PatientInformation patient)
        {
            if (study != null && patient != null)
            {
                AppSingleton.DatabaseService.SaveStudy(study, patient);
            }
        }

        private void WriteSeriesObjectToDb(DicomSeries series, DicomStudy study, PatientInformation patient)
        {
            if (series != null && study != null && patient != null)
            {
                AppSingleton.DatabaseService.SaveSeries(series, study, patient);
            }
        }
    }
}
