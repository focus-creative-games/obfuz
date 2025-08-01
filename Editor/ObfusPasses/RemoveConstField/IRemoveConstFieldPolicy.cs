using dnlib.DotNet;

namespace Obfuz.ObfusPasses.RemoveConstField
{
    public interface IRemoveConstFieldPolicy
    {
        bool NeedPreserved(FieldDef field);
    }

    public abstract class RemoveConstFieldBase : IRemoveConstFieldPolicy
    {
        public abstract bool NeedPreserved(FieldDef field);
    }
}
