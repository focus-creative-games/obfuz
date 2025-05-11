using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.EncryptionVM
{
    public class VirtualMachineCodeGenerator
    {
        private readonly string _vmCodeGenerateSecretKey;
        private readonly IRandom _random;
        private VirtualMachine _vm;

        public VirtualMachineCodeGenerator(string vmCodeGenerateSecretKey, int opCount)
        {
            _vmCodeGenerateSecretKey = vmCodeGenerateSecretKey;
            _vm = new VirtualMachineCreator(_vmCodeGenerateSecretKey).CreateVirtualMachine(opCount);
        }

        public void Generate(string outputFile)
        {

        }
    }
}
