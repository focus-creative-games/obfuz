using System;

namespace Obfuz
{
    [Flags]
    public enum ObfuzScope
    {
        None = 0x0,
        TypeName = 0x1,
        Field = 0x2,
        MethodName = 0x4,
        MethodParameter = 0x8,
        MethodBody = 0x10,
        Method = MethodName | MethodParameter | MethodBody,
        PropertyName = 0x20,
        PropertyGetterSetterName = 0x40,
        Property = PropertyName | PropertyGetterSetterName,
        EventName = 0x100,
        EventAddRemoveFireName = 0x200,
        Event = EventName | PropertyGetterSetterName,
        Module = 0x1000,
        All = TypeName | Field | Method | Property | Event,
    }
}
