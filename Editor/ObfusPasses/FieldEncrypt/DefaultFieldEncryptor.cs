using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public class DefaultFieldEncryptor : FieldEncryptorBase
    {
        private readonly IRandom _random;
        private readonly IEncryptor _encryptor;
        private readonly GroupByModuleEntityManager _moduleEntityManager;
        private readonly int _encryptionLevel;

        public DefaultFieldEncryptor(IRandom random, IEncryptor encryptor, GroupByModuleEntityManager moduleEntityManager, int encryptionLevel)
        {
            _random = random;
            _encryptor = encryptor;
            _moduleEntityManager = moduleEntityManager;
            _encryptionLevel = encryptionLevel;
        }

        private DefaultMetadataImporter GetMetadataImporter(MethodDef method)
        {
            return _moduleEntityManager.GetDefaultModuleMetadataImporter(method.Module);
        }

         class FieldEncryptInfo
        {
            public int encryptOps;
            public int salt;
            public ElementType fieldType;
            public long xorValueForZero;
        }

        private readonly Dictionary<FieldDef, FieldEncryptInfo> _fieldEncryptInfoCache = new Dictionary<FieldDef, FieldEncryptInfo>();


        private long CalcXorValueForZero(ElementType type, int encryptOps, int salt)
        {
            switch (type)
            {
                case ElementType.I4:
                case ElementType.U4:
                case ElementType.R4:
                    return _encryptor.Encrypt(0, encryptOps, salt);
                case ElementType.I8:
                case ElementType.U8:
                case ElementType.R8:
                return _encryptor.Encrypt(0L, encryptOps, salt);
                default:
                throw new NotSupportedException($"Unsupported field type: {type} for encryption");
            }
        }


        private int GenerateEncryptionOperations()
        {
            return EncryptionUtil.GenerateEncryptionOpCodes(_random, _encryptor, _encryptionLevel);
        }

        public int GenerateSalt()
        {
            return _random.NextInt();
        }

        private FieldEncryptInfo GetFieldEncryptInfo(FieldDef field)
        {
            if (_fieldEncryptInfoCache.TryGetValue(field, out var info))
            {
                return info;
            }

            int encryptOps = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            ElementType fieldType = field.FieldSig.Type.RemovePinnedAndModifiers().ElementType;
            long xorValueForZero = CalcXorValueForZero(fieldType, encryptOps, salt);

            info = new FieldEncryptInfo
            {
                encryptOps = encryptOps,
                salt = salt,
                fieldType = fieldType,
                xorValueForZero = xorValueForZero,
            };
            _fieldEncryptInfoCache[field] = info;
            return info;
        }

        public override void Encrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction)
        {
            DefaultMetadataImporter importer = GetMetadataImporter(method);
            FieldEncryptInfo fei = GetFieldEncryptInfo(field);
            if (fei.fieldType == ElementType.I4 || fei.fieldType == ElementType.U4 || fei.fieldType == ElementType.R4)
            {
                // value has been put on stack

                // encrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.EncryptInt));
                // xor
                outputInstructions.Add(Instruction.CreateLdcI4((int)fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));
            }
            else if (fei.fieldType == ElementType.I8 || fei.fieldType == ElementType.U8 || fei.fieldType == ElementType.R8)
            {
                // value has been put on stack

                // encrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.EncryptLong));
                // xor
                outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));
            }
            else
            {
                Assert.IsTrue(false, $"Unsupported field type: {fei.fieldType} for encryption");
            }

            outputInstructions.Add(currentInstruction.Clone());
        }

        public override void Decrypt(MethodDef method, FieldDef field, List<Instruction> outputInstructions, Instruction currentInstruction)
        {
            outputInstructions.Add(currentInstruction.Clone());
            DefaultMetadataImporter importer = GetMetadataImporter(method);
            FieldEncryptInfo fei = GetFieldEncryptInfo(field);
            if (fei.fieldType == ElementType.I4 || fei.fieldType == ElementType.U4 || fei.fieldType == ElementType.R4)
            {
                // value has been put on stack
                // xor
                if (fei.fieldType == ElementType.R4)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastFloatAsInt));
                }
                outputInstructions.Add(Instruction.CreateLdcI4((int)fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));

                // decrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
            }
            else if (fei.fieldType == ElementType.I8 || fei.fieldType == ElementType.U8 || fei.fieldType == ElementType.R8)
            {
                // value has been put on stack
                // xor
                if (fei.fieldType == ElementType.R8)
                {
                    outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.CastDoubleAsLong));
                }
                outputInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, fei.xorValueForZero));
                outputInstructions.Add(Instruction.Create(OpCodes.Xor));

                // decrypt
                outputInstructions.Add(Instruction.CreateLdcI4(fei.encryptOps));
                outputInstructions.Add(Instruction.CreateLdcI4(fei.salt));
                outputInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptLong));
            }
            else
            {
                Assert.IsTrue(false, $"Unsupported field type: {fei.fieldType} for decryption");
            }
        }
    }
}
