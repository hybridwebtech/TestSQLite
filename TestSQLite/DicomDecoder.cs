using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;

// Program to decode a DICOM image.
// Written by Amarnath S, Mahesh Reddy S, Bangalore, India, April 2009.
// Updated by Harsha T, Apr 2010.
// Updated by Amarnath S, July 2010, to include Ultrasound images of 8-bit depth, 3 
//   samples per pixel. This was proposed by Dott. Guiseppe Marchi, www.peppedotnet.it and
//   www.sharepointcommunity.it
// Updated by Amarnath S, Dec 2010, to fix a bug with respect to displaying images containing 
//   pixel values between -32768 and 32767.
// Updated Aug 2012, to accommodate rescale-slope and rescale-intercept.

// Inspired heavily by ImageJ

namespace KentInterface
{
    public enum TypeOfDicomFile
    {
        NotDicom,
        Dicom3File,
        DicomOldTypeFile,
        DicomUnknownTransferSyntax
    };

    // Values for Color Pixel Value Lookup
    public enum COLORID
    {
        RED = 0,
        GREEN = 1,
        BLUE = 2
    }

    class DicomDecoder
    {
        const uint PIXEL_REPRESENTATION = 0x00280103;
        const uint TRANSFER_SYNTAX_UID = 0x00020010;
        const uint MODALITY = 0x00080060;
        const uint SLICE_THICKNESS = 0x00180050;
        const uint SLICE_SPACING = 0x00180088;
        const uint SAMPLES_PER_PIXEL = 0x00280002;
        const uint PHOTOMETRIC_INTERPRETATION = 0x00280004;
        const uint PLANAR_CONFIGURATION = 0x00280006;
        const uint NUMBER_OF_FRAMES = 0x00280008;
        const uint ROWS = 0x00280010;
        const uint COLUMNS = 0x00280011;
        const uint PIXEL_SPACING = 0x00280030;
        const uint BITS_ALLOCATED = 0x00280100;
        const uint WINDOW_CENTER = 0x00281050;
        const uint WINDOW_WIDTH = 0x00281051;
        const uint RESCALE_INTERCEPT = 0x00281052;
        const uint RESCALE_SLOPE = 0x00281053;
        const uint RED_PALETTE = 0x00281201;
        const uint GREEN_PALETTE = 0x00281202;
        const uint BLUE_PALETTE = 0x00281203;
        const uint ICON_IMAGE_SEQUENCE = 0x00880200;
        const uint PIXEL_DATA = 0x7FE00010;

        const string ITEM = "FFFEE000";
        const string ITEM_DELIMITATION = "FFFEE00D";
        const string SEQUENCE_DELIMITATION = "FFFEE0DD";

        const int
            AE = 0x4145,
            AS = 0x4153,
            AT = 0x4154,
            CS = 0x4353,
            DA = 0x4441,
            DS = 0x4453,
            DT = 0x4454,
            FD = 0x4644,
            FL = 0x464C,
            IS = 0x4953,
            LO = 0x4C4F,
            LT = 0x4C54,
            PN = 0x504E,
            SH = 0x5348,
            SL = 0x534C,
            SS = 0x5353,
            ST = 0x5354,
            TM = 0x544D,
            UI = 0x5549,
            UL = 0x554C,
            US = 0x5553,
            UT = 0x5554,
            OB = 0x4F42,
            OW = 0x4F57,
            SQ = 0x5351,
            UN = 0x554E,
            QQ = 0x3F3F,
            RT = 0x5254;
        const int ID_OFFSET = 128;  //location of "DICM"
        const int IMPLICIT_VR = 0x2D2D; // '--' 
        const String DICM = "DICM";

        public int _bitsAllocated;
        public int _imageWidth;
        public int _imageHeight;
        public int _offset;
        public int _nImages;
        public int _samplesPerPixel;
        public double _pixelDepth = 1.0;
        public double _pixelWidth = 1.0;
        public double _pixelHeight = 1.0;
        public string _unit;
        public double _windowCentre, _windowWidth;
        public bool _signedImage;
        public TypeOfDicomFile _typeofDicomFile;
        public List<string> _dicomInfo;
        public bool _dicmFound; // "DICM" found at offset 128

        DicomDictionary dic;
        BinaryReader file;
        BinaryWriter _fileToWriteTo;
        public String DicomFileName { get; set; }
        String _photoInterpretation;
        bool littleEndian = true;
        bool oddLocations;  // one or more tags at odd locations
        bool bigEndianTransferSyntax = false;
        bool inSequence;
        bool _widthTagFound;
        bool _heightTagFound;
        bool _pixelDataTagFound;
        int location = 0;
        int elementLength;
        int vr;  // Value Representation
        int min8 = Byte.MinValue;
        int max8 = Byte.MaxValue;

