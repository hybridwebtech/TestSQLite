
using AuraAPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TestSQLite;

namespace KentInterface
{
    
    public class DicomSeries : IEquatable<DicomSeries>, IComparable<DicomSeries>, IDicomSeries
    {
        private DicomSeriesDescription _seriesDescription = new DicomSeriesDescription();
        public DicomStudy ParentStudy { get; set; }
        public DicomImage _NIRImages;
        public DicomImage _STO2Image;
        public DicomImage _PNGImage;
        public DicomImage _HbDeoxyImage;
        public DicomImage _HbOxyImage;
        public DicomImage _TotalyOxyImage;
        public DicomImage _MelaninCorrectedImage;
        public DicomImage _WhiteRefImages;
        public DicomImage _eFuzzyRGBmage;
        public DicomImage _FuzzyStO2Images;
        public DicomImage _eColonRGBmage;
        public DicomImage _ColonStO2Images;

        // TODO: need to retrieve this from DICOM data
        public bool DrapeUsedAtCapture { get; private set; }

        public static string SERIES_FILE_EXTENSION = "_seriesdesc.txt";

        public Guid ID { get; set; }

        public String PatientID
        {
            get { return _seriesDescription.PatientID; }
            set { _seriesDescription.PatientID = value; }
        }
        public String StudyID
        {
            get { return _seriesDescription.StudyID; }
            set { _seriesDescription.StudyID = value; }
        }

        public String SeriesDate
        {
            get { return _seriesDescription.SeriesDate; }
            set { _seriesDescription.SeriesDate = value; }
        }

        public String SeriesTime
        {
            get { return _seriesDescription.SeriesTime; }
            set { _seriesDescription.SeriesTime = value; }
        }

        public String SeriesFileName
        {
            get { return _seriesDescription.SeriesFileName; }
            set { _seriesDescription.SeriesFileName = value; }
        }
        public String SeriesID
        {
            get { return _seriesDescription.SeriesID; }
            set { _seriesDescription.SeriesID = value; }
        }

        public String SeriesDescription
        {
            get { return _seriesDescription.SeriesDescription; }
            set { _seriesDescription.SeriesDescription = value; }
        }

        public String StudyDescription
        {
            get { return _seriesDescription.StudyDescription; }
            set { _seriesDescription.StudyDescription = value; }
        }

        public String KentStudyDate { get { return _seriesDescription.KentStudyDate; } }



        public DicomSeries() { }



        public DicomSeries(string patientID, DicomStudy parentStudy, DicomImage dicomImage)
        {
            PatientID = patientID;
            if (parentStudy != null)
            {
                ParentStudy = parentStudy;
                StudyID = parentStudy.StudyId;
            }
            InitializeFromDicom(dicomImage);
        }

        public void InitializeFromDicom(DicomImage dicomImage)
        {
            if (dicomImage == null) return;
            string seriesName = Path.GetFileName(dicomImage.DicomFileName);
            _seriesDescription.SeriesFileName = seriesName.Substring(0, seriesName.Length - 1);
            SeriesDescription = dicomImage.SeriesDescription;
            SeriesID = SeriesFileName;
            StudyDescription = dicomImage.StudyDescription;
            SeriesDate = dicomImage.StudyDate;
            SeriesTime = dicomImage.KentStudyDate;
            _seriesDescription.KentStudyDate = dicomImage.KentStudyDate;

            // set the thumbnail
            string thumbnailFilename = dicomImage.DicomFileName;
            thumbnailFilename = thumbnailFilename.Substring(0, thumbnailFilename.Length - 1) + "2.png";

            try
            {
                _seriesDescription.Thumbnail = Utilities.CreateBase64EncodedThumbnailFromFile(thumbnailFilename, 64, 64);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _seriesDescription.Thumbnail = "";
                // TODO - substitute: suitable blank thumbnail
            }

            SetDicomImage(dicomImage);
        }

        public String StudyDateLongForm
        {
            get
            {
                try
                {
                    int year = Convert.ToInt16(SeriesDate.Substring(0, 5));
                    int month = Convert.ToInt16(SeriesDate.Substring(5, 2));
                    int day = Convert.ToInt16(SeriesDate.Substring(7, 2));
                    DateTime dt = new DateTime(year, month, day);

                    return dt.ToLongDateString();
                }
                catch
                {
                    return SeriesDate;
                }
            }
        }

        internal void InitializeFromDescription(DicomSeriesDescription seriesDesc)
        {
            _seriesDescription = seriesDesc;
        }

        public int NIRImageHeight
		{
			get
			{
				if (_NIRImages == null)
					return 0;
				return _NIRImages.ImageHeight;
			}
		}

