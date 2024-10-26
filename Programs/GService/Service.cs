using System;

using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;



namespace GService
{
    public class Service
    {

        #region public-members   
        public Guid guid { get; private set; }
        public string type { get; private set; }
/* 
        [JsonIgnore]
        public IPAddress managerIpAddress { get; set; }
        [JsonIgnore]
        public int managerPort { get; set; } */

        //поля http серверва
        public string ipAddress;
        public int port;
        public HttpListener _HttpListener;
        //private Func<HttpRequest, Task<HttpResponse>> OptionsRoute = null;







        //поля для служебного сервера
        /* public IPAddress localIpAddress { get; set; } 
       public int localPort  { get; set; }

        public List<string> RoutesSupported { get; private set; } //тут лежат регулярные выражения 
        public List<string> RoutesRequired { get; private set; } //тут можно класть как регулярки так и просто метод + url
 */

        #endregion

        #region private-members 
        ////поля внешнего серверва
        //private TcpListener tcpListener;
        //private readonly EventWaitHandle Terminator = new EventWaitHandle(false, EventResetMode.ManualReset);
        //private CancellationTokenSource TokenSource;
        //private CancellationToken Token;
        //поля для служебного сервера
        /* private TcpListener localTcpListener;
        private readonly EventWaitHandle TerminatorLocalServer = new EventWaitHandle(false, EventResetMode.ManualReset);
        private CancellationTokenSource TokenSourceLocalServer;
        private CancellationToken TokenLocalServer; */
        #endregion

        #region Constructors
        public Service() { }



        private CancellationTokenSource _TokenSource;
        private CancellationToken _Token;


        public Service(string _ipAddress, int _port){
            ipAddress = _ipAddress;
            port = _port;

            



            _TokenSource = new CancellationTokenSource();
            _Token = _TokenSource.Token;
            Task.Run(() => StartServer(_Token), _Token);

            
        }

        /* public Service(string _type, IPAddress _managerIpAddress, int _managerPort, IPAddress _localIpAddress, int _localPort, List<string> _RoutesSupported, List<string> _RoutesRequired)
        {
            //ipAddress = _ipAddress;
            //port = _port;

            type = _type;

            localIpAddress = _localIpAddress;
            localPort = _localPort;

            RoutesSupported = _RoutesSupported;
            RoutesRequired = _RoutesRequired;

            managerIpAddress = _managerIpAddress;
            managerPort = _managerPort;

            InitializeService();

            //запуск служебного сервера
            localTcpListener = new TcpListener(localIpAddress, localPort);
            TokenSourceLocalServer = new CancellationTokenSource();
            TokenLocalServer = TokenSourceLocalServer.Token;
            //Task.Run(() => StartLocalServer(TokenLocalServer), TokenLocalServer);

            //запуск внешнего сервера
        } */
        #endregion



        private void StartServer(CancellationToken token)
        {
            Task.Run(() => AcceptConnections(token), token);
        }

        private void AcceptConnections(CancellationToken token)
        {
            try
            {
                _HttpListener.Prefixes.Add($"https://{ipAddress}:{port}/");
                _HttpListener.Start();
                
                while (_HttpListener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        if (token.IsCancellationRequested) throw new OperationCanceledException();

                        var context = c as HttpListenerContext;

                        HttpRequest req = new HttpRequest(context);
                            

                        // // Process OPTIONS request
                        // if (req.Method == HttpMethod.OPTIONS && OptionsRoute != null)
                        // { 
                        //     OptionsRoute(req);
                        //     return;
                        // }
                        // Send to handler
                        Task.Run(() =>
                        {
                            HttpResponse resp = null;
                            Func<HttpRequest, HttpResponse> handler = null;

                            // Check content routes
                            if (req.Method == HttpMethod.GET || req.Method == HttpMethod.HEAD)
                            { 
                                if (Routes.Exists(req.RawUrlWithoutQuery, RouteType.Content))
                                    resp = _ContentRouteProcessor.Process(req);
                            }

                            // Check static routes
                            if (resp == null)
                            {
                                handler = StaticRoutes.Match(req.Method, req.RawUrlWithoutQuery);
                                if (handler != null) 
                                    resp = handler(req); 
                                else
                                {
                                    // Check dynamic routes
                                    handler = DynamicRoutes.Match(req.Method, req.RawUrlWithoutQuery);
                                    if (handler != null)
                                        resp = handler(req);
                                    else
                                    {
                                        // Use default route
                                        resp = DefaultRouteProcessor(context, req);
                                    }
                                }
                            }
                
                        });





                    }, _HttpListener.GetContext());
                }
            }
            catch (System.Exception)
            {
                throw;
            }
        }






















        private void InitializeService()
        {

        }
        









        #region Severization-methods
        
        public static string SerializeObject(Service service, bool pretty = true)
        {
            return JsonSerializer.Serialize(service, new JsonSerializerOptions { WriteIndented = pretty });
        }
        public static Service DeserializeObject(string json)
        {
            return JsonSerializer.Deserialize<Service>(json);
        }

        public static string SerializeArrayObjects(Service[] services, bool pretty = true)
        {
            return JsonSerializer.Serialize(services, new JsonSerializerOptions { WriteIndented = pretty });
        }
        public static Service[] DeserializeArrayObjects(string json)
        {
            return JsonSerializer.Deserialize<Service[]>(json);
        }

        #endregion
    }
}
