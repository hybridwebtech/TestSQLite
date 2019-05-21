using AuraAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataEntities;

namespace KentInterface
{
    

    

    /// <summary>
    /// Class to read and represent one dicom file
    /// </summary>
    public enum ImageBitsPerPixel { Eight, Sixteen, TwentyFour };

    public class DicomImage : IEquatable<DicomImage>, IComparable<DicomImage>, IDicomImage
    {
       
        const int VALUESPERPNGPIXEL = 2;

        // Collection of dicom tags
        //List<String> _dicomTags;

        // Flag indicating if the current DICOMImage is valid...
        bool _validDicomFile;
        private String _PrivateStudyDate;
        private String _StudyDate;
        private String _StudyTime;
        private String _SeriesDescription;
        private String _StudyDescription;
        private PatientInformation _patientInfo;
        private String _SensorBoardTemperature;
        private String _LEDBoardTemperature;

        // TODO: need to retrieve this from DICOM data
        private bool _drapeUsedAtCapture;


        private ImageType _ImageType;
        public ImageType TypeOfFile { get { return _ImageType; } }

        private DicomDecoder _dicomDecoder;

        public DicomImage()
        {
            _dicomDecoder = new DicomDecoder();
            _validDicomFile = false;
            _ImageType = ImageType.Undefined;
            _patientInfo = new PatientInformation();
        }

        public DicomImage CloneAndReplacePixels(List<List<ushort>> pixels, ImageType imageType, bool saveFile, String newFileName = "")
        {
            DicomImage clonedImage = Clone(pixels);

            if (imageType != ImageType.Undefined)
                clonedImage.SetImageType(imageType);

            // Save the file out to disk...
            if (newFileName == "")
            {
                newFileName = clonedImage.DetermineFileName();
            }

            if (saveFile)
            {
                
                clonedImage.SaveDicom16(newFileName);
                clonedImage.DicomFileName = newFileName;
            }
            return clonedImage;
        }

		public void ReplacePixels(UInt16[,] pixels, bool pixelsValuesAreScaled)
		{

			List<List<ushort>> pixelsList = ConvertUintToUList(pixels, pixelsValuesAreScaled);
			ReplacePixels(pixelsList);
		}

		public void ReplacePixels(List<List<ushort>> pixels)
        {
            if ((_dicomDecoder._bitsAllocated != 16) || (_dicomDecoder._samplesPerPixel != 1))
                throw new Exception("Invalid pixels for image: expected bits of 16 and samples per Pixel of 1.");

            this._dicomDecoder.SetPixels16(pixels);

        }
		/*
        public DicomImage CloneAndReplacePixels(DoubleMatrix pixelMatrix, ImageType imageType, bool saveFile, String newFileName = "")
        {
            List<List<ushort>> pixelsList = new List<List<ushort>>();
            for (int row = 0; row < pixelMatrix.Rows; row++)
            {
                DoubleVector pixelVector = pixelMatrix.Row(row);
                IDoubleEnumerator elements = pixelVector.GetDoubleEnumerator();
                List<ushort> pixels = new List<ushort>(pixelMatrix.Cols);
                
                while (elements.MoveNext())
                {
                    double currentValue = elements.Current;
                    if (Double.IsNaN(currentValue)) currentValue = 0;
                    if (Double.IsInfinity(currentValue)) currentValue = 4095;
                    if (currentValue < 0)
                        currentValue = 0;
                    else if (currentValue > 1)
                        currentValue = 4095;
                    else
                        currentValue = currentValue * 4095F;
                    pixels.Add(System.Convert.ToUInt16(currentValue));
                }
                pixelsList.Add(pixels);
            }
            return CloneAndReplacePixels(pixelsList, imageType, saveFile, newFileName);
        }

        public DicomImage CloneAndReplacePixels(DoubleVector pixelVector, ImageType imageType, bool pixelsValuesAreScaled, bool saveFile, String newFileName = "")
        {
            List<List<ushort>> pixelsList = new List<List<ushort>>();

            IDoubleEnumerator elements = pixelVector.GetDoubleEnumerator();
            List<ushort> pixels = new List<ushort>(pixelVector.Count());

			if (pixelsValuesAreScaled == false)
			{
				while (elements.MoveNext())
				{
					try
					{
						if (elements.Current < 0)
							pixels.Add(0);
						else
							pixels.Add(System.Convert.ToUInt16(elements.Current));
					}
					catch 
					{
						pixels.Add(4096);
					}
				}

			}
			else
			{

				int maxScaledValue = pixelsValuesAreScaled ? 1 : 4095;

				while (elements.MoveNext())
				{
					double currentValue = elements.Current;
					if (Double.IsNaN(currentValue)) currentValue = 0;
					if (Double.IsInfinity(currentValue)) currentValue = 4095;
					if (currentValue < 0)
						currentValue = 0;
					else if (currentValue > maxScaledValue)
						currentValue = 4095;
					else
						currentValue = pixelsValuesAreScaled ? currentValue * 4095F : currentValue;
					pixels.Add(System.Convert.ToUInt16(currentValue));
				}
			}
            pixelsList.Add(pixels);
            
            return CloneAndReplacePixels(pixelsList, imageType, saveFile, newFileName);
        }
		*/

