﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2009 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

namespace pwiz.Topograph.Data
{
    public class DbTracerDef : DbEntity<DbTracerDef>
    {
        public DbTracerDef()
        {
            FinalEnrichment = 100;
        }
        public virtual DbWorkspace Workspace { get; set; }
        public virtual String Name { get; set; }
        public virtual String TracerSymbol { get; set; }
        public virtual double DeltaMass
        {
            get; set;
        }
        public virtual int AtomCount
        {
            get; set;
        }
        public virtual double AtomPercentEnrichment
        {
            get; set;
        }
        public virtual double InitialEnrichment
        {
            get; set;
        }
        public virtual double FinalEnrichment
        {
            get; set;
        }
        public virtual bool IsotopesEluteEarlier
        {
            get; set;
        }
        public virtual bool IsotopesEluteLater
        {
            get; set;
        }
    }
}
