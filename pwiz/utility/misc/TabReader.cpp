//
// TabReader.cpp
//
//
// Original author: Robert Burke <robert.burke@proteowizard.org>
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

#include <fstream>
#include <vector>
#include <stdexcept>
#include <iostream>

#include <unistd.h>
#include <sys/param.h>

#include "TabReader.hpp"

namespace pwiz {
namespace utility {

using namespace std;
using namespace boost;

class DefaultTabHandler::Impl 
{
    public:
    
    Impl(bool need_headers, char comment_char)
        : got_headers(false), need_headers(need_headers),
          comment_char(comment_char)
    {}

    Impl(const Impl& c)
    {
        got_headers = c.got_headers;
        need_headers = c.need_headers;
        comment_char = comment_char;
        columns = c.columns;
        headers = c.headers;
        records = c.records;
    }
    
	bool got_headers;
    bool need_headers;

    char comment_char;

	size_t columns;
	vector<string> headers;

    vector< vector<string> > records;
};

DefaultTabHandler::DefaultTabHandler(bool need_headers,
                                     char comment_char)
    : pimpl(new Impl(need_headers, comment_char))
{
}


DefaultTabHandler::DefaultTabHandler(const DefaultTabHandler& c)
    : pimpl(new Impl(*(c.pimpl)))
{
}

bool DefaultTabHandler::getHeaders()
{
    return pimpl->need_headers;
}

char DefaultTabHandler::useComment()
{
    return pimpl->comment_char;
}

bool DefaultTabHandler::open()
{
    return true;
}

bool DefaultTabHandler::updateLine(const std::string& line)
{
    return true;
}

bool DefaultTabHandler::updateRecord(const std::vector<std::string>& fields)
{
    pimpl->records.push_back(fields);
    return true;
}

size_t DefaultTabHandler::getHeader(const std::string& name) const
{
    size_t idx = 0;
    bool found = false;

    for(size_t i=0; i<pimpl->headers.size(); i++)
    {
        if (pimpl->headers[i] == name)
        {
            idx = i;
            found = true;
            break;
        }
    }

    if (!found)
        throw runtime_error("blah");
    
    return idx;
}

std::string DefaultTabHandler::getHeader(size_t index) const
{
    string name("");

    if (pimpl->headers.size() < index)
        name = pimpl->headers.at(index);
    else
        throw runtime_error("blah");
    
    return name;
}

bool DefaultTabHandler::close()
{
    return true;
}

void DefaultTabHandler::dump(ostream* os)
{
    (*os) << "DefaultTabHandler::dump(ostream* os)" << endl;
    (*os) << "# records=" << pimpl->records.size() << endl;
    vector< vector<string> >::const_iterator it1 = pimpl->records.begin();
    vector<string>::const_iterator it2;

    for(;it1!=pimpl->records.end(); it1++)
    {
        for(it2=(*it1).begin(); it2!=(*it1).end(); it2++)
        {
            (*os) << (*it2) <<"_";
        }
        cout << endl;
    }
}

class TabReader::Impl
{
    public:
    
    Impl()
        : filename_(NULL), comment_char('#'), delim('\t')
    {
        th_.reset(new DefaultTabHandler());
    }

    Impl(const Impl& c)
    {
        th_ = c.th_;
        filename_ = c.filename_;
        comment_char = c.comment_char;
        delim = c.delim;

        //in = c.in;
    }

    void setFilename(const char* filename)
    {
        filename_ = filename;
    }

    const char* getFilename() const
    {
        return filename_;
    }
        
    void setHandler(shared_ptr<TabHandler> th)
    {
		th_ = th;
        comment_char = th->useComment();
    }

	shared_ptr<TabHandler> setHandler() const
    {
        return th_;
    }
    
	bool process(const char* filename);

    bool getFields(string& line, vector<string>& fields);

    bool isComment(string& line);
    
    shared_ptr<TabHandler> th_;
	const char* filename_;
    char comment_char;
    char delim;
    
	ifstream in;
};

bool TabReader::Impl::process(const char* filename)
{
    bool success=false;

    if (filename == NULL)
        throw runtime_error("NULL pointer in filename");
    else if (th_ == NULL)
        throw runtime_error("No handler assigned");

    char path[MAXPATHLEN];
    getcwd(path, MAXPATHLEN);

    ifstream in(filename);

    string line;

    if (in.is_open())
    {
        th_->open();
        
        getline(in, line);
        while(getline(in, line))
        {
            if (isComment(line))
                continue;
            else
            {
                th_->updateLine(line);
                vector<string> fields;
                getFields(line, fields);
                th_->updateRecord(fields);
            }
           
        }
    }

    in.close();
    th_->close();

    return success;
}

bool TabReader::Impl::isComment(string& line)
{
    return line.size() > 0 && line.at(0) == comment_char;
}

bool TabReader::Impl::getFields(string& line, vector<string>& fields)
{
    bool success = false;
    size_t f_pos = 0, l_pos = 0;

    while (line.size() > 0 && l_pos < line.size() - 1)
    {
        string field;

        l_pos = line.find(delim, f_pos+1);
        if (l_pos == string::npos || l_pos >= line.size())
            l_pos = line.size();

        // sanity check
        if (f_pos >= l_pos)
            break;

        field = line.substr(f_pos, l_pos - f_pos);
        fields.push_back(field);

        f_pos=line.find_first_not_of(delim, l_pos);
        
        success = true;
    }

    return success;
}

TabReader::TabReader()
    : pimpl(new TabReader::Impl())
{
}

void TabReader::setHandler(boost::shared_ptr<TabHandler> handler)
{
    pimpl->th_ = handler;
}

boost::shared_ptr<TabHandler> TabReader::getHandler()
{
    return pimpl->th_;
}

bool TabReader::process(const char* filename)
{
    return pimpl->process(filename);
}

} // namespace pwiz
} // namespace utility
