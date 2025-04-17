using dnlib.DotNet;
using Obfuz.Rename;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace Obfuz
{
    public class ObfuscateRuleConfig : IRenamePolicy
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
            Public = 0x1,
            NotPublic = 0x2,
            All = 0x3,
        }

        interface IRule
        {

        }

        class MethodRuleSpec
        {
            public string namePattern;
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
            public string namePattern;
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
            public string namePattern;
            public ModifierType modifierType;
            public PropertyRule rule;
        }

        class PropertyRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
            public bool obfuscateGetter;
            public bool obfuscateSetter;
        }

        class EventRuleSpec
        {
            public string namePattern;
            public ModifierType modifierType;
            public EventRule rule;
        }

        class EventRule : IRule
        {
            public string ruleName;
            public bool obfuscateName;
            public bool obfuscateAdd;
            public bool obfuscateRemove;
            public bool obfuscateFire;
        }

        class TypeRuleSpec
        {
            public string namePattern;
            public ModifierType modifierType;
            public TypeRule rule;
        }

        class TypeRule : IRule
        {
            public string ruleName;

            public bool obfuscateName;
            public bool obfuscateNamespace;

            public List<TypeRuleSpec> nestTypeRuleSpecs;
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

        public List<string> ObfuscatedAssemblyNames => _assemblyRuleSpecs.Keys.ToList();

        private static readonly EventRule s_defaultEventRule = new EventRule
        {
            ruleName = "default",
            obfuscateName = true,
            obfuscateAdd = true,
            obfuscateRemove = true,
            obfuscateFire = true,
        };

        private static readonly PropertyRule s_defaultPropertyRule = new PropertyRule
        {
            ruleName = "default",
            obfuscateName = true,
            obfuscateGetter = true,
            obfuscateSetter = true,
        };

        private static readonly MethodRule s_defaultMethodRule = new MethodRule
        {
            ruleName = "default",
            obfuscateName = true,
            obfuscateParam = true,
            obfuscateBody = true,
        };

        private static readonly FieldRule s_defaultFieldRule = new FieldRule
        {
            ruleName = "default",
            obfuscateName = true,
        };

        private static readonly TypeRule s_defaultTypeRule = new TypeRule
        {
            ruleName = "default",
            obfuscateName = true,
            obfuscateNamespace = true,
            nestTypeRuleSpecs = new List<TypeRuleSpec>(),
            fieldRuleSpecs = new List<FieldRuleSpec>() { new FieldRuleSpec { rule = s_defaultFieldRule} },
            methodRuleSpecs = new List<MethodRuleSpec>() { new MethodRuleSpec { rule = s_defaultMethodRule} },
            propertyRuleSpecs = new List<PropertyRuleSpec>() {  new PropertyRuleSpec {  rule = s_defaultPropertyRule} },
            eventRuleSpecs = new List<EventRuleSpec>() {  new EventRuleSpec { rule = s_defaultEventRule} },
        };

        private static readonly AssemblyRule s_defaultAssemblyRule = new AssemblyRule
        {
            ruleName = "default",
            obfuscateName = false,
            typeRuleSpecs = new List<TypeRuleSpec>() { new TypeRuleSpec {  rule = s_defaultTypeRule} },
        };


        private static readonly EventRule s_noneEventRule = new EventRule
        {
            ruleName = "none",
            obfuscateName = false,
            obfuscateAdd = false,
            obfuscateRemove = false,
            obfuscateFire = false,
        };

        private static readonly PropertyRule s_nonePropertyRule = new PropertyRule
        {
            ruleName = "none",
            obfuscateName = false,
            obfuscateGetter = false,
            obfuscateSetter = false,
        };

        private static readonly MethodRule s_noneMethodRule = new MethodRule
        {
            ruleName = "none",
            obfuscateName = false,
            obfuscateParam = false,
            obfuscateBody = false,
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
            nestTypeRuleSpecs = new List<TypeRuleSpec>(),
            fieldRuleSpecs = new List<FieldRuleSpec> {  new FieldRuleSpec { rule = s_noneFieldRule} },
            methodRuleSpecs = new List<MethodRuleSpec> { new MethodRuleSpec { rule = s_noneMethodRule} },
            propertyRuleSpecs = new List<PropertyRuleSpec> { new PropertyRuleSpec { rule = s_nonePropertyRule} },
            eventRuleSpecs = new List<EventRuleSpec> { new EventRuleSpec { rule = s_noneEventRule} },
        };

        private static readonly AssemblyRule s_noneAssemblyRule = new AssemblyRule
        {
            ruleName = "none",
            obfuscateName = false,
            typeRuleSpecs = null,
        };

        static ObfuscateRuleConfig()
        {
            s_defaultTypeRule.nestTypeRuleSpecs.Add(new TypeRuleSpec
            {
                rule = s_defaultTypeRule,
            });
            s_noneTypeRule.nestTypeRuleSpecs.Add(new TypeRuleSpec
            {
                rule = s_noneTypeRule,
            });
        }

        private IRule GetOrParseRule(string ruleName, RuleType ruleType, XmlElement ele)
        {
            IRule rule = null;
            XmlElement element;
            if (!string.IsNullOrEmpty(ruleName))
            {
                if (ruleName == "default")
                {
                    switch (ruleType)
                    {
                        case RuleType.Assembly: return s_defaultAssemblyRule;
                        case RuleType.Type: return s_defaultTypeRule;
                        case RuleType.Method: return s_defaultMethodRule;
                        case RuleType.Field: return s_defaultFieldRule;
                        case RuleType.Property: return s_defaultPropertyRule;
                        case RuleType.Event: return s_defaultEventRule;
                        default: throw new Exception($"Invalid rule type {ruleType}");
                    }
                }
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

        private static bool ParseBool(string str, bool defaultValue)
        {
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "true": return true;
                case "0":
                case "false": return false;
                default: throw new Exception($"Invalid bool value {str}");
            }
        }

        private AssemblyRule ParseAssemblyRule(string ruleName, XmlElement element)
        {
            var rule = new AssemblyRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), false);
            rule.typeRuleSpecs = new List<TypeRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is not XmlElement childElement)
                {
                    continue;
                }
                if (childElement.Name != "type")
                {
                    throw new Exception($"Invalid xml file, unknown node {childElement.Name}");
                }
                var typeRuleSpec = new TypeRuleSpec();
                typeRuleSpec.namePattern = childElement.GetAttribute("name");
                typeRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                typeRuleSpec.rule = (TypeRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Type, childElement);
                rule.typeRuleSpecs.Add(typeRuleSpec);
            }
            return rule;
        }

        private TypeRule ParseTypeRule(string ruleName, XmlElement element)
        {
            var rule = new TypeRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), true);
            rule.obfuscateNamespace = ParseBool(element.GetAttribute("ob-namespace"), true);
            rule.nestTypeRuleSpecs = new List<TypeRuleSpec>();
            rule.fieldRuleSpecs = new List<FieldRuleSpec>();
            rule.methodRuleSpecs = new List<MethodRuleSpec>();
            rule.propertyRuleSpecs = new List<PropertyRuleSpec>();
            rule.eventRuleSpecs = new List<EventRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node is not XmlElement childElement)
                {
                    continue;
                }
                switch (childElement.Name)
                {
                    case "nesttype":
                    {
                        var typeRuleSpec = new TypeRuleSpec();
                        typeRuleSpec.namePattern = childElement.GetAttribute("namePattern");
                        typeRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                        typeRuleSpec.rule = (TypeRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Type, childElement);
                        rule.nestTypeRuleSpecs.Add(typeRuleSpec);
                        break;
                    }
                    case "field":
                    {
                        var fieldRuleSpec = new FieldRuleSpec();
                        fieldRuleSpec.namePattern = childElement.GetAttribute("namePattern");
                        fieldRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                        fieldRuleSpec.rule = (FieldRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Field, childElement);
                        rule.fieldRuleSpecs.Add(fieldRuleSpec);
                        break;
                    }
                    case "method":
                    {
                        var methodRuleSpec = new MethodRuleSpec();
                        methodRuleSpec.namePattern = childElement.GetAttribute("namePattern");
                        methodRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                        methodRuleSpec.rule = (MethodRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Method, childElement);
                        rule.methodRuleSpecs.Add(methodRuleSpec);
                        break;
                    }
                    case "property":
                    {
                        var propertyRulerRef = new PropertyRuleSpec();
                        propertyRulerRef.namePattern = childElement.GetAttribute("namePattern");
                        propertyRulerRef.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                        propertyRulerRef.rule = (PropertyRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Property, childElement);
                        break;
                    }
                    case "event":
                    {
                        var eventRuleSpec = new EventRuleSpec();
                        eventRuleSpec.namePattern = childElement.GetAttribute("namePattern");
                        eventRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifierType"));
                        eventRuleSpec.rule = (EventRule)GetOrParseRule(childElement.GetAttribute("rule"), RuleType.Event, childElement);
                        rule.eventRuleSpecs.Add(eventRuleSpec);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in type rule {ruleName}");
                }
            }
            return rule;
        }

        private MethodRule ParseMethodRule(string ruleName, XmlElement element)
        {
            var rule = new MethodRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), true);
            rule.obfuscateParam = ParseBool(element.GetAttribute("ob-param"), true);
            rule.obfuscateBody = ParseBool(element.GetAttribute("ob-body"), true);
            return rule;
        }

        private FieldRule ParseFieldRule(string ruleName, XmlElement element)
        {
            var rule = new FieldRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), true);
            return rule;
        }

        private PropertyRule ParsePropertyRule(string ruleName, XmlElement element)
        {
            var rule = new PropertyRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), true);
            rule.obfuscateGetter = ParseBool(element.GetAttribute("ob-getter"), true);
            rule.obfuscateSetter = ParseBool(element.GetAttribute("ob-setter"), true);
            return rule;
        }

        private EventRule ParseEventRule(string ruleName, XmlElement element)
        {
            var rule = new EventRule();
            rule.ruleName = ruleName;
            rule.obfuscateName = ParseBool(element.GetAttribute("ob-name"), true);
            rule.obfuscateAdd = ParseBool(element.GetAttribute("ob-add"), true);
            rule.obfuscateRemove = ParseBool(element.GetAttribute("ob-remove"), true);
            rule.obfuscateFire = ParseBool(element.GetAttribute("ob-fire"), true);
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
                return ModifierType.All;
            }
            switch (modifierType)
            {
                case "public": return ModifierType.Public;
                case "notPublic": return ModifierType.NotPublic;
                case "all": return ModifierType.All;
                default: throw new Exception($"Invalid modifier type {modifierType}");
            }
        }

        private void LoadRawXml(string xmlFile, List<XmlElement> rawAssemblyElements)
        {
            Debug.Log($"ObfuscateRule::LoadXml {xmlFile}");
            var doc = new XmlDocument();
            doc.Load(xmlFile);
            var root = doc.DocumentElement;
            if (root.Name != "obfuz")
            {
                throw new Exception($"Invalid xml file {xmlFile}, root name should be 'obfuz'");
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is not XmlElement element)
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

        public bool NeedRename(ModuleDefMD mod)
        {
            return false;
        }

        public bool NeedRename(TypeDef typeDef)
        {
            return true;
        }

        public bool NeedRename(MethodDef methodDef)
        {
            return true;
        }

        public bool NeedRename(FieldDef fieldDef)
        {
            return true;
        }

        public bool NeedRename(PropertyDef propertyDef)
        {
            return true;
        }

        public bool NeedRename(EventDef eventDef)
        {
            return true;
        }

        public bool NeedRename(ParamDef paramDef)
        {
            return true;
        }
    }
}
