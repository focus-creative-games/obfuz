using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public enum ObfuzScope
    {
        None = 0x0,
        Self = 0x1,
        Field = 0x2,
        MethodName = 0x4,
        MethodParameter = 0x8,
        MethodBody = 0x10,
        Method = MethodName | MethodParameter | MethodBody,
        PropertyName = 020,
        PropertyGetter = 0x40,
        PropertySetter = 0x80,
        Property = PropertyName | PropertyGetter | PropertySetter,
        EventName = 0x100,
        EventAdd = 0x200,
        EventRemove = 0x400,
        EventFire = 0x800,
        Event = EventName | EventAdd | EventRemove,

        NestedTypeSelf = 0x1000,
        NestedTypeField = 0x2000,
        NestedTypeMethod = 0x4000,
        NestedTypeProperty = 0x8000,
        NestedTypeEvent = 0x10000,

        NestedTypeAll = NestedTypeSelf | NestedTypeField | NestedTypeMethod | NestedTypeProperty | NestedTypeEvent,

        Member = Field | Method | Property | Event,
        MemberAndNestedTypeSelf = Member | NestedTypeSelf,
        MemberAndNestedTypeAll = Member | NestedTypeAll,
        SelfAndNestedTypeSelf = Self | NestedTypeSelf,
        All = Self | MemberAndNestedTypeAll,
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class ObfuzIgnoreAttribute : Attribute
    {
        public ObfuzScope Scope { get; set; }

        public ObfuzIgnoreAttribute(ObfuzScope scope = ObfuzScope.All)
        {
            this.Scope = scope;
        }
    }
}
