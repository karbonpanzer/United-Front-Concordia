using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace UnitedFront.Comps
{
    public sealed class CompProperties_ColorMarker : CompProperties
    {
        public bool setColorOne = false;
        public Color colorOne = Color.white;

        public bool setColorTwo = true;
        public Color colorTwo = new Color(0.2f, 0.2f, 0.2f);

        public CompProperties_ColorMarker() => compClass = typeof(Comp_ColorMarker);

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (parentDef.apparel != null && !parentDef.apparel.useWornGraphicMask)
                yield return parentDef.defName + " has CompColorMarker but apparel.useWornGraphicMask is false; colors will not render on the pawn.";
        }
    }
}