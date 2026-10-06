// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SAM.Analytical.UI.WPF.Tests
{
    /// <summary>
    /// The one xUnit collection every class that instantiates a WPF component belongs to, so that no two
    /// of them load BAML at the same moment.
    /// <para>
    /// xUnit's default is one collection per test class and collections run in parallel, so three classes
    /// each constructing a <c>PartOIterationWindow</c> could call
    /// <see cref="System.Windows.Application.LoadComponent(object, Uri)"/> for the same compiled XAML
    /// concurrently. That load goes through a single <c>System.IO.Packaging.PackagePart</c> per resource
    /// URI, and the part tracks its handed-out streams in a plain <c>List&lt;Stream&gt;</c> that
    /// <c>GetStream</c> mutates without a lock. Two threads inside it leave the list in a state where
    /// <c>CleanUpRequestedStreamsList</c> reads a slot the other thread has already removed, and the
    /// framework throws <see cref="NullReferenceException"/> from
    /// <c>PackagePart.IsStreamClosed(Stream)</c> before any of our code runs.
    /// </para>
    /// <para>
    /// It is a test-host hazard only. The application constructs each window once, from the STA thread
    /// that owns the UI, so production never has two threads loading one component; the parallel work in
    /// <c>Modify.RunWorkflow</c> runs calculations and marshals progress text, and loads no components.
    /// Serialising here therefore fixes the harness without conceding anything about the product.
    /// </para>
    /// <para>
    /// Membership is the whole mechanism: tests inside one collection never run concurrently with each
    /// other, while everything else in the assembly keeps running in parallel.
    /// <see cref="WpfCollectionTests"/> keeps the membership honest.
    /// </para>
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class WpfCollection
    {
        /// <summary>
        /// The collection name. Referenced as <c>[Collection(WpfCollection.Name)]</c> rather than as a
        /// loose string so a rename cannot silently drop a class out of the collection.
        /// </summary>
        public const string Name = "WPF component loading";
    }

    /// <summary>
    /// Proves <see cref="WpfCollection"/> still contains every class it needs to.
    /// </summary>
    public class WpfCollectionTests
    {
        /// <summary>
        /// A class that instantiates a WPF component has to say so with a StaFact-family attribute -
        /// <c>[WpfFact]</c>, <c>[WpfTheory]</c>, <c>[StaFact]</c> and the rest all come from Xunit.StaFact -
        /// because the component can only be constructed on an STA thread. That makes the attribute a
        /// reliable marker for "this class loads BAML", and the rule this test enforces is the one a future
        /// author is most likely to miss: mark the test STA, forget the collection, and quietly reintroduce
        /// the race for whoever runs the suite next.
        /// </summary>
        [Fact]
        public void EveryClassWithStaTests_IsInTheWpfCollection()
        {
            List<string> unenrolled = new List<string>();

            foreach (Type type in typeof(WpfCollection).Assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract)
                {
                    continue;
                }

                bool hasStaTest = type
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SelectMany(x => x.GetCustomAttributes(inherit: true))
                    .Any(x => x.GetType().Assembly.GetName().Name == "Xunit.StaFact");

                if (!hasStaTest)
                {
                    continue;
                }

                // xUnit's CollectionAttribute keeps the name to itself - the framework reads it
                // reflectively and there is no public property - so the constructor argument is read the
                // same way rather than through the attribute instance.
                string collection = type
                    .GetCustomAttributesData()
                    .Where(x => x.AttributeType == typeof(CollectionAttribute))
                    .Select(x => x.ConstructorArguments.Count == 1 ? x.ConstructorArguments[0].Value as string : null)
                    .FirstOrDefault();

                if (collection != WpfCollection.Name)
                {
                    unenrolled.Add(type.FullName);
                }
            }

            Assert.True(
                unenrolled.Count == 0,
                string.Format(
                    "These classes run STA tests but are not in the \"{0}\" collection, so they can load BAML "
                        + "concurrently with another class and fail intermittently inside PackagePart.GetStream. "
                        + "Add [Collection(WpfCollection.Name)] to: {1}",
                    WpfCollection.Name,
                    string.Join(", ", unenrolled)));
        }
    }
}
