using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Virtualization.Functions
{
    public class BytesInitializeFromFieldRvaDataCreator : NodeCreatorBase
    {
        public override IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options)
        {
            return new BytesInitializeFromFieldRvaDataNode {  Type = type, Value = value };
        }
    }
}
