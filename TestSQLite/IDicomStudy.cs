using System.Collections.Generic;

namespace AuraAPI
{
    public interface IDicomStudy
    {
        List<IDicomSeries> GetSeriesInStudy();
        string StudyDirectory { get; }
		string StudyDescription { get; }

	}
}