// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// How one saved view presents the Part F airflow overlay.
    /// <para>
    /// <b>Presentation only.</b> There is not a single flow rate, compliance status, terminal or transfer
    /// route on this class, and there must never be. Every number the overlay draws is read live from
    /// <c>PartFSpaceData</c>, <c>PartFVentilationTerminalRequirement</c>, <c>PartFDoorTransferData</c> and
    /// <c>PartFComplianceResult</c> at draw time. A copy cached in a view would go stale the moment the
    /// model was recalculated, and a drawing that disagrees with its own assessment is worse than no
    /// drawing.
    /// </para>
    /// <para>
    /// What it does own: whether the overlay is shown, at which operating condition, for which dwellings,
    /// with which layers visible, at what annotation scale - and where a person has dragged individual
    /// labels. All of that is a property of the drawing, not of the building.
    /// </para>
    /// <para>
    /// Attached to a view through <see cref="AnalyticalViewSettingsParameter.PartFAirflow"/>, so it round-trips
    /// with <c>UIGeometrySettings</c> and the model itself. A view saved before this existed carries no
    /// such parameter, and the absence must read as "overlay off" rather than as "defaults on": otherwise
    /// every existing saved view sprouts arrows the first time it is reopened.
    /// </para>
    /// </summary>
    public class PartFAirflowViewSettings : SAMObject
    {
        public PartFAirflowViewSettings()
            : base(Guid.NewGuid(), "Part F Airflow")
        {
        }

        public PartFAirflowViewSettings(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public PartFAirflowViewSettings(PartFAirflowViewSettings partFAirflowViewSettings)
            : base(partFAirflowViewSettings)
        {
            if (partFAirflowViewSettings is not null)
            {
                Enabled = partFAirflowViewSettings.Enabled;
                OperatingMode = partFAirflowViewSettings.OperatingMode;
                DwellingFilter = partFAirflowViewSettings.DwellingFilter;
                DwellingGuid = partFAirflowViewSettings.DwellingGuid;
                DwellingScope = partFAirflowViewSettings.DwellingScope;
                ZoneCategoryName = partFAirflowViewSettings.ZoneCategoryName;
                AnnotationScale = partFAirflowViewSettings.AnnotationScale;

                ShowSupply = partFAirflowViewSettings.ShowSupply;
                ShowGeneralExtract = partFAirflowViewSettings.ShowGeneralExtract;
                ShowLocalKitchenExtract = partFAirflowViewSettings.ShowLocalKitchenExtract;
                ShowTransfer = partFAirflowViewSettings.ShowTransfer;
                ShowDoorRequirements = partFAirflowViewSettings.ShowDoorRequirements;
                ShowValues = partFAirflowViewSettings.ShowValues;
                ShowSpaceNetAirflow = partFAirflowViewSettings.ShowSpaceNetAirflow;
                ShowCompliance = partFAirflowViewSettings.ShowCompliance;
                ShowUnresolved = partFAirflowViewSettings.ShowUnresolved;
                ShowOutdoorAndExhaust = partFAirflowViewSettings.ShowOutdoorAndExhaust;
                ShowContextGeometry = partFAirflowViewSettings.ShowContextGeometry;

                AnnotationOverrides = [.. partFAirflowViewSettings.AnnotationOverrides.ConvertAll(x => new PartFAnnotationOverride(x))];
            }
        }

        /// <summary>
        /// Whether this view wants the Part F airflow annotation. False by default, so a view that has never
        /// been told about Part F never shows it.
        /// <para>
        /// <b>A statement of intent, and not the safety mechanism.</b> This says the drawing is meant to
        /// carry Part F annotation; <see cref="HasDwellingScope"/> says whether SAM knows enough to
        /// calculate any. The two are separate on purpose - an engineer who has asked for the annotation
        /// and not yet said which dwellings it is about should get the drawing the moment they answer, not
        /// have to remember to come back and switch it on again.
        /// </para>
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>The operating condition the view is drawn at.</summary>
        public PartFOperatingMode OperatingMode { get; set; } = PartFOperatingMode.ContinuousDesign;

        /// <summary>Which dwellings on the level are drawn.</summary>
        public PartFDwellingFilter DwellingFilter { get; set; } = PartFDwellingFilter.AllDwellingsOnLevel;

        /// <summary>
        /// What this view treats as its dwellings: the whole model, the zones of a category, or - the
        /// default - nothing decided yet.
        /// <para>
        /// <b>An empty <see cref="ZoneCategoryName"/> is not a decision.</b> It used to be read as one,
        /// because a blank name reaches the calculation as single-house mode; so a new view of a block of
        /// flats whose category was still ambiguous would assess and draw the whole building as one
        /// dwelling while waiting to be told which flats it was about. This says which of the three
        /// situations a blank name meant. See <see cref="HasDwellingScope"/>.
        /// </para>
        /// </summary>
        public PartFDwellingScope DwellingScope { get; set; } = PartFDwellingScope.Undefined;

        /// <summary>
        /// The zone category whose zones are the dwellings this view reports on - "Flats", typically. Read
        /// only where <see cref="DwellingScope"/> is <see cref="PartFDwellingScope.ZoneCategory"/>; the
        /// whole-model case is <see cref="PartFDwellingScope.WholeModel"/> and not a blank name here.
        /// <para>
        /// A SCOPE and not a regulatory value. It says which dwellings this drawing is about, exactly as
        /// <see cref="DwellingFilter"/> and <see cref="DwellingGuid"/> do, and it is stored so that reopening
        /// the view reproduces the same assessment instead of asking again. Nothing calculated from it is
        /// stored here - the assessment is re-read from the model every time the view is drawn.
        /// </para>
        /// </summary>
        public string ZoneCategoryName { get; set; } = null;

        /// <summary>
        /// Whether somebody - the preset or a person - has actually said what this drawing reports on.
        /// <b>Nothing may be assessed or drawn until this is true</b>, and it is the one predicate that
        /// decides it, so the rule cannot be enforced in one place and forgotten in another. This is the
        /// safety mechanism; <see cref="Enabled"/> is the user's intent and never stands in for it.
        /// <para>
        /// A category scope with no category named is still undecided. That combination is not something
        /// the dialog produces, and treating it as whole-house would reintroduce exactly the ambiguity this
        /// property exists to remove.
        /// </para>
        /// </summary>
        public bool HasDwellingScope
        {
            get
            {
                return DwellingScope switch
                {
                    PartFDwellingScope.WholeModel => true,
                    PartFDwellingScope.ZoneCategory => !string.IsNullOrWhiteSpace(ZoneCategoryName),
                    _ => false,
                };
            }
        }

        /// <summary>
        /// The dwelling zone drawn when <see cref="DwellingFilter"/> selects a single dwelling. A guid, so
        /// renaming a flat does not silently change what the saved view shows.
        /// </summary>
        public Guid DwellingGuid { get; set; } = Guid.Empty;

        /// <summary>
        /// The drawing scale the annotation is laid out for, as its denominator: 50 means 1:50.
        /// <para>
        /// <b>This is the layout scale, and it is not the camera.</b> Tags are a fixed size on the sheet, so
        /// how much of the building one covers depends on a scale - and that scale has to be a property of
        /// the drawing, not of wherever the view happens to be zoomed to at the moment. Solving against the
        /// viewport would make ordinary navigation an implicit auto-arrange command: pan and zoom around a
        /// plan and the labels of an engineering drawing would keep rearranging themselves. So panning and
        /// zooming only transform and redraw, and the layout changes when THIS changes.
        /// </para>
        /// <para>
        /// A real drawing scale rather than a pixel size on purpose: it is the same on a laptop and on a
        /// build server, at any window size and at any display scaling, which is what lets a saved view
        /// reopen with its annotation exactly where it was left. See <c>PartFTagPlacement.PixelsPerMetre</c>.
        /// </para>
        /// </summary>
        public double AnnotationScale { get; set; } = PartFTagPlacement.DefaultAnnotationScale;

        public bool ShowSupply { get; set; } = true;

        public bool ShowGeneralExtract { get; set; } = true;

        public bool ShowLocalKitchenExtract { get; set; } = true;

        public bool ShowTransfer { get; set; } = true;

        public bool ShowDoorRequirements { get; set; } = true;

        public bool ShowValues { get; set; } = true;

        public bool ShowSpaceNetAirflow { get; set; }

        public bool ShowCompliance { get; set; } = true;

        public bool ShowUnresolved { get; set; } = true;

        public bool ShowOutdoorAndExhaust { get; set; }

        public bool ShowContextGeometry { get; set; } = true;

        /// <summary>
        /// Labels a person has moved. Only positions - never a value, never a status.
        /// </summary>
        public List<PartFAnnotationOverride> AnnotationOverrides { get; set; } = [];

        /// <summary>
        /// The override for one annotation, or null where the person has not moved it and automatic
        /// placement applies.
        /// </summary>
        public PartFAnnotationOverride Override(Guid guid, PartFAnnotationType partFAnnotationType)
        {
            return AnnotationOverrides?.Find(x => x is not null && x.ObjectGuid == guid && x.AnnotationType == partFAnnotationType);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            Enabled = PartFViewJson.Boolean(jsonObject, "Enabled", Enabled);

            if (jsonObject.ContainsKey("OperatingMode"))
            {
                OperatingMode = Core.Query.Enum<PartFOperatingMode>(PartFViewJson.String(jsonObject, "OperatingMode"));
            }

            if (jsonObject.ContainsKey("DwellingFilter"))
            {
                DwellingFilter = Core.Query.Enum<PartFDwellingFilter>(PartFViewJson.String(jsonObject, "DwellingFilter"));
            }

            DwellingGuid = PartFViewJson.Guid(jsonObject, "DwellingGuid");
            ZoneCategoryName = PartFViewJson.String(jsonObject, "ZoneCategoryName");

            //Read after the category name, because a view saved before the scope existed has to be
            //interpreted from it: a named category is a category, and a blank one is UNDECIDED rather than
            //whole-house. That is the safe direction - such a view reopens drawing nothing and says why,
            //instead of quietly presenting a block of flats as a single dwelling.
            DwellingScope = jsonObject.ContainsKey("DwellingScope")
                ? Core.Query.Enum<PartFDwellingScope>(PartFViewJson.String(jsonObject, "DwellingScope"))
                : string.IsNullOrWhiteSpace(ZoneCategoryName) ? PartFDwellingScope.Undefined : PartFDwellingScope.ZoneCategory;
            AnnotationScale = PartFViewJson.NullableDouble(jsonObject, "AnnotationScale") ?? AnnotationScale;

            ShowSupply = PartFViewJson.Boolean(jsonObject, "ShowSupply", ShowSupply);
            ShowGeneralExtract = PartFViewJson.Boolean(jsonObject, "ShowGeneralExtract", ShowGeneralExtract);
            ShowLocalKitchenExtract = PartFViewJson.Boolean(jsonObject, "ShowLocalKitchenExtract", ShowLocalKitchenExtract);
            ShowTransfer = PartFViewJson.Boolean(jsonObject, "ShowTransfer", ShowTransfer);
            ShowDoorRequirements = PartFViewJson.Boolean(jsonObject, "ShowDoorRequirements", ShowDoorRequirements);
            ShowValues = PartFViewJson.Boolean(jsonObject, "ShowValues", ShowValues);
            ShowSpaceNetAirflow = PartFViewJson.Boolean(jsonObject, "ShowSpaceNetAirflow", ShowSpaceNetAirflow);
            ShowCompliance = PartFViewJson.Boolean(jsonObject, "ShowCompliance", ShowCompliance);
            ShowUnresolved = PartFViewJson.Boolean(jsonObject, "ShowUnresolved", ShowUnresolved);
            ShowOutdoorAndExhaust = PartFViewJson.Boolean(jsonObject, "ShowOutdoorAndExhaust", ShowOutdoorAndExhaust);
            ShowContextGeometry = PartFViewJson.Boolean(jsonObject, "ShowContextGeometry", ShowContextGeometry);

            AnnotationOverrides = [];

            if (jsonObject["AnnotationOverrides"] is JsonArray jsonArray)
            {
                foreach (JsonNode jsonNode in jsonArray)
                {
                    if (jsonNode is JsonObject jsonObject_Override)
                    {
                        AnnotationOverrides.Add(new PartFAnnotationOverride(jsonObject_Override));
                    }
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            result["Enabled"] = Enabled;
            result["OperatingMode"] = OperatingMode.ToString();
            result["DwellingFilter"] = DwellingFilter.ToString();
            result["DwellingGuid"] = DwellingGuid.ToString();
            result["DwellingScope"] = DwellingScope.ToString();

            if (ZoneCategoryName is not null)
            {
                result["ZoneCategoryName"] = ZoneCategoryName;
            }

            result["AnnotationScale"] = AnnotationScale;

            result["ShowSupply"] = ShowSupply;
            result["ShowGeneralExtract"] = ShowGeneralExtract;
            result["ShowLocalKitchenExtract"] = ShowLocalKitchenExtract;
            result["ShowTransfer"] = ShowTransfer;
            result["ShowDoorRequirements"] = ShowDoorRequirements;
            result["ShowValues"] = ShowValues;
            result["ShowSpaceNetAirflow"] = ShowSpaceNetAirflow;
            result["ShowCompliance"] = ShowCompliance;
            result["ShowUnresolved"] = ShowUnresolved;
            result["ShowOutdoorAndExhaust"] = ShowOutdoorAndExhaust;
            result["ShowContextGeometry"] = ShowContextGeometry;

            JsonArray jsonArray = [];
            foreach (PartFAnnotationOverride partFAnnotationOverride in AnnotationOverrides ?? [])
            {
                JsonObject jsonObject = partFAnnotationOverride?.ToJsonObject();
                if (jsonObject is not null)
                {
                    jsonArray.Add(jsonObject);
                }
            }

            result["AnnotationOverrides"] = jsonArray;

            return result;
        }
    }
}
