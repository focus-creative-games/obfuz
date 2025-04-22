using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NUnit.Framework;
using Obfuz.Emit;
using System.Collections.Generic;

namespace Obfuz.Virtualization
{
    public class CompileContext
    {
        public MethodDef method;
        public List<Instruction> output;
        public RvaDataAllocator rvaDataAllocator;
    }
}
