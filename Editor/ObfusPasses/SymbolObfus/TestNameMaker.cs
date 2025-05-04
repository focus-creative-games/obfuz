using System.Text;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class TestNameMaker : NameMakerBase
    {
        private class TestNameScope : NameScopeBase
        {
            private int _nextIndex;
            protected override void BuildNewName(StringBuilder nameBuilder, string originalName)
            {
                nameBuilder.Append($"<{originalName}>{_nextIndex++}");
            }
        }

        protected override INameScope CreateNameScope()
        {
            return new TestNameScope();
        }

    }
}