        int pixelRepresentation;
        double _rescaleIntercept;
        double _rescaleSlope;
        byte[] reds;
        byte[] greens;
        byte[] blues;
        byte[] vrLetters = new byte[2];
        List<byte> _pixels8;
        List<byte> _pixels24; // 8 bits bit depth, 3 samples per pixel
        List<List<ushort>> _pixels16;

        // Flag indicating if pixels have been read from the file yet..
        private bool _PixelsHaveBeenRead;

        private double _minPixelValue;
        public double MinPixelValue {  get { return _minPixelValue; } }
        private double _maxPixelValue;

        public double MaxPixelValue { get { return _maxPixelValue; } }

        public void SetPixels8(List<byte> pixels8)
        {
            _pixels8 = pixels8;
        }
        public void SetPixels24(List<byte> pixels24)
        {
            _pixels24 = pixels24;
        }
        public void SetPixels16(List<List<ushort>> pixels16)
        {
            _pixels16 = pixels16;
        }

        public DicomDecoder()
        {
            dic = new DicomDictionary();
            _signedImage = false;
            _dicomInfo = new List<string>();
            InitializeDicom();
        }

        public DicomDecoder(DicomDecoder dicomDecoder, List<List<ushort>> newPixels)
        {
            CopyFromClone(dicomDecoder);

            _pixels16 = newPixels;
            _typeofDicomFile = dicomDecoder._typeofDicomFile;
            _PixelsHaveBeenRead = true;
        }

        public DicomDecoder(DicomDecoder dicomDecoder, List<byte> pixels24)
        {
            CopyFromClone(dicomDecoder);

            _pixels24 = pixels24;
            _typeofDicomFile = dicomDecoder._typeofDicomFile;
            _PixelsHaveBeenRead = true;
        }

        private void CopyFromClone(DicomDecoder dicomDecoder)
        {
            _bitsAllocated = dicomDecoder._bitsAllocated;
            _imageWidth = dicomDecoder._imageWidth;
            _imageHeight = dicomDecoder._imageHeight;
            _offset = dicomDecoder._offset;
            _nImages = dicomDecoder._nImages;
            _samplesPerPixel = dicomDecoder._samplesPerPixel;
            _photoInterpretation = dicomDecoder._photoInterpretation;
            _unit = dicomDecoder._unit;
            _windowCentre = dicomDecoder._windowCentre;
            _windowWidth = dicomDecoder._windowWidth;
            _signedImage = dicomDecoder._signedImage;
            _widthTagFound = dicomDecoder._widthTagFound;
            _heightTagFound = dicomDecoder._heightTagFound;
            _pixelDataTagFound = dicomDecoder._pixelDataTagFound;
            _rescaleIntercept = dicomDecoder._rescaleIntercept;
            _rescaleSlope = dicomDecoder._rescaleSlope;
        }
        
        void InitializeDicom()
        {
            _bitsAllocated = 0;
            _imageWidth = 1;
            _imageHeight = 1;
            _offset = 1;
            _nImages = 1;
            _samplesPerPixel = 1;
            _photoInterpretation = "";
            _unit = "mm";
            _windowCentre = 0;
            _windowWidth = 0;
            _signedImage = false;
            _widthTagFound = false;
            _heightTagFound = false;
            _pixelDataTagFound = false;
            _rescaleIntercept = 0.0; // Default value
            _rescaleSlope = 1.0; // Default value
            _pixels16 = new List<List<ushort>>();
            _typeofDicomFile = TypeOfDicomFile.NotDicom;
            _PixelsHaveBeenRead = false;
        }

