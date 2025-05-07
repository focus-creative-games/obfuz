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
    public struct RvaData
    {
        public readonly FieldDef field;
        public readonly int offset;
        public readonly int size;

        public RvaData(FieldDef field, int offset, int size)
        {
            this.field = field;
            this.offset = offset;
            this.size = size;
        }
    }

    public class ModuleRvaDataAllocator
    {
        // randomized
        const int maxRvaDataSize = 0x100;

        private readonly ModuleDef _module;
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;


        class RvaField
        {
            public FieldDef holderDataField;
            public FieldDef runtimeValueField;
            public long encryptionOps;
            public uint size;
            public List<byte> bytes;
            public int salt;

            public void FillPaddingToSize(int newSize)
            {
                for (int i = bytes.Count; i < newSize; i++)
                {
                    bytes.Add(0xAB);
                }
            }

            public void FillPaddingToEnd()
            {
                // fill with random value
                for (int i = bytes.Count; i < size; i++)
                {
                    bytes.Add(0xAB);
                }
            }
        }

        private readonly List<RvaField> _rvaFields = new List<RvaField>();
        private RvaField _currentField;


        private TypeDef _rvaTypeDef;

        private readonly Dictionary<int, TypeDef> _dataHolderTypeBySizes = new Dictionary<int, TypeDef>();

        public ModuleRvaDataAllocator(ModuleDef mod, IRandom random, IEncryptor encryptor)
        {
            _module = mod;
            _random = random;
            _encryptor = encryptor;
        }

        private (FieldDef, FieldDef) CreateDataHolderRvaField(TypeDef dataHolderType)
        {
            if (_rvaTypeDef == null)
            {
                _module.EnableTypeDefFindCache = false;
                //_rvaTypeDef = _module.Find("$ObfuzRVA$", true);
                //if (_rvaTypeDef != null)
                //{
                //    throw new Exception($"can't obfuscate a obfuscated assembly");
                //}
                ITypeDefOrRef objectTypeRef = _module.Import(typeof(object));
                _rvaTypeDef = new TypeDefUser("$Obfuz$RVA$", objectTypeRef);
                _module.Types.Add(_rvaTypeDef);
                _module.EnableTypeDefFindCache = true;
            }


            var holderField = new FieldDefUser($"$RVA_Data{_rvaTypeDef.Fields.Count}", new FieldSig(dataHolderType.ToTypeSig()), FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
            holderField.DeclaringType = _rvaTypeDef;

            var runtimeValueField = new FieldDefUser($"$RVA_Value{_rvaTypeDef.Fields.Count}", new FieldSig(new SZArraySig(_module.CorLibTypes.Byte)), FieldAttributes.Static);
            runtimeValueField.DeclaringType = _rvaTypeDef;
            return (holderField, runtimeValueField);
        }

        private TypeDef GetDataHolderType(int size)
        {
            size = (size + 15) & ~15; // align to 6 bytes
            if (_dataHolderTypeBySizes.TryGetValue(size, out var type))
                return type;
            var dataHolderType = new TypeDefUser($"$ObfuzRVA$DataHolder{size}", _module.Import(typeof(ValueType)));
            dataHolderType.Layout = TypeAttributes.ExplicitLayout;
            dataHolderType.PackingSize = 1;
            dataHolderType.ClassSize = (uint)size;
            _dataHolderTypeBySizes.Add(size, dataHolderType);
            _module.Types.Add(dataHolderType);
            return dataHolderType;
        }

        private static int AlignTo(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }

        private RvaField CreateRvaField(int size)
        {
            TypeDef dataHolderType = GetDataHolderType(size);
            var (holderDataField, runtimeValueField) = CreateDataHolderRvaField(dataHolderType);
            var newRvaField = new RvaField
            {
                holderDataField = holderDataField,
                runtimeValueField = runtimeValueField,
                size = dataHolderType.ClassSize,
                bytes = new List<byte>((int)dataHolderType.ClassSize),
                encryptionOps = _random.NextLong(),
                salt = _random.NextInt(),
            };
            _rvaFields.Add(newRvaField);
            return newRvaField;
        }

        private RvaField GetRvaField(int preservedSize, int alignment)
        {
            Assert.IsTrue(preservedSize % alignment == 0);
            // for big size, create a new field
            if (preservedSize >= maxRvaDataSize)
            {
                return CreateRvaField(preservedSize);
            }

            if (_currentField != null)
            {
                int offset = AlignTo(_currentField.bytes.Count, alignment);

                int expectedSize = offset + preservedSize;
                if (expectedSize <= _currentField.size)
                {
                    _currentField.FillPaddingToSize(offset);
                    return _currentField;
                }

                _currentField.FillPaddingToEnd();
            }
            _currentField = CreateRvaField(maxRvaDataSize);
            return _currentField;
        }

        public RvaData Allocate(int value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(long value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(float value)
        {
            RvaField field = GetRvaField(4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(double value)
        {
            RvaField field = GetRvaField(8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Allocate(bytes);
        }

        public RvaData Allocate(byte[] value)
        {
            RvaField field = GetRvaField(value.Length, 1);
            int offset = field.bytes.Count;
            field.bytes.AddRange(value);
            return new RvaData(field.runtimeValueField, offset, value.Length);
        }

        private void CreateCCtorOfRvaTypeDef()
        {
            if (_rvaTypeDef == null)
            {
                return;
            }
            ModuleDef mod = _rvaTypeDef.Module;
            var cctorMethod = new MethodDefUser(".cctor",
                MethodSig.CreateStatic(_module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Private);
            cctorMethod.DeclaringType = _rvaTypeDef;
            //_rvaTypeDef.Methods.Add(cctor);
            var body = new CilBody();
            cctorMethod.Body = body;
            var ins = body.Instructions;

            IMethod initializeArrayMethod = mod.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            IMethod decryptArrayMethod = mod.Import(typeof(EncryptionService).GetMethod("DecryptBlock", new[] { typeof(byte[]), typeof(long),  typeof(int) }));

            Assert.IsNotNull(initializeArrayMethod);
            foreach (var field in _rvaFields)
            {
                // ldc
                // newarr
                // dup
                // stsfld
                // ldtoken
                // RuntimeHelpers.InitializeArray(array, fieldHandle);
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, (int)field.size));
                ins.Add(Instruction.Create(OpCodes.Newarr, field.runtimeValueField.FieldType.Next.ToTypeDefOrRef()));
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.Create(OpCodes.Dup));
                ins.Add(Instruction.Create(OpCodes.Stsfld, field.runtimeValueField));
                ins.Add(Instruction.Create(OpCodes.Ldtoken, field.holderDataField));
                ins.Add(Instruction.Create(OpCodes.Call, initializeArrayMethod));

                // EncryptionService.DecryptBlock(array, field.encryptionOps, field.salt);
                ins.Add(Instruction.Create(OpCodes.Ldc_I8, field.encryptionOps));
                ins.Add(Instruction.Create(OpCodes.Ldc_I4, field.salt));
                ins.Add(Instruction.Create(OpCodes.Call, decryptArrayMethod));

            }
            ins.Add(Instruction.Create(OpCodes.Ret));
        }

        private void SetFieldsRVA()
        {
            foreach (var field in _rvaFields)
            {
                Assert.IsTrue(field.bytes.Count <= field.size);
                if (field.bytes.Count < field.size)
                {
                    field.FillPaddingToEnd();
                }
                byte[] data = field.bytes.ToArray();
                _encryptor.EncryptBlock(data, field.encryptionOps, field.salt);
                field.holderDataField.InitialValue = data;
            }
        }

        public void Done()
        {
            SetFieldsRVA();
            CreateCCtorOfRvaTypeDef();
        }
    }

    public class RvaDataAllocator
    {

        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly Dictionary<ModuleDef, ModuleRvaDataAllocator> _modules = new Dictionary<ModuleDef, ModuleRvaDataAllocator>();

        public RvaDataAllocator(IRandom random, IEncryptor encryptor)
        {
            _random = random;
            _encryptor = encryptor;
        }

        private ModuleRvaDataAllocator GetModuleRvaDataAllocator(ModuleDef mod)
        {
            if (!_modules.TryGetValue(mod, out var allocator))
            {
                allocator = new ModuleRvaDataAllocator(mod, _random, _encryptor);
                _modules.Add(mod, allocator);
            }
            return allocator;
        }

        public RvaData Allocate(ModuleDef mod, int value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, long value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, float value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, double value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, string value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public RvaData Allocate(ModuleDef mod, byte[] value)
        {
            return GetModuleRvaDataAllocator(mod).Allocate(value);
        }

        public void Done()
        {
            foreach (var allocator in _modules.Values)
            {
                allocator.Done();
            }
        }
    }
}
