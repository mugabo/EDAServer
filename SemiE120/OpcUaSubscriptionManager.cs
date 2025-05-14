// OpcUaSubscriptionManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Opc.Ua;
using Opc.Ua.Client;

namespace SemiE120.OpcUaIntegration
{
    public class OpcUaSubscriptionManager
    {
        private readonly OpcUaClient _client;
        private readonly OpcUaToCemMapper _mapper;
        private readonly SemiE120.CEM.Equipment _equipment;
        private Subscription _subscription;
        private readonly Dictionary<uint, NodeId> _monitoredItemsMap = new Dictionary<uint, NodeId>();

        public event EventHandler<MonitoredItemNotificationEventArgs> DataChanged;

        public OpcUaSubscriptionManager(OpcUaClient client, OpcUaToCemMapper mapper, SemiE120.CEM.Equipment equipment)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public void CreateSubscription(int publishingInterval = 1000)
        {
            if (!_client.IsConnected)
                throw new InvalidOperationException("OPC UA 서버에 연결되어 있지 않습니다.");

            var session = _client.GetSession();

            // 구독 생성
            _subscription = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = publishingInterval,
                PublishingEnabled = true,
                Priority = 100,
                KeepAliveCount = 10,
                LifetimeCount = 1000,
                MaxNotificationsPerPublish = 1000
            };

            session.AddSubscription(_subscription);
            _subscription.Create();
        }

        public MonitoredItem AddMonitoredItem(NodeId nodeId, int samplingInterval = 1000)
        {
            if (_subscription == null)
                throw new InvalidOperationException("먼저 구독을 생성해야 합니다.");

            var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
            {
                StartNodeId = nodeId,
                AttributeId = Attributes.Value,
                DisplayName = nodeId.ToString(),
                SamplingInterval = samplingInterval,
                QueueSize = 1, // 최신 값만 유지
                DiscardOldest = true,
            };

            monitoredItem.Notification += OnMonitoredItemNotification;
            _subscription.AddItem(monitoredItem);
            _subscription.ApplyChanges();

            _monitoredItemsMap[monitoredItem.ClientHandle] = nodeId;

            return monitoredItem;
        }

        public void AddMonitoredItems(IEnumerable<NodeId> nodeIds, int samplingInterval = 1000)
        {
            foreach (var nodeId in nodeIds)
            {
                AddMonitoredItem(nodeId, samplingInterval);
            }
        }

        public void RemoveMonitoredItem(MonitoredItem item)
        {
            if (_subscription == null)
                return;

            item.Notification -= OnMonitoredItemNotification;
            _subscription.RemoveItem(item);
            _subscription.ApplyChanges();

            _monitoredItemsMap.Remove(item.ClientHandle);
        }

        public void RemoveAllMonitoredItems()
        {
            if (_subscription == null)
                return;

            var items = _subscription.MonitoredItems.ToList();
            foreach (var item in items)
            {
                item.Notification -= OnMonitoredItemNotification;
            }

            _subscription.RemoveItems(_subscription.MonitoredItems);
            _subscription.ApplyChanges();
            _monitoredItemsMap.Clear();
        }

        private void OnMonitoredItemNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            if (!_monitoredItemsMap.TryGetValue(monitoredItem.ClientHandle, out var nodeId))
                return;

            // 데이터 변경 이벤트 발생
            DataChanged?.Invoke(monitoredItem, e);

            // 모든 데이터 변경 사항을 CEM 모델에 반영
            foreach (var value in e.NotificationValue as MonitoredItemNotificationCollection)
            {
                if (value is MonitoredItemNotification notification)
                {
                    _mapper.UpdateCemModelFromOpcUa(_equipment, nodeId, notification.Value);
                }
            }
        }

        public void Dispose()
        {
            if (_subscription != null)
            {
                RemoveAllMonitoredItems();
                _subscription.Delete(true);
                _subscription = null;
            }
            _monitoredItemsMap.Clear();
        }
    }
}