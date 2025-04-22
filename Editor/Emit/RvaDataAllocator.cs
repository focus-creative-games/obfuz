using dnlib.DotNet;
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

    public class RvaDataAllocator
    {
        // randomized
        const int maxRvaDataSize = 0x100;

        private readonly IRandom _random;

        class RvaField
        {
            public FieldDef holderDataField;
            public FieldDef runtimeValueField;
            public uint size;
            public List<byte> bytes;

            public void FillPadding()
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

        private readonly Dictionary<(ModuleDef, int), TypeDef> _dataHolderTypeBySizes = new Dictionary<(ModuleDef, int), TypeDef>();

        public RvaDataAllocator(IRandom random)
        {
            _random = random;
        }

        private (FieldDef, FieldDef) CreateDataHolderRvaField(ModuleDef mod, TypeDef dataHolderType)
        {
            if (_rvaTypeDef == null)
            {
                mod.EnableTypeDefFindCache = false;
                //_rvaTypeDef = mod.Find("$ObfuzRVA$", false);
                //if (_rvaTypeDef != null)
                //{
                //    throw new Exception($"can't obfuscate a obfuscated assembly");
                //}
                ITypeDefOrRef objectTypeRef = mod.Import(typeof(object));
                _rvaTypeDef = new TypeDefUser("$Obfuz$RVA$",objectTypeRef);
                mod.Types.Add(_rvaTypeDef);
                mod.EnableTypeDefFindCache = true;
            }


            var holderField = new FieldDefUser($"$RVA_Data{_rvaTypeDef.Fields.Count}",  new FieldSig(dataHolderType.ToTypeSig()), FieldAttributes.InitOnly | FieldAttributes.Static | FieldAttributes.HasFieldRVA);
            holderField.DeclaringType = _rvaTypeDef;

            var runtimeValueField = new FieldDefUser($"$RVA_Value{_rvaTypeDef.Fields.Count}", new FieldSig(new SZArraySig(mod.CorLibTypes.Byte)), FieldAttributes.Static);
            runtimeValueField.DeclaringType = _rvaTypeDef;
            return (holderField, runtimeValueField);
        }

        private TypeDef GetDataHolderType(ModuleDef mod, int size)
        {
            size = (size + 15) & ~15; // align to 6 bytes
            if (_dataHolderTypeBySizes.TryGetValue((mod, size), out var type))
                return type;
            var dataHolderType = new TypeDefUser($"$ObfuzRVA$DataHolder{size}", mod.Import(typeof(ValueType)));
            dataHolderType.Layout = TypeAttributes.ExplicitLayout;
            dataHolderType.PackingSize = 1;
            dataHolderType.ClassSize = (uint)size;
            _dataHolderTypeBySizes.Add((mod, size), dataHolderType);
            mod.Types.Add(dataHolderType);
            return dataHolderType;
        }

        private static int AlignTo(int size, int alignment)
        {
            return (size + alignment - 1) & ~(alignment - 1);
        }

        private RvaField CreateRvaField(ModuleDef mod, int size)
        {
            TypeDef dataHolderType = GetDataHolderType(mod, size);
            var (holderDataField, runtimeValueField) = CreateDataHolderRvaField(mod, dataHolderType);
            var newRvaField = new RvaField
            {
                holderDataField = holderDataField,
                runtimeValueField = runtimeValueField,
                size = dataHolderType.ClassSize,
                bytes = new List<byte>((int)dataHolderType.ClassSize),
            };
            _rvaFields.Add(newRvaField);
            return newRvaField;
        }

        private RvaField GetRvaField(ModuleDef mod, int preservedSize, int alignment)
        {
            Assert.IsTrue(preservedSize % alignment == 0);
            // for big size, create a new field
            if (preservedSize >= maxRvaDataSize)
            {
                return CreateRvaField(mod, preservedSize);
            }

            if (_currentField != null)
            {
                int offset = AlignTo(_currentField.bytes.Count, alignment);

                int expectedSize = offset + preservedSize;
                if (expectedSize <= _currentField.size)
                {
                    // insert random padding
                    for (int i = _currentField.bytes.Count; i < offset; i++)
                    {
                        //_currentField.bytes.Add((byte)_random.NextInt(0, 256));
                        // TODO replace with random value
                        _currentField.bytes.Add(0xAB);
                    }
                    return _currentField;
                }

                _currentField.FillPadding();
            }
            _currentField = CreateRvaField(mod, maxRvaDataSize);
            return _currentField;
        }

        public RvaData Allocate(ModuleDef mod, int value)
        {
            RvaField field = GetRvaField(mod, 4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(ModuleDef mod, long value)
        {
            RvaField field = GetRvaField(mod, 8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(ModuleDef mod, float value)
        {
            RvaField field = GetRvaField(mod, 4, 4);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 4 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 4);
        }

        public RvaData Allocate(ModuleDef mod, double value)
        {
            RvaField field = GetRvaField(mod, 8, 8);
            int offset = field.bytes.Count;
            Assert.IsTrue(offset % 8 == 0);
            field.bytes.AddRange(BitConverter.GetBytes(value));
            return new RvaData(field.runtimeValueField, offset, 8);
        }

        public RvaData Allocate(ModuleDef mod, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Allocate(mod, bytes);
        }

        public RvaData Allocate(ModuleDef mod, byte[] value)
        {
            RvaField field = GetRvaField(mod, value.Length, 1);
            int offset = field.bytes.Count;
            field.bytes.AddRange(value);
            return new RvaData(field.runtimeValueField, offset, value.Length);
        }

        public void SetFieldsRVA()
        {
            foreach (var field in _rvaFields)
            {
                Assert.IsTrue(field.bytes.Count <= field.size);
                if (field.bytes.Count < field.size)
                {
                    field.FillPadding();
                }
                field.holderDataField.InitialValue = field.bytes.ToArray();
            }
        }
    }
}
