using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Settings;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    struct ObfuscationRuleData
    {
        public readonly ObfuscationLevel obfuscationLevel;
        public ObfuscationRuleData(ObfuscationLevel level)
        {
            obfuscationLevel = level;
        }
    }

    interface IObfuscationPolicy
    {
        bool NeedObfuscate(MethodDef method);

        ObfuscationRuleData GetObfuscationRuleData(MethodDef method);
    }

    abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscate(MethodDef method);

        public abstract ObfuscationRuleData GetObfuscationRuleData(MethodDef method);
    }

    class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public ObfuscationLevel? obfuscationLevel;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (obfuscationLevel == null)
                    obfuscationLevel = parentRule.obfuscationLevel;
            }
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            obfuscationLevel = ObfuscationLevel.Basic,
        };

        private ObfuscationRule _global;

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _xmlParser;

        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _xmlParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(
                toObfuscatedAssemblyNames, ParseObfuscationRule, ParseGlobal);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _xmlParser.LoadConfigs(configFiles);

            if (_global == null)
            {
                _global = s_default;
            }
            else
            {
                _global.InheritParent(s_default);
            }
            _xmlParser.InheritParentRules(_global);
        }

        private void ParseGlobal(string configFile, XmlElement ele)
        {
            switch (ele.Name)
            {
                case "global": _global = ParseObfuscationRule(configFile, ele); break;
                default: throw new Exception($"Invalid xml file {configFile}, unknown node {ele.Name}");
            }
        }

        private ObfuscationLevel ParseObfuscationLevel(string str)
        {
            return (ObfuscationLevel)Enum.Parse(typeof(ObfuscationLevel), str);
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("obfuscationLevel"))
            {
                rule.obfuscationLevel = ParseObfuscationLevel(ele.GetAttribute("obfuscationLevel"));
            }
            return rule;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _xmlParser.GetMethodRule(method, _global);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscate(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.obfuscationLevel.Value > ObfuscationLevel.None;
        }

        public override ObfuscationRuleData GetObfuscationRuleData(MethodDef method)
        {
            var rule = GetMethodObfuscationRule(method);
            return new ObfuscationRuleData(rule.obfuscationLevel.Value);
        }
    }
}
