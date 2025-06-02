
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.Unity
{

    public class LiteSymbolMappingReader
    {
        class TypeMappingInfo
        {
            public string oldFullName;
            public string newFullName;

            //public Dictionary<string, string> MethodMappings = new Dictionary<string, string>();
        }

        class AssemblyMappingInfo
        {
            public Dictionary<string, string> TypeMappings = new Dictionary<string, string>();
            public Dictionary<string, string> MethodMappings = new Dictionary<string, string>();
        }

        private readonly Dictionary<string, AssemblyMappingInfo> _assemblies = new Dictionary<string, AssemblyMappingInfo>();

        public LiteSymbolMappingReader(string mappingFile)
        {
            LoadXmlMappingFile(mappingFile);
        }

        private void LoadXmlMappingFile(string mappingFile)
        {
            var doc = new XmlDocument();
            doc.Load(mappingFile);
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                LoadAssemblyMapping(element);
            }
        }

        private void LoadAssemblyMapping(XmlElement ele)
        {
            if (ele.Name != "assembly")
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. Expected 'assembly'.");
            }
            string assName = ele.GetAttribute("name");
            if (string.IsNullOrEmpty(assName))
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. attribute 'name' missing.");
            }
            if (!_assemblies.TryGetValue(assName, out var assemblyMappingInfo))
            {
                assemblyMappingInfo = new AssemblyMappingInfo();
                _assemblies[assName] = assemblyMappingInfo;
            }

            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                if (element.Name == "type")
                {
                    LoadTypeMapping(element, assemblyMappingInfo);
                }
            }
        }

        private void LoadTypeMapping(XmlElement ele, AssemblyMappingInfo assemblyMappingInfo)
        {
            string oldFullName = ele.GetAttribute("fullName");
            string newFullName = ele.GetAttribute("newFullName");
            string status = ele.GetAttribute("status");
            if (status == "Renamed")
            {
                if (string.IsNullOrEmpty(oldFullName) || string.IsNullOrEmpty(newFullName))
                {
                    throw new System.Exception($"Invalid node name: {ele.Name}. attributes 'fullName' or 'newFullName' missing.");
                }
                assemblyMappingInfo.TypeMappings[oldFullName] = newFullName;
            }
            //foreach (XmlNode node in ele.ChildNodes)
            //{
            //    if (!(node is XmlElement c))
            //    {
            //        continue;
            //    }
            //    if (node.Name == "method")
            //    {
            //        LoadMethodMapping(c);
            //    }
            //}
        }

        public bool TryGetNewTypeName(string assemblyName, string oldFullName, out string newFullName)
        {
            newFullName = null;
            if (_assemblies.TryGetValue(assemblyName, out var assemblyMappingInfo))
            {
                if (assemblyMappingInfo.TypeMappings.TryGetValue(oldFullName, out newFullName))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