		public DicomImage CloneAndReplacePixels(UInt16[,] pixelArray, ImageType imageType, bool pixelsValuesAreScaled, bool saveFile, String newFileName = "")
		{
			List<List<ushort>> pixelsList = ConvertUintToUList(pixelArray, pixelsValuesAreScaled);


			return CloneAndReplacePixels(pixelsList, imageType, saveFile, newFileName);
		}

		public List<List<ushort>> ConvertUintToUList(UInt16[,] pixelArray, bool pixelsValuesAreScaled)
		{
			int height = pixelArray.GetLength(0);
			int width = pixelArray.GetLength(1);
			int maxScaledValue = pixelsValuesAreScaled ? 1 : 4095;
			List<List<ushort>> pixelsList = new List<List<ushort>>();

			List<ushort> pixels = new List<ushort>(height * width);
			for (int row = 0; row < height; row++)
				for (int column = 0; column < width; column++)
				{
					ushort currentValue = pixelArray[row, column];
					if (pixelsValuesAreScaled == false)
					{
						try
						{
							if (currentValue < 0)
								pixels.Add(0);
							else
								pixels.Add(currentValue);
						}
						catch
						{
							pixels.Add(4096);
						}
					}
					else
					{
						try
						{
							if (Double.IsNaN(currentValue)) currentValue = 0;
							if (Double.IsInfinity(currentValue)) currentValue = 4095;
							if (currentValue < 0)
								currentValue = 0;
							else if (currentValue > maxScaledValue)
								currentValue = 4095;
							else
							{
								float resultF = pixelsValuesAreScaled ? currentValue * 4095F : currentValue;
								currentValue = (ushort)resultF;
							}
						}
						catch
						{
							currentValue = 0;
						}
						pixels.Add(currentValue);
					}
				}

			pixelsList.Add(pixels);

			return pixelsList;
		}

		public DicomImage CloneAndReplacePixels(double[,] pixelArray, ImageType imageType, bool pixelsValuesAreScaled, bool saveFile, String newFileName = "")
		{
			int height = pixelArray.GetLength(0);
			int width = pixelArray.GetLength(1);
			int maxScaledValue = pixelsValuesAreScaled ? 1 : 4095;
			List<List<ushort>> pixelsList = new List<List<ushort>>();

			List<ushort> pixels = new List<ushort>(height * width);
			for (int row = 0; row < height; row++)
				for (int column = 0; column < width; column++)
				{
					double currentValue = pixelArray[row, column];
					if (pixelsValuesAreScaled == false)
					{
						try
						{
							if (currentValue < 0)
								pixels.Add(0);
							else
								pixels.Add(System.Convert.ToUInt16(currentValue));
						}
						catch
						{
							pixels.Add(4096);
						}
					}
					else
					{
						try
						{
							if (Double.IsNaN(currentValue)) currentValue = 0;
							if (Double.IsInfinity(currentValue)) currentValue = 4095;
							if (currentValue < 0)
								currentValue = 0;
							else if (currentValue > maxScaledValue)
								currentValue = 4095;
							else
							{
								currentValue = pixelsValuesAreScaled ? currentValue * 4095F : currentValue;

							}
						}
						catch 
						{
							currentValue = 0;
						}
						pixels.Add(System.Convert.ToUInt16(currentValue));
		            }
				}

			pixelsList.Add(pixels);

			return CloneAndReplacePixels(pixelsList, imageType, saveFile, newFileName);
		}

