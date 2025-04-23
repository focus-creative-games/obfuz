
using Obfuz.Utils;
using Obfuz.Virtualization.DataNodes;
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
            var intFuncs = new List<IFunction>()
            {
                new IntAdd(),
                new IntXor(),
                new IntRotateShift(),
                //new ConstFromFieldRvaDataCreator(),
                //new ConstDataCreator(),
            };
            _functions.Add(DataNodeType.Int32, intFuncs);
            _functions.Add(DataNodeType.Int64, intFuncs);

            var floatFuncs = new List<IFunction>()
            {
                new MemoryCastIntAsFloat(),
            };
            _functions.Add(DataNodeType.Float32, floatFuncs);
            _functions.Add(DataNodeType.Float64, floatFuncs);

            var stringFuncs = new List<IFunction>()
            {
                new ConstFieldDataCreator(),
            };
            _functions.Add(DataNodeType.String, stringFuncs);
        }

        public override IDataNode CreateRandom(DataNodeType type, object value, CreateExpressionOptions options)
        {
            if (!_functions.TryGetValue(type, out var funcs))
            {
                throw new System.Exception($"No functions available for type {type}");
            }
            if (options.depth >= 4)
            {
                //return new ConstDataNode() { Type = type, Value = value };
                return _random.NextInt(100) < 50 ?
                //return true ?
                    new ConstFromFieldRvaDataNode() { Type = type, Value = value } :
                    new ConstDataNode() { Type = type, Value = value };
            }
            var func = funcs[options.random.NextInt(funcs.Count)];
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
