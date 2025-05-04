using dnlib.DotNet;

namespace Obfuz.MemEncrypt
{
    public class ConfigEncryptionPolicy : EncryptionPolicyBase
    {

        private bool IsSupportedFieldType(TypeSig type)
        {
            type = type.RemovePinnedAndModifiers();
            switch (type.ElementType)
            {
                case ElementType.I4:
                case ElementType.I8:
                case ElementType.U4:
                case ElementType.U8:
                case ElementType.R4:
                case ElementType.R8:
                    return true;
                default: return false;
            }
        }

        public override bool NeedEncrypt(FieldDef field)
        {
            TypeDef type = field.DeclaringType;
            if (!IsSupportedFieldType(field.FieldType))
            {
                return false;
            }
            // TODO 
            if (type.Name == "EncryptField" || type.Name == "EncryptProperty")
            {
                return true;
            }
            return false;
        }
    }
}
