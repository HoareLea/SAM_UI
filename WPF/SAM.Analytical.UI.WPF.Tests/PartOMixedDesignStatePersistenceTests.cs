// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.UI;
using System;
using System.IO;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// When the mixed Part O design keeps its <c>&lt;model&gt;.partomixed.json</c> beside the model.
    /// <para>
    /// Acceptance of 30 Sep 2026 opened Mixed Design on an Iteration 2 result: the window correctly refused the model
    /// as a baseline, and closing it still wrote a sidecar into <c>Iteration2/tas</c>. The command writes the state
    /// every time the window closes (<c>Modify.RunPartOMixedDesign</c>), through
    /// <see cref="Modify.WritePartOMixedDesignState"/> - so that is the seam driven here, with the session the
    /// command builds.
    /// </para>
    /// </summary>
    public class PartOMixedDesignStatePersistenceTests : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "SAM_PartOMixedState_" + Guid.NewGuid().ToString("N"));

        public PartOMixedDesignStatePersistenceTests()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
            }
        }

        /// <summary>A run output - SAM's own materialisation of the baseline - which SAM refuses as a baseline.</summary>
        private static AnalyticalModel Refused()
        {
            PartOMaterialisation partOMaterialisation = PartOMixedDesignFixture.WithStrategies(PartOMixedDesignFixture.Baseline(), PartOMixedDesignFixture.Natural).MaterialisePartODwellingStrategies();
            Assert.True(partOMaterialisation.IsMaterialised, partOMaterialisation.Refusal);

            return partOMaterialisation.AnalyticalModel;
        }

        /// <summary>The session exactly as the command opens it: the model's path, and whatever state is beside it.</summary>
        private static PartOMixedDesignSession Open(AnalyticalModel analyticalModel, string path_Model)
        {
            PartOMixedDesignSession partOMixedDesignSession = new(analyticalModel, path_Model, null, PartOMixedDesignState.Read(PartOMixedDesignState.Path_State(path_Model)));
            partOMixedDesignSession.SimulationCaseKey = PartOMixedDesignFixture.CaseKey;

            return partOMixedDesignSession;
        }

        [Fact]
        public void Closing_after_a_refused_baseline_writes_no_sidecar()
        {
            string path_Model = Path.Combine(directory, "Iteration2.sam");
            string path_State = PartOMixedDesignState.Path_State(path_Model);

            PartOMixedDesignSession partOMixedDesignSession = Open(Refused(), path_Model);
            Assert.False(partOMixedDesignSession.IsCleanBaseline);
            Assert.False(partOMixedDesignSession.OpenedOnCleanBaseline);

            //Whatever was touched in the window before it was closed.
            partOMixedDesignSession.Constraints.CoolingAllowed = !partOMixedDesignSession.Constraints.CoolingAllowed;

            Assert.False(Modify.WritePartOMixedDesignState(partOMixedDesignSession, path_State));
            Assert.False(File.Exists(path_State));
            Assert.Empty(Directory.GetFiles(directory));
        }

        [Fact]
        public void Closing_after_a_refused_baseline_leaves_an_existing_sidecar_as_it_was()
        {
            string path_Model = Path.Combine(directory, "Iteration2.sam");
            string path_State = PartOMixedDesignState.Path_State(path_Model);

            PartOMixedDesignState partOMixedDesignState = new();
            partOMixedDesignState.Constraints.OptimisationAllowed = false;
            Assert.True(partOMixedDesignState.Write(path_State, out string note), note);
            byte[] bytes = File.ReadAllBytes(path_State);
            DateTime dateTime = File.GetLastWriteTimeUtc(path_State);

            PartOMixedDesignSession partOMixedDesignSession = Open(Refused(), path_Model);
            Assert.False(partOMixedDesignSession.Constraints.OptimisationAllowed);
            partOMixedDesignSession.Constraints.OptimisationAllowed = true;

            Assert.False(Modify.WritePartOMixedDesignState(partOMixedDesignSession, path_State));
            Assert.Equal(bytes, File.ReadAllBytes(path_State));
            Assert.Equal(dateTime, File.GetLastWriteTimeUtc(path_State));
        }

        /// <summary>A clean baseline keeps its state exactly as before - created on first close, with the edits made.</summary>
        [Fact]
        public void Closing_on_a_clean_baseline_keeps_the_state_beside_the_model()
        {
            string path_Model = Path.Combine(directory, "Block.sam");
            string path_State = PartOMixedDesignState.Path_State(path_Model);

            PartOMixedDesignSession partOMixedDesignSession = Open(PartOMixedDesignFixture.Baseline(), path_Model);
            Assert.True(partOMixedDesignSession.IsCleanBaseline);
            Assert.True(partOMixedDesignSession.OpenedOnCleanBaseline);

            partOMixedDesignSession.Constraints.OptimisationAllowed = false;

            Assert.True(Modify.WritePartOMixedDesignState(partOMixedDesignSession, path_State));
            Assert.True(File.Exists(path_State));
            Assert.Equal(partOMixedDesignSession.State.ToJsonObject().ToJsonString(), File.ReadAllText(path_State));

            //And it is what the next opening reads.
            Assert.False(Open(PartOMixedDesignFixture.Baseline(), path_Model).Constraints.OptimisationAllowed);
        }

        /// <summary>An unsaved model has nowhere to keep state, clean or not - as before.</summary>
        [Fact]
        public void An_unsaved_model_writes_nothing()
        {
            PartOMixedDesignSession partOMixedDesignSession = new(PartOMixedDesignFixture.Baseline(), null, null, null);

            Assert.False(Modify.WritePartOMixedDesignState(partOMixedDesignSession, PartOMixedDesignState.Path_State(null)));
            Assert.Empty(Directory.GetFiles(directory));
        }
    }
}
