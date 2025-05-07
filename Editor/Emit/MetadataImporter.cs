using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public interface IModuleMetadataImporter
    {
        void Init(ModuleDef mod);
    }

    public abstract class ModuleMetadataImporterBase : IModuleMetadataImporter
    {
        public abstract void Init(ModuleDef mod);
    }

    public class DefaultModuleMetadataImporter : ModuleMetadataImporterBase
    {
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
        }

        private ModuleDef _module;
        private IMethod _castIntAsFloat;
        private IMethod _castLongAsDouble;
        private IMethod _castFloatAsInt;
        private IMethod _castDoubleAsLong;
        private IMethod _initializeArray;
        private IMethod _encryptBlock;
        private IMethod _decryptBlock;

        public IMethod CastIntAsFloat => _castIntAsFloat;

        public IMethod CastLongAsDouble => _castLongAsDouble;

        public IMethod CastFloatAsInt => _castFloatAsInt;

        public IMethod CastDoubleAsLong => _castDoubleAsLong;

        public IMethod InitializedArrayMethod => _initializeArray;

        public IMethod EncryptBlock => _encryptBlock;

        public IMethod DecryptBlock => _decryptBlock;
    }

    public class MetadataImporter
    {
        private readonly Dictionary<(ModuleDef, Type), IModuleMetadataImporter> _customModuleMetadataImporters = new Dictionary<(ModuleDef, Type), IModuleMetadataImporter>();

        public static MetadataImporter Instance { get; private set; }

        public static void Reset()
        {
            Instance = new MetadataImporter();
        }

        public DefaultModuleMetadataImporter GetDefaultModuleMetadataImporter(ModuleDef module)
        {
            return GetCustomModuleMetadataImporter<DefaultModuleMetadataImporter>(module);
        }

        public List<DefaultModuleMetadataImporter> GetDefaultModuleMetadataImporters()
        {
            return GetCustomModuleMetadataImporters<DefaultModuleMetadataImporter>();
        }

        public T GetCustomModuleMetadataImporter<T>(ModuleDef module, Func<ModuleDef, T> creator = null) where T : IModuleMetadataImporter
        {
            var key = (module, typeof(T));
            if (!_customModuleMetadataImporters.TryGetValue(key, out var importer))
            {
                if (creator != null)
                {
                    importer = creator(module);
                }
                else
                {
                    importer = (IModuleMetadataImporter)Activator.CreateInstance(typeof(T), module);
                }
                importer.Init(module);
                _customModuleMetadataImporters[key] = importer;
            }
            return (T)importer;
        }

        public List<T> GetCustomModuleMetadataImporters<T>()
        {
            var result = new List<T>();
            foreach (var kvp in _customModuleMetadataImporters)
            {
                if (kvp.Key.Item2 == typeof(T))
                {
                    result.Add((T)kvp.Value);
                }
            }
            return result;
        }
    }
}
