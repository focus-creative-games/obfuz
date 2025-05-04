using dnlib.DotNet;

namespace Obfuz.MemEncrypt
{
    public abstract class EncryptionPolicyBase : IEncryptionPolicy
    {
        public abstract bool NeedEncrypt(FieldDef field);
    }
}
