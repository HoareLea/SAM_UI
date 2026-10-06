// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.UI.WPF;
using SAM.Geometry.Spatial;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// Tests for <see cref="MapInternalConditionsControl.BuildSpaceForSemantics(Space, string)"/> - the
    /// proxy that lets the shared-classification cell resolve against a mapping row's current, possibly
    /// auto-proposed or manually edited, ComboBox text rather than the underlying Space's own
    /// InternalCondition, which auto-map and manual edits never mutate.
    /// </summary>
    public class BuildSpaceForSemanticsTests
    {
        private static Space Space(string name)
        {
            return new Space(name, new Point3D(0, 0, 1.5));
        }

        [Fact]
        public void UsesTheGivenTextAsInternalCondition_RegardlessOfTheSpaceSOwn()
        {
            Space space = Space("Bathroom_2");
            space.InternalCondition = new InternalCondition("Old Condition");

            Space result = MapInternalConditionsControl.BuildSpaceForSemantics(space, "TM59_Bathroom");

            Assert.Equal("TM59_Bathroom", result.InternalCondition?.Name);
        }

        [Fact]
        public void BlankText_ClearsInternalCondition()
        {
            Space space = Space("Bathroom_2");
            space.InternalCondition = new InternalCondition("Old Condition");

            Space result = MapInternalConditionsControl.BuildSpaceForSemantics(space, "");

            Assert.Null(result.InternalCondition);
        }

        [Fact]
        public void PreservesTheSpaceSNameAndGuid()
        {
            Space space = Space("Bathroom_2");

            Space result = MapInternalConditionsControl.BuildSpaceForSemantics(space, "TM59_Bathroom");

            Assert.Equal(space.Name, result.Name);
            Assert.Equal(space.Guid, result.Guid);
        }

        [Fact]
        public void DoesNotMutateTheOriginalSpace()
        {
            Space space = Space("Bathroom_2");
            space.InternalCondition = new InternalCondition("Old Condition");

            MapInternalConditionsControl.BuildSpaceForSemantics(space, "TM59_Bathroom");

            Assert.Equal("Old Condition", space.InternalCondition?.Name);
        }
    }
}
