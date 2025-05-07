using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Obfuz.Emit;
using Obfuz.Data;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Obfuz.ObfusPasses.ConstObfus
{
    public class DefaultConstObfuscator : IDataObfuscator
    {
        private readonly IRandom _random;
        private readonly RandomDataNodeCreator _nodeCreator;
        private readonly RvaDataAllocator _rvaDataAllocator;
        private readonly ConstFieldAllocator _constFieldAllocator;
        private readonly IEncryptor _encryptor;

        public DefaultConstObfuscator()
        {
            _random = new RandomWithKey(new byte[] { 0x1, 0x2, 0x3, 0x4 }, 0x5);
            _encryptor = new DefaultEncryptor(new byte[] { 0x1A, 0x2B, 0x3C, 0x4D });
            _nodeCreator = new RandomDataNodeCreator(_random);
            _rvaDataAllocator = new RvaDataAllocator(_random, _encryptor);
            _constFieldAllocator = new ConstFieldAllocator(_random);
        }

        private void CompileNode(IDataNode node, MethodDef method, List<Instruction> obfuscatedInstructions)
        {
            var ctx = new CompileContext
            {
                method = method,
                output = obfuscatedInstructions,
                rvaDataAllocator = _rvaDataAllocator,
                constFieldAllocator = _constFieldAllocator,
            };
            node.Compile(ctx);
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
            return MetadataImporter.Instance.GetDefaultModuleMetadataImporter(method.Module);
        }

        public void ObfuscateInt(MethodDef method, int value, List<Instruction> obfuscatedInstructions)
        {
            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            int encryptedValue = _encryptor.Encrypt(value, ops, salt);
            RvaData rvaData = _rvaDataAllocator.Allocate(method.Module, encryptedValue);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(encryptedValue));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptInt));
        }

        public void ObfuscateLong(MethodDef method, long value, List<Instruction> obfuscatedInstructions)
        {
            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            long encryptedValue = _encryptor.Encrypt(value, ops, salt);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_I8, encryptedValue));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptLong));
        }

        public void ObfuscateFloat(MethodDef method, float value, List<Instruction> obfuscatedInstructions)
        {
            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            float encryptedValue = _encryptor.Encrypt(value, ops, salt);

            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R4, encryptedValue));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptFloat));
        }

        public void ObfuscateDouble(MethodDef method, double value, List<Instruction> obfuscatedInstructions)
        {
            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();
            double encryptedValue = _encryptor.Encrypt(value, ops, salt);
            DefaultModuleMetadataImporter importer = GetModuleMetadataImporter(method);
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldc_R8, encryptedValue));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(ops));
            obfuscatedInstructions.Add(Instruction.CreateLdcI4(salt));
            obfuscatedInstructions.Add(Instruction.Create(OpCodes.Call, importer.DecryptDouble));
        }

        public void ObfuscateBytes(MethodDef method, byte[] value, List<Instruction> obfuscatedInstructions)
        {
            int ops = GenerateEncryptionOperations();
            int salt = GenerateSalt();

            Assert.IsTrue(value.Length % 4 == 0);
            int[] intArr = new int[value.Length / 4];
            Buffer.BlockCopy(value, 0, intArr, 0, value.Length);
            byte[] encryptedValue = _encryptor.Decrypt(intArr, 0, value.Length, ops, salt);
        }

        public void ObfuscateString(MethodDef method, string value, List<Instruction> obfuscatedInstructions)
        {
            IDataNode node = _nodeCreator.CreateRandom(DataNodeType.String, value);
            CompileNode(node, method, obfuscatedInstructions);
            //obfuscatedInstructions.Add(Instruction.Create(OpCodes.Ldstr, value));
        }

        public void Done()
        {
            _rvaDataAllocator.Done();
            _constFieldAllocator.Done();
        }
    }
}
