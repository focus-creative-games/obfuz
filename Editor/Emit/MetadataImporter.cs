using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace Obfuz.Emit
{
    public class ModuleMetadataImporter
    {
        private readonly ModuleDef _module;
        public ModuleMetadataImporter(ModuleDef module)
        {
            _module = module;
            _module = module;
            InitMetadatas(module);
        }

        private static IMethod s_castIntAsFloat;
        private static IMethod s_castLongAsDouble;
        private static IMethod s_castFloatAsInt;
        private static IMethod s_castDoubleAsLong;

        private void InitMetadatas(ModuleDef mod)
        {
            if (s_castFloatAsInt != null)
            {
                return;
            }
            var constUtilityType = typeof(ConstUtility);

            s_castIntAsFloat = mod.Import(constUtilityType.GetMethod("CastIntAsFloat"));
            Assert.IsNotNull(s_castIntAsFloat, "CastIntAsFloat not found");
            s_castLongAsDouble = mod.Import(constUtilityType.GetMethod("CastLongAsDouble"));
            Assert.IsNotNull(s_castLongAsDouble, "CastLongAsDouble not found");
            s_castFloatAsInt = mod.Import(constUtilityType.GetMethod("CastFloatAsInt"));
            Assert.IsNotNull(s_castFloatAsInt, "CastFloatAsInt not found");
            s_castDoubleAsLong = mod.Import(constUtilityType.GetMethod("CastDoubleAsLong"));
            Assert.IsNotNull(s_castDoubleAsLong, "CastDoubleAsLong not found");
        }

        public IMethod GetCastIntAsFloat()
        {
            return s_castIntAsFloat;
        }

        public IMethod GetCastLongAsDouble()
        {
            return s_castLongAsDouble;
        }

        public IMethod GetCastFloatAsInt()
        {
            return s_castFloatAsInt;
        }

        public IMethod GetCastDoubleAsLong()
        {
            return s_castDoubleAsLong;
        }
    }

    public class MetadataImporter
    {

        private readonly Dictionary<ModuleDef, ModuleMetadataImporter> _moduleMetadataImporters = new Dictionary<ModuleDef, ModuleMetadataImporter>();

        public static MetadataImporter Instance { get; private set; }

        public static void Reset()
        {
            Instance = new MetadataImporter();
        }

        public ModuleMetadataImporter GetModuleMetadataImporter(ModuleDef module)
        {
            if (!_moduleMetadataImporters.TryGetValue(module, out var importer))
            {
                importer = new ModuleMetadataImporter(module);
                _moduleMetadataImporters[module] = importer;
            }
            return importer;
        }
    }
}
