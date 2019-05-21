using System;
using System.Collections.Generic;

namespace AuraAPI
{
    public interface IDicomImage
    {
        int ImageHeight { get; }
        int ImageWidth { get; }


		void ReplacePixels(UInt16[,] pixels, bool pixelsValuesAreScaled);

		void ReplacePixels(List<ushort> pixels);
		void ReplacePixels(List<List<ushort>> pixels);
		List<ushort> GetPixels(int wavelength);


        void SetColorPixelValue(int row, int col, int v1, int v2, int v3);
        void SetImageType(ImageType hbDeoxy);
    }
}