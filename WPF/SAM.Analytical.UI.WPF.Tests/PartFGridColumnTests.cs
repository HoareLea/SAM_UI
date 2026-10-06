// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI.WPF;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Regression tests for the rule that decides which Part F assessment grid columns are editable.
    /// <para>
    /// The Terminals tab crashed on first display. Every one of its columns was built with the default
    /// two-way binding while <see cref="PartFTerminalRow"/> is get-only throughout, and WPF throws
    /// <see cref="InvalidOperationException"/> when it attaches a two-way binding to a read-only property.
    /// It attaches that binding when the cell is first realised - which is when the tab is first clicked,
    /// not when the window opens - so the window came up looking healthy and fell over on the first click.
    /// </para>
    /// <para>
    /// The fix took the decision away from the call site: editability is now derived from the row type. No
    /// per-column boolean can disagree with the property it binds to, because there is no per-column
    /// boolean. These tests hold that.
    /// </para>
    /// </summary>
    public class PartFGridColumnTests
    {
        /// <summary>A property with no setter must produce a read-only, one-way column.</summary>
        [Theory]
        [InlineData(typeof(PartFTerminalRow), "SpaceName")]
        [InlineData(typeof(PartFTerminalRow), "Status")]
        [InlineData(typeof(PartFTerminalRow), "Required")]
        [InlineData(typeof(PartFTerminalRow), "Proposed")]
        [InlineData(typeof(PartFTerminalRow), "Provided")]
        [InlineData(typeof(PartFTerminalRow), "Provision")]
        [InlineData(typeof(PartFDoorRow), "RequiredArea")]
        [InlineData(typeof(PartFDoorRow), "Continuous")]
        [InlineData(typeof(PartFPurgeRow), "Required")]
        [InlineData(typeof(PartFCheckRow), "Calculated")]
        [InlineData(typeof(PartFCheckRow), "Status")]
        public void GetOnlyProperty_GivesAReadOnlyColumn(Type rowType, string path)
        {
            Assert.True(PartFGridColumn.IsReadOnly(rowType, path));
        }

        /// <summary>A property with a setter must produce an editable, two-way column.</summary>
        [Theory]
        [InlineData(typeof(PartFDoorRow), "ProvidedUndercut")]
        [InlineData(typeof(PartFDoorRow), "Device")]
        [InlineData(typeof(PartFPurgeRow), "OpeningType")]
        [InlineData(typeof(PartFCheckRow), "UserEvidence")]
        [InlineData(typeof(PartFCheckRow), "AlternativeComplianceMethod")]
        [InlineData(typeof(PartFCheckRow), "OverrideReason")]
        public void SettableProperty_GivesAnEditableColumn(Type rowType, string path)
        {
            Assert.False(PartFGridColumn.IsReadOnly(rowType, path));
        }

        /// <summary>
        /// A column bound to a property that does not exist fails loudly at construction. Silently binding
        /// to nothing renders an empty cell, which is indistinguishable from a value nobody recorded.
        /// </summary>
        [Fact]
        public void UnknownPath_Throws()
        {
            Assert.Throws<ArgumentException>(() => PartFGridColumn.IsReadOnly(typeof(PartFTerminalRow), "NoSuchProperty"));
        }

        /// <summary>
        /// Every value on a terminal row is calculated from the Approved Document and none of it is an
        /// engineering choice, so the whole row is get-only. Adding a setter here would silently make that
        /// column editable, which is why this is asserted rather than left to the class comment.
        /// </summary>
        [Fact]
        public void TerminalRow_IsReadOnlyThroughout()
        {
            PropertyInfo[] settable = [.. typeof(PartFTerminalRow)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.SetMethod is not null && x.SetMethod.IsPublic)];

            Assert.True(settable.Length == 0, string.Format("PartFTerminalRow should be read-only throughout, but these properties have public setters: {0}.", string.Join(", ", settable.Select(x => x.Name))));
        }
    }
}
