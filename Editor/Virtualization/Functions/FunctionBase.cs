using dnlib.DotNet.Emit;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Obfuz.Virtualization
{

    public abstract class FunctionBase : IFunction
    {

        public abstract void CreateArguments(DataNodeType type, object value, CreateExpressionOptions options, List<ConstValue> args);

        public abstract void CompileSelf(CompileContext ctx, List<Instruction> output);

        public virtual void Compile(CompileContext ctx, List<IDataNode> inputs, ConstValue result)
        {
            foreach (var input in inputs)
            {
                input.Compile(ctx);
            }
            CompileSelf(ctx, ctx.output);
        }

        public virtual IDataNode CreateExpr(DataNodeType type, object value, CreateExpressionOptions options)
        {
            var args = new List<ConstValue>();
            CreateArguments(type, value, options, args);

            options.depth += 1;
            var argNodes = new List<IDataNode>();
            foreach (ConstValue cv in args)
            {
                var argNode = options.expressionCreator.CreateRandom(cv.type, cv.value, options);
                argNodes.Add(argNode);
            }
            
            return new ConstExpression(this, args.Select(a => options.expressionCreator.CreateRandom(a.type, a.value, options)).ToList(), new ConstValue(type, value));
        }
    }
}
