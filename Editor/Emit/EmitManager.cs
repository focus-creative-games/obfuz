using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Emit
{
    public interface IModuleEmitManager
    {
        void Init(ModuleDef mod);
    }

    public abstract class ModuleEmitManagerBase : IModuleEmitManager
    {
        public abstract void Init(ModuleDef mod);
    }

    public class EmitManager
    {
        public static EmitManager Ins { get; private set; }


        private readonly Dictionary<(ModuleDef, Type), IModuleEmitManager> _moduleEmitManagers = new System.Collections.Generic.Dictionary<(ModuleDef, Type), IModuleEmitManager>();

        public static void Reset()
        {
            Ins = new EmitManager();
        }

        public T GetEmitManager<T>(ModuleDef mod, Func<T> creator = null) where T : IModuleEmitManager
        {
            var key = (mod, typeof(T));
            if (_moduleEmitManagers.TryGetValue(key, out var emitManager))
            {
                return (T)emitManager;
            }
            else
            {
                T newEmitManager;
                if (creator != null)
                {
                    newEmitManager = creator();
                }
                else
                {
                    newEmitManager = (T)Activator.CreateInstance(typeof(T));
                }
                newEmitManager.Init(mod);
                _moduleEmitManagers[key] = newEmitManager;
                return newEmitManager;
            }
        }

        public List<T> GetEmitManagers<T>()  where T: IModuleEmitManager
        {
            var managers = new List<T>();
            foreach (var kv in _moduleEmitManagers)
            {
                if (kv.Key.Item2 == typeof(T))
                {
                    managers.Add((T)kv.Value);
                }
            }
            return managers;
        }
    }
}
