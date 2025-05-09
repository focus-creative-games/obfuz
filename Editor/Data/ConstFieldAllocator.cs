using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Obfuz.Data
{
    public class ModuleConstFieldAllocator : IModuleEmitManager
    {
        private ModuleDef _module;
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly RvaDataAllocator _rvaDataAllocator;

        private TypeDef _holderTypeDef;

        class ConstFieldInfo
        {
            public FieldDef field;
            public object value;
        }
        private readonly Dictionary<object, ConstFieldInfo> _allocatedFields = new Dictionary<object, ConstFieldInfo>();
        private readonly Dictionary<FieldDef, ConstFieldInfo> _field2Fields = new Dictionary<FieldDef, ConstFieldInfo>();

        private readonly List<TypeDef> _holderTypeDefs = new List<TypeDef>();


        public ModuleConstFieldAllocator(IEncryptor encryptor, IRandom random, RvaDataAllocator rvaDataAllocator)
        {
            _encryptor = encryptor;
            _random = random;
            _rvaDataAllocator = rvaDataAllocator;
        }

        public void Init(ModuleDef mod)
        {
            _module = mod;
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

        public FieldDef Allocate(byte[] value)
        {
            return AllocateAny(value);
        }

        private int GenerateEncryptionOperations()
        {
            return _random.NextInt();
        }

        public int GenerateSalt()
        {
            return _random.NextInt();
        }

        private DefaultModuleMetadataImporter GetModuleMetadataImporter()
        {
            return MetadataImporter.Instance.GetDefaultModuleMetadataImporter(_module);
        }

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


            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter();
            // TODO. obfuscate init codes
            foreach (var field in type.Fields)
            {
                ConstFieldInfo constInfo = _field2Fields[field];
                int ops = GenerateEncryptionOperations();
                int salt = GenerateSalt();
                switch (constInfo.value)
                {
                    case int i:
                    {
                        int encryptedValue = _encryptor.Encrypt(i, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
                        break;
                    }
                    case long l:
                    {
                        long encryptedValue = _encryptor.Encrypt(l, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
                        break;
                    }
                    case float f:
                    {
                        float encryptedValue = _encryptor.Encrypt(f, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
                        break;
                    }
                    case double d:
                    {
                        double encryptedValue = _encryptor.Encrypt(d, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
                        break;
                    }
                    case string s:
                    {
                        int stringByteLength = Encoding.UTF8.GetByteCount(s);
                        byte[] encryptedValue = _encryptor.Encrypt(s, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        //// should use stringByteLength, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
                        ins.Add(Instruction.CreateLdcI4(stringByteLength));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
                        break;
                    }
                    case byte[] bs:
                    {
                        byte[] encryptedValue = _encryptor.Encrypt(bs, 0, bs.Length, ops, salt);
                        RvaData rvaData = _rvaDataAllocator.Allocate(_module, encryptedValue);
                        ins.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
                        ins.Add(Instruction.CreateLdcI4(rvaData.offset));
                        //// should use stringByteLength, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
                        ins.Add(Instruction.CreateLdcI4(bs.Length));
                        ins.Add(Instruction.CreateLdcI4(ops));
                        ins.Add(Instruction.CreateLdcI4(salt));
                        ins.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaBytes));
                        break;
                    }
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
        private readonly IEncryptor _encryptor;
        private readonly IRandom _random;
        private readonly RvaDataAllocator _rvaDataAllocator;

        public ConstFieldAllocator(IEncryptor encryptor, IRandom random, RvaDataAllocator rvaDataAllocator)
        {
            _encryptor = encryptor;
            _random = random;
            _rvaDataAllocator = rvaDataAllocator;
        }

        private ModuleConstFieldAllocator GetModuleAllocator(ModuleDef mod)
        {
            return EmitManager.Ins.GetEmitManager<ModuleConstFieldAllocator>(mod, m => new ModuleConstFieldAllocator(_encryptor, _random, _rvaDataAllocator));
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

        public FieldDef Allocate(ModuleDef mod, byte[] value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public FieldDef Allocate(ModuleDef mod, string value)
        {
            return GetModuleAllocator(mod).Allocate(value);
        }

        public void Done()
        {
            foreach (var moduleAllocator in EmitManager.Ins.GetEmitManagers<ModuleConstFieldAllocator>())
            {
                moduleAllocator.Done();
            }
        }
    }
}
