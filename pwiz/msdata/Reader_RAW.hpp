//
// Reader_RAW.hpp
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


#ifndef _READER_RAW_HPP_ 
#define _READER_RAW_HPP_ 


#include "Reader.hpp"


// Xcalibur DLL usage is msvc only - mingw doesn't provide com support
#if (!defined(_MSC_VER) && !defined(PWIZ_NO_READER_RAW))
#define PWIZ_NO_READER_RAW
#endif


namespace pwiz {
namespace msdata {


class Reader_RAW : public Reader
{
    public:

    virtual bool accept(const std::string& filename, 
                        const std::string& head) const; 

    virtual void read(const std::string& filename, 
                      const std::string& head, 
                      MSData& result) const;

    /// checks header for "Finnigan" wide char string
	static bool hasRAWHeader(const std::string& head); 
};


} // namespace msdata
} // namespace pwiz


#endif // _READER_RAW_HPP_ 

