using Obfuz.Emit;

namespace Obfuz.ObfusPasses.ExprObfus.Obfuscators
{
    class MostAdvancedObfuscator : AdvancedObfuscator
    {
        public MostAdvancedObfuscator(EncryptionScopeProvider encryptionScopeProvider, GroupByModuleEntityManager moduleEntityManager)
            : base(encryptionScopeProvider, moduleEntityManager)
        {
        }
    }
}
