using dnlib.DotNet;

namespace Obfuz.ObfusPasses.MemEncrypt
{
    public abstract class EncryptionPolicyBase : IEncryptionPolicy
    {
        public abstract bool NeedEncrypt(FieldDef field);
    }
}
