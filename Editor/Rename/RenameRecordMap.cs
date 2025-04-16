using dnlib.DotNet;
using Obfuz.Rename;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;

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
            public string signature;
            public string oldName;
            public string newName;
        }

        private class RenameMappingField
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingMethod
        {
            public RenameStatus status;
            public string signature;
            public string newName;

            public List<RenameMappingMethodParam> parameters = new List<RenameMappingMethodParam>();
        }

        private class RenameMappingMethodParam
        {
            public RenameStatus status;
            public int index;
            public string newName;
        }

        private class RenameMappingProperty
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingEvent
        {
            public RenameStatus status;
            public string signature;
            public string newName;
        }

        private class RenameMappingType
        {
            public RenameStatus status;
            public string oldFullName;
            public string newFullName;

            public Dictionary<string, RenameMappingField> fields = new Dictionary<string, RenameMappingField>();
            public Dictionary<string, RenameMappingMethod> methods = new Dictionary<string, RenameMappingMethod>();
            public Dictionary<string, RenameMappingProperty> properties = new Dictionary<string, RenameMappingProperty>();
            public Dictionary<string, RenameMappingEvent> events = new Dictionary<string, RenameMappingEvent>();
        }

        private class RenameMappingAssembly
        {
            public RenameStatus status;
            public string oldAssName;
            public string newAssName;

            public Dictionary<string, RenameMappingType> types = new Dictionary<string, RenameMappingType>();
        }

        private readonly string _mappingFile;
        private readonly Dictionary<string, RenameMappingAssembly> _assemblies = new Dictionary<string, RenameMappingAssembly>();


        private readonly Dictionary<ModuleDefMD, RenameRecord> _modRenames = new Dictionary<ModuleDefMD, RenameRecord>();
        private readonly Dictionary<TypeDef, RenameRecord> _typeRenames = new Dictionary<TypeDef, RenameRecord>();
        private readonly Dictionary<MethodDef, RenameRecord> _methodRenames = new Dictionary<MethodDef, RenameRecord>();
        private readonly Dictionary<ParamDef, RenameRecord> _paramRenames = new Dictionary<ParamDef, RenameRecord>();
        private readonly Dictionary<FieldDef, RenameRecord> _fieldRenames = new Dictionary<FieldDef, RenameRecord>();
        private readonly Dictionary<PropertyDef, RenameRecord> _propertyRenames = new Dictionary<PropertyDef, RenameRecord>();
        private readonly Dictionary<EventDef, RenameRecord> _eventRenames = new Dictionary<EventDef, RenameRecord>();
        private readonly Dictionary<VirtualMethodGroup, RenameRecord> _virtualMethodGroups = new Dictionary<VirtualMethodGroup, RenameRecord>();


        public RenameRecordMap(string mappingFile)
        {
            _mappingFile = mappingFile;
            LoadXmlMappingFile(mappingFile);
        }


        public void Init(List<ObfuzAssemblyInfo> assemblies)
        {
            foreach (var ObfuzAssemblyInfo in assemblies)
            {
                ModuleDefMD mod = ObfuzAssemblyInfo.module;
                string name = mod.Assembly.Name;
                _modRenames.Add(mod, new RenameRecord
                {
                    status = RenameStatus.NotRenamed,
                    signature = name,
                    oldName = name,
                    newName = null,
                });
                foreach (TypeDef type in mod.GetTypes())
                {
                    _typeRenames.Add(type, new RenameRecord
                    {
                        status = RenameStatus.NotRenamed,
                        signature = type.FullName,
                        oldName = type.FullName,
                        newName = null,
                    });
                    foreach (MethodDef method in type.Methods)
                    {
                        _methodRenames.Add(method, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = TypeSigUtil.ComputeMethodDefSignature(method),
                            oldName = method.Name,
                            newName = null,
                        });
                        foreach (Parameter param in method.Parameters)
                        {
                            if (param.ParamDef != null)
                            {
                                _paramRenames.Add(param.ParamDef, new RenameRecord
                                {
                                    status = RenameStatus.NotRenamed,
                                    signature = param.Name,
                                    oldName = param.Name,
                                    newName = null,
                                });
                            }
                        }
                    }
                    foreach (FieldDef field in type.Fields)
                    {
                        _fieldRenames.Add(field, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = TypeSigUtil.ComputeFieldDefSignature(field),
                            oldName = field.Name,
                            newName = null,
                        });
                    }
                    foreach (PropertyDef property in type.Properties)
                    {
                        _propertyRenames.Add(property, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = TypeSigUtil.ComputePropertyDefSignature(property),
                            oldName = property.Name,
                            newName = null,
                        });
                    }
                    foreach (EventDef eventDef in type.Events)
                    {
                        _eventRenames.Add(eventDef, new RenameRecord
                        {
                            status = RenameStatus.NotRenamed,
                            signature = TypeSigUtil.ComputeEventDefSignature(eventDef),
                            oldName = eventDef.Name,
                            newName = null,
                        });
                    }
                }
            }
        }

        private void LoadXmlMappingFile(string mappingFile)
        {
            if (!File.Exists(mappingFile))
            {
                return;
            }
            var doc = new XmlDocument();
            doc.Load(mappingFile);
            var root = doc.DocumentElement;
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is not XmlElement element)
                {
                    continue;
                }
                LoadAssemblyMapping(element);
            }
        }



        private void LoadAssemblyMapping(XmlElement ele)
        {
            if (ele.Name != "assembly")
            {
                throw new System.Exception($"Invalid node name: {ele.Name}. Expected 'assembly'.");
            }

            var assemblyName = ele.Attributes["name"].Value;
            var newAssemblyName = ele.Attributes["newName"].Value;
            var rma = new RenameMappingAssembly
            {
                oldAssName = assemblyName,
                newAssName = newAssemblyName,
                status = System.Enum.Parse<RenameStatus>(ele.Attributes["status"].Value),
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement element)
                {
                    continue;
                }
                if (element.Name != "type")
                {
                    throw new System.Exception($"Invalid node name: {element.Name}. Expected 'type'.");
                }
                LoadTypeMapping(element, rma);
            }
            _assemblies.Add(assemblyName, rma);
        }

        private void LoadTypeMapping(XmlElement ele, RenameMappingAssembly ass)
        {
            var typeName = ele.Attributes["fullName"].Value;
            var newTypeName = ele.Attributes["newFullName"].Value;
            var rmt = new RenameMappingType
            {
                oldFullName = typeName,
                newFullName = newTypeName,
                status = System.Enum.Parse<RenameStatus>(ele.Attributes["status"].Value),
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement c)
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "field": LoadFieldMapping(c, rmt); break;
                    case "event": LoadEventMapping(c, rmt); break;
                    case "property": LoadPropertyMapping(c, rmt); break;
                    case "method": LoadMethodMapping(c, rmt); break;
                    default: throw new System.Exception($"Invalid node name:{node.Name}");
                }
            }
            ass.types.Add(typeName, rmt);
        }

        private void LoadMethodMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmm = new RenameMappingMethod
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            foreach (XmlNode node in ele.ChildNodes)
            {
                if (node is not XmlElement c)
                {
                    continue;
                }
                switch (node.Name)
                {
                    case "param": LoadMethodParamMapping(c, rmm); break;
                    default: throw new System.Exception($"unknown node name:{node.Name}, expect 'param'");
                }
            }
            type.methods.Add(signature, rmm);
        }

        private void LoadMethodParamMapping(XmlElement ele, RenameMappingMethod method)
        {
            string index = ele.Attributes["index"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmp = new RenameMappingMethodParam
            {
                index = int.Parse(index),
                newName = newName,
                status = RenameStatus.Renamed,
            };
            method.parameters.Add(rmp);
        }

        private void LoadFieldMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmf = new RenameMappingField
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.fields.Add(signature, rmf);
        }

        private void LoadPropertyMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rmp = new RenameMappingProperty
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.properties.Add(signature, rmp);
        }

        private void LoadEventMapping(XmlElement ele, RenameMappingType type)
        {
            string signature = ele.Attributes["signature"].Value;
            string newName = ele.Attributes["newName"].Value;
            var rme = new RenameMappingEvent
            {
                signature = signature,
                newName = newName,
                status = RenameStatus.Renamed,
            };
            type.events.Add(signature, rme);
        }

        public void WriteXmlMappingFile()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("mapping");
            doc.AppendChild(root);
            foreach (var kvp in _modRenames)
            {
                ModuleDefMD mod = kvp.Key;
                RenameRecord record = kvp.Value;
                var assemblyNode = doc.CreateElement("assembly");
                assemblyNode.SetAttribute("name", mod.Assembly.Name);
                assemblyNode.SetAttribute("newName", record.status == RenameStatus.Renamed ? record.newName : "");
                assemblyNode.SetAttribute("status", record.status.ToString());
                foreach (TypeDef type in mod.GetTypes())
                {
                    WriteTypeMapping(assemblyNode, type);
                }
                root.AppendChild(assemblyNode);
            }
            doc.Save(_mappingFile);
            Debug.Log($"Mapping file saved to {Path.GetFullPath(_mappingFile)}");
        }

        private void WriteTypeMapping(XmlElement assNode, TypeDef type)
        {
            _typeRenames.TryGetValue(type, out var record);
            var typeNode = assNode.OwnerDocument.CreateElement("type");
            typeNode.SetAttribute("fullName", record?.signature ?? type.FullName);
            typeNode.SetAttribute("newFullName", record != null && record.status == RenameStatus.Renamed ? record.newName : "");
            typeNode.SetAttribute("status", record != null ? record.status.ToString() : RenameStatus.NotRenamed.ToString());

            foreach (FieldDef field in type.Fields)
            {
                WriteFieldMapping(typeNode, field);
            }
            foreach (PropertyDef property in type.Properties)
            {
                WritePropertyMapping(typeNode, property);
            }
            foreach (EventDef eventDef in type.Events)
            {
                WriteEventMapping(typeNode, eventDef);
            }
            foreach (MethodDef method in type.Methods)
            {
                WriteMethodMapping(typeNode, method);
            }
            if ((record != null && record.status == RenameStatus.Renamed) || typeNode.ChildNodes.Count > 0)
            {
                assNode.AppendChild(typeNode);
            }
        }

        private void WriteFieldMapping(XmlElement typeEle, FieldDef field)
        {
            if (!_fieldRenames.TryGetValue(field, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var fieldNode = typeEle.OwnerDocument.CreateElement("field");
            fieldNode.SetAttribute("signature", record?.signature);
            fieldNode.SetAttribute("newName", record.newName);
            //fieldNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(fieldNode);
        }

        private void WritePropertyMapping(XmlElement typeEle, PropertyDef property)
        {
            if (!_propertyRenames.TryGetValue(property, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var propertyNode = typeEle.OwnerDocument.CreateElement("property");
            propertyNode.SetAttribute("signature", record.signature);
            propertyNode.SetAttribute("newName", record.newName);
            //propertyNode.SetAttribute("status", record.status.ToString());
            typeEle.AppendChild(propertyNode);
        }

        private void WriteEventMapping(XmlElement typeEle, EventDef eventDef)
        {
            if (!_eventRenames.TryGetValue(eventDef, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var eventNode = typeEle.OwnerDocument.CreateElement("event");
            eventNode.SetAttribute("signature", record.signature);
            eventNode.SetAttribute("newName", record.newName);
            typeEle.AppendChild(eventNode);
        }

        private void WriteMethodMapping(XmlElement typeEle, MethodDef method)
        {
            if (!_methodRenames.TryGetValue(method, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var methodNode = typeEle.OwnerDocument.CreateElement("method");
            methodNode.SetAttribute("signature", record.signature);
            methodNode.SetAttribute("newName", record.newName);
            //methodNode.SetAttribute("status", record != null ? record.status.ToString() : RenameStatus.NotRenamed.ToString());
            foreach (Parameter param in method.Parameters)
            {
                if (param.ParamDef != null)
                {
                    WriteMethodParamMapping(methodNode, param.ParamDef);
                }
            }
            typeEle.AppendChild(methodNode);
        }

        private void WriteMethodParamMapping(XmlElement methodEle, ParamDef param)
        {
            if (!_paramRenames.TryGetValue(param, out var record) || record.status == RenameStatus.NotRenamed)
            {
                return;
            }
            var paramNode = methodEle.OwnerDocument.CreateElement("param");
            paramNode.SetAttribute("index", param.Sequence.ToString());
            paramNode.SetAttribute("newName", record.newName);
            //paramNode.SetAttribute("status", record.status.ToString());
            methodEle.AppendChild(paramNode);
        }

        public void AddRename(ModuleDefMD mod, string newName)
        {
            RenameRecord record = _modRenames[mod];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(TypeDef type, string newName)
        {
            RenameRecord record = _typeRenames[type];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(MethodDef method, string newName)
        {
            RenameRecord record = _methodRenames[method];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(ParamDef paramDef, string newName)
        {
            RenameRecord record = _paramRenames[paramDef];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void InitAndAddRename(VirtualMethodGroup methodGroup, string newName)
        {
            RenameRecord methodRecord = _methodRenames[methodGroup.methods[0]];
            _virtualMethodGroups.Add(methodGroup, new RenameRecord
            {
                status = RenameStatus.Renamed,
                signature = methodRecord.signature,
                oldName = methodRecord.oldName,
                newName = newName,
            });
        }

        public bool TryGetRename(VirtualMethodGroup group, out string newName)
        {
            if (_virtualMethodGroups.TryGetValue(group, out var record))
            {
                newName = record.newName;
                return true;
            }
            newName = null;
            return false;
        }

        public void AddRename(FieldDef field, string newName)
        {
            RenameRecord record = _fieldRenames[field];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(PropertyDef property, string newName)
        {
            RenameRecord record = _propertyRenames[property];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }

        public void AddRename(EventDef eventDef, string newName)
        {
            RenameRecord record = _eventRenames[eventDef];
            record.status = RenameStatus.Renamed;
            record.newName = newName;
        }
    }
}
