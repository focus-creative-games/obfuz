using dnlib.DotNet;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Obfuz.ObfusPasses.SymbolObfus
{

    public class ObfuscateRuleConfig : RenamePolicyBase
    {
        enum ObfuscationType
        {
            Name = 1,
            Namespace = 2,
            NestType = 3,
            Method = 4,
            Field = 5,
            Property = 6,
            Event = 7,
            Param = 8,
            MethodBody = 9,
            Getter = 10,
            Setter = 11,
            Add = 12,
            Remove = 13,
            Fire = 14,
        }

        enum RuleType
        {
            Assembly = 1,
            Type = 2,
            Method = 3,
            Field = 4,
            Property = 5,
            Event = 6,
        }

        enum ModifierType
        {
            Private = 0,
            Protected = 1,
            Public = 2,
        }

        interface IRule
        {

        }

        class MethodRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType modifierType;
            public MethodRule rule;
        }

        class MethodRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
            public bool obfuscateParam;
            public bool obfuscateBody;
        }

        class FieldRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType modifierType;
            public FieldRule rule;
        }

        class FieldRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
        }

        class PropertyRuleSpec
        {
            public NameMatcher nameMatcher;
            public PropertyRule rule;
        }

        class PropertyRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
            public MethodRuleSpec getter;
            public MethodRuleSpec setter;
        }

        class EventRuleSpec
        {
            public NameMatcher nameMatcher;
            public EventRule rule;
        }

        class EventRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
            public MethodRuleSpec add;
            public MethodRuleSpec remove;
            public MethodRuleSpec fire;
        }

        class TypeRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType modifierType;
            public TypeRule rule;
        }

        class TypeRule : IRule
        {
            public string ruleName;

            public bool obfuscateName;
            public bool obfuscateNamespace;

            //public List<TypeRuleSpec> nestTypeRuleSpecs;
            public List<FieldRuleSpec> fieldRuleSpecs;
            public List<MethodRuleSpec> methodRuleSpecs;
            public List<PropertyRuleSpec> propertyRuleSpecs;
            public List<EventRuleSpec> eventRuleSpecs;
        }

        class AssemblyRule : IRule
        {
            public string ruleName;

            public bool obfuscateName;

            public List<TypeRuleSpec> typeRuleSpecs;
        }

        class AssemblyRuleSpec
        {
            public string assemblyName;
            public AssemblyRule rule;
        }

        //class DefaultRule : IRule
        //{
            
        //}

        //private readonly static IRule _defaultRule = new DefaultRule();
        //private readonly static IRule _noneRule = new DefaultRule();

        private readonly Dictionary<(string, RuleType), IRule> _rules = new Dictionary<(string, RuleType), IRule>();

        private readonly Dictionary<(string, RuleType), XmlElement> _rawRuleElements = new Dictionary<(string, RuleType), XmlElement>();

        private readonly Dictionary<string, AssemblyRuleSpec> _assemblyRuleSpecs = new Dictionary<string, AssemblyRuleSpec>();


        private static readonly MethodRule s_noneMethodRule = new MethodRule
        {
            ruleName = "none",
            obfuscateName = false,
            obfuscateParam = false,
            obfuscateBody = false,
        };

        private static readonly EventRule s_noneEventRule = new EventRule
        {
            ruleName = "none",
            obfuscateName = false,
            add = new MethodRuleSpec { rule = s_noneMethodRule},
            remove = new MethodRuleSpec { rule = s_noneMethodRule },
            fire = new MethodRuleSpec { rule = s_noneMethodRule },
        };

        private static readonly PropertyRule s_nonePropertyRule = new PropertyRule
        {
            ruleName = "none",
            obfuscateName = false,
            getter = new MethodRuleSpec { rule = s_noneMethodRule },
            setter = new MethodRuleSpec { rule = s_noneMethodRule },
        };

        private static readonly FieldRule s_noneFieldRule = new FieldRule
        {
            ruleName = "none",
            obfuscateName = false,
        };

        private static readonly TypeRule s_noneTypeRule = new TypeRule
        {
            ruleName = "none",
            obfuscateName = false,
            obfuscateNamespace = false,
            //nestTypeRuleSpecs = new List<TypeRuleSpec>(),
            fieldRuleSpecs = new List<FieldRuleSpec> {  new FieldRuleSpec { rule = s_noneFieldRule} },
            methodRuleSpecs = new List<MethodRuleSpec> { new MethodRuleSpec { rule = s_noneMethodRule} },
            propertyRuleSpecs = new List<PropertyRuleSpec> { new PropertyRuleSpec { rule = s_nonePropertyRule} },
            eventRuleSpecs = new List<EventRuleSpec> { new EventRuleSpec { rule = s_noneEventRule} },
        };

        private static readonly AssemblyRule s_noneAssemblyRule = new AssemblyRule
        {
            ruleName = "none",
            obfuscateName = false,
            typeRuleSpecs = new List<TypeRuleSpec> { new TypeRuleSpec { rule = s_noneTypeRule } },
        };

        //static ObfuscateRuleConfig()
        //{
        //    s_noneTypeRule.nestTypeRuleSpecs.Add(new TypeRuleSpec
        //    {
        //        rule = s_noneTypeRule,
        //    });
        //}

        private IRule GetOrParseRule(string ruleName, RuleType ruleType, XmlElement ele)
        {
            IRule rule = null;
            XmlElement element;
            if (!string.IsNullOrEmpty(ruleName))
            {
                if (ruleName == "none")
                {
                    switch (ruleType)
                    {
                        case RuleType.Assembly: return s_noneAssemblyRule;
                        case RuleType.Type: return s_noneTypeRule;
                        case RuleType.Method: return s_noneMethodRule;
                        case RuleType.Field: return s_noneFieldRule;
                        case RuleType.Property: return s_nonePropertyRule;
                        case RuleType.Event: return s_noneEventRule;
                        default: throw new Exception($"Invalid rule type {ruleType}");
                    }
                }
                if (_rules.TryGetValue((ruleName, ruleType), out rule))
                {
                    return rule;
                }
                if (!_rawRuleElements.TryGetValue((ruleName, ruleType), out element))
                {
                    throw new Exception($"Invalid xml file, rule {ruleName} type {ruleType} not found");
                }
            }
            else
            {
                element = ele;
            }
            switch (ruleType)
            {
                case RuleType.Assembly:
                rule = ParseAssemblyRule(ruleName, element);
                break;
                case RuleType.Type:
                rule = ParseTypeRule(ruleName, element);
                break;
                case RuleType.Method:
                rule = ParseMethodRule(ruleName, element);
                break;
                case RuleType.Field:
                rule = ParseFieldRule(ruleName, element);
                break;
                case RuleType.Property:
                rule = ParsePropertyRule(ruleName, element);
                break;
                case RuleType.Event:
                rule = ParseEventRule(ruleName, element);
                break;
                default:
                throw new Exception($"Invalid rule type {ruleType}");
            }
            if (!string.IsNullOrEmpty(ruleName))
            {
                _rules.Add((ruleName, ruleType), rule);
            }
            return rule;
        }

        private static bool ParseBoolNoneOrFalse(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return true;
            }
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "true": throw new Exception($"Invalid bool value '{str}', only accept '0' or 'false' because default is true"); // true is default
                case "0":
                case "false": return false;
                default: throw new Exception($"Invalid bool value {str}");
            }
        }

        private AssemblyRule ParseAssemblyRule(string ruleName, XmlElement element)
        {
            var rule = new AssemblyRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));
            rule.typeRuleSpecs = new List<TypeRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                if (childElement.Name != "type")
                {
                    throw new Exception($"Invalid xml file, unknown node {childElement.Name}");
                }
                var typeRuleSpec = new TypeRuleSpec();
                typeRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                typeRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                typeRuleSpec.rule = (TypeRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Type, childElement);
                rule.typeRuleSpecs.Add(typeRuleSpec);
            }
            return rule;
        }

        private TypeRule ParseTypeRule(string ruleName, XmlElement element)
        {
            var rule = new TypeRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));
            rule.obfuscateNamespace = ParseBoolNoneOrFalse(element.GetAttribute("ob-namespace"));
            //rule.nestTypeRuleSpecs = new List<TypeRuleSpec>();
            rule.fieldRuleSpecs = new List<FieldRuleSpec>();
            rule.methodRuleSpecs = new List<MethodRuleSpec>();
            rule.propertyRuleSpecs = new List<PropertyRuleSpec>();
            rule.eventRuleSpecs = new List<EventRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                switch (childElement.Name)
                {
                    //case "nesttype":
                    //{
                    //    var typeRuleSpec = new TypeRuleSpec();
                    //    typeRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                    //    typeRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                    //    typeRuleSpec.rule = (TypeRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Type, childElement);
                    //    rule.nestTypeRuleSpecs.Add(typeRuleSpec);
                    //    break;
                    //}
                    case "field":
                    {
                        var fieldRuleSpec = new FieldRuleSpec();
                        fieldRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        fieldRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        fieldRuleSpec.rule = (FieldRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Field, childElement);
                        rule.fieldRuleSpecs.Add(fieldRuleSpec);
                        break;
                    }
                    case "method":
                    {
                        rule.methodRuleSpecs.Add(ParseMethodRuleSpec(childElement));
                        break;
                    }
                    case "property":
                    {
                        var propertyRulerSpec = new PropertyRuleSpec();
                        propertyRulerSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        propertyRulerSpec.rule = (PropertyRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Property, childElement);
                        rule.propertyRuleSpecs.Add(propertyRulerSpec);
                        break;
                    }
                    case "event":
                    {
                        var eventRuleSpec = new EventRuleSpec();
                        eventRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        eventRuleSpec.rule = (EventRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Event, childElement);
                        rule.eventRuleSpecs.Add(eventRuleSpec);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in type rule {ruleName}");
                }
            }
            return rule;
        }

        private MethodRuleSpec ParseMethodRuleSpec(XmlElement el)
        {
            var methodRuleSpec = new MethodRuleSpec();
            methodRuleSpec.nameMatcher = new NameMatcher(el.GetAttribute("name"));
            methodRuleSpec.modifierType = ParseModifierType(el.GetAttribute("modifier"));
            methodRuleSpec.rule = (MethodRule)GetOrParseRule(el.GetAttribute("rule"), RuleType.Method, el);
            return methodRuleSpec;
        }

        private MethodRule ParseMethodRule(string ruleName, XmlElement element)
        {
            var rule = new MethodRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));
            rule.obfuscateParam = ParseBoolNoneOrFalse(element.GetAttribute("ob-param"));
            rule.obfuscateBody = ParseBoolNoneOrFalse(element.GetAttribute("ob-body"));
            return rule;
        }

        private FieldRule ParseFieldRule(string ruleName, XmlElement element)
        {
            var rule = new FieldRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));
            return rule;
        }

        private PropertyRule ParsePropertyRule(string ruleName, XmlElement element)
        {
            var rule = new PropertyRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "getter":
                    {
                        if (rule.getter != null)
                        {
                            throw new Exception($"Invalid xml file, duplicate getter rule in property rule {ruleName}");
                        }
                        rule.getter = ParseMethodRuleSpec(childElement);
                        break;
                    }
                    case "setter":
                    {
                        if (rule.setter != null)
                        {
                            throw new Exception($"Invalid xml file, duplicate setter rule in property rule {ruleName}");
                        }
                        rule.setter = ParseMethodRuleSpec(childElement);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in property rule {ruleName}");
                }
            }
            return rule;
        }

        private EventRule ParseEventRule(string ruleName, XmlElement element)
        {
            var rule = new EventRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBoolNoneOrFalse(element.GetAttribute("ob-name"));

            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "add":
                    {
                        if (rule.add != null)
                        {
                            throw new Exception($"Invalid xml file, duplicate getter rule in property rule {ruleName}");
                        }
                        rule.add = ParseMethodRuleSpec(childElement);
                        break;
                    }
                    case "remove":
                    {
                        if (rule.remove != null)
                        {
                            throw new Exception($"Invalid xml file, duplicate setter rule in property rule {ruleName}");
                        }
                        rule.remove = ParseMethodRuleSpec(childElement);
                        break;
                    }
                    case "fire":
                    {
                        if (rule.fire != null)
                        {
                            throw new Exception($"Invalid xml file, duplicate fire rule in property rule {ruleName}");
                        }
                        rule.fire = ParseMethodRuleSpec(childElement);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in event rule {ruleName}");
                }
            }

            return rule;
        }

        public void LoadXmls(List<string> xmlFiles)
        {
            var rawAssemblySpecElements = new List<XmlElement>();
            foreach (string file in xmlFiles)
            {
                LoadRawXml(file, rawAssemblySpecElements);
            }
            ResolveAssemblySpecs(rawAssemblySpecElements);
        }

        private void ResolveAssemblySpecs(List<XmlElement> rawAssemblySpecElements)
        {
            foreach (XmlElement ele in rawAssemblySpecElements)
            {
                string assemblyName = ele.GetAttribute("name");
                if (string.IsNullOrEmpty(assemblyName))
                {
                    throw new Exception($"Invalid xml file, assembly name is empty");
                }
                if (!_obfuscationAssemblyNames.Contains(assemblyName))
                {
                    throw new Exception($"unknown assembly name:{assemblyName}, not in ObfuzSettings.obfuscationAssemblyNames");
                }
                if (_assemblyRuleSpecs.ContainsKey(assemblyName))
                {
                    throw new Exception($"Invalid xml file, duplicate assembly name {assemblyName}");
                }
                var assemblyRule = new AssemblyRuleSpec()
                {
                    assemblyName = assemblyName,
                    rule = (AssemblyRule)GetOrParseRule(ele.GetAttribute("rule"), RuleType.Assembly, ele),
                };
                _assemblyRuleSpecs.Add(assemblyName, assemblyRule);
            }
        }


        private RuleType ParseRuleType(string ruleType)
        {
            switch (ruleType)
            {
                case "assembly": return RuleType.Assembly;
                case "type": return RuleType.Type;
                case "method": return RuleType.Method;
                case "field": return RuleType.Field;
                case "property": return RuleType.Property;
                case "event": return RuleType.Event;
                default: throw new Exception($"Invalid rule type {ruleType}");
            }
        }

        private ModifierType ParseModifierType(string modifierType)
        {
            if (string.IsNullOrEmpty(modifierType))
            {
                return ModifierType.Private;
            }
            switch (modifierType)
            {
                case "public": return ModifierType.Public;
                case "protected": return ModifierType.Protected;
                case "private": return ModifierType.Private;
                default: throw new Exception($"Invalid modifier type {modifierType}");
            }
        }

        private void LoadRawXml(string xmlFile, List<XmlElement> rawAssemblyElements)
        {
            Debug.Log($"ObfuscateRule::LoadXml {xmlFile}");
            var doc = new XmlDocument();
            //var nsManager = new XmlNamespaceManager(doc.NameTable);
            //nsManager.AddNamespace("ob", "https://github.com/focus-creative-games/Obfuz"); // 绑定前缀到 URI
            doc.Load(xmlFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {xmlFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }
                switch (element.Name)
                {
                    case "rule":
                    {
                        string ruleName = element.GetAttribute("name");
                        RuleType ruleType = ParseRuleType(element.GetAttribute("type"));
                        var key = (ruleName, ruleType);
                        if (_rawRuleElements.ContainsKey(key))
                        {
                            throw new Exception($"Invalid xml file {xmlFile}, duplicate rule name:{ruleName} type:{ruleType}");
                        }
                        _rawRuleElements.Add(key, element);
                        break;
                    }
                    case "assembly":
                    {
                        rawAssemblyElements.Add(element);
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Invalid xml file {xmlFile}, unknown node {element.Name}");
                    }
                }
            }
        }

        public override bool NeedRename(ModuleDef mod)
        {
            //string name = mod.Assembly.Name;
            //if (!_assemblyRuleSpecs.TryGetValue(name, out var assemblyRuleSpec))
            //{
            //    return false;
            //}
            //return assemblyRuleSpec.rule.obfuscateName;

            // TODO support rename assembly
            return false;
        }

        private ModifierType ComputeModifierType(TypeAttributes visibility)
        {
            if (visibility == TypeAttributes.NotPublic || visibility == TypeAttributes.NestedPrivate)
            {
                return ModifierType.Private;
            }
            if (visibility == TypeAttributes.Public || visibility == TypeAttributes.NestedPublic)
            {
                return ModifierType.Public;
            }
            return ModifierType.Protected;
        }

        private ModifierType ComputeModifierType(FieldAttributes access)
        {
            if (access == FieldAttributes.Private || access == FieldAttributes.PrivateScope)
            {
                return ModifierType.Private;
            }
            if (access == FieldAttributes.Public)
            {
                return ModifierType.Public;
            }
            return ModifierType.Protected;
        }

        //private ModifierType ComputeModifierType(MethodAttributes access)
        //{
        //    if (access == MethodAttributes.Private || access == MethodAttributes.PrivateScope)
        //    {
        //        return ModifierType.Private;
        //    }
        //    if (access == MethodAttributes.Public)
        //    {
        //        return ModifierType.Public;
        //    }
        //    return ModifierType.Protected;
        //}

        private bool MatchModifier(ModifierType modifierType, TypeDef typeDef)
        {
            return modifierType <= ComputeModifierType(typeDef.Visibility);
        }

        private bool MatchModifier(ModifierType modifierType, FieldDef fieldDef)
        {
            return modifierType <= ComputeModifierType(fieldDef.Access);
        }

        private bool MatchModifier(ModifierType modifierType, MethodDef methodDef)
        {
            return modifierType <= ComputeModifierType((FieldAttributes)methodDef.Access);
        }

        //private bool MatchModifier(ModifierType modifierType, PropertyDef propertyDef)
        //{
        //    return modifierType <= ComputeModifierType((FieldAttributes)propertyDef.Attributes);
        //}

        private class MethodComputeCache
        {
            public bool obfuscateName = true;
            public bool obfuscateParam = true;
            public bool obfuscateBody = true;
        }

        private class TypeDefComputeCache
        {
            public bool obfuscateName = true;
            public bool obfuscateNamespace = true;

            public readonly Dictionary<MethodDef, MethodComputeCache> methods = new Dictionary<MethodDef, MethodComputeCache>();

            public readonly HashSet<FieldDef> notObfuscatedFields = new HashSet<FieldDef>();

            public readonly HashSet<PropertyDef> notObfuscatedProperties = new HashSet<PropertyDef>();

            public readonly HashSet<EventDef> notObfuscatedEvents = new HashSet<EventDef>();
        }

        private readonly Dictionary<TypeDef, TypeDefComputeCache> _typeRenameCache = new Dictionary<TypeDef, TypeDefComputeCache>();

        private readonly HashSet<string> _obfuscationAssemblyNames;

        public ObfuscateRuleConfig(List<string> obfuscationAssemblyNames)
        {
            this._obfuscationAssemblyNames = obfuscationAssemblyNames.ToHashSet();
        }

        private TypeDefComputeCache GetOrCreateTypeDefRenameComputeCache(TypeDef typeDef)
        {
            if (_typeRenameCache.TryGetValue(typeDef, out var cache))
            {
                return cache;
            }
            cache = new TypeDefComputeCache();
            _typeRenameCache.Add(typeDef, cache);

            if (!_assemblyRuleSpecs.TryGetValue(typeDef.Module.Assembly.Name, out var assemblyRuleSpec) || assemblyRuleSpec.rule == null)
            {
                return cache;
            }
            string typeName = typeDef.FullName;
            var totalMethodSpecs = new List<MethodRuleSpec>();
            var totalMethodSpecFromPropertyAndEvents = new List<(MethodDef, MethodRuleSpec)>();
            foreach (var typeRule in assemblyRuleSpec.rule.typeRuleSpecs)
            {
                if ((typeRule.nameMatcher != null && !typeRule.nameMatcher.IsMatch(typeName)) || !MatchModifier(typeRule.modifierType, typeDef))
                {
                    continue;
                }
                cache.obfuscateNamespace &= typeRule.rule.obfuscateNamespace;
                cache.obfuscateName &= typeRule.rule.obfuscateName;

                totalMethodSpecs.AddRange(typeRule.rule.methodRuleSpecs);

                foreach (var eventDef in typeDef.Events)
                {
                    foreach (var eventSpec in typeRule.rule.eventRuleSpecs)
                    {
                        if (eventSpec.nameMatcher != null && !eventSpec.nameMatcher.IsMatch(eventDef.Name))
                        {
                            continue;
                        }
                        EventRule eventRule = eventSpec.rule;
                        if (!eventRule.obfuscateName)
                        {
                            cache.notObfuscatedEvents.Add(eventDef);
                        }
                        if (eventDef.AddMethod != null && eventRule.add != null && MatchModifier(eventRule.add.modifierType, eventDef.AddMethod))
                        {
                            totalMethodSpecFromPropertyAndEvents.Add((eventDef.AddMethod,eventRule.add));
                        }
                        if (eventDef.RemoveMethod != null && eventRule.remove != null && MatchModifier(eventRule.remove.modifierType, eventDef.RemoveMethod))
                        {
                            totalMethodSpecFromPropertyAndEvents.Add((eventDef.RemoveMethod, eventRule.remove));
                        }
                        if (eventDef.InvokeMethod != null && eventRule.fire != null && MatchModifier(eventRule.fire.modifierType, eventDef.InvokeMethod))
                        {
                            totalMethodSpecFromPropertyAndEvents.Add((eventDef.InvokeMethod, eventRule.fire));
                        }
                    }
                }
                foreach (var propertyDef in typeDef.Properties)
                {
                    foreach (var propertySpec in typeRule.rule.propertyRuleSpecs)
                    {
                        if (propertySpec.nameMatcher != null && !propertySpec.nameMatcher.IsMatch(propertyDef.Name))
                        {
                            continue;
                        }
                        PropertyRule propertyRule = propertySpec.rule;
                        if (!propertyRule.obfuscateName)
                        {
                            cache.notObfuscatedProperties.Add(propertyDef);
                        }
                        if (propertyDef.GetMethod != null && propertyRule.getter != null && MatchModifier(propertyRule.getter.modifierType, propertyDef.GetMethod))
                        {
                            totalMethodSpecFromPropertyAndEvents.Add((propertyDef.GetMethod, propertyRule.getter));
                        }
                        if (propertyDef.SetMethod != null && propertyRule.setter != null && MatchModifier(propertyRule.setter.modifierType, propertyDef.SetMethod))
                        {
                            totalMethodSpecFromPropertyAndEvents.Add((propertyDef.SetMethod, propertyRule.setter));
                        }
                    }
                }
                
                foreach (var fieldDef in typeDef.Fields)
                {
                    foreach (var fieldRule in typeRule.rule.fieldRuleSpecs)
                    {
                        if ((fieldRule.nameMatcher == null || fieldRule.nameMatcher.IsMatch(fieldDef.Name)) && MatchModifier(fieldRule.modifierType, fieldDef) && !fieldRule.rule.obfuscateName)
                        {
                            cache.notObfuscatedFields.Add(fieldDef);
                        }
                    }
                }
                foreach (MethodDef methodDef in typeDef.Methods)
                {
                    foreach (var e in totalMethodSpecFromPropertyAndEvents)
                    {
                        if (e.Item1 != methodDef)
                        {
                            continue;
                        }
                        if (!cache.methods.TryGetValue(methodDef, out var methodCache))
                        {
                            methodCache = new MethodComputeCache();
                            cache.methods.Add(methodDef, methodCache);
                        }
                        MethodRule methodRule = e.Item2.rule;
                        methodCache.obfuscateName &= methodRule.obfuscateName;
                        methodCache.obfuscateParam &= methodRule.obfuscateParam;
                        methodCache.obfuscateBody &= methodRule.obfuscateBody;
                    }
                    foreach (MethodRuleSpec methodSpec in totalMethodSpecs)
                    {
                        if ((methodSpec.nameMatcher != null && !methodSpec.nameMatcher.IsMatch(methodDef.Name)) || !MatchModifier(methodSpec.modifierType, methodDef))
                        {
                            continue;
                        }
                        if (!cache.methods.TryGetValue(methodDef, out var methodCache))
                        {
                            methodCache = new MethodComputeCache();
                            cache.methods.Add(methodDef, methodCache);
                        }
                        MethodRule methodRule = methodSpec.rule;
                        methodCache.obfuscateName &= methodRule.obfuscateName;
                        methodCache.obfuscateParam &= methodRule.obfuscateParam;
                        methodCache.obfuscateBody &= methodRule.obfuscateBody;
                    }
                }
            }
            return cache;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            return cache.obfuscateName;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            TypeDef typeDef = methodDef.DeclaringType;
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            if (!cache.methods.TryGetValue(methodDef, out var methodCache))
            {
                return true;
            }
            return methodCache.obfuscateName;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            return !cache.notObfuscatedFields.Contains(fieldDef);
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            TypeDef typeDef = propertyDef.DeclaringType;
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            return !cache.notObfuscatedProperties.Contains(propertyDef);
        }

        public override bool NeedRename(EventDef eventDef)
        {
            TypeDef typeDef = eventDef.DeclaringType;
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            return !cache.notObfuscatedEvents.Contains(eventDef);
        }

        public override bool NeedRename(ParamDef paramDef)
        {
            MethodDef methodDef = paramDef.DeclaringMethod;
            TypeDef typeDef = methodDef.DeclaringType;
            TypeDefComputeCache cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            if (!cache.methods.TryGetValue(methodDef, out var methodCache))
            {
                return true;
            }
            return methodCache.obfuscateParam;
        }
    }
}
