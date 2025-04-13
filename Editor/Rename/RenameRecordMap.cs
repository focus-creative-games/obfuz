using dnlib.DotNet;
using System.Collections.Generic;

namespace Obfuz
{
    public class RenameRecordMap
    {
        private enum RenameStatus
        {
            NotRenamed,
            Renamed,
        }

        private class RenameRecord
        {
            public RenameStatus status;
            public string oldName;
            public string newName;
        }

        private readonly Dictionary<ModuleDefMD, RenameRecord> _modRenames = new Dictionary<ModuleDefMD, RenameRecord>();
        private readonly Dictionary<TypeDef, RenameRecord> _typeRenames = new Dictionary<TypeDef, RenameRecord>();
        private readonly Dictionary<MethodDef, RenameRecord> _methodRenames = new Dictionary<MethodDef, RenameRecord>();
        private readonly Dictionary<FieldDef, RenameRecord> _fieldRenames = new Dictionary<FieldDef, RenameRecord>();
        private readonly Dictionary<PropertyDef, RenameRecord> _propertyRenames = new Dictionary<PropertyDef, RenameRecord>();
        private readonly Dictionary<EventDef, RenameRecord> _eventRenames = new Dictionary<EventDef, RenameRecord>();


        public void AddRenameRecord(ModuleDefMD mod, string oldName, string newName)
        {
            _modRenames.Add(mod, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddRenameRecord(TypeDef type, string oldName, string newName)
        {
            _typeRenames.Add(type, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddRenameRecord(MethodDef method, string oldName, string newName)
        {
            _methodRenames.Add(method, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddRenameRecord(FieldDef field, string oldName, string newName)
        {
            _fieldRenames.Add(field, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddRenameRecord(PropertyDef property, string oldName, string newName)
        {
            _propertyRenames.Add(property, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddRenameRecord(EventDef eventDef, string oldName, string newName)
        {
            _eventRenames.Add(eventDef, new RenameRecord
            {
                status = RenameStatus.Renamed,
                oldName = oldName,
                newName = newName
            });
        }

        public void AddUnRenameRecord(ModuleDefMD mod)
        {
            _modRenames.Add(mod, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = mod.Assembly.Name,
                newName = null,
            });
        }

        public void AddUnRenameRecord(TypeDef typeDef)
        {
            _typeRenames.Add(typeDef, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = typeDef.FullName,
                newName = null,
            });
        }

        public void AddUnRenameRecord(MethodDef methodDef)
        {
            _methodRenames.Add(methodDef, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = methodDef.Name,
                newName = null,
            });
        }

        public void AddUnRenameRecord(FieldDef fieldDef)
        {
            _fieldRenames.Add(fieldDef, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = fieldDef.Name,
                newName = null,
            });
        }

        public void AddUnRenameRecord(PropertyDef propertyDef)
        {
            _propertyRenames.Add(propertyDef, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = propertyDef.Name,
                newName = null,
            });
        }

        public void AddUnRenameRecord(EventDef eventDef)
        {
            _eventRenames.Add(eventDef, new RenameRecord
            {
                status = RenameStatus.NotRenamed,
                oldName = eventDef.Name,
                newName = null,
            });
        }

    }
}