		public int NIRImageWidth
		{
			get
			{
				if (_NIRImages == null)
					return 0;
				return _NIRImages.ImageWidth;
			}
		}

		public int NumberofNIRFrames
		{
			get
			{
				if (_NIRImages == null)
					return 0;
				return _NIRImages.NumberofFrames;
			}
		}

		public void ReplaceImage(IDicomImage newImage)
        {
            SetDicomImage((DicomImage) newImage);
        }

        public void SetDicomImage(DicomImage dicomImage)
        {
            switch (dicomImage.TypeOfFile)
            {
                case ImageType.NIR:
                    _NIRImages = dicomImage;
                    break;
                case ImageType.ST02:
                    _STO2Image = dicomImage;
                    break;
                case ImageType.PNGImage:
                    _PNGImage = dicomImage;
                    break;
                case ImageType.HbDeoxy:
                    _HbDeoxyImage = dicomImage;
                    break;
                case ImageType.HbOxy:
                    _HbOxyImage = dicomImage;
                    break;
                case ImageType.TotalHbOxy:
                    _TotalyOxyImage = dicomImage;
                    break;

                case ImageType.MelaninCorrectedSt02:
                    _MelaninCorrectedImage = dicomImage;
                    break;

                case ImageType.WhiteRefs:
                    _WhiteRefImages = dicomImage;
                    break;

                case ImageType.eFuzzyRGB:
                    _eFuzzyRGBmage = dicomImage;
                    break;

                case ImageType.eColonRGB:
                    _eColonRGBmage = dicomImage;
                    break;

                case ImageType.FuzzyStO2:
                    _FuzzyStO2Images = dicomImage;
                    break;

                case ImageType.ColonStO2:
                    _ColonStO2Images = dicomImage;
                    break;

                default:
                    throw new Exception("Dicom File Type not set.");
            }

            DrapeUsedAtCapture = false;
        }

        public DicomImage GetDicomImage(ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.NIR:
                    return (_NIRImages);
                case ImageType.ST02:
                    return _STO2Image;
                case ImageType.PNGImage:
                    return _PNGImage;
                case ImageType.HbDeoxy:
                    return _HbDeoxyImage;
                case ImageType.HbOxy:
                    return _HbOxyImage;
                case ImageType.TotalHbOxy:
                    return _TotalyOxyImage;
                case ImageType.MelaninCorrectedSt02:
                    return _MelaninCorrectedImage;
                case ImageType.WhiteRefs:
                    return _WhiteRefImages;

                case ImageType.FuzzyStO2:
                    return _FuzzyStO2Images;

                case ImageType.ColonStO2:
                    return _ColonStO2Images;

                default:
                    throw new Exception("Dicom File Type not set.");
            }
        }
        public bool Equals(DicomSeries other)
        {

            if (other == null) return false;
            if (this.SeriesFileName.CompareTo(other.SeriesFileName) != 0) return false;

            return true;
        }

        public int CompareTo(DicomSeries other)
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

       
		public void SetSt02Image(UInt16[,] st02, bool valuesAreScaled = true, ImageType imageType = ImageType.ST02)
		{
			if (st02 == null)
				throw new Exception("Invalid St02 (null) passed to SetSt02Image.");

			DicomImage newSt02Dicom = _STO2Image.CloneAndReplacePixels(st02, imageType, valuesAreScaled, false);
			SetDicomImage(newSt02Dicom);
		}

		public void SetSt02Image(double[,] st02, bool valuesAreScaled = true, ImageType imageType = ImageType.ST02)
		{
			if (st02 == null)
				throw new Exception("Invalid St02 (null) passed to SetSt02Image.");

			DicomImage newSt02Dicom = _STO2Image.CloneAndReplacePixels(st02, imageType, valuesAreScaled, false);
			SetDicomImage(newSt02Dicom);
		}


        public void SetERGBImage(byte[,] colorPixels, ImageType imageType)
        {
            if (colorPixels == null)
                throw new ArgumentNullException("Invalid colorPixels passed to SetSt02Image.");

            DicomImage newSt02Dicom = this._PNGImage.CloneAndReplaceColorPixels(colorPixels, imageType, false);
            SetDicomImage(newSt02Dicom);
        }




        string IDicomSeries.ParentStudyDirectory
        {
            get
            {
                return ParentStudy.StudyDirectory;
            }
        }

        string IDicomSeries.SeriesFileName
        {
            get
            {
                return SeriesFileName;
            }
        }

        string IDicomSeries.SeriesID
        {
            get
            {
                return SeriesID;
            }
        }

