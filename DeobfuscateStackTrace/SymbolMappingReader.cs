
using System.Xml;

namespace DeobfuscateStackTrace
{

    public class SymbolMappingReader
    {

        private readonly Dictionary<string, List<string>> _fullSignatureMapper = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<string>> _signatureWithParamsMapper = new Dictionary<string, List<string>>();


        private enum RenameStatus
        {
            NotRenamed,
            Renamed,
        }

        private class RenameRecord
        {
            public RenameStatus status;
            public string signature;
            public string oldName;
            public string newName;
            public string oldStackTraceSignature; // only for MethodDef
            public object renameMappingData;
        }

        private class RenameMappingField
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingMethod
        {
            public RenameStatus status;
            public string signature;
            public string newName;

            public List<RenameMappingMethodParam> parameters = new List<RenameMappingMethodParam>();
        }

        private class RenameMappingMethodParam
        {
            public RenameStatus status;
            public int index;
            public string newName;
        }

        private class RenameMappingProperty
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingEvent
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingType
        {
            public RenameStatus status;
            public string oldFullName;
            public string newFullName;

            public Dictionary<string, RenameMappingField> fields = new Dictionary<string, RenameMappingField>();
            public Dictionary<string, RenameMappingMethod> methods = new Dictionary<string, RenameMappingMethod>();
            public Dictionary<string, RenameMappingProperty> properties = new Dictionary<string, RenameMappingProperty>();
            public Dictionary<string, RenameMappingEvent> events = new Dictionary<string, RenameMappingEvent>();
        }

        private class RenameMappingAssembly
        {
            public RenameStatus status;
            public string oldAssName;
            public string newAssName;

            public Dictionary<string, RenameMappingType> types = new Dictionary<string, RenameMappingType>();
        }


        public SymbolMappingReader(string mappingFile)
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
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                if (element.Name == "type")
                {
                    LoadTypeMapping(element);
                }
            }
        }

        private void LoadTypeMapping(XmlElement ele)
        {
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement c))
                {
                    continue;
                }
                if (node.Name == "method")
                {
                    LoadMethodMapping(c);
                }
            }
        }


        private string GetMethodSignatureWithoutParams(string signature)
        {
            int index = signature.IndexOf('(');
            if (index < 0)
            {
                return signature;
            }
            return signature.Substring(0, index);
        }

        private void LoadMethodMapping(XmlElement ele)
        {
            if (!ele.HasAttribute("oldStackTraceSignature"))
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. attribute 'oldStackTraceSignature' missing.");
            }
            if (!ele.HasAttribute("newStackTraceSignature"))
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. attribute 'newStackTraceSignature' missing.");
            }
            string oldStackTraceSignature = ele.Attributes["oldStackTraceSignature"].Value;
            string newStackTraceSignature = ele.Attributes["newStackTraceSignature"].Value;

            if (!_fullSignatureMapper.TryGetValue(newStackTraceSignature, out var oldFullSignatures))
            {
                oldFullSignatures = new List<string>();
                _fullSignatureMapper[newStackTraceSignature] = oldFullSignatures;
            }
            oldFullSignatures.Add(oldStackTraceSignature);

            string oldStackTraceSignatureWithoutParams = GetMethodSignatureWithoutParams(oldStackTraceSignature);
            string newStackTraceSignatureWithoutParams = GetMethodSignatureWithoutParams(newStackTraceSignature);
            if (!_signatureWithParamsMapper.TryGetValue(newStackTraceSignatureWithoutParams, out var oldSignaturesWithoutParams))
            {
                oldSignaturesWithoutParams = new List<string>();
                _signatureWithParamsMapper[newStackTraceSignatureWithoutParams] = oldSignaturesWithoutParams;
            }
            oldSignaturesWithoutParams.Add(oldStackTraceSignatureWithoutParams);
        }


        public bool TryDeObfuscateStackTrace(string obfuscatedStackTraceLog, out string deObfuscatedStackTrace)
        {
            obfuscatedStackTraceLog = obfuscatedStackTraceLog.Trim();
            if (_fullSignatureMapper.TryGetValue(obfuscatedStackTraceLog, out var oldFullSignatures))
            {
                deObfuscatedStackTrace = string.Join("|", oldFullSignatures);
                return true;
            }
            
            string obfuscatedStackTraceSignatureWithoutParams = GetMethodSignatureWithoutParams(obfuscatedStackTraceLog);
            if (_signatureWithParamsMapper.TryGetValue(obfuscatedStackTraceSignatureWithoutParams, out var oldSignaturesWithoutParams))
            {
                deObfuscatedStackTrace = obfuscatedStackTraceLog.Replace(obfuscatedStackTraceSignatureWithoutParams, string.Join("|", oldSignaturesWithoutParams));
                return true;
            }
            deObfuscatedStackTrace = null;
            return false;
        }
    }
}