        public DicomImage CloneAndReplaceColorPixels(byte[,] pixelArray, ImageType imageType, bool saveFile, String newFileName = "")
        {
            List<byte> pixels = new List<byte>(pixelArray.GetLength(0) * pixelArray.GetLength(1));
            for (int i = 0; i < pixelArray.GetLength(0); i++)
            {
                pixels.Add(pixelArray[i, (int)COLORID.RED]);
                pixels.Add(pixelArray[i, (int)COLORID.GREEN]);
                pixels.Add(pixelArray[i, (int)COLORID.BLUE]);

            }

            DicomImage clonedImage = CloneColorImage(pixels);

            if (imageType != ImageType.Undefined)
                clonedImage.SetImageType(imageType);

            // Save the file out to disk...
            if (newFileName == "")
            {
                newFileName = clonedImage.DetermineFileName();
            }

            if (saveFile)
            {

                clonedImage.SaveDicom16(newFileName);
                clonedImage.DicomFileName = newFileName;
            }
            return clonedImage;
        }

        public byte GetColorPixelValue(float row, float col, COLORID colorId)
        {
            return _dicomDecoder.GetColorPixelValue((int)row, (int)col, colorId);
        }


 
        public void SaveDicom16(String fileName = "")
        {
            // Save the file out to disk...
            if (fileName == "")
                if (DicomFileName == "")
                    fileName = DetermineFileName();
                else
                    fileName = DicomFileName;
            this._dicomDecoder.CopyandSaveDicomFile(fileName, _dicomDecoder.GetPixels16());
            DicomFileName = fileName;

        }


        public DicomImage Clone(List<List<ushort>> pixels =  null)
        {
            DicomImage clonedImage = new DicomImage();
            if (pixels != null)
                clonedImage._dicomDecoder = new DicomDecoder(this._dicomDecoder, pixels);

            else
                clonedImage._dicomDecoder = new DicomDecoder(this._dicomDecoder, _dicomDecoder.GetCopyPixels16());

            clonedImage._ImageType = this.TypeOfFile;
            clonedImage.DicomFileName = this.DicomFileName;


            // Set up Dicom Tags....
            clonedImage._StudyDate = this._StudyDate;
            clonedImage._PrivateStudyDate = this._PrivateStudyDate;
            clonedImage._StudyTime = this._StudyTime;
            clonedImage._SeriesDescription = this._SeriesDescription;
            clonedImage._StudyDescription = this._StudyDescription;
            clonedImage._patientInfo = this.PatientInfo;

            clonedImage._validDicomFile = true;

            return clonedImage;
        }

        public DicomImage CloneColorImage(List<byte> pixels = null)
        {
            DicomImage clonedImage = new DicomImage();
            if (pixels != null)
                clonedImage._dicomDecoder = new DicomDecoder(this._dicomDecoder, pixels);

            else
                clonedImage._dicomDecoder = new DicomDecoder(this._dicomDecoder, _dicomDecoder.GetCopyPixels16());

            clonedImage._ImageType = this.TypeOfFile;
            clonedImage.DicomFileName = this.DicomFileName;


            // Set up Dicom Tags....
            clonedImage._StudyDate = this._StudyDate;
            clonedImage._PrivateStudyDate = this._PrivateStudyDate;
            clonedImage._StudyTime = this._StudyTime;
            clonedImage._SeriesDescription = this._SeriesDescription;
            clonedImage._StudyDescription = this._StudyDescription;
            clonedImage._patientInfo = this.PatientInfo;

            clonedImage._validDicomFile = true;

            return clonedImage;
        }



        public void SetImageType(ImageType imageType)
        {
            _ImageType = imageType;
        }