        IDicomStudy IDicomSeries.ParentStudy
        {
            get
            {
                return (IDicomStudy)ParentStudy;
            }
        }

        // Return true if there is data corresponding to the image type
        public bool DataIsAvailableForImageType(ImageType imageType)
        {
            switch (imageType)
            {
                case ImageType.NIR:
                    return _NIRImages != null;
                case ImageType.ST02:
                    return _STO2Image != null;
                case ImageType.PNGImage:
                    return _PNGImage != null;
                case ImageType.MelaninCorrectedSt02:
                    return _MelaninCorrectedImage != null;
                case ImageType.HbDeoxy:
                    return _HbDeoxyImage != null;
                case ImageType.HbOxy:
                    return _HbOxyImage != null;
                case ImageType.TotalHbOxy:
                    return _TotalyOxyImage != null;
                case ImageType.WhiteRefs:
                    return _WhiteRefImages != null;

                case ImageType.FuzzyStO2:
                    return _FuzzyStO2Images != null;

                case ImageType.ColonStO2:
                    return _ColonStO2Images != null;

                default:
                    return false;
            }
        }
    

        //create a new image that is not bound to the series
        public DicomImage CloneDicomImage(ImageType imageType)
        {
            try
            {
                switch (imageType)
                {
                    case ImageType.NIR:
                        return _NIRImages.Clone();
                    case ImageType.ST02:
                        return _STO2Image.Clone();
                    case ImageType.PNGImage:
                        return _PNGImage.Clone();
                    case ImageType.MelaninCorrectedSt02:
                        return _MelaninCorrectedImage.Clone();
                    case ImageType.HbDeoxy:
                        return _HbDeoxyImage.Clone();
                    case ImageType.HbOxy:
                        return _HbOxyImage.Clone();
                    case ImageType.TotalHbOxy:
                        return _TotalyOxyImage.Clone();
                    case ImageType.WhiteRefs:
                        return _WhiteRefImages.Clone();

                    case ImageType.FuzzyStO2:
                        return _FuzzyStO2Images.Clone();

                    case ImageType.ColonStO2:
                        return _ColonStO2Images.Clone();

                    default:
                        return null;
                }
            }
            catch 
            {
                return null;
            }
        }

        //create a new image that is not bound to the series
        public IDicomImage CloneImage(ImageType imageType)
        {
            DicomImage clonedImage = CloneDicomImage(imageType);
            return (IDicomImage)clonedImage;

        }


        public IDicomImage GetImage(ImageType imageType)
        {
            return (IDicomImage)GetDicomImage(imageType);
        }

		public int GetNIRPixelValue(float row, float col, int frame = 0)
		{
			if (_NIRImages == null) return 0;

			return _NIRImages.GetPixelValue(row, col, frame);
		}


		public int PNGImageHeight
		{
			get
			{
				if (_PNGImage == null)
					return 0;
				return _PNGImage.ImageHeight;
			}
		}

		public int PNGImageWidth
		{
			get
			{
				if (_PNGImage == null)
					return 0;
				return _PNGImage.ImageWidth;
			}
		}

        public int GetRGBPixelValue(float row, float col, int frame = 0)
		{
			if (_PNGImage == null) return 0;

			return _PNGImage.GetPixelValue(row, col, frame);
		}
        public DicomSeriesDescription DicomSeriesDescription { get { return _seriesDescription; } }

        public bool LoadSeriesDescription(string patientDirectoryPath)
        {
            return _seriesDescription.LoadSeriesDescription(patientDirectoryPath);
        }

        public static String GetSeriesDescription(string pathToSeries)
        {
            return DicomSeriesDescription.GetSeriesDescription(pathToSeries);
        }

        public void SaveSeriesDescription(string pathToSeries)
        {
            _seriesDescription.SaveSeriesDescription(pathToSeries);
        }
        public string SerializeObject()
        {
            return JsonConvert.SerializeObject(_seriesDescription);
        }

        public DicomSeries(string json)
        {
            ParentStudy = null;
            _seriesDescription = JsonConvert.DeserializeObject<DicomSeriesDescription>(json);
        }

        public static int CompareDates(DicomSeries x, DicomSeries y)
        {
            if (x.SeriesDate == null && y.SeriesDate == null) return 0;
            else if (x.SeriesDate == null) return 1;
            else if (y.SeriesDate == null) return -1;
            else if (y.SeriesDate != x.SeriesDate)
                return y.SeriesDate.CompareTo(x.SeriesDate);
            else return y.SeriesTime.CompareTo(x.SeriesTime);
        }

    }
}
