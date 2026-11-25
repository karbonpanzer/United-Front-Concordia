using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using UnitedFront.UI;
using UnitedFront.Utils;

namespace UnitedFront.Comps
{
    [StaticConstructorOnStartup]
    public sealed class Comp_ColorMarker : ThingComp
    {
        private static readonly Texture2D DevEditColorsIcon = ContentFinder<Texture2D>.Get("UFR/UI/Paint/UFR_CustomizeColor", false);

        public Color colorOne = Color.white;
        public Color colorTwo = Color.white;
        private bool customized;

        public CompProperties_ColorMarker Props => (CompProperties_ColorMarker)props;

        private CompColorable? Colorable => parent.TryGetComp<CompColorable>();

        private Color BaseColor
        {
            get
            {
                if (parent.Stuff != null) return parent.def.GetColorForStuff(parent.Stuff);
                return parent.def.graphicData != null ? parent.def.graphicData.color : Color.white;
            }
        }

        public Color DefaultColorOne => Props.setColorOne ? Props.colorOne : BaseColor;
        public Color DefaultColorTwo => Props.setColorTwo ? Props.colorTwo : BaseColor;

        public Color DisplayColorOne => Rotted(colorOne);
        public Color DisplayColorTwo => Rotted(colorTwo);

        private Color Rotted(Color color)
        {
            if (parent is Apparel ap && ap.WornByCorpse)
                return PawnRenderUtility.GetRottenColor(color);
            return color;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            if (customized) return;

            colorOne = DefaultColorOne;
            colorTwo = DefaultColorTwo;
            SetDirty();
        }

        public sealed class EditSnapshot
        {
            public Color colorOne;
            public Color colorTwo;
            public bool customized;
            public bool colorableActive;
            public Color colorableColor;
            public Color? desiredColor;
        }

        public EditSnapshot BeginEdit()
        {
            CompColorable? colorable = Colorable;
            if (colorable != null && colorable.Active && !colorOne.IndistinguishableFrom(colorable.Color))
                colorOne = colorable.Color;

            return new EditSnapshot
            {
                colorOne = colorOne,
                colorTwo = colorTwo,
                customized = customized,
                colorableActive = colorable != null && colorable.Active,
                colorableColor = colorable?.Color ?? Color.white,
                desiredColor = colorable?.DesiredColor
            };
        }

        public void Preview(Color one, Color two)
        {
            colorOne = one;
            colorTwo = two;
            SyncColorableToPrimary();
            SetDirty();
        }

        public void Commit(Color one, Color two)
        {
            colorOne = one;
            colorTwo = two;
            customized = true;
            SyncColorableToPrimary();
            SetDirty();
        }

        public void RestoreEdit(EditSnapshot snapshot)
        {
            colorOne = snapshot.colorOne;
            colorTwo = snapshot.colorTwo;
            customized = snapshot.customized;

            CompColorable? colorable = Colorable;
            if (colorable != null)
            {
                if (snapshot.colorableActive && colorable.Active
                    && !colorable.Color.IndistinguishableFrom(snapshot.colorableColor))
                    colorable.SetColor(snapshot.colorableColor);

                colorable.DesiredColor = snapshot.desiredColor;
            }

            SetDirty();
        }

        private void SyncColorableToPrimary()
        {
            CompColorable? colorable = Colorable;
            if (colorable == null || !colorable.Active) return;
            if (!colorable.Color.IndistinguishableFrom(colorOne))
                colorable.SetColor(colorOne);
        }

        public override Color? ForceColor()
        {
            return colorOne;
        }

        public void SyncPrimaryFromColorable(Color dyed)
        {
            if (props == null || colorOne.IndistinguishableFrom(dyed)) return;

            colorOne = dyed;
            customized = true;
            SetDirty();
        }

        private void SetDirty()
        {
            parent.Notify_ColorChanged();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref colorOne, "UnitedFrontColorOne", Color.clear);
            Scribe_Values.Look(ref colorTwo, "UnitedFrontColorTwo", Color.clear);
            Scribe_Values.Look(ref customized, "UnitedFrontCustomized", false);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<Color>? legacyZones = null;
                Scribe_Collections.Look(ref legacyZones, "UnitedFrontZoneColors", LookMode.Value);
                if (legacyZones != null)
                {
                    if (legacyZones.Count > 0) colorOne = legacyZones[0];
                    if (legacyZones.Count > 1) colorTwo = legacyZones[1];

                    bool legacyCustomized = false;
                    Scribe_Values.Look(ref legacyCustomized, "UnitedFrontZonesCustomized", false);
                    customized = legacyCustomized;
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (colorOne.a <= 0f) colorOne = DefaultColorOne;
                if (colorTwo.a <= 0f) colorTwo = DefaultColorTwo;
            }
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetWornGizmosExtra())
                yield return gizmo;

            if (!Prefs.DevMode || !DebugSettings.godMode) yield break;

            Pawn? wearer = (parent as Apparel)?.Wearer;
            if (wearer == null || !wearer.RaceProps.Humanlike || ColorMarkerUtil.FirstMarkerOn(wearer) != this) yield break;

            yield return new Command_Action
            {
                defaultLabel = "UFR_DevEditColors".Translate(),
                defaultDesc = "UFR_DevEditColorsDesc".Translate(),
                icon = DevEditColorsIcon,
                action = () => Find.WindowStack.Add(new Dialog_EditColors(wearer))
            };
        }
    }
}