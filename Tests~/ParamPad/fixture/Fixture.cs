extern alias LA;
extern alias LB;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Fx
{
    public interface IShape
    {
        int Area(int scale);
        string Describe(string prefix);
    }

    public delegate int Combine(int a, int b);

    public abstract class ShapeBase
    {
        public abstract int Sides();
        public virtual int Weight(int density) { return density * 2; }
    }

    public class Square : ShapeBase, IShape
    {
        private int _side;

        // ctor: excluded
        public Square(int side) { _side = side; }
        static Square() { Origin = 7; }

        public static int Origin;

        // implicit interface impl: virtual newslot, excluded
        public int Area(int scale) { return _side * _side * scale; }
        public string Describe(string prefix) { return prefix + ":square:" + _side; }

        // abstract impl: excluded
        public override int Sides() { return 4; }
        // virtual override: excluded
        public override int Weight(int density) { return density * 3; }

        // property accessors: excluded
        public int Side { get { return _side; } set { _side = value; } }

        // event accessors: excluded
        public event Action<int> Resized;
        public void RaiseResized(int v) { Resized?.Invoke(v); }

        // ordinary instance method: PADDED
        public int Scaled(int factor, int offset) { return _side * factor + offset; }

        // zero arg instance method: PADDED
        public int Bare() { return _side; }
    }

    public class Explicit : IShape
    {
        // explicit interface impl: excluded
        int IShape.Area(int scale) { return scale; }
        string IShape.Describe(string prefix) { return prefix + ":explicit"; }
    }

    public static class Ops
    {
        // taken with ldftn via method group conversion: excluded
        public static int Add(int a, int b) { return a + b; }

        // PADDED, ordinary statics
        public static int Mul(int a, int b) { return a * b; }
        public static int Neg(int a) { return -a; }
        public static int Zero() { return 0; }

        // PADDED, called as a nested argument and as a branch target
        public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

        // PADDED, recursion
        public static int Fact(int n) { return n <= 1 ? 1 : n * Fact(n - 1); }

        // PADDED, byref parameters
        public static void Split(int v, out int lo, ref int hi, in int bump)
        {
            lo = v & 0xFF;
            hi = (v >> 8) + bump;
        }

        // PADDED, generic
        public static string Pair<T>(T a, T b) { return a + "|" + b; }

        // PADDED, many argument types
        public static string Mixed(byte b, short s, long l, float f, double d, char c, bool t, string str)
        {
            return b + "/" + s + "/" + l + "/" + f.ToString("F1") + "/" + d.ToString("F1") + "/" + c + "/" + t + "/" + str;
        }

        // PADDED, called inside a try/catch and throws
        public static int Boom(int v) { if (v > 0) throw new InvalidOperationException("boom" + v); return v; }

        // PADDED, loop with a backward branch over a call
        public static int Sum(int n)
        {
            int acc = 0;
            for (int i = 0; i < n; i++) { acc = Add3(acc, i); }
            return acc;
        }

        // PADDED, called from the loop above
        public static int Add3(int a, int b) { return a + b; }

        // whole body wrapped in try/catch: the consume prologue must not branch into the
        // protected region
        public static int Guarded(int v)
        {
            try { return 100 / v; }
            catch (DivideByZeroException) { return -1; }
            finally { Touched++; }
        }

        public static int Touched;

        // try block starting at the very first instruction, with a nested call
        public static string Wrapped(int a, int b)
        {
            try { return "w" + Mul(a, b); }
            catch (Exception e) { return e.Message; }
        }

        // Two DIFFERENT types that share a namespace-qualified name. Spilling both at the same
        // ordinal must not reuse one local: TypeSig.FullName omits the assembly, so keying on it
        // would type the second spill as the first type.
        public static int TakeA(LA::Shared.Thing t, int n) { return t.V + n; }
        public static int TakeB(LB::Shared.Thing t, int n) { return t.V * n; }

        public static int SameName()
        {
            return TakeA(new LA::Shared.Thing(1), 2) + TakeB(new LB::Shared.Thing(3), 4);
        }

        // Arguments must still be EVALUATED left to right even though they are PUSHED in a
        // permuted order. Tick records evaluation order; Order records value routing.
        public static string OrderLog = "";
        public static int Tick(int n) { OrderLog += n; return n; }
        public static int Order(int a, int b, int c) { return a * 100 + b * 10 + c; }

        // ldtoken / reflection target: excluded
        public static int Reflected(int a) { return a + 1000; }

        public static string UseReflection()
        {
            var m = typeof(Ops).GetMethod("Reflected");
            return m == null ? "null" : m.Name + ":" + m.GetParameters().Length;
        }

        // [DllImport]: excluded (no body, never a candidate)
        [DllImport("nonexistent", EntryPoint = "never_called")]
        public static extern int Native(int a);

        public static int UseDelegate(int a, int b)
        {
            Combine c = Add;                    // ldftn Ops::Add
            Func<int, int> lam = x => x * 5;    // ldftn on the lambda
            return c(a, b) + lam(a);
        }
    }

    [Serializable]
    public class Payload
    {
        public int Value;
        public static int AfterCount;

        // located by attribute, so the rename policy permits renaming it - but BinaryFormatter and
        // Newtonsoft both validate the signature, so its argument list must not change.
        [OnDeserialized]
        internal void AfterLoad(StreamingContext ctx) { AfterCount++; }

        [OnSerializing]
        internal void BeforeSave(StreamingContext ctx) { AfterCount += 2; }
    }

    public class Counter
    {
        private int _n;
        public int Step(int by) { _n += by; return _n; }
        public int Value() { return _n; }
    }

    public static class Entry
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            var sq = new Square(5);
            sb.Append(sq.Area(2)).Append(';');
            sb.Append(sq.Describe("p")).Append(';');
            sb.Append(sq.Sides()).Append(';');
            sb.Append(sq.Weight(4)).Append(';');
            sq.Side = 6;
            sb.Append(sq.Side).Append(';');
            sb.Append(sq.Scaled(3, 1)).Append(';');
            sb.Append(sq.Bare()).Append(';');
            sb.Append(Square.Origin).Append(';');

            int captured = -1;
            sq.Resized += v => captured = v;
            sq.RaiseResized(42);
            sb.Append(captured).Append(';');

            IShape ex = new Explicit();
            sb.Append(ex.Area(9)).Append(';');
            sb.Append(ex.Describe("q")).Append(';');

            sb.Append(Ops.Mul(6, 7)).Append(';');
            sb.Append(Ops.Neg(11)).Append(';');
            sb.Append(Ops.Zero()).Append(';');
            // nested call as an argument, exercises overlapping spills
            sb.Append(Ops.Clamp(Ops.Mul(3, 40), 10, 100)).Append(';');
            sb.Append(Ops.Clamp(5, Ops.Neg(-20), Ops.Mul(5, 5))).Append(';');
            sb.Append(Ops.Fact(6)).Append(';');
            sb.Append(Ops.Sum(10)).Append(';');

            int lo, hi = 3, bump = 4;
            Ops.Split(0x1234, out lo, ref hi, in bump);
            sb.Append(lo).Append(',').Append(hi).Append(';');

            sb.Append(Ops.Pair<int>(1, 2)).Append(';');
            sb.Append(Ops.Pair<string>("a", "b")).Append(';');
            sb.Append(Ops.Mixed(1, -2, 3L, 4.5f, 6.5, 'z', true, "s")).Append(';');

            try { Ops.Boom(3); }
            catch (InvalidOperationException e) { sb.Append(e.Message).Append(';'); }
            sb.Append(Ops.Boom(0)).Append(';');

            Ops.OrderLog = "";
            int ordered = Ops.Order(Ops.Tick(1), Ops.Tick(2), Ops.Tick(3));
            sb.Append(ordered).Append(',').Append(Ops.OrderLog).Append(';');
            sb.Append(Ops.SameName()).Append(';');
            sb.Append(Ops.Guarded(4)).Append(';');
            sb.Append(Ops.Guarded(0)).Append(';');
            sb.Append(Ops.Touched).Append(';');
            sb.Append(Ops.Wrapped(3, 4)).Append(';');
            sb.Append(Ops.UseDelegate(2, 3)).Append(';');
            sb.Append(Ops.UseReflection()).Append(';');

            var c = new Counter();
            for (int i = 0; i < 4; i++) { c.Step(i); }
            sb.Append(c.Value()).Append(';');

            return sb.ToString();
        }
    }
}
