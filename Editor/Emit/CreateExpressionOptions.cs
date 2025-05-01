using Obfuz.Emit;
using Obfuz.Utils;

namespace Obfuz.Emit
{
    public struct CreateExpressionOptions
    {
        public IRandom random;
        public IDataNodeCreator expressionCreator;
        public int depth;
    }
}
