using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Obfuz.Virtualization
{

    public interface IDataNode
    {
        DataNodeType Type { get; }

        object Value { get; }

        IDataNode Expr { get; }

        void Compile(CompileContext ctx);
    }
}
