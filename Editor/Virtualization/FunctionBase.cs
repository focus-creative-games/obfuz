using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Virtualization
{

    public abstract class FunctionBase : IFunction
    {
        public abstract DataNodeType ReturnType { get; }

        public abstract void CreateArguments(DataNodeType type, object value, CreateExpressionOptions options, List<ConstValue> args);

        public ConstExpression CreateCallable(IDataNode result, CreateExpressionOptions options)
        {
            var args = new List<ConstValue>();
            CreateArguments(result.Type, result.Value, options, args);

            options.depth += 1;
            var argNodes = new List<IDataNode>();
            foreach (ConstValue cv in args)
            {
                var argNode = options.expressionCreator.CreateRandom(cv.type, cv.value, options);
                argNodes.Add(argNode);
            }
            
            return new ConstExpression(this, args.Select(a => options.expressionCreator.CreateRandom(a.type, a.value, options)).ToList(), new ConstValue(result.Type, result.Value));
        }
    }
}
