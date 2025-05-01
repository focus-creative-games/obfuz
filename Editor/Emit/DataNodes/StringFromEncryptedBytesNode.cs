using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit
{
    public class StringFromEncryptedBytesNode : DataNodeAny
    {

        public override void Compile(CompileContext ctx)
        {
            // only support Int32, int64, bytes.
            // string can only create from StringFromBytesNode
            // x = memcpy array.GetRange(index, length);
        }
    }
}
