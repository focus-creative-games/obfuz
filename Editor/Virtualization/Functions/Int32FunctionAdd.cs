using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Networking.UnityWebRequest;

namespace Obfuz.Virtualization.Functions
{
    public class Int32FunctionAdd : Int32FunctionBase
    {
        public override DataNodeType ReturnType => DataNodeType.Int32;

        public override void CreateArguments(DataNodeType type, object v, CreateExpressionOptions options, List<ConstValue> args)
        {
            int value = (int)v;

            int op1 = value >= 0 ? options.random.NextInt(value) : -options.random.NextInt(-value);
            int op2 = value - op1;
            args.Add(new ConstValue(DataNodeType.Int32, op1));
            args.Add(new ConstValue(DataNodeType.Int32, op2));
        }
    }
}
