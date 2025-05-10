using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Obfuz.ObfusPasses.CallObfus
{
    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        private readonly List<string> _toObfuscatedAssemblyNames;


        class WhiteListAssembly
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscateNone;
            public List<WhiteListType> types = new List<WhiteListType>();
        }

        class WhiteListType
        {
            public string name;
            public NameMatcher nameMatcher;
            public bool? obfuscateNone;
            public List<WhiteListMethod> methods = new List<WhiteListMethod>();
        }

        class WhiteListMethod
        {
            public string name;
            public NameMatcher nameMatcher;
        }

        class ObfuscationRule
        {
            public bool? disableObfuscation;
            public bool? obfuscateCallInLoop;
            public bool? cacheCallIndexInLoop;
            public bool? cacheCallIndexNotLoop;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (disableObfuscation == null)
                {
                    disableObfuscation = parentRule.disableObfuscation;
                }
                if (obfuscateCallInLoop == null)
                {
                    obfuscateCallInLoop = parentRule.obfuscateCallInLoop;
                }
                if (cacheCallIndexInLoop == null)
                {
                    cacheCallIndexInLoop = parentRule.cacheCallIndexInLoop;
                }
                if (cacheCallIndexNotLoop == null)
                {
                    cacheCallIndexNotLoop = parentRule.cacheCallIndexNotLoop;
                }
            }
        }
        class AssemblySpec
        {
            public string name;
            public ObfuscationRule rule;
            public List<TypeSpec> types = new List<TypeSpec>();
        }

        class TypeSpec
        {
            public string name;
            public NameMatcher nameMatcher;
            public ObfuscationRule rule;
            public List<MethodSpec> methods = new List<MethodSpec>();
        }

        class MethodSpec
        {
            public string name;
            public NameMatcher nameMatcher;
            public ObfuscationRule rule;
        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            disableObfuscation = false,
            obfuscateCallInLoop = true,
            cacheCallIndexInLoop = true,
            cacheCallIndexNotLoop = false,
        };

        private ObfuscationRule _global;
        private readonly List<WhiteListAssembly> _whiteListAssemblies = new List<WhiteListAssembly>();
        private readonly Dictionary<string, AssemblySpec> _assemblySpecs = new Dictionary<string, AssemblySpec>();

        private readonly Dictionary<IMethod, bool> _whiteListMethodCache = new Dictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes);
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _toObfuscatedAssemblyNames = toObfuscatedAssemblyNames;
            LoadConfigs(xmlConfigFiles);
            InheritParentRules();
        }

        private void LoadConfigs(List<string> configFiles)
        {
            if (configFiles == null || configFiles.Count == 0)
            {
                Debug.LogWarning($"ConfigurableObfuscationPolicy::LoadConfigs configFiles is empty, using default policy");
                return;
            }
            foreach (var configFile in configFiles)
            {
                if (string.IsNullOrEmpty(configFile))
                {
                    throw new Exception($"ObfusSettings.callObfusSettings.configFiles contains empty file name");
                }
                LoadConfig(configFile);
            }
        }

        private void LoadConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                Debug.LogWarning($"ConfigurableObfuscationPolicy::LoadConfig configFile is empty, using default policy");
                return;
            }
            Debug.Log($"ConfigurableObfuscationPolicy::LoadConfig {configFile}");
            var doc = new XmlDocument();
            doc.Load(configFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {configFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "global": _global = ParseObfuscationRule(ele, true); break;
                    case "whitelist": ParseWhitelist(ele); break;
                    case "assembly":
                    {
                        AssemblySpec assSpec = ParseAssembly(ele);
                        string name = assSpec.name;
                        if (!_toObfuscatedAssemblyNames.Contains(name))
                        {
                            throw new Exception($"Invalid xml file {configFile}, assembly name {name} isn't in toObfuscatedAssemblyNames");
                        }
                        if (_assemblySpecs.ContainsKey(name))
                        {
                            throw new Exception($"Invalid xml file {configFile}, assembly name {name} is duplicated");
                        }
                        _assemblySpecs.Add(name, assSpec);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
                }
            }
        }

        private void InheritParentRules()
        {
            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            foreach (AssemblySpec assSpec in _assemblySpecs.Values)
            {
                assSpec.rule.InheritParent(_global);
                foreach (TypeSpec typeSpec in assSpec.types)
                {
                    typeSpec.rule.InheritParent(assSpec.rule);
                    foreach (MethodSpec methodSpec in typeSpec.methods)
                    {
                        methodSpec.rule.InheritParent(typeSpec.rule);
                    }
                }
            }
        }

        private ObfuscationRule ParseObfuscationRule(XmlElement ele, bool parseWhitelist)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("disableObfuscation"))
            {
                rule.disableObfuscation = ConfigUtil.ParseBool(ele.GetAttribute("disableObfuscation"));
            }
            if (ele.HasAttribute("obfuscateCallInLoop"))
            {
                rule.obfuscateCallInLoop = ConfigUtil.ParseBool(ele.GetAttribute("obfuscateCallInLoop"));
            }
            if (ele.HasAttribute("cacheCallIndexInLoop"))
            {
                rule.cacheCallIndexInLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheCallIndexInLoop"));
            }
            if (ele.HasAttribute("cacheCallIndexNotLoop"))
            {
                rule.cacheCallIndexNotLoop = ConfigUtil.ParseBool(ele.GetAttribute("cacheCallIndexNotLoop"));
            }
            return rule;
        }

        private void ParseWhitelist(XmlElement ruleEle)
        {
            foreach (XmlNode xmlNode in ruleEle.ChildNodes)
            {
                if (!(xmlNode is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "assembly":
                    {
                        var ass = ParseWhiteListtAssembly(childEle);
                        _whiteListAssemblies.Add(ass);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                }
            }
        }

        private WhiteListAssembly ParseWhiteListtAssembly(XmlElement element)
        {
            var ass = new WhiteListAssembly();
            ass.name = element.GetAttribute("name");
            ass.nameMatcher = new NameMatcher(ass.name);
            if (element.HasAttribute("obfuscateNone"))
            {
                ass.obfuscateNone = ConfigUtil.ParseBool(element.GetAttribute("obfuscateNone"));
            }
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "type":
                    ass.types.Add(ParseWhiteListType(ele));
                    break;
                    default:
                    throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }
            return ass;
        }

        private WhiteListType ParseWhiteListType(XmlElement element)
        {
            var type = new WhiteListType();
            type.name = element.GetAttribute("name");
            type.nameMatcher = new NameMatcher(type.name);
            if (element.HasAttribute("obfuscateNone"))
            {
                type.obfuscateNone = ConfigUtil.ParseBool(element.GetAttribute("obfuscateNone"));
            }

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "method":
                    {
                        type.methods.Add(ParseWhiteListMethod(ele));
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }

            return type;
        }

        private WhiteListMethod ParseWhiteListMethod(XmlElement element)
        {
            var method = new WhiteListMethod();
            method.name = element.GetAttribute("name");
            method.nameMatcher = new NameMatcher(method.name);
            return method;
        }

        private AssemblySpec ParseAssembly(XmlElement element)
        {
            var assemblySpec = new AssemblySpec();
            assemblySpec.name = element.GetAttribute("name");
            if (string.IsNullOrEmpty(assemblySpec.name))
            {
                throw new Exception($"Invalid xml file, assembly name is empty");
            }
            assemblySpec.rule = ParseObfuscationRule(element, false);
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "type":
                    assemblySpec.types.Add(ParseType(ele));
                    break;
                    default:
                    throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }
            return assemblySpec;
        }

        private TypeSpec ParseType(XmlElement element)
        {
            var typeSpec = new TypeSpec();
            typeSpec.name = element.GetAttribute("name");
            typeSpec.nameMatcher = new NameMatcher(typeSpec.name);
            if (string.IsNullOrEmpty(typeSpec.name))
            {
                throw new Exception($"Invalid xml file, type name is empty");
            }
            typeSpec.rule = ParseObfuscationRule(element, false);
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement ele))
                {
                    continue;
                }
                switch (ele.Name)
                {
                    case "method":
                    typeSpec.methods.Add(ParseMethod(ele));
                    break;
                    default:
                    throw new Exception($"Invalid xml file, unknown node {ele.Name}");
                }
            }
            return typeSpec;
        }

        private MethodSpec ParseMethod(XmlElement element)
        {
            var methodSpec = new MethodSpec();
            methodSpec.name = element.GetAttribute("name");
            methodSpec.nameMatcher = new NameMatcher(methodSpec.name);
            if (string.IsNullOrEmpty(methodSpec.name))
            {
                throw new Exception($"Invalid xml file, method name is empty");
            }
            methodSpec.rule = ParseObfuscationRule(element, false);
            return methodSpec;
        }

        private ObfuscationRule ComputeMethodObfuscationRule(MethodDef method)
        {
            var assemblyName = method.DeclaringType.Module.Assembly.Name;
            if (!_assemblySpecs.TryGetValue(assemblyName, out var assSpec))
            {
                return _global;
            }
            string declaringTypeName = method.DeclaringType.FullName;
            foreach (var typeSpec in assSpec.types)
            {
                if (typeSpec.nameMatcher.IsMatch(declaringTypeName))
                {
                    foreach (var methodSpec in typeSpec.methods)
                    {
                        if (methodSpec.nameMatcher.IsMatch(method.Name))
                        {
                            return methodSpec.rule;
                        }
                    }
                    return typeSpec.rule;
                }
            }
            return assSpec.rule;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = ComputeMethodObfuscationRule(method);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedDynamicProxyCallInMethod(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.disableObfuscation != true;
        }

        public override ObfuscationCachePolicy GetMethodObfuscationCachePolicy(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return new ObfuscationCachePolicy()
            {
                cacheInLoop = rule.cacheCallIndexInLoop.Value,
                cacheNotInLoop = rule.cacheCallIndexNotLoop.Value,
            };
        }

        private bool ComputeIsInWhiteList(IMethod calledMethod)
        {
            ITypeDefOrRef declaringType = calledMethod.DeclaringType;
            TypeSig declaringTypeSig = calledMethod.DeclaringType.ToTypeSig();
            declaringTypeSig = declaringTypeSig.RemovePinnedAndModifiers();
            switch (declaringTypeSig.ElementType)
            {
                case ElementType.ValueType:
                case ElementType.Class:
                {
                    break;
                }
                case ElementType.GenericInst:
                {
                    if (MetaUtil.ContainsContainsGenericParameter(calledMethod))
                    {
                        return true;
                    }
                    break;
                }
                default: return true;
            }

            TypeDef typeDef = declaringType.ResolveTypeDef();

            if (typeDef.IsDelegate || typeDef.IsEnum)
                return true;

            string assName = typeDef.Module.Assembly.Name;
            string typeFullName = typeDef.FullName;
            string methodName = calledMethod.Name;

            // doesn't proxy call if the method is a constructor
            if (methodName == ".ctor")
            {
                return true;
            }
            // special handle
            // don't proxy call for List<T>.Enumerator GetEnumerator()
            if (methodName == "GetEnumerator")
            {
                return true;
            }

            foreach (var ass in _whiteListAssemblies)
            {
                if (!ass.nameMatcher.IsMatch(assName))
                {
                    continue;
                }
                if (ass.obfuscateNone == true)
                {
                    return true;
                }
                foreach (var type in ass.types)
                {
                    if (!type.nameMatcher.IsMatch(typeFullName))
                    {
                        continue;
                    }
                    if (type.obfuscateNone == true)
                    {
                        return true;
                    }
                    foreach (var method in type.methods)
                    {
                        if (method.nameMatcher.IsMatch(methodName))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsInWhiteList(IMethod method)
        {
            if (!_whiteListMethodCache.TryGetValue(method, out var isWhiteList))
            {
                isWhiteList = ComputeIsInWhiteList(method);
                _whiteListMethodCache.Add(method, isWhiteList);
            }
            return isWhiteList;
        }

        public override bool NeedDynamicProxyCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop)
        {
            if (IsInWhiteList(calledMethod))
            {
                return false;
            }
            ObfuscationRule rule = GetMethodObfuscationRule(callerMethod);
            if (currentInLoop && rule.obfuscateCallInLoop == false)
            {
                return false;
            }
            return true;
        }
    }
}
