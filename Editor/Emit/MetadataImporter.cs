//using dnlib.DotNet;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Obfuz.Emit
//{
//    public interface IModuleMetadataImporter
//    {
//        void Init(ModuleDef mod);
//    }

//    public abstract class ModuleMetadataImporterBase : IModuleMetadataImporter
//    {
//        public abstract void Init(ModuleDef mod);
//    }

//    public class MetadataImporter
//    {
//        private readonly Dictionary<(ModuleDef, Type), IModuleMetadataImporter> _customModuleMetadataImporters = new Dictionary<(ModuleDef, Type), IModuleMetadataImporter>();

//        public static MetadataImporter Instance { get; private set; }

//        public static void Reset()
//        {
//            Instance = new MetadataImporter();
//        }

//        public DefaultModuleMetadataImporter GetDefaultModuleMetadataImporter(ModuleDef module)
//        {
//            return GetCustomModuleMetadataImporter<DefaultModuleMetadataImporter>(module);
//        }

//        public List<DefaultModuleMetadataImporter> GetDefaultModuleMetadataImporters()
//        {
//            return GetCustomModuleMetadataImporters<DefaultModuleMetadataImporter>();
//        }

//        public T GetCustomModuleMetadataImporter<T>(ModuleDef module, Func<ModuleDef, T> creator = null) where T : IModuleMetadataImporter
//        {
//            var key = (module, typeof(T));
//            if (!_customModuleMetadataImporters.TryGetValue(key, out var importer))
//            {
//                if (creator != null)
//                {
//                    importer = creator(module);
//                }
//                else
//                {
//                    importer = (IModuleMetadataImporter)Activator.CreateInstance(typeof(T));
//                }
//                importer.Init(module);
//                _customModuleMetadataImporters[key] = importer;
//            }
//            return (T)importer;
//        }

//        public List<T> GetCustomModuleMetadataImporters<T>()
//        {
//            var result = new List<T>();
//            foreach (var kvp in _customModuleMetadataImporters)
//            {
//                if (kvp.Key.Item2 == typeof(T))
//                {
//                    result.Add((T)kvp.Value);
//                }
//            }
//            return result;
//        }
//    }
//}
