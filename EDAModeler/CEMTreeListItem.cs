using SemiE120.CEM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDAModeler
{
    internal class CEMTreeListItem
    {
        public string Uid { get; set; }
        public string ParentUid { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public object Object { get; set; }

        public CEMTreeListItem(Nameable nameable, string parentUid)
        {
            Uid = nameable.Uid;
            ParentUid = parentUid;
            Name = nameable.Name;
            Description = nameable.Description;
            Object = nameable;

            // 객체 유형 설정
            if (nameable is Equipment)
                Type = "Equipment";
            else if (nameable is Module)
                Type = "Module";
            else if (nameable is Subsystem)
                Type = "Subsystem";
            else if (nameable is IODevice)
                Type = "IODevice";
            else if (nameable is MaterialLocation)
                Type = "MaterialLocation";
            else if (nameable is LogicalElement)
                Type = "LogicalElement";
        }
    }
}
