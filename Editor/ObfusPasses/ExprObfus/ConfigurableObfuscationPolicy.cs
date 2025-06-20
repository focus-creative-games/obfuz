using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Settings;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Obfuz.ObfusPasses.ExprObfus
{

    public interface IObfuscationPolicy
    {
        bool NeedObfuscate(MethodDef method);
    }

    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscate(MethodDef method);
    }

    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public ObfuscationLevel? obfuscationLevel;
            public float? obfuscationPercentage;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (obfuscationLevel == null)
                    obfuscationLevel = parentRule.obfuscationLevel;
                if (obfuscationPercentage == null)
                    obfuscationPercentage = parentRule.obfuscationPercentage;
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
            obfuscationLevel = ObfuscationLevel.None,
            obfuscationPercentage = 0.5f,
        };

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _xmlParser;

        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _xmlParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(
                toObfuscatedAssemblyNames, ParseObfuscationRule, null);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _xmlParser.LoadConfigs(configFiles);
            _xmlParser.InheritParentRules(s_default);
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
            if (ele.HasAttribute("obfuscationPercentage"))
            {
                rule.obfuscationPercentage = float.Parse(ele.GetAttribute("obfuscationPercentage"));
            }
            return rule;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _xmlParser.GetMethodRule(method, s_default);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscate(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.obfuscationLevel.Value > ObfuscationLevel.None;
        }
    }
}
