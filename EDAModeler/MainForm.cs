using SemiE120.CEM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDAModeler
{
    public partial class MainForm : DevExpress.XtraEditors.XtraForm
    {
        Equipment rootEquipment = null;
        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CreateSampleData();

            //treeListCEM.DataSource;
        }

        private void CreateSampleData()
        {
            // 루트 장비 생성
            rootEquipment = new Equipment
            {
                Name = "Main Equipment",
                Description = "Sample equipment model",
                ElementType = "Process"
            };

            // 모듈 추가
            Module module1 = new Module
            {
                Name = "Process Chamber 1",
                Description = "Main process chamber",
                ElementType = "Process Area"
            };
            rootEquipment.Modules.Add(module1);

            // 서브시스템 추가
            Subsystem subsystem1 = new Subsystem
            {
                Name = "Gas System",
                Description = "Gas distribution system",
                ElementType = "Gas Distributor"
            };
            rootEquipment.Subsystems.Add(subsystem1);

            // IO 디바이스 추가
            IODevice ioDevice1 = new IODevice
            {
                Name = "Temperature Sensor",
                Description = "Main chamber temperature sensor"
            };
            module1.IODevices.Add(ioDevice1);

            // 머티리얼 로케이션 추가
            MaterialLocation materialLocation1 = new MaterialLocation
            {
                Name = "Wafer Position 1",
                Description = "Wafer processing position",
                //MaterialType = MaterialType.Substrate
            };
            module1.MaterialLocations.Add(materialLocation1);

            RefreshTreeList();
        }

        private void RefreshTreeList()
        {
            // 트리 리스트 데이터 소스 초기화
            List<CEMTreeListItem> treeItems = new List<CEMTreeListItem>();

            // 루트 장비 추가
            AddEquipmentToList(rootEquipment, null, treeItems);

            // 데이터 바인딩
            treeListCEM.DataSource = treeItems;
            treeListCEM.ExpandAll();
        }
        private void AddEquipmentToList(Equipment equipment, string parentUid, List<CEMTreeListItem> items)
        {
            if (equipment == null || items == null) return;

            string equipmentUid = equipment.Uid;

            // 여기서는 장비 자체를 추가하지 않음 (이미 RefreshTreeList에서 추가됨)
            // 자식 요소만 추가

            // 모듈 추가
            if (equipment.Modules != null)
            {
                foreach (Module module in equipment.Modules)
                {
                    if (module != null)
                    {
                        items.Add(new CEMTreeListItem(module, equipmentUid));
                        AddModuleToList(module, module.Uid, items);
                    }
                }
            }

            // 서브시스템 추가
            if (equipment.Subsystems != null)
            {
                foreach (Subsystem subsystem in equipment.Subsystems)
                {
                    if (subsystem != null)
                    {
                        items.Add(new CEMTreeListItem(subsystem, equipmentUid));
                        AddSubsystemToList(subsystem, subsystem.Uid, items);
                    }
                }
            }

            // 다른 콜렉션들도 동일하게 null 체크 추가...
        }
        private void AddModuleToList(Module module, string parentUid, List<CEMTreeListItem> items)
        {
            string moduleUid = module.Uid;
            items.Add(new CEMTreeListItem(module, parentUid));

            // 하위 모듈 추가
            foreach (Module childModule in module.Modules)
            {
                AddModuleToList(childModule, moduleUid, items);
            }

            // 서브시스템 추가
            foreach (Subsystem subsystem in module.Subsystems)
            {
                AddSubsystemToList(subsystem, moduleUid, items);
            }

            // IO 디바이스 추가
            foreach (IODevice ioDevice in module.IODevices)
            {
                items.Add(new CEMTreeListItem(ioDevice, moduleUid));
            }

            // 머티리얼 로케이션 추가
            foreach (MaterialLocation location in module.MaterialLocations)
            {
                items.Add(new CEMTreeListItem(location, moduleUid));
            }
        }

        private void AddSubsystemToList(Subsystem subsystem, string parentUid, List<CEMTreeListItem> items)
        {
            string subsystemUid = subsystem.Uid;
            items.Add(new CEMTreeListItem(subsystem, parentUid));

            // 하위 서브시스템 추가
            foreach (Subsystem childSubsystem in subsystem.Subsystems)
            {
                AddSubsystemToList(childSubsystem, subsystemUid, items);
            }

            // IO 디바이스 추가
            foreach (IODevice ioDevice in subsystem.IODevices)
            {
                items.Add(new CEMTreeListItem(ioDevice, subsystemUid));
            }

            // 머티리얼 로케이션 추가
            foreach (MaterialLocation location in subsystem.MaterialLocations)
            {
                items.Add(new CEMTreeListItem(location, subsystemUid));
            }
        }
    }
}
