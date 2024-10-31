using GServer;

using System;

using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using System.Text.Json;
using System.Text.Json.Serialization;

using System.Text.RegularExpressions;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace GService
{
    public class Service
    {
        #region service-members  

        public Guid _Guid { get; set; }
        public string Type { get; private set; }

        [JsonIgnore]
        public string ManagerMachineName { get; set; }
        [JsonIgnore]
        public string ManagerHostname { get; set; }
        [JsonIgnore]
        public int ManagerPort { get; set; }

        #endregion


        #region web-server-members

        public string IpAddress;
        public int Port;
        [JsonIgnore]
        public Server WebServer;

        public List<Regex> RoutesSupported { get; private set; } //тут лежат регулярные выражения
        public List<string> RoutesRequired { get; private set; } //тут url-ы на который будут от сервиса слаться запросы
        [JsonIgnore]
        private ConcurrentDictionary<Guid, Service> Servers = new ConcurrentDictionary<Guid, Service>();
        [JsonIgnore]
        public ConcurrentBag<(Regex, Guid)> AvailableRequiredRoutes { get; private set; }

        #endregion


        #region local-server-members

        public string LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
        [JsonIgnore]
        public Server LocalServer;

        #endregion


        #region constructors

        public Service() { }
        public Service(string type, string managerHostname, int managerPort)
        {
            Type = type;

            WebServer = null;
            LocalServer = null;

            RoutesSupported = new List<Regex>();
            RoutesRequired = new List<string>();
            Servers = new ConcurrentDictionary<Guid, Service>();
            AvailableRequiredRoutes = new ConcurrentBag<(Regex, Guid)>();

            ManagerHostname = managerHostname;
            ManagerPort = managerPort;
        }
        public Service(string type, string localIpAddress, int localPort, string managerHostname, int managerPort, Func<string, int, Task> InitializeService)
        {
            Type = type;

            WebServer = null;

            RoutesSupported = new List<Regex>();
            RoutesRequired = new List<string>();
            Servers = new ConcurrentDictionary<Guid, Service>();
            AvailableRequiredRoutes = new ConcurrentBag<(Regex, Guid)>();

            LocalIpAddress = localIpAddress;
            LocalPort = localPort;
            LocalServer = new Server(LocalIpAddress, LocalPort);

            ManagerHostname = managerHostname;
            ManagerPort = managerPort;

            InitializeService(managerHostname, managerPort);
        }
        public Service(Guid guid, string type, string localIpAddress, int localPort, string managerHostname, int managerPort, Func<string, int, Task> InitializeService)
        {
            _Guid = guid;
            Type = type;

            WebServer = null;

            RoutesSupported = new List<Regex>();
            RoutesRequired = new List<string>();
            Servers = new ConcurrentDictionary<Guid, Service>();
            AvailableRequiredRoutes = new ConcurrentBag<(Regex, Guid)>();

            LocalIpAddress = localIpAddress;
            LocalPort = localPort;
            LocalServer = new Server(LocalIpAddress, LocalPort);

            ManagerHostname = managerHostname;
            ManagerPort = managerPort;

            InitializeService(managerHostname, managerPort);
        }
        public Service(string type, string ipAddress, int port, string localIpAddress, int localPort, string managerHostname, int managerPort, Func<string, int, Task> InitializeService)
        {
            Type = type;


            RoutesSupported = new List<Regex>();
            RoutesRequired = new List<string>();
            Servers = new ConcurrentDictionary<Guid, Service>();
            AvailableRequiredRoutes = new ConcurrentBag<(Regex, Guid)>();

            IpAddress = ipAddress;
            Port = port;
            WebServer = new Server(IpAddress, Port);

            LocalIpAddress = localIpAddress;
            LocalPort = localPort;
            LocalServer = new Server(LocalIpAddress, LocalPort);

            ManagerHostname = managerHostname;
            ManagerPort = managerPort;

            InitializeService(ManagerHostname, ManagerPort);
        }
        public Service(Guid guid, string type, string ipAddress, int port, string localIpAddress, int localPort, string managerHostname, int managerPort, Func<string, int, Task> InitializeService)
        {
            _Guid = guid;
            Type = type;


            RoutesSupported = new List<Regex>();
            RoutesRequired = new List<string>();
            Servers = new ConcurrentDictionary<Guid, Service>();
            AvailableRequiredRoutes = new ConcurrentBag<(Regex, Guid)>();

            IpAddress = ipAddress;
            Port = port;
            WebServer = new Server(IpAddress, Port);

            LocalIpAddress = localIpAddress;
            LocalPort = localPort;
            LocalServer = new Server(LocalIpAddress, LocalPort);

            ManagerHostname = managerHostname;
            ManagerPort = managerPort;

            InitializeService(ManagerHostname, ManagerPort);
        }

        public static void InitializeService(ref Service service)
        {
            string managerHostname = service.ManagerHostname;
            int managerPort = service.ManagerPort;
            using (TcpClient client = new TcpClient())
            {
                string req;
                NetworkStream stream;

                #region /service/create/

                req = "GET /service/create/ HTTP/1.1\r\n\r\n" +
                    Service.SerializeObject(service);

                client.Connect(IPAddress.Parse(managerHostname), managerPort);
                Console.WriteLine($"InitializeService connected to {managerHostname} {managerPort}");

                try
                {
                    stream = client.GetStream();

                    byte[] message = Encoding.UTF8.GetBytes(req);
                    stream.Write(message);
                    stream.Flush();

                    byte[] buffer = new byte[2048];
                    StringBuilder res = new StringBuilder();
                    int bytes = -1;
                    do
                    {
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        res.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
                    } while (bytes > 0);

                    int bodyStartIndex = res.ToString().IndexOf("\r\n\r\n") + 4; // +4 для перехода к началу тела

                    Console.WriteLine(res);
                    Console.WriteLine(res.ToString().Substring(bodyStartIndex));

                    if (bodyStartIndex >= 4)
                    {
                        service._Guid = Guid.Parse(res.ToString().Substring(bodyStartIndex));
                    }
                }
                //catch (AuthenticationException e)
                //{
                //    Console.WriteLine("Authentication failed - closing the connection.");
                //    Console.WriteLine("Exception: {0}", e.Message);
                //    if (e.InnerException != null)
                //        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                //    throw;
                //}
                catch (Exception)
                {
                    throw;
                }

                #endregion


                #region /service/update/
                /*client.Connect(IPAddress.Parse(managerHostname), managerPort);
                Console.WriteLine($"InitializeService connected to {managerHostname} {managerPort}");

                req = "GET /service/update/ HTTP/1.1\r\nHost: example.com\r\n\r\n" +
                    Service.SerializeObject(service);

                try
                {
                    stream = client.GetStream();

                    byte[] message = Encoding.UTF8.GetBytes(req);
                    stream.Write(message);
                    stream.Flush();

                    byte[] buffer = new byte[2048];
                    StringBuilder res = new StringBuilder();
                    int bytes = -1;
                    do
                    {
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        res.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
                    } while (bytes > 0);
                }
                //catch (AuthenticationException e)
                //{
                //    Console.WriteLine("Authentication failed - closing the connection.");
                //    Console.WriteLine("Exception: {0}", e.Message);
                //    if (e.InnerException != null)
                //        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                //    throw;
                //}
                catch (Exception)
                {
                    throw;
                }*/
                #endregion


                #region /services/read/
                client.Connect(IPAddress.Parse(managerHostname), managerPort);
                Console.WriteLine($"InitializeService connected to {managerHostname} {managerPort}");

                req = "GET /services/read/ HTTP/1.1\r\nHost: example.com\r\n\r\n" +
                JsonSerializer.Serialize<string[]>(service.RoutesRequired.ToArray());

                try
                {
                    stream = client.GetStream();

                    byte[] message = Encoding.UTF8.GetBytes(req);
                    stream.Write(message);
                    stream.Flush();

                    byte[] buffer = new byte[2048];
                    StringBuilder res = new StringBuilder();
                    int bytes = -1;
                    do
                    {
                        bytes = stream.Read(buffer, 0, buffer.Length);
                        res.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
                    } while (bytes > 0);

                    int bodyStartIndex = res.ToString().IndexOf("\r\n\r\n") + 4; // +4 для перехода к началу тела

                    if (bodyStartIndex >= 4)
                    {
                        foreach (var item in Service.DeserializeArrayObjects(res.ToString().Substring(bodyStartIndex)))
                        {
                            service.Servers.TryAdd(item._Guid, item);

                            foreach (var item1 in item.WebServer._RouteManager.Routes)
                            {
                                service.AvailableRequiredRoutes.Add((item1.Path, item._Guid));
                            }
                        }
                    }
                }
                //catch (AuthenticationException e)
                //{
                //    Console.WriteLine("Authentication failed - closing the connection.");
                //    Console.WriteLine("Exception: {0}", e.Message);
                //    if (e.InnerException != null)
                //        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                //    throw;
                //}
                catch (Exception)
                {
                    throw;
                }

                //AvailableRequiredRoutes

                #endregion

            }

        }

        #endregion


        #region public-methods

        public void UpdateListRoutesSupported()
        {
            if (WebServer._RouteManager.Routes != null && WebServer._RouteManager.Routes.Count > 0)
                RoutesSupported = WebServer._RouteManager.Routes.Select(route => route.Path).ToList();
        }

        #endregion


        #region private-methods

        private void StartServer(CancellationToken token)
        {
            if (LocalServer != null)
                LocalServer.SatartServer(token);
        }
        private void StartLocalServer(CancellationToken token)
        {
            if (WebServer != null)
                WebServer.SatartServer(token);
        }

        #endregion


        #region severization-methods

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


/*пример привязки сертификата к порту
 
static void Maiin(){
 string certificatePath = "path/to/your/certificate.pfx";
        string certificatePassword = "your_certificate_password"; // Если требуется

        // Загрузка сертификата
        X509Certificate2 certificate = new X509Certificate2(certificatePath, certificatePassword);

        // Создание и настройка HttpListener
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("https://localhost:5001/"); // Укажите ваш URL и порт

        // Привязка сертификата к порту
        listener.Start();
        
        // Привязка сертификата (требуется запуск от имени администратора)
        AddCertificateToPort(certificate, 5001);
        
        Console.WriteLine("Listening...");
        
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            // Обработка запроса...
            context.Response.StatusCode = 200;
            context.Response.Close();
        }
    }

    private static void AddCertificateToPort(X509Certificate2 certificate, int Port)
    {
        var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        
        try
        {
            store.Add(certificate);
            // Используйте netsh для привязки сертификата к порту
            string command = $"netsh http add sslcert ipport=0.0.0.0:{Port} certhash={certificate.GetCertHashString()} appid={{YOUR-APP-ID}}";
            System.Diagnostics.Process.Start("cmd.exe", "/C " + command);
        }
        finally
        {
            store.Close();
        }
    }
 
 */