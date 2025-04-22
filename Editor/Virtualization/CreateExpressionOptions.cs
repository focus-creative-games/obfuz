using Obfuz.Utils;

namespace Obfuz.Virtualization
{
    public struct CreateExpressionOptions
    {
        public IRandom random;
        public IDataNodeCreator expressionCreator;
        public int depth;
    }
}
