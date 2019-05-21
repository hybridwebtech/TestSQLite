using AuraAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentInterface
{

    public class DicomStudy : IDicomStudy
    {
        private DicomStudyDescription _studyDescriptionDetails;
        public List<DicomSeries> SeriesInStudy = new List<DicomSeries>();  

        public String StudyDirectory { get { return _studyDescriptionDetails.StudyDirectory; } }

        public DicomStudy()
        {
            _studyDescriptionDetails = new DicomStudyDescription("");
        }

        public void InitializeFromFile(String fileDetails)
        {
            _studyDescriptionDetails = JsonConvert.DeserializeObject<DicomStudyDescription>(fileDetails);
            foreach (DicomSeriesDescription seriesDesc in _studyDescriptionDetails.SeriesInStudy)
            {
                DicomSeries dicomSeries = new DicomSeries("", this, null);
                dicomSeries.InitializeFromDescription(seriesDesc);
                SeriesInStudy.Add(dicomSeries);

            }

        }

        public DicomStudy(String studyDirectory, string studyId)
        {
            _studyDescriptionDetails = new DicomStudyDescription(studyDirectory);
            StudyId = studyId;
        }

        public void AddSeries(DicomSeries dicomSeries)
        {
            SeriesInStudy.Add(dicomSeries);
            _studyDescriptionDetails.AddSeries(dicomSeries);
        }


        public String StudyDateLongForm
        {
            get
            {
                return _studyDescriptionDetails.StudyDateLongForm;
            }
        }
        public String StudyDate
        {
            get
            {
                return _studyDescriptionDetails.StudyDate;
            }
            set
            {
                _studyDescriptionDetails.StudyDate = value;
            }
        }

        public String StudyDescription
        {
            get
            {
                return _studyDescriptionDetails.StudyDescription;
            }
            set
            {
                _studyDescriptionDetails.StudyDescription = value;
            }
        }


        public void Add(DicomSeries newSeries)
        {
            if (newSeries != null)
                SeriesInStudy.Add(newSeries);
        }

        public List<IDicomSeries> GetSeriesInStudy()
        {
            List<IDicomSeries> dicomSeriesList = new List<IDicomSeries>();
            foreach (DicomSeries series in SeriesInStudy)
                dicomSeriesList.Add((IDicomSeries)series);
            return dicomSeriesList;
        }

        public List<DicomSeries> GetInternalSeriesInStudy()
        {
            List<DicomSeries> dicomSeriesList = new List<DicomSeries>();
            foreach (DicomSeries series in SeriesInStudy)
                dicomSeriesList.Add(series);
            return dicomSeriesList;
        }

        internal string SerializeObject()
        {
            return JsonConvert.SerializeObject(_studyDescriptionDetails);
        }

        public String StudyId
        {
            get
            {
                return _studyDescriptionDetails.StudyId;
            }
            set
            {
                _studyDescriptionDetails.StudyId = value;
            }
        }

        public static void SortDicomStudyList(List<DicomStudy> list, bool sortSeries=false)
        {
            list.Sort(delegate (DicomStudy x, DicomStudy y)
            {
                return CompareDates(x, y);
            });

            if (!sortSeries)
                return;

            foreach (DicomStudy study in list)
            {
                SortSeriesList(study.SeriesInStudy);
            }
        }

        public static void SortSeriesList(List<DicomSeries> list)
        {
            list.Sort(delegate (DicomSeries x, DicomSeries y)
            {
                return DicomSeries.CompareDates(x, y);
            });
        }

        public static int CompareDates(DicomStudy x, DicomStudy y)
        {
            if (x.StudyDate == null && y.StudyDate == null) return 0;
            else if (x.StudyDate == null) return 1;
            else if (y.StudyDate == null) return -1;
            else return y.StudyDate.CompareTo(x.StudyDate);
        }
        public static int CompareDates(DicomStudy x, DicomSeries y)
        {
            if (x.StudyDate == null && y.SeriesDate == null) return 0;
            else if (x.StudyDate == null) return 1;
            else if (y.SeriesDate == null) return -1;
            else return y.SeriesDate.CompareTo(x.StudyDate);
        }
    }
    
}