        public void SetDicomFileName(String filename, bool lazyLoad = false)
        {
            try
            {
                DicomFileName = filename;
                InitializeDicom();

                // Thanks to CodeProject member Alphons van der Heijden for 
                //   suggesting to add this - FileAccess.Read  (July 2010)
                file = new BinaryReader(File.Open(DicomFileName, FileMode.Open, FileAccess.Read));
                location = 0; // Reset the location
                _dicomInfo.Clear();

                bool readResult = ReadFileInfo();
                if (readResult && _widthTagFound && _heightTagFound && _pixelDataTagFound)
                {

                    if (_dicmFound == true)
                        _typeofDicomFile = TypeOfDicomFile.Dicom3File;
                    else
                        _typeofDicomFile = TypeOfDicomFile.DicomOldTypeFile;
                    if (!lazyLoad)
                        ReadPixels();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
            finally
            {
                if (file != null)
                    file.Close();
            }
        }

        public void LoadDicomFromStream(byte[] fileContents)
        {
            try
            {
                if (DicomFileName == "")
                    DicomFileName = "NOTSET";
                InitializeDicom();

                MemoryStream stream = new MemoryStream(fileContents);
                file = new BinaryReader(stream);
                //file = new BinaryReader(File.Open(DicomFileName, FileMode.Open, FileAccess.Read));
                location = 0; // Reset the location
                _dicomInfo.Clear();

                bool readResult = ReadFileInfo();
                if (readResult && _widthTagFound && _heightTagFound && _pixelDataTagFound)
                {

                    if (_dicmFound == true)
                        _typeofDicomFile = TypeOfDicomFile.Dicom3File;
                    else
                        _typeofDicomFile = TypeOfDicomFile.DicomOldTypeFile;
                    ReadPixels();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
            finally
            {
                if (file != null)
                    file.Close();
            }
        }

        private void LazyLoadPixels()
        {
            // check to see if the pixels have been read yet.
            if (this._PixelsHaveBeenRead == true) return;

            // Pixels haven't been read so reopen the file....
            file = new BinaryReader(File.Open(DicomFileName, FileMode.Open, FileAccess.Read));
            try
            {
                ReadPixels();
                this._PixelsHaveBeenRead = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading Pixels: " + ex.ToString());
            }
            finally
            {
                file.Close();
            }
           
        }

        public void ComputeWindowLevel()
        {
            LazyLoadPixels();

            if (_typeofDicomFile == TypeOfDicomFile.Dicom3File ||
                _typeofDicomFile == TypeOfDicomFile.DicomOldTypeFile)
            {


                if (_samplesPerPixel == 1 && _bitsAllocated == 8)
                {

                    _minPixelValue = _pixels8.Min();
                    _maxPixelValue = _pixels8.Max();

                    // Bug fix dated 24 Aug 2013 - for proper window/level of signed images
                    // Thanks to Matias Montroull from Argentina for pointing this out.
                    if (_signedImage)
                    {
                        _windowCentre -= char.MinValue;
                    }

                    if (Math.Abs(_windowWidth) < 0.001)
                    {
                        _windowWidth = _maxPixelValue - _minPixelValue;
                    }

                    if ((_windowCentre == 0) ||
                        (_minPixelValue > _windowCentre) || (_maxPixelValue < _windowCentre))
                    {
                        _windowCentre = (_maxPixelValue + _minPixelValue) / 2;
                    }

                }

                if (_samplesPerPixel == 1 && _bitsAllocated == 16)
                {

                    bool first = true;
                    for (int i = 0; i < _pixels16.Count; i++)
                    {
                        if (first)
                        {
                            _minPixelValue = _pixels16[i].Min();
                            _maxPixelValue = _pixels16[i].Max();
                            first = false;
                        }
                        else
                        {
                            if (_pixels16[i].Min() < _minPixelValue) _minPixelValue = _pixels16[i].Min();
                            if (_pixels16[i].Max() > _maxPixelValue) _maxPixelValue = _pixels16[i].Max();
                        }
                    }



                    // Bug fix dated 24 Aug 2013 - for proper window/level of signed images
                    // Thanks to Matias Montroull from Argentina for pointing this out.
                    if (_signedImage)
                    {
                        _windowCentre -= short.MinValue;
                    }

                    if (Math.Abs(_windowWidth) < 0.001)
                    {
                        _windowWidth = _maxPixelValue - _minPixelValue;
                    }

                    if ((_windowCentre == 0) ||
                        (_minPixelValue > _windowCentre) || (_maxPixelValue < _windowCentre))
                    {
                        _windowCentre = (_maxPixelValue + _minPixelValue) / 2;
                    }

                   

                }

                
            }
            else
            {
                // Show a plain grayscale image instead
                _pixels8.Clear();
                _pixels16.Clear();
                _pixels24.Clear();
                _samplesPerPixel = 1;
                _bitsAllocated = 8;

                _imageWidth = 480; // hardcoded value...
                _imageHeight = 750; // 
                int iNoPix = _imageWidth * _imageHeight;

                for (int i = 0; i < iNoPix; ++i)
                {
                    _pixels8.Add(240);// 240 is the grayvalue corresponding to the Control colour
                }
                _windowWidth = 256;
                _windowCentre = 127;
            }
        }


        internal List<List<ushort>> GetPixels16()
        {
            LazyLoadPixels();
            return this._pixels16;
        }

        internal List<ushort> GetPixels16(int nFrameNo)
        {
            LazyLoadPixels();
            if (this._pixels16.Count > 0)
                return this._pixels16[nFrameNo];
            else
                return null;
        }

        internal List<List<ushort>> GetCopyPixels16()
        {
            LazyLoadPixels();
            return new List<List<ushort>>(_pixels16);
        }

        internal int GetPixelValue(int row, int col, int frame)
        {

           

            LazyLoadPixels();
            if (_samplesPerPixel == 1 && _bitsAllocated == 8)
            {
                if (row * _imageWidth + col > _imageHeight * _imageWidth) return -1;
                return _pixels8[row * _imageWidth + col];
            }
            else if (_samplesPerPixel == 1 && _bitsAllocated == 16)
            {
				if (frame >= _nImages) return -1;
				if (row * _imageWidth + col > _imageHeight * _imageWidth) return -1;
                return _pixels16[frame][row * _imageWidth + col];
            }
            else if (_samplesPerPixel == 3 && _bitsAllocated == 8)
            {
                //if (row * _imageWidth + col > _imageHeight * _imageWidth * 3) return -1;
               // return _pixels24[row * _imageWidth + (col * _samplesPerPixel ) + frame];
				return GetColorPixelValue(row, col, (COLORID)frame);


			}
            return -1;
                        
        }

        
        internal void SetColorPixelValue(int row, int col, int red, int green, int blue)
        {
            if ((_samplesPerPixel != 3) || (_bitsAllocated != 8))
                throw (new Exception("Invalid dicom image passed to SetColorPixelValue! (Invalid Samples or BitDepth)"));
            LazyLoadPixels();

            int index = row * (_imageWidth * _samplesPerPixel) + (col * _samplesPerPixel);

            if (index + (int)COLORID.BLUE > _imageHeight * _imageWidth * 3) return;
            _pixels24[index] = Convert.ToByte(red);
            _pixels24[index + (int)COLORID.GREEN] = Convert.ToByte(green);
            _pixels24[index + (int)COLORID.BLUE] = Convert.ToByte(blue);

        }

        public byte GetColorPixelValue(int row, int col, COLORID colorId)
        {

            LazyLoadPixels();

			if ((_samplesPerPixel == 3) && (_bitsAllocated == 8))
			{	
				int index = row * (_imageWidth * _samplesPerPixel) + (col * _samplesPerPixel) + (int)colorId;
				if (index > _imageHeight * _imageWidth * 3) return 0;
				return _pixels24[index];
			}
			if ((_samplesPerPixel == 1) && (_bitsAllocated == 16))
			{
				// color images have 4 ushorts per color value when _bitsAllocated == 16
				// Also assume that there is only 1 frame!
				int index = (row * _imageWidth * 2) + (col * 2);

				if (index >= _imageHeight * _imageWidth)
					return 0;

				// we have a valid index representing the pixel to retrieve the color for...
				switch (colorId)
				{
					case COLORID.RED:
						return (byte)(_pixels16[0][index] / 4095.0 * 255);
					case COLORID.GREEN:
						return (byte)(_pixels16[0][index+3] / 4095.0 * 255);
					case COLORID.BLUE:
						return (byte)(_pixels16[0][index+1] / 4095.0 * 255);
				}
				
			}

			throw (new Exception("Invalid dicom image passed to GetColorPixelValue! (Invalid Samples or BitDepth)"));
		}

        public void CopyandSaveDicomFile (String fileToSave, List<List<ushort>> pixels16)
        {
             
            try
            {

                // copy the current file 
                File.Copy(DicomFileName, fileToSave, true);
                DicomFileName = fileToSave;

                // open the new copy of the file and write the updated pixels...
                _fileToWriteTo = new BinaryWriter(File.Open(DicomFileName, FileMode.Open, FileAccess.Write));

                for (int i = 0; i < pixels16.Count; i++)
                    WritePixels16(pixels16[i], i);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calculating ST: " + ex.ToString());
            }
            finally
            {
                if (_fileToWriteTo != null) _fileToWriteTo.Close();
            }
        }


        public void GetPixels8(ref List<byte> pixels)
        {
            LazyLoadPixels();
            pixels = _pixels8;
        }

        public void GetPixels16(ref List<List<ushort>> pixels)
        {
            LazyLoadPixels();
            pixels = _pixels16;

            // we have more than one image so use the Frame Number specified....
        }

        public void GetPixels24(ref List<byte> pixels)
        {
            LazyLoadPixels();
            pixels = _pixels24;
        }

        

        String GetString(int length)
        {
            byte[] buf = new byte[length];
            file.BaseStream.Position = location;
            int count = file.Read(buf, 0, length);
            location += length;
            string s = System.Text.ASCIIEncoding.ASCII.GetString(buf);
            return s;
        }

        byte GetByte() // Changed return type to byte
        {
            file.BaseStream.Position = location;
            byte b = file.ReadByte();
            ++location;
            return b;
        }

        ushort GetShort() // Changed return type to ushort
        {
            byte b0 = GetByte();
            byte b1 = GetByte();
            ushort s;
            if (littleEndian)
                s = Convert.ToUInt16((b1 << 8) + b0);
            else
                s = Convert.ToUInt16((b0 << 8) + b1);
            return s;
        }

        int GetInt()
        {
            byte b0 = GetByte();
            byte b1 = GetByte();
            byte b2 = GetByte();
            byte b3 = GetByte();
            int i;
            if (littleEndian)
                i = (b3 << 24) + (b2 << 16) + (b1 << 8) + b0;
            else
                i = (b0 << 24) + (b1 << 16) + (b2 << 8) + b3;
            return i;
        }

        double GetDouble()
        {
            byte b0 = GetByte();
            byte b1 = GetByte();
            byte b2 = GetByte();
            byte b3 = GetByte();
            byte b4 = GetByte();
            byte b5 = GetByte();
            byte b6 = GetByte();
            byte b7 = GetByte();

            long res = 0;
            if (littleEndian)
            {
                res += b0;
                res += (((long)b1) << 8);
                res += (((long)b2) << 16);
                res += (((long)b3) << 24);
                res += (((long)b4) << 32);
                res += (((long)b5) << 40);
                res += (((long)b6) << 48);
                res += (((long)b7) << 56);
            }
            else
            {
                res += b7;
                res += (((long)b6) << 8);
                res += (((long)b5) << 16);
                res += (((long)b4) << 24);
                res += (((long)b3) << 32);
                res += (((long)b2) << 40);
                res += (((long)b1) << 48);
                res += (((long)b0) << 56);
            }

            double d = Convert.ToDouble(res, new CultureInfo("en-US"));
            return d;
        }

        float GetFloat()
        {
            byte b0 = GetByte();
            byte b1 = GetByte();
            byte b2 = GetByte();
            byte b3 = GetByte();

            int res = 0;

            if (littleEndian)
            {
                res += b0;
                res += (((int)b1) << 8);
                res += (((int)b2) << 16);
                res += (((int)b3) << 24);
            }
            else
            {
                res += b3;
                res += (((int)b2) << 8);
                res += (((int)b1) << 16);
                res += (((int)b0) << 24);
            }

            float f1;
            f1 = Convert.ToSingle(res, new CultureInfo("en-US"));
            return f1;
        }

        byte[] GetLut(int length)
        {
            if ((length & 1) != 0) // odd
            {
                String dummy = GetString(length);
                return null;
            }

            length /= 2;
            byte[] lut = new byte[length];
            for (int i = 0; i < length; ++i)
                lut[i] = Convert.ToByte(GetShort() >> 8);
            return lut;
        }

        int GetLength()
        {
            byte b0 = GetByte();
            byte b1 = GetByte();
            byte b2 = GetByte();
            byte b3 = GetByte();

            // Cannot know whether the VR is implicit or explicit without the 
            // complete Dicom Data Dictionary. 
            vr = (b0 << 8) + b1;

            switch (vr)
            {
                case OB:
                case OW:
                case SQ:
                case UN:
                case UT:
                    // Explicit VR with 32-bit length if other two bytes are zero
                    if ((b2 == 0) || (b3 == 0)) return GetInt();
                    // Implicit VR with 32-bit length
                    vr = IMPLICIT_VR;
                    if (littleEndian)
                        return ((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
                    else
                        return ((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);
                // break; // Not necessary
                case AE:
                case AS:
                case AT:
                case CS:
                case DA:
                case DS:
                case DT:
                case FD:
                case FL:
                case IS:
                case LO:
                case LT:
                case PN:
                case SH:
                case SL:
                case SS:
                case ST:
                case TM:
                case UI:
                case UL:
                case US:
                case QQ:
                case RT:
                    // Explicit vr with 16-bit length
                    if (littleEndian)
                        return ((b3 << 8) + b2);
                    else
                        return ((b2 << 8) + b3);
                default:
                    // Implicit VR with 32-bit length...
                    vr = IMPLICIT_VR;
                    if (littleEndian)
                        return ((b3 << 24) + (b2 << 16) + (b1 << 8) + b0);
                    else
                        return ((b0 << 24) + (b1 << 16) + (b2 << 8) + b3);
            }
        }

        int GetNextTag()
        {
            int groupWord = GetShort();
            if (groupWord == 0x0800 && bigEndianTransferSyntax)
            {
                littleEndian = false;
                groupWord = 0x0008;
            }
            int elementWord = GetShort();
            int tag = groupWord << 16 | elementWord;

            elementLength = GetLength();

            // Hack to read some GE files
            if (elementLength == 13 && !oddLocations)
                elementLength = 10;

            // "Undefined" element length.
            // This is a sort of bracket that encloses a sequence of elements.
            if (elementLength == -1)
            {
                elementLength = 0;
                inSequence = true;
            }
            return tag;
        }

        String GetHeaderInfo(int tag, String value)
        {
            string str = tag.ToString("X8");
            if (str == ITEM_DELIMITATION || str == SEQUENCE_DELIMITATION)
            {
                inSequence = false;
                return null;
            }

            string id = null;

            if (dic.dict.ContainsKey(str))
            {
                id = dic.dict[str];
                if (id != null)
                {
                    if (vr == IMPLICIT_VR)
                        vr = (id[0] << 8) + id[1];
                    id = id.Substring(2);
                }
            }

            if (str == ITEM)
                return (id != null ? id : ":null");
            if (value != null)
                return id + ": " + value;

            switch (vr)
            {
                case FD:
                    for (int i = 0; i < elementLength; ++i)
                        GetByte();
                    break;
                case FL:
                    for (int i = 0; i < elementLength; i++)
                        GetByte();
                    break;
                case AE:
                case AS:
                case AT:
                case CS:
                case DA:
                case DS:
                case DT:
                case IS:
                case LO:
                case LT:
                case PN:
                case SH:
                case ST:
                case TM:
                case UI:
                    value = GetString(elementLength);
                    break;
                case US:
                    if (elementLength == 2)
                        value = Convert.ToString(GetShort());
                    else
                    {
                        value = "";
                        int n = elementLength / 2;
                        for (int i = 0; i < n; i++)
                            value += Convert.ToString(GetShort()) + " ";
                    }
                    break;
                case IMPLICIT_VR:
                    value = GetString(elementLength);
                    if (elementLength > 44)
                        value = null;
                    break;
                case SQ:
                    value = "";
                    bool privateTag = ((tag >> 16) & 1) != 0;
                    if (tag != ICON_IMAGE_SEQUENCE && !privateTag)
                        break;
                    goto default;
                default:
                    location += elementLength;
                    value = "";
                    break;
            }

            if (value != null && id == null && value != "")
                return "---: " + value;
            else if (id == null)
                return null;
            else
                return id + ": " + value;
        }

        void AddInfo(int tag, string value)
        {
            string info = GetHeaderInfo(tag, value);

            string str = tag.ToString("X");
            string strPadded = str.PadLeft(8, '0');
            string strInfo;
            if (inSequence && info != null && vr != SQ)
                info = ">" + info;
            if (info != null && str != ITEM)
            {
                if (info.Contains("---"))
                    strInfo = info.Replace("---", "Private Tag");
                else
                    strInfo = info;

                _dicomInfo.Add(strPadded + "//" + strInfo);
            }
        }

        void AddInfo(int tag, int value)
        {
            AddInfo(tag, Convert.ToString(value));
        }

        void GetSpatialScale(String scale)
        {
            double xscale = 0, yscale = 0;
            int i = scale.IndexOf('\\');
            if (i == 1) // Aug 2012, Fixed an issue found while opening some images
            {
                yscale = Convert.ToDouble(scale.Substring(0, i), new CultureInfo("en-US"));
                xscale = Convert.ToDouble(scale.Substring(i + 1), new CultureInfo("en-US"));
            }
            if (xscale != 0.0 && yscale != 0.0)
            {
                _pixelWidth = xscale;
                _pixelHeight = yscale;
                _unit = "mm";
            }
        }

        public bool ReadFileInfo()
        {
            long skipCount = Convert.ToInt32(ID_OFFSET);
            _bitsAllocated = 16;

            file.BaseStream.Seek(skipCount, SeekOrigin.Begin);
            location += ID_OFFSET;

            if (GetString(4) != DICM)
            {
                // This is for reading older DICOM files (Before 3.0)
                // Seek from the beginning of the file
                file.BaseStream.Seek(0, SeekOrigin.Begin);
                location = 0;

                // Older DICOM files do not have the preamble and prefix
                _dicmFound = false;

                // Continue reading further.
                // See whether the width, height and pixel data tags
                // are present. If these tags are present, then it we conclude that this is a 
                // DICOM file, because all DICOM files should have at least these three tags.
                // Otherwise, it is not a DICOM file.
            }
            else
            {
                // We have a DICOM 3.0 file
                _dicmFound = true;
            }

            bool decodingTags = true;
            _samplesPerPixel = 1;
            int planarConfiguration = 0;
            _photoInterpretation = "";
            string modality;

            while (decodingTags)
            {
                int tag = GetNextTag();
                if ((location & 1) != 0)
                    oddLocations = true;

                if (inSequence)
                {
                    AddInfo(tag, null);
                    continue;
                }

                string s;
                switch (tag)
                {
                    case (int)(TRANSFER_SYNTAX_UID):
                        s = GetString(elementLength);
                        AddInfo(tag, s);
                        if (s.IndexOf("1.2.4") > -1 || s.IndexOf("1.2.5") > -1)
                        {
                            file.Close();
                            _typeofDicomFile = TypeOfDicomFile.DicomUnknownTransferSyntax;
                            // Return gracefully indicating that this type of 
                            // Transfer Syntax cannot be handled
                            return false;
                        }
                        if (s.IndexOf("1.2.840.10008.1.2.2") >= 0)
                            bigEndianTransferSyntax = true;
                        break;
                    case (int)MODALITY:
                        modality = GetString(elementLength);
                        AddInfo(tag, modality);
                        break;
                    case (int)(NUMBER_OF_FRAMES):
                        s = GetString(elementLength);
                        AddInfo(tag, s);
                        double frames = Convert.ToDouble(s, new CultureInfo("en-US"));
                        if (frames > 1.0)
                            _nImages = (int)frames;
                        break;
                    case (int)(SAMPLES_PER_PIXEL):
                        _samplesPerPixel = GetShort();
                        AddInfo(tag, _samplesPerPixel);
                        break;
                    case (int)(PHOTOMETRIC_INTERPRETATION):
                        _photoInterpretation = GetString(elementLength);
                        _photoInterpretation = _photoInterpretation.Trim();
                        AddInfo(tag, _photoInterpretation);
                        break;
                    case (int)(PLANAR_CONFIGURATION):
                        planarConfiguration = GetShort();
                        AddInfo(tag, planarConfiguration);
                        break;
                    case (int)(ROWS):
                        _imageHeight = GetShort();
                        AddInfo(tag, _imageHeight);
                        _heightTagFound = true;
                        break;
                    case (int)(COLUMNS):
                        _imageWidth = GetShort();
                        AddInfo(tag, _imageWidth);
                        _widthTagFound = true;
                        break;
                    case (int)(PIXEL_SPACING):
                        String scale = GetString(elementLength);
                        GetSpatialScale(scale);
                        AddInfo(tag, scale);
                        break;
                    case (int)(SLICE_THICKNESS):
                    case (int)(SLICE_SPACING):
                        String spacing = GetString(elementLength);
                        _pixelDepth = Convert.ToDouble(spacing, new CultureInfo("en-US"));
                        AddInfo(tag, spacing);
                        break;
                    case (int)(BITS_ALLOCATED):
                        _bitsAllocated = GetShort();
                        AddInfo(tag, _bitsAllocated);
                        break;
                    case (int)(PIXEL_REPRESENTATION):
                        pixelRepresentation = GetShort();
                        AddInfo(tag, pixelRepresentation);
                        break;
                    case (int)(WINDOW_CENTER):
                        String center = GetString(elementLength);
                        int index = center.IndexOf('\\');
                        if (index != -1) center = center.Substring(index + 1);
                        _windowCentre = Convert.ToDouble(center, new CultureInfo("en-US"));
                        AddInfo(tag, center);
                        break;
                    case (int)(WINDOW_WIDTH):
                        String widthS = GetString(elementLength);
                        index = widthS.IndexOf('\\');
                        if (index != -1) widthS = widthS.Substring(index + 1);
                        _windowWidth = Convert.ToDouble(widthS, new CultureInfo("en-US"));
                        AddInfo(tag, widthS);
                        break;
                    case (int)(RESCALE_INTERCEPT):
                        String intercept = GetString(elementLength);
                        _rescaleIntercept = Convert.ToDouble(intercept, new CultureInfo("en-US"));
                        AddInfo(tag, intercept);
                        break;
                    case (int)(RESCALE_SLOPE):
                        String slop = GetString(elementLength);
                        _rescaleSlope = Convert.ToDouble(slop, new CultureInfo("en-US"));
                        AddInfo(tag, slop);
                        break;
                    case (int)(RED_PALETTE):
                        reds = GetLut(elementLength);
                        AddInfo(tag, elementLength / 2);
                        break;
                    case (int)(GREEN_PALETTE):
                        greens = GetLut(elementLength);
                        AddInfo(tag, elementLength / 2);
                        break;
                    case (int)(BLUE_PALETTE):
                        blues = GetLut(elementLength);
                        AddInfo(tag, elementLength / 2);
                        break;
                    case (int)(PIXEL_DATA):
                        // Start of image data...
                        if (elementLength != 0)
                        {
                            _offset = location;
                            AddInfo(tag, location);
                            decodingTags = false;
                        }
                        else
                            AddInfo(tag, null);
                        _pixelDataTagFound = true;
                        break;
                    default:
                        AddInfo(tag, null);
                        break;
                }
            }
            return true;
        }


        void ReadPixels()
        {
            if (_samplesPerPixel == 1 && _bitsAllocated == 8)
            {
                if (_pixels8 != null)
                    _pixels8.Clear();
                _pixels8 = new List<byte>();
                int numPixels = _imageWidth * _imageHeight;
                byte[] buf = new byte[numPixels];
                file.BaseStream.Position = _offset;
                file.Read(buf, 0, numPixels);

                for (int i = 0; i < numPixels; ++i)
                {
                    int pixVal = (int)(buf[i] * _rescaleSlope + _rescaleIntercept);
                    // We internally convert all 8-bit images to the range 0 - 255
                    //if (photoInterpretation.Equals("MONOCHROME1", StringComparison.OrdinalIgnoreCase))
                    //    pixVal = 65535 - pixVal;
                    if (_photoInterpretation == "MONOCHROME1")
                        pixVal = max8 - pixVal;

                    _pixels8.Add((byte)(pixelRepresentation == 1 ? pixVal : (pixVal - min8)));
                }
            }

            if (_samplesPerPixel == 1 && _bitsAllocated == 16)
            {
                if (_pixels16 != null)
                    _pixels16.Clear();

                for (int i = 0; i < _nImages; i++)
                    _pixels16.Add(ReadPixels16(i));

            }

            // 30 July 2010 - to account for Ultrasound images
            if (_samplesPerPixel == 3 && _bitsAllocated == 8)
            {
                _signedImage = false;
                if (_pixels24 != null)
                    _pixels24.Clear();
                _pixels24 = new List<byte>();
                int numPixels = _imageWidth * _imageHeight;
                int numBytes = numPixels * _samplesPerPixel;
                byte[] buf = new byte[numBytes];
                file.BaseStream.Position = _offset;
                file.Read(buf, 0, numBytes);

                for (int i = 0; i < numBytes; ++i)
                {
                    _pixels24.Add(buf[i]);
                }
            }
            this._PixelsHaveBeenRead = true;
        }


        private List<ushort> ReadPixels16(int frameNoToRead)
        {
            List<ushort> oneimagePixels16 = new List<ushort>();
            List<int> pixels16Int = new List<int>();
            int numPixels = _imageWidth * _imageHeight;
            byte[] bufByte = new byte[numPixels * 2];
            byte[] signedData = new byte[2];
            file.BaseStream.Position = _offset + (frameNoToRead * numPixels*2);
            file.Read(bufByte, 0, numPixels * 2);
            ushort unsignedS;
            int i, i1, pixVal;
            byte b0, b1;

            for (i = 0; i < numPixels; ++i)
            {
                i1 = i * 2;
                b0 = bufByte[i1];
                b1 = bufByte[i1 + 1];
                unsignedS = Convert.ToUInt16((b1 << 8) + b0);
                if (pixelRepresentation == 0) // Unsigned
                {
                    pixVal = (int)(unsignedS * _rescaleSlope + _rescaleIntercept);
                    if (_photoInterpretation == "MONOCHROME1")
                        pixVal = ushort.MaxValue - pixVal;
                }
                else  // Pixel representation is 1, indicating a 2s complement image
                {
                    signedData[0] = b0;
                    signedData[1] = b1;
                    short sVal = System.BitConverter.ToInt16(signedData, 0);

                    // Need to consider rescale slope and intercepts to compute the final pixel value
                    pixVal = (int)(sVal * _rescaleSlope + _rescaleIntercept);
                    if (_photoInterpretation == "MONOCHROME1")
                        pixVal = ushort.MaxValue - pixVal;
                }
                pixels16Int.Add(pixVal);
            }

            int minPixVal = pixels16Int.Min();
            _signedImage = false;
            if (minPixVal < 0) _signedImage = true;

            // Use the above pixel data to populate the list pixels16 
            foreach (int pixel in pixels16Int)
            {
                // We internally convert all 16-bit images to the range 0 - 65535
                if (_signedImage)
                    oneimagePixels16.Add((ushort)(pixel - short.MinValue));
                else
                    oneimagePixels16.Add((ushort)(pixel));
            }

            return oneimagePixels16;
        }

        private void WritePixels16(List<ushort> pixels16, int frameNoToWrite)
        {
            // allocate array for the pixels to write out
            int numPixels = _imageWidth * _imageHeight;
            byte[] bufByte = new byte[numPixels * 2];
            
            // convert data from ushort to bytes
            for (int i = 0; i < pixels16.Count; ++i)
            {
                byte[] byteArray = BitConverter.GetBytes(pixels16[i]);
                Buffer.BlockCopy(byteArray, 0, bufByte, i * 2, 2);
                                
            }

            // now write the data to the file
            
            _fileToWriteTo.BaseStream.Position = _offset + (frameNoToWrite * numPixels * 2);
            _fileToWriteTo.Write(bufByte, 0, numPixels * 2);

        }

        internal void GetPixels16(ref List<ushort> pixels, int nFrameNo)
        {
            if (_nImages == 0)
            {
                throw new Exception("No data has been loaded.  No images found.");
            }

            if (nFrameNo + 1 > _nImages)
                nFrameNo = _nImages - 1;
            pixels = _pixels16[nFrameNo];
        }

        // Pull a specific tag from the list of DICOM Tags...
        public String FindTag(String tagId)
        {
            for (int i = 0; i < _dicomInfo.Count(); i++)
            {
                if (_dicomInfo[i].StartsWith(tagId))
                {
                    // Get the tag Name and Value with the tag code (e.g. 0080020//) stripped 
                    String tagValue = _dicomInfo[i].Substring(tagId.Length + 2);

                    // Now parse the TagName from the Value (e.g. "Study Date: 20160603")
                    int colonIndex = tagValue.IndexOf(":");
                    tagValue = tagValue.Substring(colonIndex + 1);
                    return tagValue;
                }
            }
            return null;

        }

        internal List<byte> GetPixels8()
        {
            throw new NotImplementedException();
        }
        internal List<byte> GetPixels24()
        {
            LazyLoadPixels();
            return _pixels24;
        }

        internal List<byte> GetCopyPixels24()
        {
            LazyLoadPixels();
            return new List<byte>(_pixels24);
        }



    }
}
