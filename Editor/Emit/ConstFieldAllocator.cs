using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class ModuleConstFieldAllocator
    {
        private readonly ModuleDef _module;
        private readonly IRandom _random;

        private TypeDef _holderTypeDef;

        class ConstFieldInfo
        {
            public FieldDef field;
            public object value;
        }
        private readonly Dictionary<object, ConstFieldInfo> _allocatedFields = new Dictionary<object, ConstFieldInfo>();
        private readonly Dictionary<FieldDef, ConstFieldInfo> _field2Fields = new Dictionary<FieldDef, ConstFieldInfo>();

        private readonly List<TypeDef> _holderTypeDefs = new List<TypeDef>();


        public ModuleConstFieldAllocator(ModuleDef mod, IRandom random)
        {
            _module = mod;
            _random = random;
        }

        const int maxFieldCount = 1000;


        private TypeSig GetTypeSigOfValue(object value)
        {
            if (value is int)
                return _module.CorLibTypes.Int32;
            if (value is long)
                return _module.CorLibTypes.Int64;
            if (value is float)
                return _module.CorLibTypes.Single;
            if (value is double)
                return _module.CorLibTypes.Double;
            if (value is string)
                return _module.CorLibTypes.String;
            if (value is byte[])
                return new SZArraySig(_module.CorLibTypes.Byte);
            throw new NotSupportedException($"Unsupported type: {value.GetType()}");
        }

        private ConstFieldInfo CreateConstFieldInfo(object value)
        {
            if (_holderTypeDef == null || _holderTypeDef.Fields.Count >= maxFieldCount)
            {
                _module.EnableTypeDefFindCache = false;
                ITypeDefOrRef objectTypeRef = _module.Import(typeof(object));
                _holderTypeDef = new TypeDefUser("$Obfuz$ConstFieldHolder$", objectTypeRef);
                _module.Types.Add(_holderTypeDef);
                _holderTypeDefs.Add(_holderTypeDef);
                _module.EnableTypeDefFindCache = true;
            }

            var field = new FieldDefUser($"$RVA_Value{_holderTypeDef.Fields.Count}", new FieldSig(GetTypeSigOfValue(value)), FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly);
            field.DeclaringType = _holderTypeDef;
            return new ConstFieldInfo
            {
                field = field,
                value = value,
            };
        }

        private FieldDef AllocateAny(object value)
        {
            if (!_allocatedFields.TryGetValue(value, out var field))
            {
                field = CreateConstFieldInfo(value);
                _allocatedFields.Add(value, field);
                _field2Fields.Add(field.field, field);
            }
            return field.field;
        }

        public FieldDef Allocate(int value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(long value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(float value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(double value)
        {
            return AllocateAny(value);
        }

        public FieldDef Allocate(string value)
        {
            return AllocateAny(value);
        }

        //public FieldDef Allocate(byte[] value)
        //{
        //    throw new NotImplementedException();
        //}

        private void CreateCCtorOfRvaTypeDef(TypeDef type)
        {
            var cctor = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(_module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctor.DeclaringType = type;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctor.Body = body;
            var ins = body.Instructions;

            //IMethod method = _module.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            //Assert.IsNotNull(method);

            // TODO. obfuscate init codes
            foreach (var field in type.Fields)
            {
                ConstFieldInfo constInfo = _field2Fields[field];
                switch (constInfo.value)
                {
                    case int i:
                    ins.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                    break;
                    case long l:
                    ins.Add(Instruction.Create(OpCodes.Ldc_I8, l));
                    break;
                    case float f:
                    ins.Add(Instruction.Create(OpCodes.Ldc_R4, f));
                    break;
                    case double d:
                    ins.Add(Instruction.Create(OpCodes.Ldc_R8, d));
                    break;
                    case string s:
                    ins.Add(Instruction.Create(OpCodes.Ldstr, s));
                    break;
                    //case byte[] b:
                    //    ins.Add(Instruction.Create(OpCodes.Ldlen, b.Length));
                    //    break;
                    default: throw new NotSupportedException($"Unsupported type: {constInfo.value.GetType()}");
                }
                ins.Add(Instruction.Create(OpCodes.Stsfld, field));
            }
            ins.Add(Instruction.Create(OpCodes.Ret));
        }

        public void Done()
        {
            foreach (var typeDef in _holderTypeDefs)
            {
                CreateCCtorOfRvaTypeDef(typeDef);
            }
        }
    }

    public class ConstFieldAllocator
    {
        private readonly IRandom _random;

        private readonly Dictionary<ModuleDef, ModuleConstFieldAllocator> _moduleAllocators = new Dictionary<ModuleDef, ModuleConstFieldAllocator>();

        public ConstFieldAllocator(IRandom random)
        {
            _random = random;
        }

        private ModuleConstFieldAllocator GetModuleAllocator(ModuleDef mod)
        {
            if (!_moduleAllocators.TryGetValue(mod, out var moduleAllocator))
            {
                moduleAllocator = new ModuleConstFieldAllocator(mod, _random);
                _moduleAllocators[mod] = moduleAllocator;
            }
            return moduleAllocator;
        }

        public FieldDef Allocate(ModuleDef mod, int value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, long value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, float value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, double value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, string value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public void Done()
        {
            foreach (var moduleAllocator in _moduleAllocators.Values)
            {
                moduleAllocator.Done();
            }
        }

        //public FieldDef Allocate(ModuleDef mod, byte[] value)
        //{
        //    return GetModuleAllocator(mod).Allocate(value);
        //}
    }
}
