//
// SpectrumInfo.cpp
//
//
// Original author: Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2008 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
//
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
//


#define PWIZ_SOURCE


#include "SpectrumInfo.hpp"
#include <iterator>


namespace pwiz {
namespace msdata {


using namespace std;
using boost::lexical_cast;
using boost::bad_lexical_cast;


PWIZ_API_DECL SpectrumInfo::SpectrumInfo()
:   index((size_t)-1), scanNumber(0), massAnalyzerType(CVID_Unknown), scanEvent(0), 
    msLevel(0), retentionTime(0), mzLow(0), mzHigh(0), basePeakMZ(0), 
    basePeakIntensity(0), totalIonCurrent(0), thermoMonoisotopicMZ(0)
{}


PWIZ_API_DECL SpectrumInfo::SpectrumInfo(const Spectrum& spectrum)
:   index((size_t)-1), scanNumber(0), massAnalyzerType(CVID_Unknown), scanEvent(0), 
    msLevel(0), retentionTime(0), mzLow(0), mzHigh(0), basePeakMZ(0), 
    basePeakIntensity(0), totalIonCurrent(0), thermoMonoisotopicMZ(0)
{
    update(spectrum);
}


namespace {
int nativeIDToScanNumber(const string& nativeID)
{
    try
    {
        return lexical_cast<int>(nativeID);
    }
    catch (bad_lexical_cast&)
    {
        return 0;
    }
}
} // namespace


PWIZ_API_DECL void SpectrumInfo::update(const Spectrum& spectrum)
{
    const SpectrumDescription& sd = spectrum.spectrumDescription;

    id = spectrum.id;
    nativeID = spectrum.nativeID;
    index = spectrum.index;
    scanNumber = nativeIDToScanNumber(spectrum.nativeID);

    massAnalyzerType = sd.scan.instrumentConfigurationPtr.get() ? 
                       sd.scan.instrumentConfigurationPtr->componentList.analyzer(0)
                           .cvParamChild(MS_mass_analyzer_type).cvid :
                       CVID_Unknown;

    scanEvent = sd.scan.cvParam(MS_preset_scan_configuration).valueAs<int>(); 
    msLevel = spectrum.cvParam(MS_ms_level).valueAs<int>();
    retentionTime = sd.scan.cvParam(MS_scan_time).timeInSeconds();
    filterString = sd.scan.cvParam(MS_filter_string).value;
    mzLow = sd.cvParam(MS_lowest_m_z_value).valueAs<double>();        
    mzHigh = sd.cvParam(MS_highest_m_z_value).valueAs<double>();        
    basePeakMZ = sd.cvParam(MS_base_peak_m_z).valueAs<double>();    
    basePeakIntensity = sd.cvParam(MS_base_peak_intensity).valueAs<double>();    
    totalIonCurrent = sd.cvParam(MS_total_ion_current).valueAs<double>();

    UserParam userParamMonoisotopicMZ = sd.scan.userParam("[Thermo Trailer Extra]Monoisotopic M/Z:");
    if (!userParamMonoisotopicMZ.name.empty())
        thermoMonoisotopicMZ = userParamMonoisotopicMZ.valueAs<double>();        
 
    for (vector<Precursor>::const_iterator it=sd.precursors.begin(); it!=sd.precursors.end(); ++it)
    {
        PrecursorInfo precursorInfo;
        precursorInfo.index = 0; // TODO
        if (!it->selectedIons.empty())
        {
            precursorInfo.mz = it->selectedIons[0].cvParam(MS_m_z).valueAs<double>();
            precursorInfo.charge = it->selectedIons[0].cvParam(MS_charge_state).valueAs<int>();
            precursorInfo.intensity = it->selectedIons[0].cvParam(MS_intensity).valueAs<double>();
        }
        precursors.push_back(precursorInfo);
    }

    if (!spectrum.binaryDataArrayPtrs.empty())
        spectrum.getMZIntensityPairs(data);
}


PWIZ_API_DECL void SpectrumInfo::clearBinaryData()
{
    vector<MZIntensityPair> nothing;
    data.swap(nothing);
}


PWIZ_API_DECL string SpectrumInfo::massAnalyzerTypeAbbreviation() const
{
    string result = "Unknown";

    if (cvIsA(massAnalyzerType, MS_ion_trap))
        result = "IonTrap";
    else if (massAnalyzerType == MS_FT_ICR)
        result = "FT";
    else if (massAnalyzerType == MS_orbitrap)
        result = "Orbitrap";

    return result;
}

PWIZ_API_DECL double SpectrumInfo::mzFromFilterString() const
{
    istringstream iss(filterString);
    vector<string> tokens;
    copy(istream_iterator<string>(iss), istream_iterator<string>(), back_inserter(tokens));
    
    for (vector<string>::const_iterator it=tokens.begin(), end=tokens.end(); it!=end; ++it)
    {
        string::size_type at = it->find("@");
        if (at != string::npos)
            return lexical_cast<double>(it->substr(0,at));
    }
    
    return 0;
}


} // namespace msdata
} // namespace pwiz

