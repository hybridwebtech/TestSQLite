using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuraAPI
{
    public enum ImageViewerToolType {
        St02Point,
        St02Poly,
        None,
        ReferenceMarker,
        FreeHandPolyROI,
        LineMeasure,
        PanTool,
        ContrastTool,
    }

    public enum ImageType
    {
        NIR = 0,
        ST02 = 1,
        PNGImage = 2,
        WhiteRefs = 3,
        MelaninCorrectedSt02 = 4,
        HbDeoxy = 5,
        HbOxy = 6,
        TotalHbOxy = 7,
        eFuzzyRGB = 8,
        FuzzyStO2 = 9,
        ColonStO2 = 10,
        eColonRGB = 11,
        Undefined
    };


    public enum Wavelength_V2
    {
        NIR_630 = 0,
        NIR_660 = 1,
        NIR_735 = 2,
        NIR_830 = 3,
        NIR_890 = 4,
        NIR_970 = 5
    };
    public enum ViewZoomMode
    {
  //      ZoomToFit = 0,
		//Zoom_25 = 25,
  //      Zoom_50 = 50,
  //      Zoom_100 = 100,
  //      Zoom_125 = 125,
  //      Zoom_150 = 150,
  //      Zoom_175 = 175,
  //      Zoom_200 = 200,
  //      Zoom_400 = 400,
        Zoom_Arbitrary
    };

	public enum LayoutType { OnebyOne, OnebyTwo, TwobyTwo, ThreebyTwo };
}
