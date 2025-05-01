using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit.Functions
{
    public class ConstDataCreator : NodeCreatorBase
    {
        public override IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options)
        {
            return new ConstDataNode {  Type = type, Value = value };
        }
    }
}
