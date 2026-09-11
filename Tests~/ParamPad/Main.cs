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
using Obfuz.ObfusPasses.ParamPad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

static class Program
{
    static int failures;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + what);
        if (!ok) failures++;
    }

    class DirLoadContext : AssemblyLoadContext
    {
        private readonly string _dir;
        public DirLoadContext(string dir, string name) : base(name, isCollectible: false) { _dir = dir; }
        protected override Assembly Load(AssemblyName name)
        {
            string candidate = Path.Combine(_dir, name.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }

    static ModuleContext MakeContext(string dir)
    {
        var ctx = ModuleDef.CreateModuleContext();
        var res = (AssemblyResolver)ctx.AssemblyResolver;
        res.EnableTypeDefCache = true;
        res.DefaultModuleContext = ctx;
        res.PreSearchPaths.Add(Path.GetFullPath(dir));
        res.PreSearchPaths.Add(Path.GetDirectoryName(typeof(object).Assembly.Location));
        res.PostSearchPaths.Add(Path.GetDirectoryName(typeof(object).Assembly.Location));
        return ctx;
    }

    // Mirrors the shape of the real predicate: the pass composes whitelist + pass policy +
    // rename policy, none of which are structural. Here we only pin by name, so what the
    // fixture actually exercises is ParameterPadding.IsCandidate.
    static readonly HashSet<string> PolicyPinned = new HashSet<string> { "Reflected" };

    static bool IsSafe(MethodDef m) => !PolicyPinned.Contains(m.Name);

    // every method the transform must refuse to touch, and why
    static readonly Dictionary<string, string> MustNotPad = new Dictionary<string, string>
    {
        { "Fx.Square::Area", "implicit interface impl (virtual newslot)" },
        { "Fx.Square::Describe", "implicit interface impl (virtual newslot)" },
        { "Fx.Square::Sides", "abstract override" },
        { "Fx.Square::Weight", "virtual override" },
        { "Fx.Square::get_Side", "property accessor" },
        { "Fx.Square::set_Side", "property accessor" },
        { "Fx.Square::add_Resized", "event accessor" },
        { "Fx.Square::remove_Resized", "event accessor" },
        { "Fx.Square::.ctor", "constructor" },
        { "Fx.Square::.cctor", "static constructor" },
        { "Fx.ShapeBase::Sides", "abstract" },
        { "Fx.ShapeBase::Weight", "virtual" },
        { "Fx.ShapeBase::.ctor", "constructor" },
        { "Fx.Explicit::Fx.IShape.Area", "explicit interface impl" },
        { "Fx.Explicit::Fx.IShape.Describe", "explicit interface impl" },
        { "Fx.IShape::Area", "interface declaration" },
        { "Fx.IShape::Describe", "interface declaration" },
        { "Fx.Ops::Add", "ldftn delegate target" },
        { "Fx.Ops::Native", "DllImport" },
        { "Fx.Ops::Reflected", "pinned by the safety predicate" },
        { "Fx.Payload::AfterLoad", "[OnDeserialized]: renameable but arity-pinned" },
        { "Fx.Payload::BeforeSave", "[OnSerializing]: renameable but arity-pinned" },
        { "Fx.Combine::Invoke", "delegate member" },
        { "Fx.Combine::.ctor", "delegate member" },
    };

    // every method that must actually gain junk parameters
    static readonly string[] MustPad =
    {
        "Fx.Square::Scaled", "Fx.Square::Bare", "Fx.Ops::Mul", "Fx.Ops::Neg", "Fx.Ops::Zero",
        "Fx.Ops::Clamp", "Fx.Ops::Fact", "Fx.Ops::Split", "Fx.Ops::Pair", "Fx.Ops::Mixed",
        "Fx.Ops::Boom", "Fx.Ops::Sum", "Fx.Ops::Add3", "Fx.Ops::UseDelegate",
        "Fx.Ops::UseReflection", "Fx.Ops::Guarded", "Fx.Ops::Wrapped", "Fx.Ops::SameName", "Fx.Ops::TakeA", "Fx.Ops::TakeB", "Fx.Ops::Order", "Fx.Ops::Tick", "Fx.Counter::Step", "Fx.Counter::Value", "Fx.Entry::RunAll",
    };

    static string Key(MethodDef m) => m.DeclaringType.FullName + "::" + m.Name;

    static Dictionary<string, MethodSig> SnapshotSigs(ModuleDefMD mod)
    {
        var d = new Dictionary<string, MethodSig>();
        foreach (TypeDef t in mod.GetTypes())
            foreach (MethodDef m in t.Methods)
                d[Key(m)] = m.MethodSig;
        return d;
    }

    const int MinCount = 5;
    const int MaxCount = 10;

    static int Pad(string srcDir, string dstDir, int seed)
    {
        Directory.CreateDirectory(dstDir);
        var ctx = MakeContext(srcDir);
        ModuleDefMD fixtureMod = ModuleDefMD.Load(Path.Combine(srcDir, "fixture.dll"), ctx);
        ModuleDefMD callerMod = ModuleDefMD.Load(Path.Combine(srcDir, "caller.dll"), ctx);
        // Obfuz's AssemblyCache does this for the real pipeline; without it the resolver would
        // load a second copy of fixture.dll when caller.dll references it.
        var resolver = (AssemblyResolver)ctx.AssemblyResolver;
        resolver.AddToCache(fixtureMod.Assembly);
        resolver.AddToCache(callerMod.Assembly);

        var padding = new ParameterPadding(seed, MinCount, MaxCount, IsSafe);
        padding.Process(new List<ModuleDef> { fixtureMod }, new List<ModuleDef> { fixtureMod, callerMod });

        Verify(fixtureMod, srcDir);

        fixtureMod.Write(Path.Combine(dstDir, "fixture.dll"));
        callerMod.Write(Path.Combine(dstDir, "caller.dll"));
        foreach (string extra in Directory.GetFiles(srcDir, "*.dll"))
        {
            string name = Path.GetFileName(extra);
            if (name != "fixture.dll" && name != "caller.dll")
                File.Copy(extra, Path.Combine(dstDir, name), true);
        }
        foreach (string cfg in Directory.GetFiles(srcDir, "*.json"))
            File.Copy(cfg, Path.Combine(dstDir, Path.GetFileName(cfg)), true);
        return padding.PaddedMethodCount;
    }

    static void Verify(ModuleDefMD padded, string srcDir)
    {
        ModuleDefMD original = ModuleDefMD.Load(Path.Combine(srcDir, "fixture.dll"), MakeContext(srcDir));
        Dictionary<string, MethodSig> before = SnapshotSigs(original);

        foreach (var e in MustNotPad)
        {
            MethodDef m = FindMethod(padded, e.Key);
            if (m == null) { Check(false, $"{e.Key} not found in fixture"); continue; }
            if (!before.TryGetValue(e.Key, out MethodSig oldSig)) { Check(false, $"{e.Key} missing baseline"); continue; }
            Check(m.MethodSig.Params.Count == oldSig.Params.Count,
                $"untouched: {e.Key} ({e.Value}) keeps {oldSig.Params.Count} params");
        }

        foreach (string key in MustPad)
        {
            MethodDef m = FindMethod(padded, key);
            if (m == null) { Check(false, $"{key} not found in fixture"); continue; }
            int oldCount = before[key].Params.Count;
            int added = m.MethodSig.Params.Count - oldCount;
            Check(added >= MinCount && added <= MaxCount,
                $"padded: {key} gained {added} params (was {oldCount})");
        }

        // every real parameter survives exactly once; its POSITION is free to move, so this is a
        // multiset check rather than a subsequence one.
        int reorderedMethods = 0;
        foreach (string key in MustPad)
        {
            MethodDef m = FindMethod(padded, key);
            if (m == null) continue;
            var oldParams = before[key].Params.Select(p => p.FullName).ToList();
            var newParams = m.MethodSig.Params.Select(p => p.FullName).ToList();
            var remaining = new List<string>(newParams);
            bool allPresent = oldParams.All(want => remaining.Remove(want));
            Check(allPresent, $"params: {key} still carries every real parameter");

            // did this one actually get its real parameters shuffled?
            int idx = 0; bool inOrder = true;
            foreach (string want in oldParams)
            {
                int at = newParams.IndexOf(want, idx);
                if (at < 0) { inOrder = false; break; }
                idx = at + 1;
            }
            if (!inOrder) reorderedMethods++;
        }
        Check(reorderedMethods > 0, $"order: real parameters are permuted, not just interleaved ({reorderedMethods} methods reordered)");

        // branching into a protected region is invalid IL per ECMA-335, and ilverify does not
        // check it. Release builds put a try at instruction 0, so the consume prologue has to
        // branch to an instruction of its own rather than to the original body start.
        int branchesIntoTry = 0;
        foreach (TypeDef t in padded.GetTypes())
        {
            foreach (MethodDef m in t.Methods)
            {
                if (!m.HasBody || m.Body.ExceptionHandlers.Count == 0) continue;
                var idx = new Dictionary<Instruction, int>();
                for (int i = 0; i < m.Body.Instructions.Count; i++) idx[m.Body.Instructions[i]] = i;
                foreach (ExceptionHandler eh in m.Body.ExceptionHandlers)
                {
                    if (eh.TryStart == null || eh.TryEnd == null) continue;
                    int lo = idx[eh.TryStart], hi = idx[eh.TryEnd];
                    for (int i = 0; i < m.Body.Instructions.Count; i++)
                    {
                        if (i >= lo && i < hi) continue;
                        if (m.Body.Instructions[i].Operand is Instruction tgt
                            && idx.TryGetValue(tgt, out int ti) && ti >= lo && ti < hi)
                        {
                            Console.WriteLine($"  branch into try: {Key(m)} #{i} -> #{ti}");
                            branchesIntoTry++;
                        }
                    }
                }
            }
        }
        Check(branchesIntoTry == 0, "protected regions: no branch jumps into a try block");

        // Downstream Obfuz passes walk these bodies with their own analyzers, which type-check
        // operands against what dnlib produces on read. A List<Instruction> switch operand is
        // valid IL and survives ilverify, but throws in BasicBlockCollection.BuildInOutGraph.
        int badOperands = 0;
        foreach (TypeDef t in padded.GetTypes())
            foreach (MethodDef m in t.Methods)
                if (m.HasBody)
                    foreach (Instruction inst in m.Body.Instructions)
                        if (inst.OpCode.Code == Code.Switch && !(inst.Operand is Instruction[])) badOperands++;
        Check(badOperands == 0, $"operands: every switch carries Instruction[], as dnlib and Obfuz's analyzers expect ({badOperands} bad)");

        // the transform must leave every body with a coherent argument count at each call site
        foreach (TypeDef t in padded.GetTypes())
        {
            foreach (MethodDef m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (Instruction inst in m.Body.Instructions)
                {
                    if (inst.Operand is Parameter p)
                        Check(p.Index < m.Parameters.Count, $"operand: {Key(m)} parameter operand in range");
                }
            }
        }
    }

    static MethodDef FindMethod(ModuleDefMD mod, string key)
    {
        foreach (TypeDef t in mod.GetTypes())
            foreach (MethodDef m in t.Methods)
                if (Key(m) == key) return m;
        return null;
    }

    static string Invoke(string dir, string tag)
    {
        var alc = new DirLoadContext(Path.GetFullPath(dir), tag);
        Assembly caller = alc.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(dir), "caller.dll"));
        Type entry = caller.GetType("Cl.Caller");
        return (string)entry.GetMethod("RunAll").Invoke(null, null);
    }

    // abstract stack simulation: catches an unbalanced path that a single behaviour run may
    // simply never take, and that ilverify does not always reach.
    static string StackCheck(MethodDef m)
    {
        CilBody body = m.Body;
        IList<Instruction> ins = body.Instructions;
        var depth = new int[ins.Count];
        for (int i = 0; i < ins.Count; i++) depth[i] = int.MinValue;
        var idx = new Dictionary<Instruction, int>();
        for (int i = 0; i < ins.Count; i++) idx[ins[i]] = i;
        depth[0] = 0;
        foreach (ExceptionHandler eh in body.ExceptionHandlers)
        {
            if (eh.HandlerStart != null && idx.TryGetValue(eh.HandlerStart, out int h))
                depth[h] = eh.HandlerType == ExceptionHandlerType.Finally || eh.HandlerType == ExceptionHandlerType.Fault ? 0 : 1;
            if (eh.FilterStart != null && idx.TryGetValue(eh.FilterStart, out int f)) depth[f] = 1;
        }
        var work = new Stack<int>();
        for (int i = 0; i < ins.Count; i++) if (depth[i] != int.MinValue) work.Push(i);
        while (work.Count > 0)
        {
            int i = work.Pop();
            Instruction inst = ins[i];
            int d = depth[i];
            inst.CalculateStackUsage(out int push, out int pop);
            if (pop == -1) d = 0; else d -= pop;
            if (d < 0) return $"underflow at #{i} {inst.OpCode}";
            d += push;
            foreach (int nxt in Successors(ins, idx, i, inst))
            {
                if (nxt < 0 || nxt >= ins.Count) continue;
                if (depth[nxt] == int.MinValue) { depth[nxt] = d; work.Push(nxt); }
                else if (depth[nxt] != d) return $"mismatch at #{nxt} {ins[nxt].OpCode}: {depth[nxt]} vs {d}";
            }
        }
        return null;
    }

    static IEnumerable<int> Successors(IList<Instruction> ins, Dictionary<Instruction, int> idx, int i, Instruction inst)
    {
        FlowControl fc = inst.OpCode.FlowControl;
        if (fc != FlowControl.Branch && fc != FlowControl.Return && fc != FlowControl.Throw) yield return i + 1;
        if (inst.Operand is Instruction t && idx.TryGetValue(t, out int ti)) yield return ti;
        if (inst.Operand is IList<Instruction> ts) foreach (Instruction x in ts) if (idx.TryGetValue(x, out int xi)) yield return xi;
    }

    static int BranchesIntoTry(ModuleDefMD mod)
    {
        int bad = 0;
        foreach (TypeDef t in mod.GetTypes())
        {
            foreach (MethodDef m in t.Methods)
            {
                if (!m.HasBody || m.Body.ExceptionHandlers.Count == 0) continue;
                var idx = new Dictionary<Instruction, int>();
                for (int i = 0; i < m.Body.Instructions.Count; i++) idx[m.Body.Instructions[i]] = i;
                foreach (ExceptionHandler eh in m.Body.ExceptionHandlers)
                {
                    if (eh.TryStart == null || eh.TryEnd == null) continue;
                    int lo = idx[eh.TryStart], hi = idx[eh.TryEnd];
                    for (int i = 0; i < m.Body.Instructions.Count; i++)
                    {
                        if (i >= lo && i < hi) continue;
                        if (m.Body.Instructions[i].Operand is Instruction tgt
                            && idx.TryGetValue(tgt, out int ti) && ti >= lo && ti < hi) bad++;
                    }
                }
            }
        }
        return bad;
    }

    // The consume code must not be one greppable shape. Fuzz many seeds, prove every one is
    // structurally sound, and count how many distinct prologue shapes the recipes produce.
    static void Fuzz(string srcDir, int seeds)
    {
        int stackFailures = 0, tryFailures = 0, padded = 0;
        var shapes = new Dictionary<string, int>();
        int paddedBodies = 0;
        for (int seed = 1; seed <= seeds; seed++)
        {
            var ctx = MakeContext(srcDir);
            ModuleDefMD fx = ModuleDefMD.Load(Path.Combine(srcDir, "fixture.dll"), ctx);
            ModuleDefMD cl = ModuleDefMD.Load(Path.Combine(srcDir, "caller.dll"), ctx);
            var resolver = (AssemblyResolver)ctx.AssemblyResolver;
            resolver.AddToCache(fx.Assembly);
            resolver.AddToCache(cl.Assembly);

            var pad = new ParameterPadding(seed, MinCount, MaxCount, IsSafe);
            pad.Process(new List<ModuleDef> { fx }, new List<ModuleDef> { fx, cl });
            padded += pad.PaddedMethodCount;

            paddedBodies += pad.PaddedMethodCount;
            foreach (ModuleDefMD mod in new[] { fx, cl })
            {
                tryFailures += BranchesIntoTry(mod);
                foreach (TypeDef t in mod.GetTypes())
                {
                    foreach (MethodDef m in t.Methods)
                    {
                        if (!m.HasBody || m.Body.Instructions.Count == 0) continue;
                        if (StackCheck(m) != null) stackFailures++;
                    }
                }
            }
            foreach (TypeDef t in fx.GetTypes())
                foreach (MethodDef m in t.Methods)
                    if (m.HasBody && m.Body.Instructions.Count > 6)
                        foreach (string w in Windows(m, 5)) { shapes.TryGetValue(w, out int c); shapes[w] = c + 1; }
        }
        Check(stackFailures == 0, $"fuzz: {seeds} seeds, {padded} padded methods, no unbalanced stack ({stackFailures} failures)");
        Check(tryFailures == 0, $"fuzz: no branch into a protected region across {seeds} seeds ({tryFailures} failures)");
        // The old fixed prologue ended `ldc.i4.0 mul brfalse br nop` in EVERY padded method, so one
        // 5-instruction grep found all of them. Measure that directly: how much of the padded
        // population does the single most common 5-instruction window cover?
        int worst = 0; string worstShape = "";
        foreach (var e in shapes) if (e.Value > worst) { worst = e.Value; worstShape = e.Key; }
        double share = paddedBodies == 0 ? 0 : 100.0 * worst / paddedBodies;
        Check(share < 25.0,
            $"fuzz: no single 5-instruction window identifies padded methods (most common covers {share:F1}%: {worstShape})");
    }

    // distinct opcode windows of the given length, deduplicated within one method so a long body
    // does not inflate the count
    static IEnumerable<string> Windows(MethodDef m, int len)
    {
        IList<Instruction> ins = m.Body.Instructions;
        var seen = new HashSet<string>();
        for (int i = 0; i + len <= ins.Count && i < 40; i++)
        {
            var w = string.Join(" ", Enumerable.Range(i, len).Select(k => ins[k].OpCode.Name));
            if (seen.Add(w)) yield return w;
        }
    }

    static int Main()
    {
        string bin = Path.GetFullPath("caller/bin/Release/net7.0");
        if (!File.Exists(Path.Combine(bin, "caller.dll")))
        {
            Console.WriteLine("FAIL  caller.dll not built; run run.sh");
            return 1;
        }

        string pristine = Path.GetFullPath("pristine");
        if (Directory.Exists(pristine)) Directory.Delete(pristine, true);
        Directory.CreateDirectory(pristine);
        foreach (string f in Directory.GetFiles(bin))
            File.Copy(f, Path.Combine(pristine, Path.GetFileName(f)), true);

        string expected = Invoke(pristine, "pristine");
        Console.WriteLine("baseline: " + expected);

        int padded = Pad(pristine, "out", 12345);
        Check(padded > 0, $"padded {padded} methods");

        // the written assemblies must round-trip
        try
        {
            ModuleDefMD.Load(Path.GetFullPath("out/fixture.dll"), MakeContext("out"));
            ModuleDefMD.Load(Path.GetFullPath("out/caller.dll"), MakeContext("out"));
            Check(true, "roundtrip: padded assemblies reload through dnlib");
        }
        catch (Exception e)
        {
            Check(false, "roundtrip: " + e.Message);
        }

        // the decisive check: the padded build still behaves identically
        try
        {
            string actual = Invoke("out", "padded");
            Check(actual == expected, "behaviour: padded build produces identical output");
            if (actual != expected)
            {
                Console.WriteLine("  expected: " + expected);
                Console.WriteLine("  actual:   " + actual);
            }
        }
        catch (Exception e)
        {
            Check(false, "behaviour: padded build threw " + e.GetType().Name + ": " + e.Message);
            Console.WriteLine(e.ToString());
        }

        // determinism and variation
        int a = Pad(pristine, "outA", 777);
        int b = Pad(pristine, "outB", 777);
        int c = Pad(pristine, "outC", 999);
        Check(SigDump("outA") == SigDump("outB"), "seed: the same seed reproduces the same signatures");
        Check(SigDump("outA") != SigDump("outC"), "seed: a different seed produces different signatures");
        Check(Invoke("outC", "outC") == expected, "behaviour: a second seed also behaves identically");

        Fuzz(pristine, 40);

        Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURES");
        return failures == 0 ? 0 : 1;
    }

    static string SigDump(string dir)
    {
        ModuleDefMD mod = ModuleDefMD.Load(Path.GetFullPath(Path.Combine(dir, "fixture.dll")), MakeContext(dir));
        var lines = new List<string>();
        foreach (TypeDef t in mod.GetTypes())
            foreach (MethodDef m in t.Methods)
                lines.Add(Key(m) + "(" + string.Join(",", m.MethodSig.Params.Select(p => p.FullName)) + ")");
        lines.Sort(StringComparer.Ordinal);
        return string.Join("\n", lines);
    }
}
