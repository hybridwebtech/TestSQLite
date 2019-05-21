using System;
using System.Collections.Generic;

namespace AuraAPI
{
    public interface IDicomSeries
    {
        string ParentStudyDirectory { get; }
        string SeriesFileName { get; }
        string SeriesID { get; }

        IDicomStudy ParentStudy { get; }
        string SeriesDate { get; }
        string SeriesTime { get; }
        string SeriesDescription { get; }
        string KentStudyDate { get; }

		int NIRImageHeight { get; }
		int NIRImageWidth { get; }
		int NumberofNIRFrames { get; }
		int GetNIRPixelValue(float row, float col, int frame = 0);


		int PNGImageHeight { get; }
		int PNGImageWidth { get; }
		int GetRGBPixelValue(float row, float col, int frame = 0);

		// Return true if there is data corresponding to the image type
		bool DataIsAvailableForImageType(ImageType imageType);

		void SetSt02Image(UInt16[,] st02, bool valuesAreScaled = true, ImageType imageType = ImageType.ST02);
		void SetSt02Image(double[,] st02, bool valuesAreScaled = true, ImageType imageType = ImageType.ST02);

		//create a new image that is not bound to the series
		IDicomImage CloneImage(ImageType imageType);

        // Get an Image of certain type in the series
        IDicomImage GetImage(ImageType imageType);

        void ReplaceImage(IDicomImage newImage);

        void SetERGBImage(byte[,] st02, ImageType imageType);
    }
}