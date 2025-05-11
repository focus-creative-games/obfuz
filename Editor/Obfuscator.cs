using dnlib.DotNet;
using dnlib.Protection;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.EncryptionVM;
using Obfuz.ObfusPasses;
using Obfuz.ObfusPasses.CleanUp;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEditor.ObjectChangeEventStream;

namespace Obfuz
{

    public class Obfuscator
    {
        private readonly string _obfuscatedAssemblyOutputDir;
        private readonly AssemblyCache _assemblyCache;

        private readonly List<string> _toObfuscatedAssemblyNames;
        private readonly List<string> _notObfuscatedAssemblyNamesReferencingObfuscated;
        private readonly List<ModuleDef> _toObfuscatedModules = new List<ModuleDef>();
        private readonly List<ModuleDef> _obfuscatedAndNotObfuscatedModules = new List<ModuleDef>();

        private readonly Pipeline _pipeline = new Pipeline();
        private readonly byte[] _secretKey;
        private readonly int _globalRandomSeed;
        private readonly string _encryptionVmGenerationSecretKey;
        private readonly int _encryptionVmOpCodeCount;

        private ObfuscationPassContext _ctx;

        public Obfuscator(ObfuscatorBuilder builder)
        {
            _secretKey = KeyGenerator.GenerateKey(builder.SecretKey, VirtualMachine.SecretKeyLength);
            _globalRandomSeed = builder.GlobalRandomSeed;
            _encryptionVmGenerationSecretKey = builder.EncryptionVmGenerationSecretKey;
            _encryptionVmOpCodeCount = builder.EncryptionVmOpCodeCount;

            _toObfuscatedAssemblyNames = builder.ToObfuscatedAssemblyNames;
            _notObfuscatedAssemblyNamesReferencingObfuscated = builder.NotObfuscatedAssemblyNamesReferencingObfuscated;
            _obfuscatedAssemblyOutputDir = builder.ObfuscatedAssemblyOutputDir;

            GroupByModuleEntityManager.Reset();
            _assemblyCache = new AssemblyCache(new PathAssemblyResolver(builder.AssemblySearchDirs.ToArray()));
            foreach (var pass in builder.ObfuscationPasses)
            {
                _pipeline.AddPass(pass);
            }
            _pipeline.AddPass(new CleanUpInstructionPass());
        }

        public void Run()
        {
            OnPreObfuscation();
            DoObfuscation();
            OnPostObfuscation();
        }

        private IEncryptor CreateEncryptionVirtualMachine()
        {
            var vmCreator = new VirtualMachineCreator(_encryptionVmGenerationSecretKey);
            var vm = vmCreator.CreateVirtualMachine(_encryptionVmOpCodeCount);
            return new VirtualMachineSimulator(vm, _secretKey);
        }

        private void OnPreObfuscation()
        {
            LoadAssemblies();


            var random = new RandomWithKey(_secretKey, _globalRandomSeed);
            var encryptor = CreateEncryptionVirtualMachine();
            var rvaDataAllocator = new RvaDataAllocator(random, encryptor);
            var constFieldAllocator = new ConstFieldAllocator(encryptor, random, rvaDataAllocator);
            _ctx = new ObfuscationPassContext
            {
                assemblyCache = _assemblyCache,
                toObfuscatedModules = _toObfuscatedModules,
                obfuscatedAndNotObfuscatedModules = _obfuscatedAndNotObfuscatedModules,
                toObfuscatedAssemblyNames = _toObfuscatedAssemblyNames,
                notObfuscatedAssemblyNamesReferencingObfuscated = _notObfuscatedAssemblyNamesReferencingObfuscated,
                obfuscatedAssemblyOutputDir = _obfuscatedAssemblyOutputDir,

                random = random,
                encryptor = encryptor,
                rvaDataAllocator = rvaDataAllocator,
                constFieldAllocator = constFieldAllocator,
            };
            _pipeline.Start(_ctx);
        }

        private void LoadAssemblies()
        {
            foreach (string assName in _toObfuscatedAssemblyNames.Concat(_notObfuscatedAssemblyNamesReferencingObfuscated))
            {
                ModuleDefMD mod = _assemblyCache.TryLoadModule(assName);
                if (mod == null)
                {
                    Debug.Log($"assembly: {assName} not found! ignore.");
                    continue;
                }
                if (_toObfuscatedAssemblyNames.Contains(assName))
                {
                    _toObfuscatedModules.Add(mod);
                }
                _obfuscatedAndNotObfuscatedModules.Add(mod);
            }
        }

        private void DoObfuscation()
        {
            FileUtil.RecreateDir(_obfuscatedAssemblyOutputDir);

            _pipeline.Run(_ctx);
        }

        private void OnPostObfuscation()
        {
            _pipeline.Stop(_ctx);

            foreach (ModuleDef mod in _obfuscatedAndNotObfuscatedModules)
            {
                string assNameWithExt = mod.Name;
                string outputFile = $"{_obfuscatedAssemblyOutputDir}/{assNameWithExt}";
                mod.Write(outputFile);
                Debug.Log($"save module. name:{mod.Assembly.Name} output:{outputFile}");
            }
        }
    }
}
