// OpcUaClient.cs
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace SemiE120.OpcUaIntegration
{
    public class OpcUaClient : IDisposable
    {
        private Session _session;
        private SessionReconnectHandler _reconnectHandler;
        private const int ReconnectPeriod = 10000;
        private Subscription _subscription;

        public event EventHandler<ConnectionStatusEventArgs> ConnectionStatusChanged;

        public bool IsConnected => _session != null && _session.Connected;

        public Session GetSession() { return _session; }

        public async Task<bool> ConnectAsync(string endpointUrl, bool useSecurity = false)
        {
            try
            {
                // 인증서 설정
                //var config = new ApplicationConfiguration
                //{
                //    ApplicationName = "SemiE120 OPC UA Client",
                //    ApplicationUri = Utils.Format(@"urn:{0}:SemiE120OpcUaClient", System.Net.Dns.GetHostName()),
                //    ApplicationType = ApplicationType.Client,
                //    SecurityConfiguration = new SecurityConfiguration
                //    {
                //        ApplicationCertificate = new CertificateIdentifier(),
                //        TrustedPeerCertificates = new CertificateTrustList(),
                //        TrustedIssuerCertificates = new CertificateTrustList(),
                //        RejectedCertificateStore = new CertificateTrustList(),
                //        AutoAcceptUntrustedCertificates = true,
                //        AddAppCertToTrustedStore = true,
                //        RejectSHA1SignedCertificates = false,    // SHA-1 서명 인증서 허용
                //        MinimumCertificateKeySize = 1024,        // 최소 키 크기 낮춤
                //    },
                //    TransportConfigurations = new TransportConfigurationCollection(),
                //    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                //    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
                //};

                //await config.Validate(ApplicationType.Client);

                //// 인증서 생성
                //var application = new ApplicationInstance
                //{
                //    ApplicationName = "SemiE120 OPC UA Client",
                //    ApplicationType = ApplicationType.Client,
                //    ApplicationConfiguration = config
                //};

                //await application.CheckApplicationInstanceCertificates(false);

                ApplicationInstance application = new ApplicationInstance();
                application.ApplicationName = "SemiE120 OPC UA Client";
                application.ApplicationType = ApplicationType.Client;
                application.ConfigSectionName = "EDAServer.UAClient";

                var certOK = application.CheckApplicationInstanceCertificates(false).Result;
                if (!certOK)
                {
                    throw new Exception("Application instance certificate invalid!");
                }

                // 서버 엔드포인트 탐색
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(application.ApplicationConfiguration, endpointUrl, useSecurity);
                selectedEndpoint.SecurityMode = MessageSecurityMode.None;
                var endpointConfiguration = EndpointConfiguration.Create(application.ApplicationConfiguration);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                // 세션 생성
                var session = await Session.Create(
                    application.ApplicationConfiguration,
                    endpoint,
                    false,
                    application.ApplicationConfiguration.ApplicationName,
                    60000,
                    new UserIdentity(new AnonymousIdentityToken()),
                    null);

                // 세션 저장 및 이벤트 핸들러 설정
                _session = session;
                _session.KeepAlive += Session_KeepAlive;

                // 성공적으로 연결됨
                OnConnectionStatusChanged(true, "연결됨");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC UA 서버 연결 실패: {ex.Message}");
                OnConnectionStatusChanged(false, $"연결 실패: {ex.Message}");
                return false;
            }
        }

        private void Session_KeepAlive(ISession session, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                // 연결 끊김
                OnConnectionStatusChanged(false, e.Status.ToString());

                if (_reconnectHandler == null)
                {
                    _reconnectHandler = new SessionReconnectHandler();
                    _reconnectHandler.BeginReconnect(_session, ReconnectPeriod, ReconnectComplete);
                }
            }
        }

        private void ReconnectComplete(object sender, EventArgs e)
        {
            // 재연결 완료
            if (_reconnectHandler != null)
            {
                _session = (Session)_reconnectHandler.Session;
                _reconnectHandler.Dispose();
                _reconnectHandler = null;

                OnConnectionStatusChanged(true, "재연결됨");
            }
        }

        public void Dispose()
        {
            if (_subscription != null)
            {
                _subscription.Delete(true);
                _subscription = null;
            }

            if (_session != null)
            {
                _session.Close();
                _session.Dispose();
                _session = null;
            }
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs(connected, message));
        }

        // 노드 읽기 메서드
        public DataValue ReadNode(NodeId nodeId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("OPC UA 서버에 연결되어 있지 않습니다.");

            return _session.ReadValue(nodeId);
        }

        // 노드 쓰기 메서드
        public StatusCode WriteNode(NodeId nodeId, object value)
        {
            if (!IsConnected)
                throw new InvalidOperationException("OPC UA 서버에 연결되어 있지 않습니다.");

            return _session.Write(
                null,
                new WriteValueCollection
                {
                    new WriteValue
                    {
                        NodeId = nodeId,
                        AttributeId = Attributes.Value,
                        Value = new DataValue(new Variant(value))
                    }
                },
                out _,
                out var results
            ).ServiceResult;
        }

        // 노드 브라우즈 메서드
        public ReferenceDescriptionCollection Browse(NodeId nodeId)
        {
            if (!IsConnected)
                throw new InvalidOperationException("OPC UA 서버에 연결되어 있지 않습니다.");

            return _session.FetchReferences(nodeId);
        }

        // 노드 트리 분석 메서드
        public List<ReferenceDescription> BrowseTree(NodeId startNodeId)
        {
            var result = new List<ReferenceDescription>();
            BrowseRecursive(startNodeId, result, 0);
            return result;
        }

        private void BrowseRecursive(NodeId nodeId, List<ReferenceDescription> results, int level, int maxLevel = 2)
        {
            if (level > maxLevel) return;

            try
            {
                var references = Browse(nodeId);
                foreach (var reference in references)
                {
                    results.Add(reference);

                    if (reference.NodeId.IsAbsolute) continue;

                    var childNodeId = new NodeId((NodeId)reference.NodeId);
                    BrowseRecursive(childNodeId, results, level + 1, maxLevel);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"브라우즈 오류: {ex.Message}");
            }
        }
    }

    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool Connected { get; }
        public string Message { get; }

        public ConnectionStatusEventArgs(bool connected, string message)
        {
            Connected = connected;
            Message = message;
        }
    }
}