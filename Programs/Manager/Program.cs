using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using static System.Runtime.InteropServices.JavaScript.JSType;


using System.Collections.Concurrent;

using GServer;
using GService;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.Text.Json;

namespace Manager
{
    internal class Program
    {
        private bool ssl = true;
        private static string Hostname;
        private static int Port;

        private static ConcurrentDictionary<Guid, Service> Servers = new ConcurrentDictionary<Guid, Service>();
        private static ConcurrentDictionary<Guid, bool> ServersStatus = new ConcurrentDictionary<Guid, bool>();
        private static ConcurrentDictionary<Regex, Guid> SupportedServersRoutes = new ConcurrentDictionary<Regex, Guid>();

        private static ConcurrentQueue<(Guid, bool)> UpdateServerStatus = new ConcurrentQueue<(Guid, bool)>();//guid сервиса и enum StatusCodeService его новый статус


        private static CancellationTokenSource TokenSource;
        private static CancellationToken Token;


        static void Main(string[] args)
        {
            Hostname = "localhost";
            Port = 8890;

            Server s = new Server(Hostname, Port);

            s._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/service/create/", service_create));
            s._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/service/update/", service_update));
            //s._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/service/delete/", service_delete));
            s._RouteManager.Add(Route.BuildStaticRoute(HttpMethod.GET, "/services/read/", services_read));

            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            s.SatartServer(Token);



            Console.ReadLine();
            Token.ThrowIfCancellationRequested();
        }

        static HttpResponse service_create(HttpRequest req)
        {
            Service service;
            try
            {
                string jsonObj = Encoding.UTF8.GetString(req.Data.ToArray());
                service = Service.DeserializeObject(jsonObj);
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }

            Guid guid = Guid.Empty;


            int numberOfAttemptsAddNewServerId = 10;
            for (int i = 0; i < numberOfAttemptsAddNewServerId; i++)
            {
                guid = Guid.NewGuid();
                service._Guid = Guid.NewGuid();
                if (Servers.TryAdd(service._Guid, service))
                    break;
                else if (i == (numberOfAttemptsAddNewServerId - 1))
                    return new HttpResponse(req, 503, null);
            }

            //добавление в очередь на рассылку
            UpdateServerStatus.Enqueue((service._Guid, true));

            return new HttpResponse(req, 201, null, req.ContentType, Encoding.UTF8.GetBytes(guid.ToString()));
        }
        static HttpResponse service_update(HttpRequest req)
        {
            Service service;
            try
            {
                string jsonObj = Encoding.UTF8.GetString(req.Data.ToArray());
                service = Service.DeserializeObject(jsonObj);
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }

            if (Servers.TryGetValue(service._Guid, out var currentService))
            {
                if (Servers.TryUpdate(service._Guid, service, currentService))
                {
                    //добавление в очередь на рассылку
                    UpdateServerStatus.Enqueue((service._Guid, true));

                    return new HttpResponse(req, 204, null);
                }
            }
            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes($"Service with ID {service._Guid} does not exist."));
        }
        static HttpResponse service_delete(HttpRequest req)
        {
            Service service;
            try
            {
                string jsonObj = Encoding.UTF8.GetString(req.Data.ToArray());
                service = Service.DeserializeObject(jsonObj);
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }

            if (Servers.TryGetValue(service._Guid, out var removedService))//решить вопрос с рассыкой инфы об изменениях, пока что просто удаление
            {
                //добавление в очередь на рассылку
                //UpdateServerStatus.Enqueue((service._Guid, false));

                return new HttpResponse(req, 204, null);
            }
            return new HttpResponse(req, 400, null, req.ContentType, Encoding.UTF8.GetBytes($"Service with ID {service._Guid} does not exist."));
        }
        static HttpResponse services_read(HttpRequest req)
        {
            string[] routes;
            try
            {
                routes = JsonSerializer.Deserialize<string[]>(Encoding.UTF8.GetString(req.Data.ToArray()));
            }
            catch (Exception)
            {
                return new HttpResponse(req, 400, null);
            }

            List<Service> matchedServiceGuids = new List<Service>();

            foreach (var item in Servers.Values)
            {
                if (routes.Any(route => item.RoutesSupported.Any(regex => regex.IsMatch(route))))
                {
                    matchedServiceGuids.Add(item);
                }
            }

            if (matchedServiceGuids.Count > 0)
            {
                return new HttpResponse(req, 200, null, req.ContentType, Encoding.UTF8.GetBytes(Service.SerializeArrayObjects(matchedServiceGuids.ToArray())) );
            }
            else
            {
                return new HttpResponse(req, 200, null);
            }
            //return new HttpResponse(req, 500, null);
        }
    }
}