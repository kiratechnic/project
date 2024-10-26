using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;


using GService;


using System.Text.RegularExpressions;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;


namespace Manager
{
    internal class Manager
    {

        #region публик-поля

        public Server server;

        #endregion

        #region приват-поля
        //локальный порт для запросов от сервисов 

        private bool ssl = true;
        private static string localHostname;
        private static int localPort;

        private static ConcurrentDictionary<Guid, Service> services = new ConcurrentDictionary<Guid, Service>();
        private static ConcurrentDictionary<Guid, bool> serverStatus = new ConcurrentDictionary<Guid, bool>();

        private static ConcurrentQueue<(Guid, bool)> updateServerStatus = new ConcurrentQueue<(Guid, bool)>();//guid сервиса и enum StatusCodeService его новый статус

        //private readonly EventWaitHandle Terminator = new EventWaitHandle(false, EventResetMode.ManualReset);
        private CancellationTokenSource TokenSource;
        private CancellationToken Token;

        #endregion

        #region конструкторы
        public Manager(string hostname, int port, Func<HttpRequest, HttpResponse> defaultRequestHandler)
        {


            localHostname = hostname;
            localPort = port;


            //старт сервера



            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            Task.Run(() => TaskManager(Token), Token);


        }






        #endregion






        public static async Task TaskManager(CancellationToken token)
        {
            //debug
            string taskName = "task1";
            string thisMetod = "task1";

            Encoding encoding = Encoding.UTF8;

            (Guid, bool) serviceStatus;
            Guid[] guidСonsumerServices;

            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException();




                    while (updateServerStatus.IsEmpty)
                    {
                        Thread.Sleep(1);
                    }
                    while (!updateServerStatus.TryDequeue(out serviceStatus))
                    {
                        if (serviceStatus.Item2)
                        {
                            guidСonsumerServices = FindConsumers(serviceStatus.Item1);

                            if (guidСonsumerServices.Length > 0)//debug вместо копирывания засунть все в функцию и функцию вызывать от сюда с делеагтом /хз че написал:)
                            {
                                try
                                {
                                    string jsonObj = ServiceSerializerClass.SerializeObject(services[serviceStatus.Item1]);
                                    string request = "GET /service/v1/service-available/ HTTP/1.1\r\nHost: example.com\r\n" +
                                        $"Content-Length: {encoding.GetBytes(jsonObj).Length}\r\n" +
                                        "\r\n" +
                                        jsonObj;
                                    tmpNameFunc2(guidСonsumerServices, request, encoding);
                                }
                                catch (Exception e)
                                {//debug
                                    Console.WriteLine($"{taskName} метод-\"{thisMetod}\": {e.Message}/");
                                }
                            }
                        }
                        else
                        {

                            guidСonsumerServices = FindConsumers(serviceStatus.Item1);


                            if (guidСonsumerServices.Length > 0)//debug вместо копирывания засунть все в функцию и функцию вызывать от сюда с делеагтом /хз че написал:)
                            {
                                try
                                {
                                    string jsonObj = ServiceSerializerClass.SerializeObject(services[serviceStatus.Item1]);
                                    string request = "GET /service/v1/service-inaccessible/ HTTP/1.1\r\nHost: example.com\r\n" +
                                        $"Content-Length: {encoding.GetBytes(jsonObj).Length}\r\n" +
                                        "\r\n" +
                                        jsonObj;
                                    tmpNameFunc2(guidСonsumerServices, request, encoding);
                                    services.TryRemove(serviceStatus.Item1, out ServiceSerializerClass removedService);
                                }
                                catch (Exception e)
                                {//debug

                                    services.TryRemove(serviceStatus.Item1, out ServiceSerializerClass removedService);
                                    Console.WriteLine($"{taskName} метод-\"{thisMetod}\": {e.Message}/");
                                }
                            }

                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception)
            {

                throw;
            }

        }


        private static Guid[] FindConsumers(Guid guidService)
        {
            var servicesProvided = (services[guidService].RoutesSupported).ToArray();

            List<Guid> matchedServiceGuids = new List<Guid>();//сервисы котор

            foreach (var service in services)
            {
                Guid serviceGuid = service.Key;
                List<string> requiredHeaders = service.Value.RoutesRequired;

                // Проверяем, есть ли хотя бы один требуемый ___ в поддерживаемых
                if (requiredHeaders.Any(requiredHeader => servicesProvided.Contains(requiredHeader)))
                {
                    matchedServiceGuids.Add(serviceGuid);
                }
            }
            return matchedServiceGuids.ToArray();
        }
        private static void tmpNameFunc2(Guid[] guidСonsumerServices, string request, Encoding encoding)
        {
            string thisMetod = "tmpNameFunc2";

            foreach (var item in guidСonsumerServices)
            {
                if (services.ContainsKey(item))
                {
                    try
                    {
                        using (TcpClient manager = new TcpClient())
                        {
                            manager.Connect(services[item].localIpAddress, services[item].localInputPort);
                            NetworkStream stream = manager.GetStream();

                            stream.Write(encoding.GetBytes(request));

                            //debug1 отдельно написать функцию которая читает ответ и на основе него либо просто закрывается либо выбрасывает исключение
                            string response = ReadFullHttpRequestOrResponse(stream);

                            tmpNameFunc1(response, new string[] { "200" });//debug
                        }
                    }
                    catch (Exception e)
                    {//debug
                        Console.WriteLine($"метод-\"{thisMetod}\": {e.Message}/");
                    }
                }
            }
        }


        //запросы которые может обработать manager
        private static HttpResponse ManagerGiveMeGuid(HttpRequest req)
        {
            Guid guid;

            while (true)//костыль
            {
                guid = Guid.NewGuid();
                if (services.TryAdd(guid, new Service()))
                    break;
            }

            HttpResponse response = new HttpResponse();

            response.ProtocolVersion = "HTTP/1.1";
            response.StatusCode = 201;
            response.ContentType = "application/json";

            response.DataStream = null;
            response.Data = null;
            if (!string.IsNullOrEmpty(guid.ToString())) response.Data = Encoding.UTF8.GetBytes(guid.ToString());
            if (response.Data != null && response.Data.Length > 0) response.ContentLength = response.Data.Length;

            return response;

        }
        private static HttpResponse ManagerAddMe(HttpRequest request)
        {
            string jsonObj;
            HttpResponse response;

            if (request.Data.CanRead && request.Data.Length > 0)
            {
                jsonObj = Encoding.UTF8.GetString(request.Data.ToArray());
            }
            else
            {
                response = new HttpResponse();
                response.ProtocolVersion = "HTTP/1.1";
                response.StatusCode = 400;

                return response;
            }


            var service = Service.DeserializeObject(jsonObj);
            services[service.guid] = service;

            //выставить в очередь чтоб оповестить остальных которым он нужен об добавление нового сервиса
            updateServerStatus.Enqueue((service.guid, true));


            response = new HttpResponse();
            response.ProtocolVersion = "HTTP/1.1";
            response.StatusCode = 200;

            return response;
        }
        private static HttpResponse ManagerDeleteMe(HttpRequest request)
        {
            Guid guid;
            HttpResponse response;


            if (request.Data.CanRead && request.Data.Length > 0)
            {
                guid = Guid.Parse(Encoding.UTF8.GetString(request.Data.ToArray()));
            }
            else
            {
                response = new HttpResponse();
                response.ProtocolVersion = "HTTP/1.1";
                response.StatusCode = 400;

                return response;
            }


            //выставить в очередь чтоб оповестить остальных которым он нужен об добавление нового сервиса
            updateServerStatus.Enqueue((guid, true));

            response = new HttpResponse();
            response.ProtocolVersion = "HTTP/1.1";
            response.StatusCode = 200;

            return response;
        }
        private static HttpResponse ManagerServices(HttpRequest request)
        {
            string jsonObj = "";

            if (services.Count > 0)
            {
                jsonObj = Service.SerializeObjects(services.Values.ToArray());
            }

            HttpResponse response = new HttpResponse();

            response.ProtocolVersion = "HTTP/1.1";
            response.StatusCode = 200;
            response.ContentType = "application/json";

            response.DataStream = null;
            response.Data = null;

            response.Data = Encoding.UTF8.GetBytes(jsonObj);
            if (response.Data != null) response.ContentLength = response.Data.Length;

            return new HttpResponse();
        }
    }
}
