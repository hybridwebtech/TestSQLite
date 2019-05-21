using AuraAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentInterface
{

    public class DicomStudyDescription 
    {
        public List<DicomSeriesDescription> SeriesInStudy = new List<DicomSeriesDescription>();
        public String StudyDate { get; set; }
        public String StudyDescription { get; set; }

        public String StudyDateLongForm {
            get {
                try
                {
                    int year = Convert.ToInt16(StudyDate.Substring(0, 5));
                    int month = Convert.ToInt16(StudyDate.Substring(5, 2));
                    int day = Convert.ToInt16(StudyDate.Substring(7, 2));
                    DateTime dt = new DateTime(year, month, day);

                    return dt.ToLongDateString();
                }
                catch 
                {
                    return StudyDate;
                }
            }
        }

        private String _studyDirectory;
        public String StudyDirectory { get { return _studyDirectory; } }

        public DicomStudyDescription(String studyDirectory)
        {
            _studyDirectory = studyDirectory;
        }

        public void AddSeries(DicomSeries newSeries)
        {
            if (newSeries == null) return;

            SeriesInStudy.Add(newSeries.DicomSeriesDescription);
        }

        public List<DicomSeriesDescription> GetSeriesInStudy()
        {
            List <DicomSeriesDescription> dicomSeriesList = new List<DicomSeriesDescription>();
            foreach (DicomSeriesDescription series in SeriesInStudy)
                dicomSeriesList.Add(series);
            return dicomSeriesList;
        }

        public string StudyId { get; set; }
    }
    
}
