using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEntities;
using KentInterface;

namespace TestSQLite
{
    public interface IDatabaseService
    {
        User CreateUser(string userName, string userEmail, bool immediateSave);

        void SaveUser(User user);

        void SavePatient(PatientInformation patient);

        void SaveStudy(DicomStudy study, PatientInformation patient);

        void SaveSeries(DicomSeries series, DicomStudy study, PatientInformation patient);

        void SaveSeriesImageFile(DicomSeries series, string studyFolderPath, string imageFilename);
    }
}
