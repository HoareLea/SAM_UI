// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.UI
{
    /// <summary>
    /// The stable identity a manually positioned Part F annotation is stored against.
    /// <para>
    /// <b>The obvious key does not work.</b> A <c>PartFVentilationTerminalRequirement</c> and a
    /// <c>PartFDoorTransferData</c> are <c>SAMObject</c>s built by the calculator, and a <c>SAMObject</c>
    /// built from a name gets <c>Guid.NewGuid()</c>. So a terminal's and a route's own guid are
    /// <b>freshly generated on every calculation</b>. They survive a save and reopen, because the
    /// assessment is serialised with them - and they do not survive a recalculation, which is the thing an
    /// engineer does most often. Keyed on those, a "TRA 8 l/s" somebody dragged out of the way would come
    /// back to the middle of the plan the next time the model was recalculated, and there would be nothing
    /// on screen to explain why.
    /// </para>
    /// <para>
    /// So an annotation is keyed on a guid <b>derived from the persistent model identities</b> the
    /// annotation actually concerns - a space, an aperture - which are serialised with the analytical model
    /// and are the same before and after a recalculation. The derivation is deterministic, so no identity
    /// has to be stored anywhere: the same model always produces the same key, on any machine, without
    /// persisting a single duplicate airflow value.
    /// </para>
    /// <para>
    /// The identities used, and why:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Terminal</b>: the space and the terminal role. The calculator creates at most one terminal
    /// per role in a space - supply, local kitchen extract, general extract - so the pair identifies it, and
    /// <c>PartFFloorPlanOverlay.Refresh</c> already relies on that when it re-reads rates.</item>
    /// <item><b>Transfer through a modelled door</b>: the aperture. It is the physical thing the route goes
    /// through, and where two doors connect the same two rooms it is the only thing that tells the two
    /// routes apart.</item>
    /// <item><b>Transfer with no modelled opening</b>: the two spaces, ordered canonically so the key does
    /// not depend on which way the air was calculated to move. The direction of an unloaded route is
    /// decided by flow sign and can legitimately flip when the model is edited; the partition between two
    /// rooms is the same partition either way, and a label a person put on it should stay there.</item>
    /// </list>
    /// <para>
    /// <b>Presentation only.</b> These keys exist so a drawing can remember where somebody put a label.
    /// Nothing about the assessment depends on them, and no airflow value is stored against them.
    /// </para>
    /// </summary>
    public static class PartFAnnotationKey
    {
        /// <summary>
        /// Namespace for the derivation, so a key can only ever collide with another Part F annotation key
        /// and never with a real model guid.
        /// </summary>
        private static readonly Guid guid_Namespace = new("6f1b0f2a-3c4d-4e5f-8a9b-0c1d2e3f4a5b");

        /// <summary>The key for a terminal's rate label, from the space it serves and its role.</summary>
        public static Guid Terminal(Guid guid_Space, PartFTerminalRole partFTerminalRole)
        {
            return Derive("terminal", guid_Space, partFTerminalRole.ToString());
        }

        /// <summary>
        /// The key for a transfer route's label: the aperture where the model has one, and otherwise the two
        /// spaces the route connects, in a canonical order so the key survives the route being reported the
        /// other way round.
        /// </summary>
        public static Guid Transfer(Guid guid_Aperture, Guid guid_Space_1, Guid guid_Space_2)
        {
            if (guid_Aperture != Guid.Empty)
            {
                return Derive("transfer.aperture", guid_Aperture, null);
            }

            //Ordered, not upstream-then-downstream: which of the two is upstream is a calculated result and
            //may change, and this is the label on the partition between them either way.
            bool ordered = guid_Space_1.CompareTo(guid_Space_2) <= 0;

            return Derive("transfer.spaces", ordered ? guid_Space_1 : guid_Space_2, (ordered ? guid_Space_2 : guid_Space_1).ToString());
        }

        /// <summary>The key for a space's net airflow label.</summary>
        public static Guid SpaceNetAirflow(Guid guid_Space)
        {
            return Derive("space.net", guid_Space, null);
        }

        /// <summary>
        /// A guid derived from a namespace and the given components, in the manner of RFC 4122's name-based
        /// UUIDs: hash the namespace and the name, take the first sixteen bytes, and stamp the version and
        /// variant bits so the result is a well-formed guid.
        /// <para>
        /// The hash is used as a spreading function and not as a security primitive - the whole point is
        /// that it is reproducible, which is why it is not salted and must never become so.
        /// </para>
        /// </summary>
        private static Guid Derive(string discriminator, Guid guid, string text)
        {
            List<byte> bytes = [.. guid_Namespace.ToByteArray()];

            bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(discriminator));
            bytes.AddRange(guid.ToByteArray());

            if (!string.IsNullOrEmpty(text))
            {
                bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(text));
            }

            byte[] hash;

            using (System.Security.Cryptography.SHA256 sHA256 = System.Security.Cryptography.SHA256.Create())
            {
                hash = sHA256.ComputeHash([.. bytes]);
            }

            byte[] result = new byte[16];
            Array.Copy(hash, result, 16);

            //Version 8 (custom) and the RFC 4122 variant, so this is a valid guid and is visibly not a
            //model identity that happens to look similar.
            result[7] = (byte)((result[7] & 0x0F) | 0x80);
            result[8] = (byte)((result[8] & 0x3F) | 0x80);

            return new Guid(result);
        }
    }
}
