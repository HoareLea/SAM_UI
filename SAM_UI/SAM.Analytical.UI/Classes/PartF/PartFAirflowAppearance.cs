// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// How each kind of air is drawn in the Part F airflow overlay: its colour, its line style, its arrow
    /// head, its terminal symbol and its label.
    /// <para>
    /// One definition, in one place. A renderer that carried its own colours would drift from the legend
    /// beside it the first time either was edited, and a printed drawing would then disagree with the key
    /// that explains it.
    /// </para>
    /// <para>
    /// Colours follow the German ventilation convention used across European ventilation drawings, which
    /// SAM already reads as familiar: supply air red, extract air yellow, outdoor air green, exhaust air
    /// brown. Transfer air has no convention of its own and is drawn grey and dashed, being the one flow
    /// that is not in a duct.
    /// </para>
    /// <para>
    /// <b>Never colour alone.</b> Every air type also carries a distinct line pattern, arrow head, terminal
    /// symbol and text abbreviation, so the drawing reads in monochrome, in print and to a reader with
    /// colour vision deficiency. That is why <see cref="LinePattern"/> and <see cref="Abbreviation"/> are
    /// not optional extras on this record.
    /// </para>
    /// </summary>
    public class PartFAirflowAppearance
    {
        /// <summary>The kinds of air the overlay distinguishes.</summary>
        public enum AirType
        {
            /// <summary>Outdoor air entering the ventilation unit. ODA.</summary>
            OutdoorAir,

            /// <summary>Mechanical supply into a habitable room. SUP.</summary>
            Supply,

            /// <summary>General wet room extract. EX.</summary>
            GeneralExtract,

            /// <summary>Extract local to the cooking function. KEX.</summary>
            LocalKitchenExtract,

            /// <summary>Air moving between rooms through a door or permanent opening. TRA.</summary>
            TransferAir,

            /// <summary>Air discharged to outside. EHA.</summary>
            ExhaustAir,
        }

        /// <summary>How a line is drawn, so the overlay reads without colour.</summary>
        public enum Pattern
        {
            /// <summary>An unbroken line.</summary>
            Solid,

            /// <summary>A dashed line.</summary>
            Dashed,

            /// <summary>Two parallel solid lines, for local kitchen extract.</summary>
            DoubleSolid,
        }

        private PartFAirflowAppearance(AirType airType, string abbreviation, string name, byte red, byte green, byte blue, Pattern linePattern, double thickness, string terminalSymbol)
        {
            Type = airType;
            Abbreviation = abbreviation;
            Name = name;
            Red = red;
            Green = green;
            Blue = blue;
            LinePattern = linePattern;
            Thickness = thickness;
            TerminalSymbol = terminalSymbol;
        }

        /// <summary>Which kind of air this describes.</summary>
        public AirType Type { get; }

        /// <summary>
        /// The label drawn on the arrow: SUP, EX, KEX, TRA, ODA or EHA. Short enough to sit on a plan and
        /// unambiguous without the colour.
        /// </summary>
        public string Abbreviation { get; }

        /// <summary>The full name, for the legend and the selection panel.</summary>
        public string Name { get; }

        /// <summary>Red component of the line colour, 0 to 255.</summary>
        public byte Red { get; }

        /// <summary>Green component of the line colour, 0 to 255.</summary>
        public byte Green { get; }

        /// <summary>Blue component of the line colour, 0 to 255.</summary>
        public byte Blue { get; }

        /// <summary>How the line is drawn, so the overlay reads in monochrome.</summary>
        public Pattern LinePattern { get; }

        /// <summary>Base line thickness. Arrow thickness may scale with flow, but never below this.</summary>
        public double Thickness { get; }

        /// <summary>
        /// The symbol drawn at the terminal end, so a supply diffuser and an extract grille are told apart
        /// by shape as well as by colour.
        /// </summary>
        public string TerminalSymbol { get; }

        /// <summary>The colour as a hex string, for a legend or an export.</summary>
        public string Hex
        {
            get { return string.Format("#{0:X2}{1:X2}{2:X2}", Red, Green, Blue); }
        }

        /// <summary>
        /// The definitions, in the order they appear in the legend: outdoor air in, then what the system
        /// does with it, then what leaves.
        /// </summary>
        public static IReadOnlyList<PartFAirflowAppearance> All { get; } =
        [
            //Green: outdoor air. The air the system takes in before it is anything else.
            new(AirType.OutdoorAir, "ODA", "Outdoor air", 0x2E, 0x8B, 0x2E, Pattern.Solid, 2.0, "▷"),

            //Red: supply air. Solid, because it is ducted.
            new(AirType.Supply, "SUP", "Supply air", 0xD0, 0x21, 0x1C, Pattern.Solid, 2.5, "▶"),

            //Yellow: extract air. Solid, because it is ducted. Drawn with a dark outline by the renderer,
            //because yellow on a light background is hard to see on its own.
            new(AirType.GeneralExtract, "EX", "General extract air", 0xE8, 0xB4, 0x00, Pattern.Solid, 2.5, "◀"),

            //Local kitchen extract shares the extract colour, because that is what it is, but is drawn
            //thicker and doubled and labelled KEX, so it is never mistaken for general wet room extract.
            //The two are different Part F requirements from different paragraphs.
            new(AirType.LocalKitchenExtract, "KEX", "Local kitchen extract air", 0xE8, 0xB4, 0x00, Pattern.DoubleSolid, 4.0, "◀"),

            //Grey and dashed: transfer air. The one flow that is not in a duct, so it is drawn as the one
            //line that is not continuous.
            new(AirType.TransferAir, "TRA", "Transfer air", 0x6E, 0x6E, 0x6E, Pattern.Dashed, 2.0, "→"),

            //Brown: exhaust air. What the system discharges.
            new(AirType.ExhaustAir, "EHA", "Exhaust air", 0x8B, 0x5A, 0x2B, Pattern.Solid, 2.0, "▷"),
        ];

        /// <summary>The definition for one kind of air.</summary>
        public static PartFAirflowAppearance Get(AirType airType)
        {
            foreach (PartFAirflowAppearance result in All)
            {
                if (result.Type == airType)
                {
                    return result;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(airType));
        }

        /// <summary>
        /// The definition for one terminal role. A local kitchen extract terminal is deliberately NOT
        /// drawn as general extract: they are different Part F requirements and the drawing has to show
        /// which is which.
        /// </summary>
        public static PartFAirflowAppearance Get(PartFTerminalRole partFTerminalRole)
        {
            return partFTerminalRole switch
            {
                PartFTerminalRole.Supply => Get(AirType.Supply),
                PartFTerminalRole.LocalKitchenExtract => Get(AirType.LocalKitchenExtract),
                PartFTerminalRole.GeneralExtract => Get(AirType.GeneralExtract),
                _ => Get(AirType.TransferAir),
            };
        }

        /// <summary>
        /// The label drawn on an arrow: the abbreviation and the rate. Every arrow shows its exact value,
        /// whatever its thickness, because thickness conveys magnitude only approximately and a reader
        /// scheduling equipment needs the number.
        /// </summary>
        public string Label(double? value_Lps)
        {
            return value_Lps is null
                ? string.Format("{0} not calculated", Abbreviation)
                : string.Format("{0} {1:0.0} l/s", Abbreviation, value_Lps.Value);
        }

        /// <summary>
        /// How a compliance status is signalled. Never colour alone: each carries a symbol as well, so a
        /// failure is visible in monochrome and to a reader with colour vision deficiency.
        /// </summary>
        public static (string Symbol, string Description, byte Red, byte Green, byte Blue) Status(PartFComplianceStatus partFComplianceStatus)
        {
            return partFComplianceStatus switch
            {
                //Neutral rather than green: a pass is the ordinary case and does not need to shout.
                PartFComplianceStatus.Pass => ("✓", "Pass", 0x33, 0x33, 0x33),
                PartFComplianceStatus.UserConfirmed => ("✓", "User confirmed", 0x1E, 0x63, 0xB0),
                PartFComplianceStatus.Fail => ("✖", "Fail", 0xC0, 0x1A, 0x1A),
                PartFComplianceStatus.CannotBeDetermined => ("?", "Cannot be determined", 0x8A, 0x6D, 0x00),
                PartFComplianceStatus.EngineeringReviewRequired => ("⚠", "Engineering review required", 0xB5, 0x6E, 0x00),
                PartFComplianceStatus.NotApplicable => ("–", "Not applicable", 0x77, 0x77, 0x77),
                _ => ("·", "Not assessed", 0x77, 0x77, 0x77),
            };
        }
    }
}
