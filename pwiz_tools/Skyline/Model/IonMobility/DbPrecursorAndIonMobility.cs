﻿/*
 * Original author: Brian Pratt <bspratt .at. proteinms.net>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2014 University of Washington - Seattle, WA
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
using pwiz.Common.Chemistry;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Irt;
using pwiz.Skyline.Model.Lib;
using pwiz.Skyline.Model.Lib.BlibData;
using pwiz.Skyline.Util;

// ReSharper disable VirtualMemberCallInConstructor

namespace pwiz.Skyline.Model.IonMobility
{

    /// <summary>
    /// A DbIonMobilityValue consists of a single precursor ion and ion mobility.
    /// This reflects the way IM data is stored in the .imdb format, where multiple conformers are represented by
    /// multiple DbIonMobilityValues with a common precursor ion.
    /// </summary>
    public class DbPrecursorAndIonMobility : DbEntity, IEquatable<DbPrecursorAndIonMobility>
    {
        public override Type EntityClass
        {
            get { return typeof(DbPrecursorAndIonMobility); }
        }

        /// <summary>
        /// For NHibernate only
        /// </summary>
        public DbPrecursorAndIonMobility()
        { }

        public DbPrecursorAndIonMobility(DbPrecursorAndIonMobility other) :
            this(other.DbPrecursorIon, other.CollisionalCrossSectionSqA, other.IonMobilityNullable,
                other.IonMobilityUnits, other.HighEnergyIonMobilityOffset)
        {
            Id = other.Id;
        }

        public DbPrecursorAndIonMobility(DbPrecursorIon precursor, double? collisionalCrossSection, double? ionMobility, eIonMobilityUnits units,
            double? highEnergyOffset)
        {
            DbPrecursorIon = precursor;
            CollisionalCrossSectionSqA = collisionalCrossSection ?? 0;
            IonMobility = ionMobility ?? 0;
            IonMobilityUnits = units;
            HighEnergyIonMobilityOffset = highEnergyOffset ?? 0;
        }

        public virtual IonMobilityAndCCS GetIonMobilityAndCCS()
        {
            return IonMobilityAndCCS.GetIonMobilityAndCCS(IonMobilityValue.GetIonMobilityValue(IonMobilityNullable, 
                IonMobilityUnits), CollisionalCrossSectionNullable, HighEnergyIonMobilityOffset);
        }

        public virtual DbPrecursorIon DbPrecursorIon { get; set; }

        public virtual double CollisionalCrossSectionSqA { get; set; }

        public virtual double? CollisionalCrossSectionNullable
        {
            get { return CollisionalCrossSectionSqA == 0 ? (double?)null : CollisionalCrossSectionSqA; }
            set { CollisionalCrossSectionSqA = value ?? 0; }
        }

        public virtual double IonMobility { get; set; }
        public virtual double? IonMobilityNullable
        {
            get { return IonMobility == 0 ? (double?)null : IonMobility; }
            set { IonMobility = value ?? 0; }
        }

        public virtual double HighEnergyIonMobilityOffset { get; set; }

        public virtual double? HighEnergyIonMobilityOffsetNullable
        {
            get { return HighEnergyIonMobilityOffset == 0 ? (double?)null : HighEnergyIonMobilityOffset; }
            set { HighEnergyIonMobilityOffset = value ?? 0; }
        }


        public virtual eIonMobilityUnits IonMobilityUnits { get; set; }

        public virtual string DisplayUnits()
        {
            return IonMobilityFilter.IonMobilityUnitsL10NString(IonMobilityUnits);
        }

        public virtual bool EqualsIgnoreId(DbPrecursorAndIonMobility other)
        {
            return DbPrecursorIon.EqualsIgnoreId(other.DbPrecursorIon) &&
                   CollisionalCrossSectionSqA.Equals(other.CollisionalCrossSectionSqA) &&
                   IonMobility.Equals(other.IonMobility) &&
                   HighEnergyIonMobilityOffset.Equals(other.HighEnergyIonMobilityOffset) &&
                   IonMobilityUnits == other.IonMobilityUnits;
        }

        public virtual bool Equals(DbPrecursorAndIonMobility other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && 
                   EqualsIgnoreId(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DbPrecursorAndIonMobility) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ DbPrecursorIon.GetHashCode();
                hashCode = (hashCode * 397) ^ CollisionalCrossSectionSqA.GetHashCode();
                hashCode = (hashCode * 397) ^ IonMobility.GetHashCode();
                hashCode = (hashCode * 397) ^ HighEnergyIonMobilityOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) IonMobilityUnits;
                return hashCode;
            }
        }
    }

    /// <summary>
    /// A DbMolecule corresponds to Skyline's Target class, and represents either a peptide or a small molecule.
    /// A single DbMolecule may be referenced by more than one DbPrecursorIon.
    /// </summary>
    public class DbMolecule : DbEntity, IPeptideData, IEquatable<DbMolecule>
    {
        public override Type EntityClass
        {
            get { return typeof(DbMolecule); }
        }

        /// <summary>
        /// For NHibernate only
        /// </summary>
        protected DbMolecule()
        {
        }

        public DbMolecule(DbMolecule other)
            : this(other.Target)
        {
            Id = other.Id;
        }

        public DbMolecule(SmallMoleculeLibraryAttributes smallMoleculeLibraryAttributes)
            : this(new Target(smallMoleculeLibraryAttributes))
        { }

        public DbMolecule(Target target)
        {
            if (target.IsProteomic)
            {
                PeptideModifiedSequence = target.Sequence;
                MoleculeName = string.Empty;
                ChemicalFormula = string.Empty;
                InChiKey = string.Empty;
                OtherKeys = string.Empty;
            }
            else
            {
                PeptideModifiedSequence = string.Empty;
                var smallMoleculeLibraryAttributes = target.Molecule.GetSmallMoleculeLibraryAttributes();
                MoleculeName = smallMoleculeLibraryAttributes.MoleculeName;
                ChemicalFormula = smallMoleculeLibraryAttributes.ChemicalFormula;
                InChiKey = smallMoleculeLibraryAttributes.InChiKey;
                OtherKeys = smallMoleculeLibraryAttributes.OtherKeys;
            }
        }

        public virtual Target Target
        {
            get
            {
                return string.IsNullOrEmpty(PeptideModifiedSequence)
                    ? new Target(SmallMoleculeLibraryAttributes.Create(MoleculeName, ChemicalFormula, null, null, InChiKey, OtherKeys))
                    : new Target(PeptideModifiedSequence);
            }
        }

        public virtual SmallMoleculeLibraryAttributes SmallMoleculeLibraryAttributes
        {
            get
            {
                return string.IsNullOrEmpty(PeptideModifiedSequence)
                    ? SmallMoleculeLibraryAttributes.Create(MoleculeName, ChemicalFormula, null, null, InChiKey, OtherKeys)
                    : SmallMoleculeLibraryAttributes.EMPTY;
            }
        }


        // For NHibernate use
        public virtual string PeptideModifiedSequence { get; set; }
        public virtual string MoleculeName { get; set; }
        public virtual string ChemicalFormula { get; set; }
        public virtual string InChiKey { get; set; }
        public virtual string OtherKeys { get; set; }


        public virtual bool EqualsIgnoreId(DbMolecule other)
        {
            return PeptideModifiedSequence == other.PeptideModifiedSequence &&
                   MoleculeName == other.MoleculeName &&
                   ChemicalFormula == other.ChemicalFormula &&
                   InChiKey == other.InChiKey &&
                   OtherKeys == other.OtherKeys;
        }

        public virtual bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(PeptideModifiedSequence) && 
                       string.IsNullOrEmpty(MoleculeName) &&
                       string.IsNullOrEmpty(ChemicalFormula) && 
                       string.IsNullOrEmpty(InChiKey) &&
                       string.IsNullOrEmpty(OtherKeys);
            }
        }

        #region object overrides
        public virtual bool Equals(DbMolecule other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && 
                   EqualsIgnoreId(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DbMolecule) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (PeptideModifiedSequence != null ? PeptideModifiedSequence.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MoleculeName != null ? MoleculeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ChemicalFormula != null ? ChemicalFormula.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (InChiKey != null ? InChiKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OtherKeys != null ? OtherKeys.GetHashCode() : 0);
                return hashCode;
            }
        }
        #endregion
    }

    /// <summary>
    /// A DbPrecursorIon is a DbMolecule and an adduct, which together define an ion.
    /// </summary>
    public class DbPrecursorIon : DbEntity, IEquatable<DbPrecursorIon>
    {
        public override Type EntityClass
        {
            get { return typeof(DbPrecursorIon); }
        }

        private Adduct _adduct;

        public virtual DbMolecule DbMolecule { get; set; }

        public virtual string PrecursorAdduct 
        {
            get
            {
                return _adduct.AsFormulaOrSignedInt();
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _adduct = Adduct.EMPTY;
                }
                else if (int.TryParse(value, out _))
                {
                    _adduct = Adduct.FromStringAssumeProtonated(value);
                }
                else
                {
                    _adduct = Adduct.FromStringAssumeProtonatedNonProteomic(value);
                }
            }
        }

        public virtual bool IsEmpty
        {
            get { return _adduct.IsEmpty && DbMolecule.IsEmpty;  }
        }

        /// <summary>
        /// For NHibernate only
        /// </summary>
        protected DbPrecursorIon()
        {
        }

        public DbPrecursorIon(DbPrecursorIon other)
            : this(other.DbMolecule, other._adduct)
        {
            Id = other.Id;
        }

        public DbPrecursorIon(SmallMoleculeLibraryAttributes smallMoleculeLibraryAttributes,
            Adduct precursorAdduct)
            : this(new Target(smallMoleculeLibraryAttributes),
                precursorAdduct)
        { }

        public DbPrecursorIon(LibKey libKey)
        {
            DbMolecule = new DbMolecule(libKey.Target);
            _adduct = libKey.Adduct;
        }
        
        public DbPrecursorIon(Target target, 
            Adduct precursorAdduct)
        {
            DbMolecule = new DbMolecule(target);
            _adduct = precursorAdduct;
        }

        public DbPrecursorIon(DbMolecule molecule,
            Adduct precursorAdduct)
        {
            DbMolecule = molecule;
            _adduct = precursorAdduct;
        }

        public virtual Adduct GetPrecursorAdduct()
        {
            return _adduct;
        }

        public virtual Target GetTarget()
        {
            return DbMolecule.Target;
        }

        public virtual LibKey GetLibKey()
        {
            var target = DbMolecule.Target;
            if (target.IsProteomic)
            {
                return new LibKey(target.Sequence, _adduct.AdductCharge);
            }
            return new LibKey(target.Molecule.PrimaryEquivalenceKey, _adduct);
        }

        public virtual bool EqualsIgnoreId(DbPrecursorIon other)
        {
            return Equals(_adduct, other._adduct) && 
                   DbMolecule.EqualsIgnoreId(other.DbMolecule);
        }

        public virtual bool Equals(DbPrecursorIon other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) &&
                EqualsIgnoreId(other); 
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DbPrecursorIon) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (_adduct != null ? _adduct.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DbMolecule != null ? DbMolecule.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

}
