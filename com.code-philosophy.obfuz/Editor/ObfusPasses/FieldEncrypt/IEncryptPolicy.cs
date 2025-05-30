using dnlib.DotNet;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public interface IEncryptPolicy
    {
        bool NeedEncrypt(FieldDef field);
    }

    public abstract class EncryptPolicyBase : IEncryptPolicy
    {
        public abstract bool NeedEncrypt(FieldDef field);
    }
}
