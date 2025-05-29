using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public class ObfuzIgnoreAttribute : Attribute
    {
        public ObfuzScope Scope { get; set; }

        public bool ApplyToNestedTypes { get; set; } = true;

        public bool ApplyToChildTypes { get; set; } = false;

        public ObfuzIgnoreAttribute(ObfuzScope scope = ObfuzScope.All)
        {
            this.Scope = scope;
        }
    }
}