        public bool LoadDicomFromStream(byte[] fileContents)
        {
            this._dicomDecoder.LoadDicomFromStream(fileContents);
            return InitializeMembers();
        }


        public bool ReadDicomFile(string fileName, bool lazyLoad = true)
        {
            // Open the DICOM file with our decoder...

            this._dicomDecoder.SetDicomFileName(fileName, lazyLoad);
            DicomFileName = fileName;


            return InitializeMembers();
        }



        public bool InitializeMembers()
        {
            if (!_dicomDecoder._dicmFound)
            {
                return false;
            }

            _StudyDate = this._dicomDecoder.FindTag("00080020");//.TrimStart(' '); ;
            _PrivateStudyDate = _dicomDecoder.FindTag("0008002A");//.TrimStart(' ');
            _StudyTime = _dicomDecoder.FindTag("00080030");

            // if Study date == "00000000", it means the study date was not filled so 
            // set the Study Date from the private study date
            if (_StudyDate.Contains("0000000"))
            {
                _StudyDate = _PrivateStudyDate.Substring(0, 9);
            }


            _SeriesDescription = _dicomDecoder.FindTag("0008103E");

            // FOR TEMPERATURE TEST!!!
            // If the file name starts with TempTest, use the filename as the description 
            String filenameWithoutPath = Path.GetFileName(DicomFileName);          
            if (filenameWithoutPath.StartsWith("TempTest_"))
            {
                _SeriesDescription = filenameWithoutPath;

            }

            _StudyDescription = _dicomDecoder.FindTag("0008103E");

            // Grab Sensor Board and LED Board Temperature
            _SensorBoardTemperature = _dicomDecoder.FindTag("00187001");
            _LEDBoardTemperature = _dicomDecoder.FindTag("00187002");


            InitializePatientInfo();

            _ImageType = DetermineImageType(DicomFileName);

            return true;
        }

        private void InitializePatientInfo()
        {
            _patientInfo.AccessionNo = _dicomDecoder.FindTag("00080050");

            String dateOfBirth = _dicomDecoder.FindTag("00100030");
            dateOfBirth = dateOfBirth.Trim(' ');
            if (dateOfBirth.Length >= 8)
            {
                _patientInfo.DOB_Day = dateOfBirth.Substring(6, 2);
                _patientInfo.DOB_Month = dateOfBirth.Substring(4, 2);
                _patientInfo.DOB_Year = dateOfBirth.Substring(0, 4);
            }

            String patientName = _dicomDecoder.FindTag("00100010");
            String[] nameParts = patientName.Split('^');
            if (nameParts.Length > 0)
                _patientInfo.Lastname = nameParts[0];
            if (nameParts.Length > 1)
                _patientInfo.Firstname = nameParts[1];
            if (nameParts.Length > 2)
                _patientInfo.Middlename = nameParts[2];

            _patientInfo.Gender = PatientInformation.ParseGenderString(_dicomDecoder.FindTag("00100040"));

            _patientInfo.PatientMRN = _dicomDecoder.FindTag("00100020");

            String referringPhysician = _dicomDecoder.FindTag("00080090");
            nameParts = referringPhysician.Split('^');
            if (nameParts.Length > 0)
                _patientInfo.ReferringPhysicianLastName = nameParts[0];
            if (nameParts.Length > 1)
                _patientInfo.ReferringPhysicianFirstName = nameParts[1];

           
        }

        public List<string> DicomTags { get { return _dicomDecoder._dicomInfo; } }

        public bool isValidDicomFile { get { return this._validDicomFile; } }


        // PNG Images from the Kent Camera are 2 times bigger in each dimension
        // because 4 pixels are required to store (RGGB). (2 x 2)
        public int ImageWidth
        {
            get
            {
                return _dicomDecoder._imageWidth;
            }
        }

        public int ImageHeight
        {
            get
            {
                return _dicomDecoder._imageHeight;
            }
        }

