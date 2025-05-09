using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Obfuz.ObfusPasses.ConstObfus.Policies
{
    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        private readonly List<string> _toObfuscatedAssemblyNames;

        class NumberRange<T> where T : struct
        {
            public readonly T? min;
            public readonly T? max;

            public NumberRange(T? min, T? max)
            {
                this.min = min;
                this.max = max;
            }
        }

        class ObfuscationRule
        {
            public bool? disableEncrypt;
            public bool? encryptInt;
            public bool? encryptLong;
            public bool? encryptFloat;
            public bool? encryptDouble;
            public bool? encryptArray;
            public bool? encryptString;

            public bool? encryptConstInLoop;
            public bool? encryptStringInLoop;

            public bool? cacheConstInLoop;
            public bool? cacheConstNotInLoop;
            public bool? cacheStringInLoop;
            public bool? cacheStringNotInLoop;

            public HashSet<int> notEncryptInts = new HashSet<int>();
            public HashSet<long> notEncryptLongs = new HashSet<long>();
            public HashSet<string> notEncryptStrings = new HashSet<string>();
            public List<NumberRange<int>> notEncryptIntRanges = new List<NumberRange<int>>();
            public List<NumberRange<long>> notEncryptLongRanges = new List<NumberRange<long>>();
            public List<NumberRange<float>> notEncryptFloatRanges = new List<NumberRange<float>>();
            public List<NumberRange<double>> notEncryptDoubleRanges = new List<NumberRange<double>>();
            public List<NumberRange<int>> notEncryptArrayLengthRanges = new List<NumberRange<int>>();
            public List<NumberRange<int>> notEncryptStringLengthRanges = new List<NumberRange<int>>();

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (disableEncrypt == null)
                    disableEncrypt = parentRule.disableEncrypt;
                if (encryptInt == null)
                    encryptInt = parentRule.encryptInt;
                if (encryptLong == null)
                    encryptLong = parentRule.encryptLong;
                if (encryptFloat == null)
                    encryptFloat = parentRule.encryptFloat;
                if (encryptDouble == null)
                    encryptDouble = parentRule.encryptDouble;
                if (encryptArray == null)
                    encryptArray = parentRule.encryptArray;
                if (encryptString == null)
                    encryptString = parentRule.encryptString;

                if (encryptConstInLoop == null)
                    encryptConstInLoop = parentRule.encryptConstInLoop;
                if (encryptStringInLoop == null)
                    encryptStringInLoop = parentRule.encryptStringInLoop;

                if (cacheConstInLoop == null)
                    cacheConstInLoop = parentRule.cacheConstInLoop;
                if (cacheConstNotInLoop == null)
                    cacheConstNotInLoop = parentRule.cacheConstNotInLoop;
                if (cacheStringInLoop == null)
                    cacheStringInLoop = parentRule.cacheStringInLoop;
                if (cacheStringNotInLoop == null)
                    cacheStringNotInLoop = parentRule.cacheStringNotInLoop;

                notEncryptInts.AddRange(parentRule.notEncryptInts);
                notEncryptLongs.AddRange(parentRule.notEncryptLongs);
                notEncryptStrings.AddRange(parentRule.notEncryptStrings);
                notEncryptIntRanges.AddRange(parentRule.notEncryptIntRanges);
                notEncryptLongRanges.AddRange(parentRule.notEncryptLongRanges);
                notEncryptFloatRanges.AddRange(parentRule.notEncryptFloatRanges);
                notEncryptDoubleRanges.AddRange(parentRule.notEncryptDoubleRanges);
                notEncryptArrayLengthRanges.AddRange(parentRule.notEncryptArrayLengthRanges);
                notEncryptStringLengthRanges.AddRange(parentRule.notEncryptStringLengthRanges);
            }
        }

        class MethodSpec
        {
            public string name;
            public NameMatcher nameMatcher;
            public ObfuscationRule rule;
        }

        class TypeSpec
        {
            public string name;
            public NameMatcher nameMatcher;
            public ObfuscationRule rule;
            public List<MethodSpec> methodSpecs = new List<MethodSpec>();
        }

        class AssemblySpec
        {
            public string name;
            public ObfuscationRule rule;
            public List<TypeSpec> typeSpecs = new List<TypeSpec>();
        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            disableEncrypt = false,
            encryptInt = true,
            encryptLong = true,
            encryptFloat = true,
            encryptDouble = true,
            encryptArray = true,
            encryptString = true,
            encryptConstInLoop = true,
            encryptStringInLoop = true,
            cacheConstInLoop = true,
            cacheConstNotInLoop = false,
            cacheStringInLoop = true,
            cacheStringNotInLoop = true,
        };

        private ObfuscationRule _global;
        private readonly Dictionary<string, AssemblySpec> _assemblySpecs = new Dictionary<string, AssemblySpec>();
        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, string xmlConfigFile)
        {
            _toObfuscatedAssemblyNames = toObfuscatedAssemblyNames;
            LoadConfig(xmlConfigFile);
            InheritParentRules();
        }

        private void LoadConfig(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
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
                    case "assembly":
                    {
                        AssemblySpec assSpec = ParseAssembly(ele);
                        string name = assSpec.name;
                        if (!_toObfuscatedAssemblyNames.Contains(name))
                        {
                            throw new Exception($"Invalid xml file {configFile}, assembly name {name} is in toObfuscatedAssemblyNames");
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
                foreach (TypeSpec typeSpec in assSpec.typeSpecs)
                {
                    typeSpec.rule.InheritParent(assSpec.rule);
                    foreach (MethodSpec methodSpec in typeSpec.methodSpecs)
                    {
                        methodSpec.rule.InheritParent(typeSpec.rule);
                    }
                }
            }
        }

        private bool ParseBool(string str)
        {
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "true": return true;
                case "0":
                case "false": return false;
                default: throw new Exception($"Invalid bool value {str}");
            }
        }

        private int? ParseNullableInt(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return int.Parse(str);
        }

        private long? ParseNullableLong(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return long.Parse(str);
        }

        private float? ParseNullableFloat(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return float.Parse(str);
        }

        private double? ParseNullableDouble(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return double.Parse(str);
        }

        private ObfuscationRule ParseObfuscationRule(XmlElement ele, bool parseWhitelist)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("disableEncrypt"))
            {
                rule.disableEncrypt = ParseBool(ele.GetAttribute("disableEncrypt"));
            }
            if (ele.HasAttribute("encryptInt"))
            {
                rule.encryptInt = ParseBool(ele.GetAttribute("encryptInt"));
            }
            if (ele.HasAttribute("encryptLong"))
            {
                rule.encryptLong = ParseBool(ele.GetAttribute("encryptLong"));
            }
            if (ele.HasAttribute("encryptFloat"))
            {
                rule.encryptFloat = ParseBool(ele.GetAttribute("encryptFloat"));
            }
            if (ele.HasAttribute("encryptDouble"))
            {
                rule.encryptDouble = ParseBool(ele.GetAttribute("encryptDouble"));
            }
            if (ele.HasAttribute("encryptBytes"))
            {
                rule.encryptArray = ParseBool(ele.GetAttribute("encryptArray"));
            }
            if (ele.HasAttribute("encryptString"))
            {
                rule.encryptString = ParseBool(ele.GetAttribute("encryptString"));
            }

            if (ele.HasAttribute("encryptConstInLoop"))
            {
                rule.encryptConstInLoop = ParseBool(ele.GetAttribute("encryptConstInLoop"));
            }
            if (ele.HasAttribute("encryptStringInLoop"))
            {
                rule.encryptStringInLoop = ParseBool(ele.GetAttribute("encryptStringInLoop"));
            }
            if (ele.HasAttribute("cacheConstInLoop"))
            {
                rule.cacheConstInLoop = ParseBool(ele.GetAttribute("cacheConstInLoop"));
            }
            if (ele.HasAttribute("cacheConstNotInLoop"))
            {
                rule.cacheConstNotInLoop = ParseBool(ele.GetAttribute("cacheConstNotInLoop"));
            }
            if (ele.HasAttribute("cacheStringInLoop"))
            {
                rule.cacheStringInLoop = ParseBool(ele.GetAttribute("cacheStringInLoop"));
            }
            if (ele.HasAttribute("cacheStringNotInLoop"))
            {
                rule.cacheStringNotInLoop = ParseBool(ele.GetAttribute("cacheStringNotInLoop"));
            }
            if (parseWhitelist)
            {
                ParseWhitelist(ele, rule);
            }
            return rule;
        }
        
        private void ParseWhitelist(XmlElement ruleEle, ObfuscationRule rule)
        {
            foreach (XmlNode xmlNode in ruleEle.ChildNodes)
            {
                if (!(xmlNode is XmlElement childEle))
                {
                    continue;
                }
                switch (childEle.Name)
                {
                    case "whitelist":
                    {
                        string type = childEle.GetAttribute("type");
                        if (string.IsNullOrEmpty(type))
                        {
                            throw new Exception($"Invalid xml file, whitelist type is empty");
                        }
                        string value = childEle.InnerText;
                        switch (type)
                        {
                            case "int":
                            {
                                rule.notEncryptInts.AddRange(value.Split(",").Select(s => int.Parse(s.Trim())));
                                break;
                            }
                            case "long":
                            {
                                rule.notEncryptLongs.AddRange(value.Split(",").Select(s => long.Parse(s.Trim())));
                                break;
                            }
                            case "string":
                            {
                                rule.notEncryptStrings.AddRange(value.Split(",").Select(s => s.Trim()));
                                break;
                            }
                            case "int-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, int-range {value} is invalid");
                                }
                                rule.notEncryptIntRanges.Add(new NumberRange<int>(ParseNullableInt(parts[0]), ParseNullableInt(parts[1])));
                                break;
                            }
                            case "long-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, long-range {value} is invalid");
                                }
                                rule.notEncryptLongRanges.Add(new NumberRange<long>(ParseNullableLong(parts[0]), ParseNullableLong(parts[1])));
                                break;
                            }
                            case "float-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, float-range {value} is invalid");
                                }
                                rule.notEncryptFloatRanges.Add(new NumberRange<float>(ParseNullableFloat(parts[0]), ParseNullableFloat(parts[1])));
                                break;
                            }
                            case "double-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, double-range {value} is invalid");
                                }
                                rule.notEncryptDoubleRanges.Add(new NumberRange<double>(ParseNullableDouble(parts[0]), ParseNullableDouble(parts[1])));
                                break;
                            }
                            case "string-length-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, string-length-range {value} is invalid");
                                }
                                rule.notEncryptStringLengthRanges.Add(new NumberRange<int>(ParseNullableInt(parts[0]), ParseNullableInt(parts[1])));
                                break;
                            }
                            case "array-length-range":
                            {
                                var parts = value.Split(",");
                                if (parts.Length != 2)
                                {
                                    throw new Exception($"Invalid xml file, array-length-range {value} is invalid");
                                }
                                rule.notEncryptArrayLengthRanges.Add(new NumberRange<int>(ParseNullableInt(parts[0]), ParseNullableInt(parts[1])));
                                break;
                            }
                            default: throw new Exception($"Invalid xml file, unknown whitelist type {type} in {childEle.Name} node");
                        }
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childEle.Name}");
                }
            }
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
                    assemblySpec.typeSpecs.Add(ParseType(ele));
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
                    typeSpec.methodSpecs.Add(ParseMethod(ele));
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
            foreach (var typeSpec in assSpec.typeSpecs)
            {
                if (typeSpec.nameMatcher.IsMatch(declaringTypeName))
                {
                    foreach (var methodSpec in typeSpec.methodSpecs)
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

        public override bool NeedObfuscateMethod(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.disableEncrypt != true;
        }

        public override ConstCachePolicy GetMethodConstCachePolicy(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return new ConstCachePolicy
            {
                cacheConstInLoop = rule.cacheConstInLoop.Value,
                cacheConstNotInLoop = rule.cacheConstNotInLoop.Value,
                cacheStringInLoop = rule.cacheStringInLoop.Value,
                cacheStringNotInLoop = rule.cacheStringNotInLoop.Value,
            };
        }

        public override bool NeedObfuscateInt(MethodDef method, bool currentInLoop, int value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptInt == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (rule.notEncryptInts.Contains(value))
            {
                return false;
            }
            foreach (var range in rule.notEncryptIntRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateLong(MethodDef method, bool currentInLoop, long value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptLong == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (rule.notEncryptLongs.Contains(value))
            {
                return false;
            }
            foreach (var range in rule.notEncryptLongRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateFloat(MethodDef method, bool currentInLoop, float value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptFloat == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in rule.notEncryptFloatRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateDouble(MethodDef method, bool currentInLoop, double value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptDouble == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in rule.notEncryptDoubleRanges)
            {
                if (range.min != null && value < range.min)
                {
                    continue;
                }
                if (range.max != null && value > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateString(MethodDef method, bool currentInLoop, string value)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptString == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            if (rule.notEncryptStrings.Contains(value))
            {
                return false;
            }
            foreach (var range in rule.notEncryptStringLengthRanges)
            {
                if (range.min != null && value.Length < range.min)
                {
                    continue;
                }
                if (range.max != null && value.Length > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        public override bool NeedObfuscateArray(MethodDef method, bool currentInLoop, byte[] array)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            if (rule.encryptArray == false)
            {
                return false;
            }
            if (currentInLoop && rule.encryptConstInLoop == false)
            {
                return false;
            }
            foreach (var range in rule.notEncryptArrayLengthRanges)
            {
                if (range.min != null && array.Length < range.min)
                {
                    continue;
                }
                if (range.max != null && array.Length > range.max)
                {
                    continue;
                }
                return false;
            }
            return true;
        }
    }
}
