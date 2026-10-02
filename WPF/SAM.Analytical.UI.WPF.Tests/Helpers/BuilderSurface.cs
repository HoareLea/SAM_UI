// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SAM.Analytical.UI.WPF.Tests.Helpers
{
    /// <summary>
    /// The Stage E0-1 Glazing System Builder code, for the structural mutation-invariant test: every Builder type (with its compiler-generated
    /// closures and state machines) and the Builder's <c>Query</c> methods, and everything in the WPF assembly they call, scanned for any
    /// field / property / parameter of a model type and any IL reference to a model type or to <c>SetJSAMObject</c>.
    /// </summary>
    internal static class BuilderSurface
    {
        private static readonly string[] QueryMethods = { "ComposeGlazingSystem", "GapMaterial", "Reverse", "Swap", "Millimetres", "CheckGlazingDraft", "SamIssues", "Records", "FromSam", "DraftIndex", "MaterialKind", "Error", "Warning" };

        private static readonly string[] ModelTypes = { "SAM.Analytical.UI.UIAnalyticalModel", "SAM.Analytical.AnalyticalModel", "SAM.Analytical.AdjacencyCluster" };

        private static readonly string[] ModelMethods = { "SetJSAMObject", "Undo", "Redo" };

        public static IEnumerable<Type> Types()
        {
            Assembly assembly = typeof(GlazingSystemDraft).Assembly;
            string[] names =
            {
                nameof(GlazingSystemDraft), nameof(DraftLayer), nameof(DraftPane), nameof(DraftGap), nameof(DraftFrame), "GlazingLayerOrder", nameof(GlazingComposeOptions),
                nameof(GlazingComposition), "GlazingMaterialMerge", nameof(GlazingValuesCache), nameof(GlazingBuilderProvenance), nameof(GlazingBuilderPaneRecord),
                nameof(GlazingBuilderGapRecord), nameof(GlazingDraftIssue), nameof(GlazingDraftIssueCodes), nameof(GlazingDraftValidation), nameof(DraftGlazingEvaluator),
                nameof(DraftGlazingEvaluation), nameof(UserGlazingLibrary), nameof(UserGlazingLibraryContent), nameof(UserGlazingSaveResult), "GlazingGapOrientation",

                // Stage E0-3: the Builder's view-model, its rows, the pane browser and the options it is given.
                nameof(GlazingBuilderViewModel), nameof(GlazingBuilderLayerRow), nameof(GlazingBuilderIssueRow), nameof(GlazingBuilderOptions), nameof(GlazingFrameChoice),
                nameof(GlazingIntendedUse), nameof(GlazingGasOption), nameof(GlazingReferenceWindow), nameof(GlazingPaneBrowser), nameof(GlazingPaneSource), nameof(GlazingPaneEntry),
            };

            return names.Select(x => assembly.GetType("SAM.Analytical.UI.WPF." + x, true));
        }

        /// <summary>Every model reference reachable from the Builder (empty = none).</summary>
        public static IReadOnlyList<string> ModelReferences()
        {
            List<MethodBase> roots = new List<MethodBase>();
            foreach (Type type in Types())
            {
                roots.AddRange(Methods(type, true));
            }

            Type query = typeof(Query);
            roots.AddRange(Methods(query, false).Where(x => QueryMethods.Contains(x.Name)));
            foreach (Type nested in query.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                roots.AddRange(Methods(nested, true).Where(x => QueryMethods.Any(name => x.Name.Contains("<" + name + ">") || x.DeclaringType.Name.Contains("<" + name + ">"))));
            }

            return Scan(Types(), roots);
        }

        /// <summary>The same scan over other types (to show it is not vacuous).</summary>
        public static IReadOnlyList<string> ModelReferences(params Type[] types)
        {
            return Scan(types, types.SelectMany(x => Methods(x, true)).ToList());
        }

        private static IReadOnlyList<string> Scan(IEnumerable<Type> types, List<MethodBase> roots)
        {
            List<string> findings = new List<string>();
            foreach (Type type in types)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (IsModel(field.FieldType))
                    {
                        findings.Add(type.Name + "." + field.Name + " is a " + field.FieldType.Name);
                    }
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (IsModel(property.PropertyType))
                    {
                        findings.Add(type.Name + "." + property.Name + " is a " + property.PropertyType.Name);
                    }
                }
            }

            Assembly assembly = typeof(GlazingSystemDraft).Assembly;
            HashSet<MethodBase> visited = new HashSet<MethodBase>();
            Queue<MethodBase> queue = new Queue<MethodBase>(roots);
            while (queue.Count != 0)
            {
                MethodBase method = queue.Dequeue();
                if (method == null || !visited.Add(method))
                {
                    continue;
                }

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    if (IsModel(parameter.ParameterType))
                    {
                        findings.Add(Describe(method) + " takes a " + parameter.ParameterType.Name);
                    }
                }

                foreach (MemberInfo member in References(method))
                {
                    Type declaringType = member is Type type ? type : member.DeclaringType;
                    if (IsModel(declaringType) || (member is MethodBase called && ModelMethods.Contains(called.Name)))
                    {
                        findings.Add(Describe(method) + " references " + (declaringType?.Name ?? "?") + "." + member.Name);
                    }

                    if (member is MethodBase target && target.Module.Assembly == assembly && target.GetMethodBody() != null)
                    {
                        queue.Enqueue(target);
                        // A compiler-generated state machine or closure reached through a call or a type token is scanned whole.
                        foreach (Type generic in target.IsGenericMethod ? ((MethodInfo)target).GetGenericArguments() : Type.EmptyTypes)
                        {
                            EnqueueGenerated(queue, generic, assembly);
                        }
                    }
                    else if (member is MethodInfo external && external.IsGenericMethod)
                    {
                        foreach (Type generic in external.GetGenericArguments())
                        {
                            EnqueueGenerated(queue, generic, assembly);
                        }
                    }
                    else if (member is Type typeReference)
                    {
                        EnqueueGenerated(queue, typeReference, assembly);
                    }
                }
            }

            return findings.Distinct().ToList();
        }

        private static void EnqueueGenerated(Queue<MethodBase> queue, Type type, Assembly assembly)
        {
            if (type != null && type.Assembly == assembly && type.Name.Contains("<"))
            {
                foreach (MethodBase method in Methods(type, true))
                {
                    queue.Enqueue(method);
                }
            }
        }

        private static bool IsModel(Type type)
        {
            while (type != null && (type.IsArray || type.IsByRef || type.IsPointer))
            {
                type = type.GetElementType();
            }

            if (type == null)
            {
                return false;
            }

            if (ModelTypes.Contains(type.FullName))
            {
                return true;
            }

            return type.IsGenericType && type.GetGenericArguments().Any(IsModel);
        }

        private static IEnumerable<MethodBase> Methods(Type type, bool nested)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            IEnumerable<MethodBase> result = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));
            if (nested)
            {
                foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    result = result.Concat(Methods(nestedType, true));
                }
            }

            return result;
        }

        private static string Describe(MethodBase method) => method.DeclaringType?.Name + "." + method.Name;

        private static Dictionary<short, OpCode> opCodes;

        private static Dictionary<short, OpCode> OpCodesByValue()
        {
            return opCodes ?? (opCodes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static).Select(x => (OpCode)x.GetValue(null)).ToDictionary(x => x.Value));
        }

        // The members an IL body refers to (methods, fields, types).
        private static IEnumerable<MemberInfo> References(MethodBase method)
        {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null)
            {
                yield break;
            }

            Module module = method.Module;
            Type[] typeArguments = method.DeclaringType != null && method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
            Type[] methodArguments = method.IsGenericMethod ? method.GetGenericArguments() : null;
            Dictionary<short, OpCode> codes = OpCodesByValue();

            int position = 0;
            while (position < il.Length)
            {
                short value = il[position++];
                if (value == 0xFE)
                {
                    value = unchecked((short)(0xFE00 | il[position++]));
                }

                if (!codes.TryGetValue(value, out OpCode opCode))
                {
                    yield break;
                }

                switch (opCode.OperandType)
                {
                    case OperandType.InlineNone:
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        position += 1;
                        break;
                    case OperandType.InlineVar:
                        position += 2;
                        break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        position += 8;
                        break;
                    case OperandType.InlineSwitch:
                        int count = BitConverter.ToInt32(il, position);
                        position += 4 + 4 * count;
                        break;
                    case OperandType.InlineField:
                    case OperandType.InlineMethod:
                    case OperandType.InlineTok:
                    case OperandType.InlineType:
                        int token = BitConverter.ToInt32(il, position);
                        position += 4;
                        MemberInfo member = null;
                        try
                        {
                            member = module.ResolveMember(token, typeArguments, methodArguments);
                        }
                        catch (Exception)
                        {
                        }

                        if (member != null)
                        {
                            yield return member;
                        }

                        break;
                    default:
                        position += 4;
                        break;
                }
            }
        }
    }
}