        public int BitDepth { get { return _dicomDecoder._bitsAllocated; } }
        public int SamplesPerPixel { get { return _dicomDecoder._samplesPerPixel; } }
        public double WinCentre { get { return _dicomDecoder._windowCentre; } }
        public double WinWidth { get { return _dicomDecoder._windowWidth; } }
        public bool SignedImage { get { return _dicomDecoder._signedImage; } }
        public double MaxPixelValue { get { return _dicomDecoder.MaxPixelValue; } }
        public double MinPixelValue { get { return _dicomDecoder.MinPixelValue; } }
        public List<byte> Pixels8 { get { return _dicomDecoder.GetPixels8(); } }
        public List<byte> Pixels24 { get { return _dicomDecoder.GetPixels24(); } }
        public int NumberofFrames { get { return _dicomDecoder._nImages; } }
        public String StudyDate { get { return this._StudyDate; } }
        public String KentStudyDate { get { return this._PrivateStudyDate; } }
        public String StudyTime { get { return this._StudyTime; } }
        public String SeriesDescription { get { return this._SeriesDescription; } set { _SeriesDescription = value; } }
        public String StudyDescription { get { return this._StudyDescription; } }

        public String SensorBoardTemperature { get { return this._SensorBoardTemperature; } }
        public String LEDBoardTemperature { get { return this._LEDBoardTemperature; } }
        public PatientInformation PatientInfo { get { return this._patientInfo; } }

        public String DicomFileName
        {
            get
            {
                if (_dicomDecoder != null)
                    return _dicomDecoder.DicomFileName;
                else
                    return "";
            }
            set
            {
                if (_dicomDecoder != null)
                    _dicomDecoder.DicomFileName = value;
            }
        }




        public List<ushort> GetPixels16(int nFrameNo)
        {
            
            return this._dicomDecoder.GetPixels16(nFrameNo);

            // we have more than one image so use the Frame Number specified....
        }

        public List<byte> ClonePngPixels()
        {

            return this._dicomDecoder.GetCopyPixels24();

        }



        public bool Equals(DicomImage other)
        {

            if (other == null) return false;
            if (this.DicomFileName.CompareTo(other.DicomFileName) != 0) return false;
            
            return true;
        }

        public int CompareTo(DicomImage other)
        {
            int compareDateResult = this._PrivateStudyDate.CompareTo(other._PrivateStudyDate);
            if (compareDateResult == 0)
            {
                // check study time..
                return this.StudyTime.CompareTo(other.StudyTime);
            }
            else
                return compareDateResult;
        }

        public int GetPixelValue(float row, float col, int frame = 0)
        {
            return _dicomDecoder.GetPixelValue((int)row, (int)col, frame ); 
        }



        private String DetermineFileName()
        {
            String extension = ((int)_ImageType).ToString();
            return DicomFileName.Substring(0, DicomFileName.Length - 1) + extension;
           
        }

        private ImageType DetermineImageType(String fileName)
        {
            //
            // TODO: use _dicomDecoder.FindTag("189528")
            // to get the image correction algorithm, and use to convert to the correct/appropriate
            // ImageType

            string imageAlgorithm = _dicomDecoder.FindTag("00189527");
            string algorithmVersion = _dicomDecoder.FindTag("00189528");
            if (!string.IsNullOrWhiteSpace(imageAlgorithm) || !string.IsNullOrWhiteSpace(algorithmVersion))
            {
                // TODO: use retrieved values.
                // TODO: Note that these string values appear to be null.
                // TODO: talk to Matt about this.
            }


            String extension = DicomFileName.Substring(DicomFileName.Length - 1, 1);
            if (extension.Equals("3"))
                return ImageType.WhiteRefs;
            else
                return (ImageType)Convert.ToInt16(extension);
        }

        public void SetColorPixelValue(int row, int col, int red, int green, int blue)
        {
            _dicomDecoder.SetColorPixelValue(row, col, red, green, blue);
        }

		public void ReplacePixels(List<ushort> pixels)
		{
			List<List<ushort>> pixelList = new List<List<ushort>>();
			pixelList.Add(pixels);
			ReplacePixels(pixelList);
		}

		public List<ushort> GetPixels(int wavelength)
		{
			return GetPixels16(wavelength);
		}
	}
}