
using AuraAPI;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using System.IO;

namespace KentInterface
{


	public class DicomSeriesDescription : IEquatable<DicomSeriesDescription>, IComparable<DicomSeriesDescription>
    {
        public static string DATABASE_VERSION = "KentDbver1.1.0.12";




        public string PatientID { get; set; }
        public string StudyID { get; set; }
		public string SeriesDate { get; set; }
		public string SeriesTime { get; set; }
		public string SeriesFileName { get; set; }
		public string SeriesID { get; set; }
		public string SeriesDescription { get; set; }
		public string StudyDescription { get; set; }
		public string KentStudyDate { get; set; }
        public string Thumbnail { get; set; }

		public DicomSeriesDescription()
		{
		}


		public bool Equals(DicomSeriesDescription other)
		{

			if (other == null) return false;
			if (this.SeriesFileName.CompareTo(other.SeriesFileName) != 0) return false;

			return true;
		}

		public int CompareTo(DicomSeriesDescription other)
		{
			int compareDateResult = this.KentStudyDate.CompareTo(other.KentStudyDate);
			if (compareDateResult == 0)
			{
				// check study time..
				return this.SeriesTime.CompareTo(other.SeriesTime);
			}
			else
				return compareDateResult;
		}

        internal bool LoadSeriesDescription(string patientDirectoryPath)
        {
            string seriesFullPath = Path.Combine(patientDirectoryPath, SeriesFileName);
            String tempDescription = GetSeriesDescription(seriesFullPath);
            if (tempDescription.Length > 0)
            {
                SeriesDescription = tempDescription;
                return true;
            }
            return false;
        }

        public static String GetSeriesDescription(string pathToSeriesFile)
        {
            try
            {
                
                string seriesDescriptionFile = pathToSeriesFile + DicomSeries.SERIES_FILE_EXTENSION;
                if (File.Exists(seriesDescriptionFile))
                {
                    // get the series description from the file
                    string[] seriesDetailsFromFile = System.IO.File.ReadAllLines(seriesDescriptionFile);
                    if (seriesDetailsFromFile.Length < 2) return "";
                    if (!seriesDetailsFromFile[0].StartsWith(DATABASE_VERSION)) // we don't support the version
                        return "";

                    return seriesDetailsFromFile[1];

                }
            }
            catch { }
            return "";
        }

        public void SaveSeriesDescription(string patientDirectoryPath)
        {
            string seriesFullPath = Path.Combine(patientDirectoryPath, SeriesFileName);
            // Get the name of the dicom file
            string seriesDescriptionFile = seriesFullPath + DicomSeries.SERIES_FILE_EXTENSION;

            // push the new description into the file
            String[] allRecords = new string[2];
            allRecords[0] = DATABASE_VERSION;
            allRecords[1] = SeriesDescription;
            System.IO.File.WriteAllLines(seriesDescriptionFile, allRecords);
        }


    }
}
