using dnlib.DotNet;
using Obfuz.Conf;
using System.Collections.Generic;
using System.Xml;

namespace Obfuz.ObfusPasses.RemoveConstField
{
    public class ConfigurableRemoveConstFieldPolicy : RemoveConstFieldBase
    {
        class ObfuscationRule
        {

        }

        private readonly XmlFieldRuleParser<ObfuscationRule> _configParser;

        public ConfigurableRemoveConstFieldPolicy(List<string> toObfuscatedAssemblyNames, List<string> configFiles)
        {
            _configParser = new XmlFieldRuleParser<ObfuscationRule>(toObfuscatedAssemblyNames, ParseRule, null);
            _configParser.LoadConfigs(configFiles);
        }

        private ObfuscationRule ParseRule(string configFile, XmlElement ele)
        {
            return new ObfuscationRule();
        }

        public override bool NeedPreserved(FieldDef field)
        {
            var rule = _configParser.GetFieldRule(field);
            return rule != null;
        }
    }
}
