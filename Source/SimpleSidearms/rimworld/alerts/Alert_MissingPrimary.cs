﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;

namespace SimpleSidearms.rimworld.alerts
{
    class Alert_MissingPrimary : Alert
    {
        protected string explanation;

        public Alert_MissingPrimary()
        {
            this.defaultLabel = "Alert_MissingPrimary_label".Translate();
            explanation = "Alert_MissingPrimary_desc";
        }

        public override string GetExplanation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Pawn current in this.AffectedPawns())
            {
                stringBuilder.AppendLine("    " + current.NameStringShort);
            }
            return explanation.Translate(new object[]
            {
                stringBuilder.ToString()
            });
        }

        public override AlertReport GetReport()
        {
            //return true;
            Pawn pawn = this.AffectedPawns().FirstOrDefault<Pawn>();
            if (pawn != null)
            {
                return AlertReport.CulpritIs(pawn);
            }
            return AlertReport.Inactive;
        }
        
        private IEnumerable<Pawn> AffectedPawns()
        {
            HashSet<Pawn> pawns = new HashSet<Pawn>();
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn.Dead)
                {
                    Log.Error("Dead pawn in PawnsFinder.AllMaps_FreeColonists:" + pawn);
                }
                else
                {
                    if (pawn.Downed)
                        continue;
                    if (pawn.Drafted)
                        continue;
                    if (pawn.CurJob != null && (pawn.CurJob.def == SidearmsDefOf.EquipSecondary || pawn.CurJob.def == SidearmsDefOf.EquipSecondaryCombat))
                        continue;
                    
                    GoldfishModule pawnMemory = GoldfishModule.GetGoldfishForPawn(pawn);
                    if (pawnMemory != null)
                    {
                        if (pawnMemory.NoPrimary)
                            continue;
                        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(pawnMemory.primary);
                        if (def == null)
                            continue;

                        if (!pawn.hasWeaponSomewhere(pawnMemory.primary))
                        {
                            if (pawns.Add(pawn))
                                yield return pawn;
                        }
                    }
                }
            }
        }
    }
}
