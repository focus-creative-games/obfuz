using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Virtualization
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class NodeOutputAttribute : Attribute
    {
        public DataNodeType Type { get; }

        public NodeOutputAttribute(DataNodeType type)
        {
            Type = type;
        }
    }
}
