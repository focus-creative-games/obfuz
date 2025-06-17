using Obfuz.Emit;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class AdvancedObfuscator : BasicObfuscator
    {
        public AdvancedObfuscator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager)
            : base(encryptionScopeProvider, moduleEntityManager)
        {
        }
    }
}
