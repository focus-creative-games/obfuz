using dnlib.DotNet;

namespace Obfuz.ObfusPasses.FieldEncrypt
{
    public class ConfigurableEncryptPolicy : EncryptPolicyBase
    {

        public override bool NeedEncrypt(FieldDef field)
        {
            TypeDef type = field.DeclaringType;
            // TODO 
            if (type.Name == "EncryptField" || type.Name == "EncryptProperty")
            {
                return true;
            }
            return false;
        }
    }
}
