using System.Text;

namespace Cl
{
    // Stands in for a nonObfuscatedButReferencingObfuscated assembly: never padded itself,
    // but its call sites into the padded assembly must still be fixed up.
    public static class Caller
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.Append(Fx.Entry.RunAll()).Append('#');
            sb.Append(Fx.Ops.Mul(9, 9)).Append(';');
            sb.Append(Fx.Ops.Clamp(500, 0, 99)).Append(';');
            sb.Append(Fx.Ops.Pair<int>(7, 8)).Append(';');
            var sq = new Fx.Square(4);
            sb.Append(sq.Scaled(2, 2)).Append(';');
            sb.Append(sq.Bare()).Append(';');
            return sb.ToString();
        }
    }
}
