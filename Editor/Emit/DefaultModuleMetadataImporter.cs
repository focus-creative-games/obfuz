using dnlib.DotNet;
using System;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class DefaultModuleMetadataImporter : ModuleMetadataImporterBase
    {
        public DefaultModuleMetadataImporter() { }

        public override void Init(ModuleDef mod)
        {
            _module = mod;
            var constUtilityType = typeof(ConstUtility);

            _castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
            Assert.IsNotNull(_castIntAsFloat, "CastIntAsFloat not found");
            _castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
            Assert.IsNotNull(_castLongAsDouble, "CastLongAsDouble not found");
            _castFloatAsInt = mod.Import(constUtilityType.GetMethod("CastFloatAsInt"));
            Assert.IsNotNull(_castFloatAsInt, "CastFloatAsInt not found");
            _castDoubleAsLong = mod.Import(constUtilityType.GetMethod("CastDoubleAsLong"));
            Assert.IsNotNull(_castDoubleAsLong, "CastDoubleAsLong not found");

            _initializeArray = mod.Import(typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("InitializeArray", new[] { typeof(Array), typeof(RuntimeFieldHandle) }));
            Assert.IsNotNull(_initializeArray);
            _encryptBlock = mod.Import(typeof(EncryptionService).GetMethod("EncryptBlock", new[] { typeof(byte[]), typeof(long), typeof(int) }));
            Assert.IsNotNull(_encryptBlock);
            _decryptBlock = mod.Import(typeof(EncryptionService).GetMethod("DecryptBlock", new[] { typeof(byte[]), typeof(long), typeof(int) }));
            Assert.IsNotNull(_decryptBlock);
            _encryptInt = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptInt);
            _decryptInt = mod.Import(typeof(EncryptionService).GetMethod("Decrypt", new[] { typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptInt);
            _encryptLong = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptLong);
            _decryptLong = mod.Import(typeof(EncryptionService).GetMethod("Decrypt", new[] { typeof(long), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptLong);
            _encryptFloat = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptFloat);
            _decryptFloat = mod.Import(typeof(EncryptionService).GetMethod("Decrypt", new[] { typeof(float), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptFloat);
            _encryptDouble = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptDouble);
            _decryptDouble = mod.Import(typeof(EncryptionService).GetMethod("Decrypt", new[] { typeof(double), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptDouble);
            _encryptString = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(string), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptString);
            _decryptString = mod.Import(typeof(EncryptionService).GetMethod("DecryptString", new[] { typeof(int[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptString);
            _encryptBytes = mod.Import(typeof(EncryptionService).GetMethod("Encrypt", new[] { typeof(byte[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_encryptBytes);
            _decryptBytes = mod.Import(typeof(EncryptionService).GetMethod("Decrypt", new[] { typeof(int[]), typeof(int), typeof(int), typeof(int), typeof(int) }));
            Assert.IsNotNull(_decryptBytes);
        }

        private ModuleDef _module;
        private IMethod _castIntAsFloat;
        private IMethod _castLongAsDouble;
        private IMethod _castFloatAsInt;
        private IMethod _castDoubleAsLong;
        private IMethod _initializeArray;

        private IMethod _encryptBlock;
        private IMethod _decryptBlock;
        private IMethod _encryptInt;
        private IMethod _decryptInt;
        private IMethod _encryptLong;
        private IMethod _decryptLong;
        private IMethod _encryptFloat;
        private IMethod _decryptFloat;
        private IMethod _encryptDouble;
        private IMethod _decryptDouble;
        private IMethod _encryptString;
        private IMethod _decryptString;
        private IMethod _encryptBytes;
        private IMethod _decryptBytes;

        public IMethod CastIntAsFloat => _castIntAsFloat;
        public IMethod CastLongAsDouble => _castLongAsDouble;
        public IMethod CastFloatAsInt => _castFloatAsInt;
        public IMethod CastDoubleAsLong => _castDoubleAsLong;

        public IMethod InitializedArrayMethod => _initializeArray;

        public IMethod EncryptBlock => _encryptBlock;
        public IMethod DecryptBlock => _decryptBlock;

        public IMethod EncryptInt => _encryptInt;
        public IMethod DecryptInt => _decryptInt;
        public IMethod EncryptLong => _encryptLong;
        public IMethod DecryptLong => _decryptLong;
        public IMethod EncryptFloat => _encryptFloat;
        public IMethod DecryptFloat => _decryptFloat;
        public IMethod EncryptDouble => _encryptDouble;
        public IMethod DecryptDouble => _decryptDouble;
        public IMethod EncryptString => _encryptString;
        public IMethod DecryptString => _decryptString;
        public IMethod EncryptBytes => _encryptBytes;
        public IMethod DecryptBytes => _decryptBytes;
    }
}
