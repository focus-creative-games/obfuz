using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Virtualization.Functions
{
    public abstract class Int32FunctionBase : FunctionBase
    {
        public override DataNodeType ReturnType => DataNodeType.Int32;
    }
}
