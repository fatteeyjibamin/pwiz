﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MathNet.Numerics.Distributions;
using pwiz.Common.Chemistry;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Model.Serialization;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Crosslinking
{
    public class ComplexFragmentIon : Immutable
    {
        public ComplexFragmentIon(Transition transition, TransitionLosses transitionLosses, bool isOrphan = false)
        {
            Transition = transition;
            Children = ImmutableSortedList<ModificationSite, ComplexFragmentIon>.EMPTY;
            TransitionLosses = transitionLosses;
            IsOrphan = isOrphan;
        }

        public static ComplexFragmentIon NewOrphanFragmentIon(TransitionGroup transitionGroup, ExplicitMods explicitMods)
        {
            var transition = new Transition(transitionGroup, IonType.precursor,
                transitionGroup.Peptide.Sequence.Length - 1, 0, transitionGroup.PrecursorAdduct);
            return new ComplexFragmentIon(transition, null, true);
        }

        public Transition Transition { get; private set; }

        public bool IsOrphan { get; private set; }

        public bool IsEmptyOrphan
        {
            get { return IsOrphan && Children.Count == 0; }
        }

        [CanBeNull]
        public TransitionLosses TransitionLosses { get; private set; }
        public ImmutableSortedList<ModificationSite, ComplexFragmentIon> Children { get; private set; }

        public IsotopeLabelType LabelType
        {
            get { return Transition.Group.LabelType; }
        }

        public ComplexFragmentIon ChangeChildren(IEnumerable<KeyValuePair<ModificationSite, ComplexFragmentIon>> children)
        {
            return ChangeProp(ImClone(this), im => im.Children = ImmutableSortedList.FromValues(children));
        }

        public ComplexFragmentIon AddChild(ModificationSite modificationSite, ComplexFragmentIon child)
        {
            if (IsOrphan && !IsEmptyOrphan)
            {
                throw new InvalidOperationException(string.Format("Cannot add {0} to {1}.", child, this));
            }

            return ChangeProp(ImClone(this), im => im.Children =
                ImmutableSortedList.FromValues(Children.Append(
                    new KeyValuePair<ModificationSite, ComplexFragmentIon>(
                        modificationSite, child))));

        }

        public int GetFragmentationEventCount()
        {
            int count = 0;
            if (!IsOrphan && !Transition.IsPrecursor())
            {
                count++;
            }

            if (null != TransitionLosses)
            {
                count += TransitionLosses.Losses.Count;
            }
            count += Children.Values.Sum(child => child.GetFragmentationEventCount());
            return count;
        }

        public bool IncludesAaIndex(int aaIndex)
        {
            switch (Transition.IonType)
            {
                case IonType.precursor:
                    return true;
                case IonType.a:
                case IonType.b:
                case IonType.c:
                    return Transition.CleavageOffset >= aaIndex;
                case IonType.x:
                case IonType.y:
                case IonType.z:
                    return Transition.CleavageOffset < aaIndex;
                default:
                    return true;
            }
        }

        public MoleculeMassOffset GetNeutralFormula(SrmSettings settings, ExplicitMods explicitMods)
        {
            var result = GetSimpleFragmentFormula(settings, explicitMods);
            if (explicitMods != null)
            {
                foreach (var explicitMod in explicitMods.StaticModifications)
                {
                    if (explicitMod.LinkedPeptide != null)
                    {
                        result = result.Plus(GetCrosslinkFormula(settings, explicitMod));
                    }
                }
            }

            return result;
        }

        public MoleculeMassOffset GetSimpleFragmentFormula(SrmSettings settings, ExplicitMods mods)
        {
            if (IsOrphan)
            {
                return MoleculeMassOffset.EMPTY;
            }
            var modifiedSequence =
                ModifiedSequence.GetModifiedSequence(settings, Transition.Group.Peptide.Sequence, mods, LabelType);
            var fragmentedMolecule = FragmentedMolecule.EMPTY.ChangeModifiedSequence(modifiedSequence);
            fragmentedMolecule = fragmentedMolecule.ChangeFragmentIon(Transition.IonType, Transition.Ordinal);
            if (null != TransitionLosses)
            {
                fragmentedMolecule = fragmentedMolecule.ChangeFragmentLosses(TransitionLosses.Losses.Select(loss => loss.Loss));
            }
            return new MoleculeMassOffset(fragmentedMolecule.FragmentFormula, 0);
        }



        public MoleculeMassOffset GetCrosslinkFormula(SrmSettings settings, ExplicitMod explicitMod)
        {
            var children = GetChildrenAtSite(explicitMod.ModificationSite).ToList();
            if (children.Count == 0)
            {
                return MoleculeMassOffset.EMPTY;
            }

            if (children.Count != 1)
            {
                throw new ArgumentException();
            }
            var result = MoleculeMassOffset.EMPTY;
            // ;
            //     explicitMod.CrosslinkerDef.IntactFormula.GetMoleculeMassOffset(GetMassType(settings));
            var linkedPeptide = explicitMod.LinkedPeptide;
            var childFragmentIon = children[0];
            var childFormula =
                childFragmentIon.GetNeutralFormula(settings, linkedPeptide.ExplicitMods);
            result = result.Plus(childFormula);
            return result;
        }

        private MassType GetMassType(SrmSettings settings)
        {
            return settings.TransitionSettings.Prediction.FragmentMassType;
        }

        private IEnumerable<ComplexFragmentIon> GetChildrenAtSite(ModificationSite site)
        {
            return Children.Where(child => child.Key.Equals(site)).Select(child => child.Value);
        }

        public TransitionDocNode MakeTransitionDocNode(SrmSettings settings, ExplicitMods explicitMods)
        {
            return MakeTransitionDocNode(settings, explicitMods, Annotations.EMPTY, TransitionDocNode.TransitionQuantInfo.DEFAULT, ExplicitTransitionValues.EMPTY, null);
        }

        public TransitionDocNode MakeTransitionDocNode(SrmSettings settings, ExplicitMods explicitMods,
            Annotations annotations,
            TransitionDocNode.TransitionQuantInfo transitionQuantInfo,
            ExplicitTransitionValues explicitTransitionValues,
            Results<TransitionChromInfo> results)
        {
            var neutralFormula = GetNeutralFormula(settings, explicitMods);
            var productMass = GetFragmentMass(settings, neutralFormula);
            var complexFragmentIon = this;
            if (Children.Count > 0)
            {
                var name = GetName();
                if (!explicitMods.Crosslinks.Skip(1).Any())
                {
                    //name = name.DisqualifyChildren();
                }

                complexFragmentIon = ChangeProp(ImClone(complexFragmentIon),
                    im => im.Transition = (Transition) im.Transition.Copy());
            }
            // TODO: TransitonQuantInfo is probably wrong since it did not know the correct mass.
            return new TransitionDocNode(complexFragmentIon, annotations, productMass, transitionQuantInfo, explicitTransitionValues, results);
        }

        public static TypedMass GetFragmentMass(SrmSettings settings, MoleculeMassOffset formula)
        {
            MassType massType = settings.TransitionSettings.Prediction.FragmentMassType;
            var massDistribution = GetMassDistribution(settings, formula.Molecule);
            double mass = massType.IsMonoisotopic() ? massDistribution.MostAbundanceMass : massDistribution.AverageMass;
            return new TypedMass(mass + BioMassCalc.MassProton, massType | MassType.bMassH);
        }

        public static MassDistribution GetMassDistribution(SrmSettings settings, Molecule molecule)
        {
            var precursorCalc = settings.GetPrecursorCalc(IsotopeLabelType.light, ExplicitMods.EMPTY);
            var massDistribution = precursorCalc.GetMZDistributionFromFormula(molecule.ToString(), Adduct.SINGLY_PROTONATED,
                settings.TransitionSettings.FullScan.IsotopeAbundances ?? BioMassCalc.DEFAULT_ABUNDANCES);
            massDistribution = massDistribution.OffsetAndDivide(-BioMassCalc.MassProton, 1);
            return massDistribution;
        }

        public static MassDistribution GetMzDistribution(SrmSettings settings, MoleculeMassOffset moleculeMassOffset, Adduct adduct)
        {
            var massDistribution = GetMassDistribution(settings, moleculeMassOffset.Molecule);
            double massOffset = moleculeMassOffset.MassOffset;
            if (adduct.IsProtonated && adduct.GetMassMultiplier() == 1)
            {
                massOffset += adduct.AdductCharge * BioMassCalc.MassProton;
            }
            else
            {
                massOffset -= adduct.AdductCharge * BioMassCalc.MassElectron;
            }
            return massDistribution.OffsetAndDivide(massOffset, adduct.AdductCharge);
        }

        public ComplexFragmentIonName GetName()
        {
            ComplexFragmentIonName name;
            if (IsOrphan)
            {
                name = ComplexFragmentIonName.ORPHAN;
            }
            else
            {
                name = new ComplexFragmentIonName(Transition.IonType, Transition.Ordinal);
            }

            foreach (var child in Children)
            {
                name = name.AddChild(child.Key, child.Value.GetName());
            }

            if (null != TransitionLosses)
            {
                foreach (var loss in TransitionLosses.Losses)
                {
                    name = name.AddLoss(null, loss.ToString());
                }
            }

            return name;
        }

        public override string ToString()
        {
            return GetName().ToString();
        }
    }
}
