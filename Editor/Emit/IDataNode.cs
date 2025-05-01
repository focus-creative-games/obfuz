using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Obfuz.Emit
{

    public interface IDataNode
    {
        DataNodeType Type { get; }

        object Value { get; }

        void Init(CreateExpressionOptions options);

        void Compile(CompileContext ctx);
    }
}
