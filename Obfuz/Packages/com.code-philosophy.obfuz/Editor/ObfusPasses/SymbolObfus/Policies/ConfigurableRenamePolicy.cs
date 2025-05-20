using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

namespace Obfuz.ObfusPasses.SymbolObfus.Policies
{

    public class ConfigurableRenamePolicy : ObfuscationPolicyBase
    {
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
            None = 0x0,
            Private = 0x1,
            Protected = 0x2,
            Public = 0x3,
        }

        class MethodRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
        }

        class FieldRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
        }

        class PropertyRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
            public bool? obfuscateGetter;
            public bool? obfuscateSetter;
        }

        class EventRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public bool? obfuscateName;
            public bool? obfuscateAdd;
            public bool? obfuscateRemove;
            public bool? obfuscateFire;
        }

        class TypeRuleSpec
        {
            public NameMatcher nameMatcher;
            public ModifierType? modifierType;
            public ClassType? classType;
            public bool? obfuscateName;
            public bool? obfuscateNamespace;
            public List<FieldRuleSpec> fields;
            public List<MethodRuleSpec> methods;
            public List<PropertyRuleSpec> properties;
            public List<EventRuleSpec> events;
        }

        class AssemblyRuleSpec
        {
            public string assemblyName;
            public bool? obfuscateName;
            public List<TypeRuleSpec> types;
        }

        private readonly Dictionary<string, AssemblyRuleSpec> _assemblyRuleSpecs = new Dictionary<string, AssemblyRuleSpec>();

        private AssemblyRuleSpec ParseAssembly(XmlElement ele)
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
            var rule = new AssemblyRuleSpec()
            {
                assemblyName = assemblyName,
                obfuscateName = ConfigUtil.ParseNullableBool(ele.GetAttribute("obName")),
                types = new List<TypeRuleSpec>(),
            };

            foreach (XmlNode node in ele.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                if (childElement.Name != "type")
                {
                    throw new Exception($"Invalid xml file, unknown node {childElement.Name}");
                }
                TypeRuleSpec type = ParseType(childElement);
                rule.types.Add(type);
            }
            return rule;
        }

        private enum ClassType
        {
            None = 0x0,
            Class = 0x1,
            Struct = 0x2,
            Interface = 0x4,
            Enum = 0x8,
            Delegate = 0x10,
        }

        private ClassType? ParseClassType(string classType)
        {
            if (string.IsNullOrEmpty(classType))
            {
                return null;
            }
            
            ClassType type = ClassType.None;
            foreach (var s in classType.Split('|'))
            {
                switch (s)
                {
                    case "class": type |= ClassType.Class; break;
                    case "struct": type |= ClassType.Struct; break;
                    case "interface": type |= ClassType.Interface; break;
                    case "enum": type |= ClassType.Enum; break;
                    case "delegate": type |= ClassType.Delegate; break;
                    default: throw new Exception($"Invalid class type {s}");
                }
            }
            return type;
        }

        private ModifierType? ParseModifierType(string modifierType)
        {
            if (string.IsNullOrEmpty(modifierType))
            {
                return null;
            }
            ModifierType type = ModifierType.None;
            foreach (var s in modifierType.Split('|'))
            {
                switch (s)
                {
                    case "public": type |= ModifierType.Public; break;
                    case "protected": type |= ModifierType.Protected; break;
                    case "private": type |= ModifierType.Private; break;
                    default: throw new Exception($"Invalid modifier type {s}");
                }
            }
            return type;
        }

        private TypeRuleSpec ParseType(XmlElement element)
        {
            var rule = new TypeRuleSpec();

            rule.nameMatcher = new NameMatcher(element.GetAttribute("name"));
            rule.obfuscateName = ConfigUtil.ParseNullableBool(element.GetAttribute("obName"));
            rule.obfuscateNamespace = ConfigUtil.ParseNullableBool(element.GetAttribute("obNamespace"));
            rule.modifierType = ParseModifierType(element.GetAttribute("modifier"));
            rule.classType = ParseClassType(element.GetAttribute("classType"));

            //rule.nestTypeRuleSpecs = new List<TypeRuleSpec>();
            rule.fields = new List<FieldRuleSpec>();
            rule.methods = new List<MethodRuleSpec>();
            rule.properties = new List<PropertyRuleSpec>();
            rule.events = new List<EventRuleSpec>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (!(node is XmlElement childElement))
                {
                    continue;
                }
                switch (childElement.Name)
                {
                    case "field":
                    {
                        var fieldRuleSpec = new FieldRuleSpec();
                        fieldRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        fieldRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        fieldRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        rule.fields.Add(fieldRuleSpec);
                        break;
                    }
                    case "method":
                    {
                        var methodRuleSpec = new MethodRuleSpec();
                        methodRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        methodRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        methodRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        rule.methods.Add(methodRuleSpec);
                        break;
                    }
                    case "property":
                    {
                        var propertyRulerSpec = new PropertyRuleSpec();
                        propertyRulerSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        propertyRulerSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        propertyRulerSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        propertyRulerSpec.obfuscateGetter = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obGetter"));
                        propertyRulerSpec.obfuscateSetter = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obSetter"));
                        rule.properties.Add(propertyRulerSpec);
                        break;
                    }
                    case "event":
                    {
                        var eventRuleSpec = new EventRuleSpec();
                        eventRuleSpec.nameMatcher = new NameMatcher(childElement.GetAttribute("name"));
                        eventRuleSpec.modifierType = ParseModifierType(childElement.GetAttribute("modifier"));
                        eventRuleSpec.obfuscateName = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obName"));
                        eventRuleSpec.obfuscateAdd = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obAdd"));
                        eventRuleSpec.obfuscateRemove = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obRemove"));
                        eventRuleSpec.obfuscateFire = ConfigUtil.ParseNullableBool(childElement.GetAttribute("obFire"));
                        rule.events.Add(eventRuleSpec);
                        break;
                    }
                    default: throw new Exception($"Invalid xml file, unknown node {childElement.Name} in type node");
                }
            }
            return rule;
        }

        private void LoadXmls(List<string> xmlFiles)
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
                var assemblyRule = ParseAssembly(ele);
                _assemblyRuleSpecs.Add(assemblyRule.assemblyName, assemblyRule);
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
                if (!(node is XmlElement element))
                {
                    continue;
                }
                switch (element.Name)
                {
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

        private bool MatchModifier(ModifierType? modifierType, TypeDef typeDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType(typeDef.Visibility)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, FieldDef fieldDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType(fieldDef.Access)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, MethodDef methodDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)methodDef.Access)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, PropertyDef propertyDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)propertyDef.Attributes)) != 0;
        }

        private bool MatchModifier(ModifierType? modifierType, EventDef eventDef)
        {
            return modifierType == null || (modifierType & ComputeModifierType((FieldAttributes)eventDef.Attributes)) != 0;
        }

        private class MethodComputeCache
        {
            public bool obfuscateName = true;
            public bool obfuscateParam = true;
            public bool obfuscateBody = true;
        }

        //private class TypeDefComputeCache
        //{
        //    public bool obfuscateName = true;
        //    public bool obfuscateNamespace = true;

        //    public readonly Dictionary<MethodDef, MethodComputeCache> methods = new Dictionary<MethodDef, MethodComputeCache>();

        //    public readonly HashSet<FieldDef> notObfuscatedFields = new HashSet<FieldDef>();

        //    public readonly HashSet<PropertyDef> notObfuscatedProperties = new HashSet<PropertyDef>();

        //    public readonly HashSet<EventDef> notObfuscatedEvents = new HashSet<EventDef>();
        //}

        private readonly Dictionary<TypeDef, TypeRuleSpec> _typeSpecCache = new Dictionary<TypeDef, TypeRuleSpec>();
        private readonly Dictionary<MethodDef, MethodRuleSpec> _methodSpecCache = new Dictionary<MethodDef, MethodRuleSpec>();
        private readonly Dictionary<FieldDef, FieldRuleSpec> _fieldSpecCache = new Dictionary<FieldDef, FieldRuleSpec>();
        private readonly Dictionary<PropertyDef, PropertyRuleSpec> _propertySpecCache = new Dictionary<PropertyDef, PropertyRuleSpec>();
        private readonly Dictionary<EventDef, EventRuleSpec> _eventSpecCache = new Dictionary<EventDef, EventRuleSpec>();


        private readonly HashSet<string> _obfuscationAssemblyNames;

        public ConfigurableRenamePolicy(List<string> obfuscationAssemblyNames, List<string> xmlFiles)
        {
            _obfuscationAssemblyNames = new HashSet<string>(obfuscationAssemblyNames);
            LoadXmls(xmlFiles);
        }

        private void BuildDefaultTypeMemberCache(TypeDef typeDef, TypeRuleSpec typeRule)
        {
            foreach (var fieldDef in typeDef.Fields)
            {
                var fieldRule = new FieldRuleSpec()
                {
                    obfuscateName = typeRule.obfuscateName,
                };
                _fieldSpecCache.Add(fieldDef, fieldRule);
            }
            foreach (var eventDef in typeDef.Events)
            {
                var eventRule = new EventRuleSpec()
                {
                    obfuscateName = typeRule.obfuscateName,
                    obfuscateAdd = typeRule.obfuscateName,
                    obfuscateRemove = typeRule.obfuscateName,
                    obfuscateFire = typeRule.obfuscateName,
                };
                _eventSpecCache.Add(eventDef, eventRule);
            }
            foreach (var propertyDef in typeDef.Properties)
            {
                var propertyRule = new PropertyRuleSpec()
                {
                    obfuscateName = typeRule.obfuscateName,
                    obfuscateGetter = typeRule.obfuscateName,
                    obfuscateSetter = typeRule.obfuscateName,
                };
                _propertySpecCache.Add(propertyDef, propertyRule);
            }
            foreach (MethodDef methodDef in typeDef.Methods)
            {
                var methodRule = new MethodRuleSpec()
                {
                    obfuscateName = typeRule.obfuscateName,
                };
                _methodSpecCache.Add(methodDef, methodRule);
            }
        }

        private bool MatchClassType(ClassType? classType, TypeDef typeDef)
        {
            if (classType == null)
            {
                return true;
            }
            if (!typeDef.IsValueType && (classType & ClassType.Class) != 0)
            {
                return true;
            }
            if (typeDef.IsValueType && (classType & ClassType.Struct) != 0)
            {
                return true;
            }
            if (typeDef.IsInterface && (classType & ClassType.Interface) != 0)
            {
                return true;
            }
            if (typeDef.IsEnum && (classType & ClassType.Enum) != 0)
            {
                return true;
            }
            if (typeDef.IsDelegate && (classType & ClassType.Delegate) != 0)
            {
                return true;
            }
            return false;
        }

        private TypeRuleSpec GetOrCreateTypeDefRenameComputeCache(TypeDef typeDef)
        {
            if (_typeSpecCache.TryGetValue(typeDef, out var typeRule))
            {
                return typeRule;
            }
            typeRule = new TypeRuleSpec();
            _typeSpecCache.Add(typeDef, typeRule);

            if (!_assemblyRuleSpecs.TryGetValue(typeDef.Module.Assembly.Name, out var assemblyRuleSpec))
            {
                typeRule.obfuscateName = true;
                typeRule.obfuscateNamespace = true;
                BuildDefaultTypeMemberCache(typeDef, typeRule);
                return typeRule;
            }

            typeRule.obfuscateName = assemblyRuleSpec.obfuscateName ?? true;
            typeRule.obfuscateNamespace = assemblyRuleSpec.obfuscateName ?? true;

            if (typeDef.DeclaringType != null)
            {
                TypeRuleSpec declaringTypeSpec = GetOrCreateTypeDefRenameComputeCache(typeDef.DeclaringType);
                if (declaringTypeSpec.obfuscateName != null)
                {
                    typeRule.obfuscateName = declaringTypeSpec.obfuscateName;
                }
                if (declaringTypeSpec.obfuscateNamespace != null)
                {
                    typeRule.obfuscateNamespace = declaringTypeSpec.obfuscateNamespace;
                }
            }

            string typeName = typeDef.FullName;
            bool findMatch = false;
            foreach (var typeSpec in assemblyRuleSpec.types)
            {
                if (!typeSpec.nameMatcher.IsMatch(typeName) || !MatchModifier(typeSpec.modifierType, typeDef) || !MatchClassType(typeSpec.classType, typeDef))
                {
                    continue;
                }
                findMatch = true;
                if (typeSpec.obfuscateName != null)
                {
                    typeRule.obfuscateName = typeSpec.obfuscateName;
                }
                if (typeSpec.obfuscateNamespace != null)
                {
                    typeRule.obfuscateNamespace = typeSpec.obfuscateNamespace;
                }


                foreach (var fieldDef in typeDef.Fields)
                {
                    var fieldRule = new FieldRuleSpec()
                    {
                        obfuscateName = typeRule.obfuscateName,
                    };
                    _fieldSpecCache.Add(fieldDef, fieldRule);
                    foreach (var fieldSpec in typeSpec.fields)
                    {
                        if (fieldSpec.nameMatcher.IsMatch(fieldDef.Name) && MatchModifier(fieldSpec.modifierType, fieldDef))
                        {
                            if (fieldSpec.obfuscateName != null)
                            {
                                fieldRule.obfuscateName = fieldSpec.obfuscateName;
                            }
                            break;
                        }
                    }
                }

                var methodObfuscateFromPropertyOrEvent = new Dictionary<MethodDef, bool>();

                foreach (var eventDef in typeDef.Events)
                {
                    var eventRule = new EventRuleSpec()
                    {
                        obfuscateName = typeRule.obfuscateName,
                        obfuscateAdd = typeRule.obfuscateName,
                        obfuscateRemove = typeRule.obfuscateName,
                        obfuscateFire = typeRule.obfuscateName,
                    };
                    _eventSpecCache.Add(eventDef, eventRule);
                    foreach (var eventSpec in typeSpec.events)
                    {
                        if (!eventSpec.nameMatcher.IsMatch(eventDef.Name) || !MatchModifier(eventSpec.modifierType, eventDef))
                        {
                            continue;
                        }
                        if (eventSpec.obfuscateName != null)
                        {
                            eventRule.obfuscateName = eventSpec.obfuscateName;
                        }
                        if (eventSpec.obfuscateAdd != null)
                        {
                            eventRule.obfuscateAdd = eventSpec.obfuscateAdd;
                        }
                        if (eventSpec.obfuscateRemove != null)
                        {
                            eventRule.obfuscateRemove = eventSpec.obfuscateRemove;
                        }
                        if (eventSpec.obfuscateFire != null)
                        {
                            eventRule.obfuscateFire = eventSpec.obfuscateFire;
                        }
                        if (eventDef.AddMethod != null && eventRule.obfuscateAdd != null)
                        {
                            methodObfuscateFromPropertyOrEvent.Add(eventDef.AddMethod, eventRule.obfuscateAdd.Value);
                        }
                        if (eventDef.RemoveMethod != null && eventRule.obfuscateRemove != null)
                        {
                            methodObfuscateFromPropertyOrEvent.Add(eventDef.RemoveMethod, eventRule.obfuscateRemove.Value);
                        }
                        if (eventDef.InvokeMethod != null && eventRule.obfuscateFire != null)
                        {
                            methodObfuscateFromPropertyOrEvent.Add(eventDef.InvokeMethod, eventRule.obfuscateFire.Value);
                        }
                        break;
                    }
                }
                foreach (var propertyDef in typeDef.Properties)
                {
                    var propertyRule = new PropertyRuleSpec()
                    {
                        obfuscateName = typeRule.obfuscateName,
                        obfuscateGetter = typeRule.obfuscateName,
                        obfuscateSetter = typeRule.obfuscateName,
                    };
                    _propertySpecCache.Add(propertyDef, propertyRule);
                    foreach (var propertySpec in typeSpec.properties)
                    {
                        if (!propertySpec.nameMatcher.IsMatch(propertyDef.Name) || !MatchModifier(propertySpec.modifierType, propertyDef))
                        {
                            continue;
                        }
                        if (propertySpec.obfuscateName != null)
                        {
                            propertyRule.obfuscateName = propertySpec.obfuscateName;
                        }
                        if (propertySpec.obfuscateGetter != null)
                        {
                            propertyRule.obfuscateGetter = propertySpec.obfuscateGetter;
                        }
                        if (propertySpec.obfuscateSetter != null)
                        {
                            propertyRule.obfuscateSetter = propertySpec.obfuscateSetter;
                        }

                        if (propertyDef.GetMethod != null && propertyRule.obfuscateGetter != null)
                        {
                            methodObfuscateFromPropertyOrEvent.Add(propertyDef.GetMethod, propertyRule.obfuscateGetter.Value);
                        }
                        if (propertyDef.SetMethod != null && propertyRule.obfuscateSetter != null)
                        {
                            methodObfuscateFromPropertyOrEvent.Add(propertyDef.SetMethod, propertyRule.obfuscateSetter.Value);
                        }
                        break;
                    }
                }
                foreach (MethodDef methodDef in typeDef.Methods)
                {
                    var methodRule = new MethodRuleSpec()
                    {
                        obfuscateName = typeRule.obfuscateName,
                    };
                    _methodSpecCache.Add(methodDef, methodRule);
                    if (methodObfuscateFromPropertyOrEvent.TryGetValue(methodDef, out var obfuscateName))
                    {
                        methodRule.obfuscateName = obfuscateName;
                    }
                    foreach (MethodRuleSpec methodSpec in typeSpec.methods)
                    {
                        if (!methodSpec.nameMatcher.IsMatch(methodDef.Name) || !MatchModifier(methodSpec.modifierType, methodDef))
                        {
                            continue;
                        }
                        if (methodSpec.obfuscateName != null)
                        {
                            methodRule.obfuscateName = methodSpec.obfuscateName;
                        }
                        break;
                    }
                }
            }
            if (!findMatch)
            {
                BuildDefaultTypeMemberCache(typeDef, typeRule);
            }

            return typeRule;
        }

        public override bool NeedRename(TypeDef typeDef)
        {
            var cache = GetOrCreateTypeDefRenameComputeCache(typeDef);
            return cache.obfuscateName != false;
        }

        public override bool NeedRename(MethodDef methodDef)
        {
            TypeDef typeDef = methodDef.DeclaringType;
            GetOrCreateTypeDefRenameComputeCache(typeDef);
            return _methodSpecCache[methodDef].obfuscateName != false;
        }

        public override bool NeedRename(FieldDef fieldDef)
        {
            TypeDef typeDef = fieldDef.DeclaringType;
            GetOrCreateTypeDefRenameComputeCache(typeDef);
            return _fieldSpecCache[fieldDef].obfuscateName != false;
        }

        public override bool NeedRename(PropertyDef propertyDef)
        {
            TypeDef typeDef = propertyDef.DeclaringType;
            GetOrCreateTypeDefRenameComputeCache(typeDef);
            return _propertySpecCache[propertyDef].obfuscateName != false;
        }

        public override bool NeedRename(EventDef eventDef)
        {
            TypeDef typeDef = eventDef.DeclaringType;
            GetOrCreateTypeDefRenameComputeCache(typeDef);
            return _eventSpecCache[eventDef].obfuscateName != false;
        }
    }
}
