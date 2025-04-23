using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Virtualization.Functions
{
    public class ConstFieldDataCreator : NodeCreatorBase
    {
        public override IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options)
        {
            return new ConstFieldDataNode { Type = type, Value = value };
        }
    }
}
