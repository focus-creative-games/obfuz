
using Obfuz.Utils;
using Obfuz.Virtualization.Functions;
using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class RandomDataNodeCreator : DataNodeCreatorBase
    {
        private readonly Dictionary<DataNodeType, List<IFunction>> _functions = new Dictionary<DataNodeType, List<IFunction>>();

        private readonly IRandom _random;

        public RandomDataNodeCreator(IRandom random)
        {
            _random = random;
            var int32Funcs = new List<IFunction>()
            {
                new Int32FunctionAdd(),
                new Int32FunctionXor(),
            };
            _functions.Add(DataNodeType.Int32, int32Funcs);
        }

        public override IDataNode CreateRandom(DataNodeType type, object value, CreateExpressionOptions options)
        {
            if (!_functions.TryGetValue(type, out var funcs))
            {
                throw new System.Exception($"No functions available for type {type}");
            }
            if (options.depth >= 2)
            {
                //return new ConstDataNode() { Type = type, Value = value };
                return new ConstFromFieldRvaDataNode() { Type = type, Value = value };
            }
            var func = funcs[options.random.NextInt(funcs.Count)];
            ++options.depth;
            return func.CreateExpr(type, value, options);
        }

        public IDataNode CreateRandom(DataNodeType type, object value)
        {
            var options = new CreateExpressionOptions
            {
                depth = 0,
                random = _random,
                expressionCreator = this,
            };
            return CreateRandom(type, value, options);
        }
    }
}
