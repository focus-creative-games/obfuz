using dnlib.DotNet;
using Obfuz.Conf;
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

        class ObfuscationRule : IRule<ObfuscationRule>
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

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {

        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            disableObfuscation = false,
            obfuscateCallInLoop = true,
            cacheCallIndexInLoop = true,
            cacheCallIndexNotLoop = false,
        };

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _configParser;

        private ObfuscationRule _global;
        private readonly List<WhiteListAssembly> _whiteListAssemblies = new List<WhiteListAssembly>();
        private readonly Dictionary<string, AssemblySpec> _assemblySpecs = new Dictionary<string, AssemblySpec>();

        private readonly Dictionary<IMethod, bool> _whiteListMethodCache = new Dictionary<IMethod, bool>(MethodEqualityComparer.CompareDeclaringTypes);
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _toObfuscatedAssemblyNames = toObfuscatedAssemblyNames;
            _configParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(_toObfuscatedAssemblyNames,
                ParseObfuscationRule, ParseGlobalElement);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _configParser.LoadConfigs(configFiles);

            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            _configParser.InheritParentRules(_global);
        }

        private void ParseGlobalElement(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                case "whitelist": ParseWhitelist(ele); break;
                default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
            }
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
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
                        var ass = ParseWhiteListAssembly(childEle);
                        _whiteListAssemblies.Add(ass);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                }
            }
        }

        private WhiteListAssembly ParseWhiteListAssembly(XmlElement element)
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

        private ObfuscationRule ComputeMethodObfuscationRule(MethodDef method)
        {
            return _configParser.GetMethodRule(method, s_default);
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

        public override bool NeedObfuscateCallInMethod(MethodDef method)
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

        public override bool NeedObfuscateCalledMethod(MethodDef callerMethod, IMethod calledMethod, bool callVir, bool currentInLoop)
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
