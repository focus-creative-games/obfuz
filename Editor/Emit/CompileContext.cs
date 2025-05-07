using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NUnit.Framework;
using Obfuz.Emit;
using Obfuz.Data;
using System.Collections.Generic;

namespace Obfuz.Emit
{
    public class CompileContext
    {
        public MethodDef method;
        public List<Instruction> output;
        public RvaDataAllocator rvaDataAllocator;
        public ConstFieldAllocator constFieldAllocator;
    }
}
