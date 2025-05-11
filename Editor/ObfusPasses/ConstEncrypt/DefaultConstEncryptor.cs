using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Data;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Text;

namespace Obfuz.ObfusPasses.ConstEncrypt
{
    public class DefaultConstEncryptor : IConstEncryptor
    {
        private readonly IRandom _random;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly IEncryptor _encryptor;

        public DefaultConstEncryptor(IRandom random, IEncryptor encryptor, RvaDataAllocator rvaDataAllocator, ConstFieldAllocator constFieldAllocator)
        {
            _random = random;
            _encryptor = encryptor;
            _rvaDataAllocator = rvaDataAllocator;
            _constFieldAllocator = constFieldAllocator;
        }

        private int GenerateEncryptionOperations()
        {
            return _random.NextInt();
        }

        public int GenerateSalt()
        {
            return _random.NextInt();
        }

        private DefaultModuleMetadataImporter GetModuleMetadataImporter(MethodDef method)
        {
            return GroupByModuleManager.Ins.GetDefaultModuleMetadataImporter(method.Module);
        }

        public void ObfuscateInt(MethodDef method, bool needCacheValue, int value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            int encryptedValue = _encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaInt));
        }

        public void ObfuscateLong(MethodDef method, bool needCacheValue, long value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            long encryptedValue = _encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaLong));
        }

        public void ObfuscateFloat(MethodDef method, bool needCacheValue, float value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            float encryptedValue = _encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaFloat));
        }

        public void ObfuscateDouble(MethodDef method, bool needCacheValue, double value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            double encryptedValue = _encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaDouble));
        }

        public void ObfuscateBytes(MethodDef method, bool needCacheValue, byte[] value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            byte[] encryptedValue = _encryptor.Encrypt(value, 0, value.Length, ops, salt);
            Assert.IsTrue(encryptedValue.Length % 4 == 0);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            // should use value.Length, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(value.Length));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaBytes));
        }

        public void ObfuscateString(MethodDef method, bool needCacheValue, string value, List<Instruction> obfuscatedInstructions)
        {
            if (needCacheValue)
            {
                FieldDef cacheField = _constFieldAllocator.Allocate(method.Module, value);
                obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, cacheField));
                return;
            }

            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            int stringByteLength = Encoding.UTF8.GetByteCount(value);
            byte[] encryptedValue = _encryptor.Encrypt(value, ops, salt);
            Assert.IsTrue(encryptedValue.Length % 4 == 0);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldsfld, rvaData.field));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(rvaData.offset));
            // should use stringByteLength, can't use rvaData.size, because rvaData.size is align to 4, it's not the actual length.
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(stringByteLength));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFromRvaString));
        }

        public void Done()
        {
            _rvaDataAllocator.Done();
            _constFieldAllocator.Done();
        }
    }
}
