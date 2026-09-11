// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Editor;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.ObfusPasses.ParamPad
{
    public enum JunkKind
    {
        Int32,
        UInt32,
        Int64,
        Single,
        Double,
        Boolean,
        Byte,
        Int16,
        Char,
    }

    /// <summary>How the junk parameters are combined into one value.</summary>
    public enum FoldOp { Xor, Add, Sub, Mul, Or, And }

    /// <summary>How that value is driven to zero. Two of these need no literal zero at all.</summary>
    public enum ZeroOp { MulZero, DupXor, DupSub, AndZero }

    /// <summary>Where the zero goes, so that the junk loads are never dead.</summary>
    public enum SinkKind { BranchPair, SwitchOne, ThreadIntoReturn }

    public class PadPlan
    {
        public MethodDef method;
        // the per-method recipe for consuming the junk. Randomising these is what stops every
        // padded method from opening with one greppable prologue.
        public FoldOp foldOp;
        public ZeroOp zeroOp;
        public SinkKind sink;
        public int[] junkFoldOrder;
        // one entry per slot of the new parameter list. -1 marks a junk slot, otherwise
        // the index of the original parameter that lives there.
        public int[] slotToReal;
        // junk descriptor per slot, only meaningful where slotToReal is -1.
        public JunkKind[] slotKind;
        public object[] slotValue;
        // original parameter index -> new slot index.
        public int[] realToSlot;

        public int RealCount => realToSlot.Length;
        public int SlotCount => slotToReal.Length;
    }

    /// <summary>
    /// Inserts junk parameters at random positions into eligible methods and fixes up every
    /// definition, reference and call site so the result still runs.
    ///
    /// Pure dnlib on purpose: no ObfuscationPassContext, no Unity, so Tests~/ParamPad can
    /// compile this file directly the way Tests~/MemberReorder compiles MemberReorder.cs.
    /// </summary>
    public class ParameterPadding
    {
        private readonly Random _random;
        private readonly int _minCount;
        private readonly int _maxCount;
        private readonly Func<MethodDef, bool> _isSafe;

        private static readonly JunkKind[] s_junkKinds = (JunkKind[])Enum.GetValues(typeof(JunkKind));

        public ParameterPadding(int seed, int minCount, int maxCount, Func<MethodDef, bool> isSafe)
        {
            if (minCount < 1 || maxCount < minCount)
            {
                throw new ArgumentException($"invalid parameter padding range [{minCount},{maxCount}]");
            }
            _random = new Random(seed);
            _minCount = minCount;
            _maxCount = maxCount;
            _isSafe = isSafe;
        }

        private class CallSite
        {
            public MethodDef host;
            public Instruction inst;
            public PadPlan plan;
            // parameter types as seen at this call site, already valid in the host module.
            public TypeSig[] argTypes;
        }

        public int PaddedMethodCount { get; private set; }

        /// <summary>Methods the safety predicate and the structural checks accepted.</summary>
        public int CandidateCount { get; private set; }

        /// <summary>Candidates dropped because a call site could not be rewritten safely.</summary>
        public int VetoedCount { get; private set; }

        /// <summary>
        /// Method operands that could not be resolved at all. Every candidate sharing a name with
        /// one of these is vetoed, because an unresolvable reference might BE that candidate.
        /// </summary>
        public int UnresolvedReferenceCount { get; private set; }

        public void Process(List<ModuleDef> toObfuscate, List<ModuleDef> allModules)
        {
            var candidates = new HashSet<MethodDef>();
            foreach (ModuleDef mod in toObfuscate)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef method in type.Methods)
                    {
                        if (IsCandidate(method))
                        {
                            candidates.Add(method);
                        }
                    }
                }
            }
            if (candidates.Count == 0)
            {
                return;
            }

            // a module can be loaded more than once, in which case resolving a reference hands
            // back a MethodDef from the other instance. Matching on identity alone would then
            // silently miss the call site and ship a broken assembly, so match on token too.
            var byToken = new Dictionary<string, MethodDef>();
            foreach (MethodDef method in candidates)
            {
                byToken[TokenKey(method.Module, method.MDToken.Raw)] = method;
            }

            CandidateCount = candidates.Count;
            var vetoed = new HashSet<MethodDef>();
            var rawSites = new List<CallSite>();
            var refsByMethod = new Dictionary<MethodDef, HashSet<MemberRef>>();
            var unresolvedNames = new HashSet<string>();
            IndexReferences(allModules, byToken, vetoed, rawSites, refsByMethod, unresolvedNames);

            // An operand we could not resolve may well be one of our candidates, so we cannot tell
            // whether its call site needs rewriting. Every other "I do not understand this" path
            // in this pass vetoes; this one must too, or the method is padded with a stale call
            // site left behind and the assembly ships broken.
            if (unresolvedNames.Count > 0)
            {
                foreach (MethodDef candidate in candidates)
                {
                    if (unresolvedNames.Contains(candidate.Name))
                    {
                        vetoed.Add(candidate);
                    }
                }
            }

            candidates.ExceptWith(vetoed);
            VetoedCount = CandidateCount - candidates.Count;
            if (candidates.Count == 0)
            {
                return;
            }

            // a method whose call sites we could not fully index is dropped wholesale, so the
            // transform is never half applied.
            var sites = rawSites.Where(s => candidates.Contains(s.plan.method)).ToList();

            var plans = new Dictionary<MethodDef, PadPlan>();
            foreach (MethodDef method in candidates.OrderBy(m => m.Module.Name.String, StringComparer.Ordinal).ThenBy(m => m.MDToken.Raw))
            {
                plans.Add(method, BuildPlan(method));
            }
            foreach (CallSite site in sites)
            {
                site.plan = plans[site.plan.method];
            }

            foreach (PadPlan plan in plans.Values)
            {
                ApplyToDefinition(plan);
            }
            foreach (var e in refsByMethod)
            {
                if (!plans.TryGetValue(e.Key, out PadPlan plan))
                {
                    continue;
                }
                foreach (MemberRef memberRef in e.Value)
                {
                    ApplyToReference(memberRef, plan);
                }
            }
            RewriteCallSites(sites);
            PaddedMethodCount = plans.Count;

            // CleanUpInstructionPass runs in the Process() phase, which is already over by the
            // time this pass works, so nothing else will compact what we emit.
            var touched = new HashSet<MethodDef>(plans.Keys);
            foreach (CallSite site in sites)
            {
                touched.Add(site.host);
            }
            foreach (MethodDef method in touched)
            {
                CilBody body = method.Body;
                body.OptimizeMacros();
                body.OptimizeBranches();
            }
        }

        /// <summary>
        /// Attributes that pin a method's ARGUMENT LIST, as opposed to its name. The rename
        /// policy does not cover these: a serialization callback is located by attribute, so it
        /// is perfectly safe to rename and still fatal to re-sign — BinaryFormatter and
        /// Newtonsoft both validate the signature and throw. Unity's ContextMenu and the editor
        /// callbacks are invoked with a fixed (usually empty) argument list for the same reason.
        /// </summary>
        private static readonly HashSet<string> s_signaturePinningAttributes = new HashSet<string>
        {
            "System.Runtime.Serialization.OnSerializingAttribute",
            "System.Runtime.Serialization.OnSerializedAttribute",
            "System.Runtime.Serialization.OnDeserializingAttribute",
            "System.Runtime.Serialization.OnDeserializedAttribute",
            "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute",
            "UnityEngine.RuntimeInitializeOnLoadMethodAttribute",
            "UnityEngine.ContextMenu",
            "UnityEditor.MenuItem",
            "UnityEditor.InitializeOnLoadMethodAttribute",
            "UnityEditor.Callbacks.DidReloadScripts",
            "UnityEditor.Callbacks.PostProcessBuildAttribute",
            "UnityEditor.Callbacks.PostProcessSceneAttribute",
            "UnityEditor.Callbacks.OnOpenAssetAttribute",
        };

        private static bool HasSignaturePinningAttribute(MethodDef method)
        {
            foreach (CustomAttribute ca in method.CustomAttributes)
            {
                ITypeDefOrRef attrType = ca.AttributeType;
                if (attrType == null)
                {
                    continue;
                }
                if (s_signaturePinningAttributes.Contains(attrType.FullName)
                    || attrType.Name == ConstValues.MonoPInvokeCallbackAttributeName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsCandidate(MethodDef method)
        {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
            {
                return false;
            }
            if (method.IsPinvokeImpl || method.IsInternalCall || method.IsNative || method.IsRuntime || method.IsUnmanagedExport)
            {
                return false;
            }
            if (method.IsRuntimeSpecialName || method.IsConstructor || method.IsStaticConstructor)
            {
                return false;
            }
            // vtable slots, interface contracts and MethodImpl entries: same pin set as
            // MemberReorder.IsPositionPinnedMethod.
            if (method.IsVirtual || method.IsAbstract || method.HasOverrides || method.IsNewSlot)
            {
                return false;
            }
            // PropertyDef/EventDef carry their own signature and nothing keeps them in step.
            if (method.SemanticsAttributes != 0)
            {
                return false;
            }
            MethodSig sig = method.MethodSig;
            if (sig == null || sig.IsVarArg || sig.ParamsAfterSentinel != null)
            {
                return false;
            }
            TypeDef declaringType = method.DeclaringType;
            if (declaringType == null || declaringType.IsDelegate || declaringType.IsInterface)
            {
                return false;
            }
            if (method.Module != null && method.Module.EntryPoint == method)
            {
                return false;
            }
            if (method.Parameters.Any(p => p.Type != null && p.Type.ElementType == ElementType.TypedByRef))
            {
                return false;
            }
            // `this` as an explicit signature entry would shift the ldarg remap by one.
            if (sig.ExplicitThis)
            {
                return false;
            }
            if (HasSignaturePinningAttribute(method))
            {
                return false;
            }
            return _isSafe(method);
        }

        private static string TokenKey(ModuleDef module, uint token)
        {
            return (module?.Name.String ?? "?") + "!" + token.ToString("X8");
        }

        private void IndexReferences(List<ModuleDef> allModules, Dictionary<string, MethodDef> byToken,
            HashSet<MethodDef> vetoed, List<CallSite> sites, Dictionary<MethodDef, HashSet<MemberRef>> refsByMethod,
            HashSet<string> unresolvedNames)
        {
            var resolveCache = new Dictionary<IMethod, MethodDef>();
            foreach (ModuleDef mod in allModules)
            {
                foreach (TypeDef type in mod.GetTypes())
                {
                    foreach (MethodDef host in type.Methods)
                    {
                        if (!host.HasBody)
                        {
                            continue;
                        }
                        IList<Instruction> instructions = host.Body.Instructions;
                        for (int i = 0; i < instructions.Count; i++)
                        {
                            Instruction inst = instructions[i];
                            if (!(inst.Operand is IMethod operand) || !operand.IsMethod)
                            {
                                continue;
                            }
                            MethodDef resolved = Resolve(operand, resolveCache);
                            if (resolved == null)
                            {
                                if (unresolvedNames.Add(operand.Name))
                                {
                                    UnresolvedReferenceCount++;
                                }
                                continue;
                            }
                            if (!byToken.TryGetValue(TokenKey(resolved.Module, resolved.MDToken.Raw), out MethodDef target))
                            {
                                continue;
                            }
                            switch (inst.OpCode.Code)
                            {
                                case Code.Call:
                                case Code.Callvirt:
                                {
                                    Instruction prev = i > 0 ? instructions[i - 1] : null;
                                    if (prev != null && (prev.OpCode.Code == Code.Constrained || prev.OpCode.Code == Code.Tailcall))
                                    {
                                        vetoed.Add(target);
                                        break;
                                    }
                                    TypeSig[] argTypes = TryGetCallSiteArgTypes(operand);
                                    if (argTypes == null || argTypes.Length != target.MethodSig.Params.Count)
                                    {
                                        vetoed.Add(target);
                                        break;
                                    }
                                    sites.Add(new CallSite
                                    {
                                        host = host,
                                        inst = inst,
                                        plan = new PadPlan { method = target },
                                        argTypes = argTypes,
                                    });
                                    break;
                                }
                                // the signature is pinned by a delegate type or handed to reflection.
                                case Code.Ldftn:
                                case Code.Ldvirtftn:
                                case Code.Ldtoken:
                                case Code.Newobj:
                                case Code.Jmp:
                                default:
                                {
                                    vetoed.Add(target);
                                    break;
                                }
                            }
                            CollectMemberRef(operand, target, refsByMethod);
                        }
                    }
                }
            }
        }

        private static void CollectMemberRef(IMethod operand, MethodDef target, Dictionary<MethodDef, HashSet<MemberRef>> refsByMethod)
        {
            MemberRef memberRef = operand as MemberRef ?? (operand as MethodSpec)?.Method as MemberRef;
            if (memberRef == null)
            {
                return;
            }
            if (!refsByMethod.TryGetValue(target, out HashSet<MemberRef> set))
            {
                set = new HashSet<MemberRef>();
                refsByMethod.Add(target, set);
            }
            set.Add(memberRef);
        }

        private static MethodDef Resolve(IMethod method, Dictionary<IMethod, MethodDef> cache)
        {
            if (method is MethodDef def)
            {
                return def;
            }
            if (cache.TryGetValue(method, out MethodDef cached))
            {
                return cached;
            }
            MethodDef resolved = null;
            try
            {
                resolved = method.ResolveMethodDef();
            }
            catch (Exception)
            {
                resolved = null;
            }
            cache.Add(method, resolved);
            return resolved;
        }

        private static TypeSig[] TryGetCallSiteArgTypes(IMethod operand)
        {
            try
            {
                MethodSig sig = MetaUtil.GetInflatedMethodSig(operand, null);
                if (sig == null || sig.IsVarArg || sig.ParamsAfterSentinel != null)
                {
                    return null;
                }
                if (sig.Params.Any(p => p == null || p.ElementType == ElementType.TypedByRef))
                {
                    return null;
                }
                return sig.Params.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private PadPlan BuildPlan(MethodDef method)
        {
            int realCount = method.MethodSig.Params.Count;
            int junkCount = _random.Next(_minCount, _maxCount + 1);
            int slotCount = realCount + junkCount;

            // choose which slots hold junk
            var junkSlots = new HashSet<int>();
            while (junkSlots.Count < junkCount)
            {
                junkSlots.Add(_random.Next(slotCount));
            }

            // and shuffle the real parameters across the slots left over. Free: the call site
            // already spills every real argument to a local and re-pushes it, so an arbitrary
            // permutation costs exactly the same instructions as the identity one. Arguments are
            // still EVALUATED in source order - only the push order changes - so side effects in
            // argument expressions keep their sequence.
            var realOrder = new int[realCount];
            for (int i = 0; i < realCount; i++)
            {
                realOrder[i] = i;
            }
            for (int i = realCount - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int tmp = realOrder[i];
                realOrder[i] = realOrder[j];
                realOrder[j] = tmp;
            }

            var plan = new PadPlan
            {
                method = method,
                slotToReal = new int[slotCount],
                slotKind = new JunkKind[slotCount],
                slotValue = new object[slotCount],
                realToSlot = new int[realCount],
            };
            int nextReal = 0;
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (junkSlots.Contains(slot))
                {
                    plan.slotToReal[slot] = -1;
                    JunkKind kind = s_junkKinds[_random.Next(s_junkKinds.Length)];
                    plan.slotKind[slot] = kind;
                    plan.slotValue[slot] = MakeJunkValue(kind);
                }
                else
                {
                    int real = realOrder[nextReal++];
                    plan.slotToReal[slot] = real;
                    plan.realToSlot[real] = slot;
                }
            }
            plan.foldOp = (FoldOp)_random.Next(6);
            plan.zeroOp = (ZeroOp)_random.Next(4);
            plan.sink = (SinkKind)_random.Next(3);

            // fold the junk in a shuffled order too, so even the ldarg sequence differs
            var junkOrder = new List<int>();
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (plan.slotToReal[slot] < 0)
                {
                    junkOrder.Add(slot);
                }
            }
            for (int i = junkOrder.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int tmp = junkOrder[i];
                junkOrder[i] = junkOrder[j];
                junkOrder[j] = tmp;
            }
            plan.junkFoldOrder = junkOrder.ToArray();
            return plan;
        }

        private object MakeJunkValue(JunkKind kind)
        {
            switch (kind)
            {
                case JunkKind.Int32: return _random.Next(int.MinValue, int.MaxValue);
                case JunkKind.UInt32: return _random.Next(int.MinValue, int.MaxValue);
                case JunkKind.Int64: return ((long)_random.Next() << 32) | (uint)_random.Next();
                case JunkKind.Single: return (float)(_random.NextDouble() * 1000.0);
                case JunkKind.Double: return _random.NextDouble() * 1000.0;
                case JunkKind.Boolean: return _random.Next(2);
                case JunkKind.Byte: return _random.Next(256);
                case JunkKind.Int16: return _random.Next(short.MinValue, short.MaxValue + 1);
                case JunkKind.Char: return _random.Next(char.MaxValue + 1);
                default: throw new NotSupportedException(kind.ToString());
            }
        }

        private static TypeSig JunkTypeSig(ICorLibTypes corLibTypes, JunkKind kind)
        {
            switch (kind)
            {
                case JunkKind.Int32: return corLibTypes.Int32;
                case JunkKind.UInt32: return corLibTypes.UInt32;
                case JunkKind.Int64: return corLibTypes.Int64;
                case JunkKind.Single: return corLibTypes.Single;
                case JunkKind.Double: return corLibTypes.Double;
                case JunkKind.Boolean: return corLibTypes.Boolean;
                case JunkKind.Byte: return corLibTypes.Byte;
                case JunkKind.Int16: return corLibTypes.Int16;
                case JunkKind.Char: return corLibTypes.Char;
                default: throw new NotSupportedException(kind.ToString());
            }
        }

        private static Instruction PushJunk(PadPlan plan, int slot)
        {
            object value = plan.slotValue[slot];
            switch (plan.slotKind[slot])
            {
                case JunkKind.Int64: return Instruction.Create(OpCodes.Ldc_I8, (long)value);
                case JunkKind.Single: return Instruction.Create(OpCodes.Ldc_R4, (float)value);
                case JunkKind.Double: return Instruction.Create(OpCodes.Ldc_R8, (double)value);
                default: return Instruction.Create(OpCodes.Ldc_I4, (int)value);
            }
        }

        private static void ApplyToDefinition(PadPlan plan)
        {
            MethodDef method = plan.method;
            CilBody body = method.Body;

            // ldarg.0 and friends carry no operand, so the remap below cannot see them until
            // they are expanded. CleanUpInstructionPass re-compacts afterwards.
            body.SimplifyMacros(method.Parameters);
            body.SimplifyBranches();

            int thisOffset = method.HasThis ? 1 : 0;
            var oldOperandIndex = new List<KeyValuePair<Instruction, int>>();
            foreach (Instruction inst in body.Instructions)
            {
                if (inst.Operand is Parameter param)
                {
                    oldOperandIndex.Add(new KeyValuePair<Instruction, int>(inst, param.Index));
                }
            }

            ICorLibTypes corLibTypes = method.Module.CorLibTypes;
            var oldParams = method.MethodSig.Params.ToList();
            method.MethodSig.Params.Clear();
            for (int slot = 0; slot < plan.SlotCount; slot++)
            {
                int real = plan.slotToReal[slot];
                method.MethodSig.Params.Add(real >= 0 ? oldParams[real] : JunkTypeSig(corLibTypes, plan.slotKind[slot]));
            }
            method.Parameters.UpdateParameterTypes();

            // ParamDef.Sequence is 1 based over the explicit parameters, 0 being the return value.
            foreach (ParamDef paramDef in method.ParamDefs)
            {
                int oldReal = paramDef.Sequence - 1;
                if (oldReal >= 0 && oldReal < plan.RealCount)
                {
                    paramDef.Sequence = (ushort)(plan.realToSlot[oldReal] + 1);
                }
                else if (paramDef.Sequence != 0)
                {
                    // Sequence 0 is the return value and stays. Anything else out of range is
                    // malformed metadata that would collide with a renumbered entry.
                    throw new Exception($"parameter padding found ParamDef sequence {paramDef.Sequence} on `{method}`, "
                        + $"which has {plan.RealCount} parameters.");
                }
            }
            var sortedParamDefs = method.ParamDefs.OrderBy(p => p.Sequence).ToList();
            method.ParamDefs.Clear();
            foreach (ParamDef paramDef in sortedParamDefs)
            {
                method.ParamDefs.Add(paramDef);
            }
            method.Parameters.UpdateParameterTypes();

            // dnlib parameters are addressed by index, so an untouched operand now means a
            // different parameter. Every one of them has to be re-pointed.
            foreach (var e in oldOperandIndex)
            {
                int oldIndex = e.Value;
                int newIndex;
                if (thisOffset == 1 && oldIndex == 0)
                {
                    newIndex = 0;
                }
                else
                {
                    int oldReal = oldIndex - thisOffset;
                    newIndex = plan.realToSlot[oldReal] + thisOffset;
                }
                e.Key.Operand = method.Parameters[newIndex];
            }

            EmitConsumePrologue(plan);
        }

        /// <summary>
        /// Makes the junk parameters load-bearing without making them cost anything.
        ///
        /// Every step is drawn per method — which operator folds the junk, in which order, how
        /// the result is driven to zero, and where the zero is consumed — so there is no single
        /// instruction sequence to grep for. That matters more than the individual tricks: a
        /// fixed prologue is a fingerprint of the obfuscator, and one script keyed on it strips
        /// every junk parameter in the assembly.
        ///
        /// Whatever the recipe, the result is provably zero and is consumed by a branch or folded
        /// into a value the method already returns, so liveness alone cannot delete the parameter
        /// loads, while clang folds the arithmetic away during IL2CPP compilation. Only holds
        /// while this pass runs after ConstEncrypt, which would otherwise turn the literal
        /// constants into VM decrypt calls.
        /// </summary>
        private static void EmitConsumePrologue(PadPlan plan)
        {
            MethodDef method = plan.method;
            CilBody body = method.Body;
            int thisOffset = method.HasThis ? 1 : 0;

            if (plan.junkFoldOrder.Length == 0)
            {
                return;
            }

            var prologue = new List<Instruction>();
            bool first = true;
            foreach (int slot in plan.junkFoldOrder)
            {
                Parameter param = method.Parameters[slot + thisOffset];
                prologue.Add(Instruction.Create(OpCodes.Ldarg, param));
                switch (plan.slotKind[slot])
                {
                    case JunkKind.Int64:
                        prologue.Add(Instruction.Create(OpCodes.Conv_I4));
                        break;
                    case JunkKind.Single:
                        prologue.Add(Instruction.Create(OpCodes.Ldc_R4, 0f));
                        prologue.Add(Instruction.Create(OpCodes.Ceq));
                        break;
                    case JunkKind.Double:
                        prologue.Add(Instruction.Create(OpCodes.Ldc_R8, 0d));
                        prologue.Add(Instruction.Create(OpCodes.Ceq));
                        break;
                }
                if (!first)
                {
                    prologue.Add(Instruction.Create(FoldOpCode(plan.foldOp)));
                }
                first = false;
            }

            // drive the fold to zero
            switch (plan.zeroOp)
            {
                case ZeroOp.MulZero:
                    prologue.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    prologue.Add(Instruction.Create(OpCodes.Mul));
                    break;
                case ZeroOp.AndZero:
                    prologue.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    prologue.Add(Instruction.Create(OpCodes.And));
                    break;
                case ZeroOp.DupXor:
                    prologue.Add(Instruction.Create(OpCodes.Dup));
                    prologue.Add(Instruction.Create(OpCodes.Xor));
                    break;
                case ZeroOp.DupSub:
                    prologue.Add(Instruction.Create(OpCodes.Dup));
                    prologue.Add(Instruction.Create(OpCodes.Sub));
                    break;
            }

            SinkKind sink = plan.sink;
            if (sink == SinkKind.ThreadIntoReturn && !TryThreadIntoReturn(plan, prologue))
            {
                sink = SinkKind.BranchPair;
            }
            if (sink != SinkKind.ThreadIntoReturn)
            {
                // The branch target has to be an instruction of our own, never the original first
                // instruction: in a Release build that is frequently the start of a try block, and
                // branching into a protected region is invalid IL.
                Instruction resume = Instruction.Create(OpCodes.Nop);
                if (sink == SinkKind.SwitchOne)
                {
                    // Instruction[] specifically, not List<Instruction>: that is what dnlib
                    // produces when reading a body, and what Obfuz's own BasicBlockCollection
                    // type-checks for when a later pass walks this method.
                    prologue.Add(new Instruction(OpCodes.Switch, new Instruction[] { resume }));
                }
                else
                {
                    prologue.Add(Instruction.Create(OpCodes.Brfalse, resume));
                }
                prologue.Add(Instruction.Create(OpCodes.Br, resume));
                prologue.Add(resume);
            }

            for (int i = prologue.Count - 1; i >= 0; i--)
            {
                body.Instructions.Insert(0, prologue[i]);
            }
        }

        private static OpCode FoldOpCode(FoldOp op)
        {
            switch (op)
            {
                case FoldOp.Add: return OpCodes.Add;
                case FoldOp.Sub: return OpCodes.Sub;
                case FoldOp.Mul: return OpCodes.Mul;
                case FoldOp.Or: return OpCodes.Or;
                case FoldOp.And: return OpCodes.And;
                default: return OpCodes.Xor;
            }
        }

        /// <summary>
        /// Stashes the zero and adds it into every returned value, so the junk parameters feed a
        /// value the method genuinely produces instead of a branch that exists only for them.
        /// Returns false when the return type cannot absorb an integer zero, leaving the caller to
        /// fall back to a branch sink.
        /// </summary>
        private static bool TryThreadIntoReturn(PadPlan plan, List<Instruction> prologue)
        {
            MethodDef method = plan.method;
            CilBody body = method.Body;
            TypeSig retType = method.MethodSig.RetType;
            if (retType == null)
            {
                return false;
            }

            OpCode widen;
            switch (retType.ElementType)
            {
                case ElementType.I1:
                case ElementType.U1:
                case ElementType.I2:
                case ElementType.U2:
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.Char:
                case ElementType.Boolean:
                    widen = OpCodes.Nop;
                    break;
                case ElementType.I8:
                case ElementType.U8:
                    widen = OpCodes.Conv_I8;
                    break;
                case ElementType.R4:
                    widen = OpCodes.Conv_R4;
                    break;
                case ElementType.R8:
                    widen = OpCodes.Conv_R8;
                    break;
                default:
                    return false;
            }

            var returns = body.Instructions.Where(i => i.OpCode.Code == Code.Ret).ToList();
            if (returns.Count == 0)
            {
                return false;
            }

            var sink = new Local(method.Module.CorLibTypes.Int32);
            body.Variables.Add(sink);
            prologue.Add(Instruction.Create(OpCodes.Stloc, sink));

            foreach (Instruction ret in returns)
            {
                // mutate the ret in place so anything branching to it still runs the fold, then
                // re-emit the ret after it. Stack stays balanced on both paths.
                var tail = new List<Instruction> { Instruction.Create(OpCodes.Ldloc, sink) };
                if (widen != OpCodes.Nop)
                {
                    tail.Add(Instruction.Create(widen));
                }
                tail.Add(Instruction.Create(OpCodes.Add));
                tail.Add(Instruction.Create(OpCodes.Ret));

                int at = body.Instructions.IndexOf(ret);
                ret.OpCode = tail[0].OpCode;
                ret.Operand = tail[0].Operand;
                for (int k = tail.Count - 1; k >= 1; k--)
                {
                    body.Instructions.Insert(at + 1, tail[k]);
                }
            }
            return true;
        }

        private static void ApplyToReference(MemberRef memberRef, PadPlan plan)
        {
            MethodSig sig = memberRef.MethodSig;
            if (sig == null || sig.Params.Count != plan.RealCount)
            {
                // Skipping here would leave the definition padded and this reference stale, so the
                // call site would push the wrong number of arguments. Fail the build instead.
                throw new Exception($"parameter padding cannot retarget reference `{memberRef}` of `{plan.method}`: "
                    + $"expected {plan.RealCount} parameters, found {(sig == null ? "no signature" : sig.Params.Count.ToString())}.");
            }
            ICorLibTypes corLibTypes = memberRef.Module.CorLibTypes;
            var oldParams = sig.Params.ToList();
            sig.Params.Clear();
            for (int slot = 0; slot < plan.SlotCount; slot++)
            {
                int real = plan.slotToReal[slot];
                sig.Params.Add(real >= 0 ? oldParams[real] : JunkTypeSig(corLibTypes, plan.slotKind[slot]));
            }
        }

        private static void RewriteCallSites(List<CallSite> sites)
        {
            foreach (var byHost in sites.GroupBy(s => s.host))
            {
                MethodDef host = byHost.Key;
                CilBody body = host.Body;
                var siteByInst = byHost.ToDictionary(s => s.inst, s => s);

                // inserting instructions can push a short branch out of range.
                body.SimplifyBranches();

                var localPool = new List<List<Local>>();
                var final = new List<Instruction>(body.Instructions.Count + siteByInst.Count * 8);
                foreach (Instruction inst in body.Instructions)
                {
                    if (!siteByInst.TryGetValue(inst, out CallSite site))
                    {
                        final.Add(inst);
                        continue;
                    }
                    List<Instruction> output = BuildCallSite(body, site, localPool);

                    // the call may be a branch target, so it keeps its identity and becomes the
                    // first emitted instruction. Same trick as InstructionObfuscationPassBase.
                    inst.OpCode = output[0].OpCode;
                    inst.Operand = output[0].Operand;
                    final.Add(inst);
                    for (int k = 1; k < output.Count; k++)
                    {
                        final.Add(output[k]);
                    }
                }
                body.Instructions.Clear();
                foreach (Instruction inst in final)
                {
                    body.Instructions.Add(inst);
                }

            }
        }

        private static List<Instruction> BuildCallSite(CilBody body, CallSite site, List<List<Local>> localPool)
        {
            PadPlan plan = site.plan;
            OpCode callOpCode = site.inst.OpCode;
            IMethod callOperand = (IMethod)site.inst.Operand;

            var output = new List<Instruction>();
            var used = new List<Local>();
            var spilled = new Local[plan.RealCount];

            // arguments are already on the stack in order, so pop them back to front.
            for (int real = plan.RealCount - 1; real >= 0; real--)
            {
                Local local = RentLocal(body, localPool, used, site.argTypes[real]);
                used.Add(local);
                spilled[real] = local;
                output.Add(Instruction.Create(OpCodes.Stloc, local));
            }
            for (int slot = 0; slot < plan.SlotCount; slot++)
            {
                int real = plan.slotToReal[slot];
                output.Add(real >= 0
                    ? Instruction.Create(OpCodes.Ldloc, spilled[real])
                    : PushJunk(plan, slot));
            }
            output.Add(Instruction.Create(callOpCode, callOperand));
            return output;
        }

        /// <summary>
        /// Rents a local of exactly this type that is not already spoken for at this call site.
        /// Matching is by type identity, never by TypeSig.FullName: that omits the assembly, so
        /// two same-named types from different assemblies would share one wrongly typed local.
        /// LocalVariableAllocator.AllocateLocal compares the same way.
        /// </summary>
        private static Local RentLocal(CilBody body, List<List<Local>> localPool, List<Local> used, TypeSig type)
        {
            foreach (List<Local> bucket in localPool)
            {
                if (bucket.Count == 0 || !TypeEqualityComparer.Instance.Equals(bucket[0].Type, type))
                {
                    continue;
                }
                foreach (Local candidate in bucket)
                {
                    if (!used.Contains(candidate))
                    {
                        return candidate;
                    }
                }
                var extra = new Local(type);
                body.Variables.Add(extra);
                bucket.Add(extra);
                return extra;
            }
            var local = new Local(type);
            body.Variables.Add(local);
            localPool.Add(new List<Local> { local });
            return local;
        }
    }
}
