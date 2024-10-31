using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace GServer
{
    public class Server
    {
        #region public-members

        public RouteManager _RouteManager { get; set; }
        //public Func<HttpRequest, HttpResponse> DefaultRoute { get; private set; } //DefaultRouteProcessor

        public int StreamReadBufferSize { get; private set; } = 1024;
        public string Hostname { get; private set; }
        //public string MachineName { get; private set; }
        public int Port { get; private set; }

        #endregion


        #region private-members

        private HttpListener _HttpListener;

        #endregion


        #region constructors

        public Server() { }
        public Server(string hostname, int port)
        {
            this.Hostname = hostname;
            this.Port = port;

            _HttpListener = new HttpListener();
            _RouteManager = new RouteManager();
        }
        public Server(string hostname, int port, Func<HttpRequest, HttpResponse> defaultRoute)
        {
            this.Hostname = hostname;
            this.Port = port;

            _HttpListener = new HttpListener();
            _RouteManager = new RouteManager();

            //DefaultRoute = defaultRoute;
            _RouteManager.Add(Route.BuildDefaultRoute(HttpMethod.GET, "/", defaultRoute));
        }

        #endregion

        public void SatartServer(CancellationToken token)
        {
            Task.Run(() => AcceptConnections(token), token);
        }

        private void AcceptConnections(CancellationToken token)
        {
            try
            {
                _HttpListener.Prefixes.Add($"http://{Hostname}:{Port}/");
                //_HttpListener.Prefixes.Add($"http://{Hostname}:{Port}/");
                _HttpListener.Start();
                    Console.WriteLine("Listener: " + _HttpListener.IsListening);

                while (_HttpListener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        if (token.IsCancellationRequested) throw new OperationCanceledException();

                        var context = c as HttpListenerContext;

                        HttpRequest req = new HttpRequest(context);

                        Console.WriteLine("-> client req:");
                        Console.WriteLine("\tUrl: " + req.Url.ToString());
                        Console.WriteLine("\tRawUrlWithoutQuery: " + req.RawUrlWithoutQuery.ToString());


                        Task.Run(() =>
                        {
                            HttpResponse resp = null;
                            Func<HttpRequest, HttpResponse> handler = null;


                            if (_RouteManager.Exists(req.RawUrlWithoutQuery))
                            {
                                handler = _RouteManager.Match(req.Method, req.RawUrlWithoutQuery);
                                if (handler != null)
                                    resp = handler(req);
                            }

                            if (resp == null)
                            {
                                resp = new HttpResponse(req, 500, null, "text/plain", "Unable to generate repsonse");
                                SendResponse(context, req, resp);
                                return;
                            }
                            else
                            {
                                Dictionary<string, string> headers = new Dictionary<string, string>();
                                if (!String.IsNullOrEmpty(resp.ContentType))
                                    headers.Add("content-type", resp.ContentType);

                                if (resp.Headers != null && resp.Headers.Count > 0)
                                {
                                    foreach (KeyValuePair<string, string> curr in resp.Headers)
                                        headers.Add(curr.Key, curr.Value);
                                }

                                SendResponse(context, req, resp);
                                return;
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

        private void SendResponse(HttpListenerContext context, HttpRequest req, HttpResponse resp)
        {
            long responseLength = 0;
            HttpListenerResponse response = null;

            try
            {
                #region Status-Code-and-Description

                response = context.Response;
                response.StatusCode = resp.StatusCode;

                switch (resp.StatusCode)
                {
                    case 200:
                        response.StatusDescription = "OK";
                        break;

                    case 201:
                        response.StatusDescription = "Created";
                        break;

                    case 301:
                        response.StatusDescription = "Moved Permanently";
                        break;

                    case 302:
                        response.StatusDescription = "Moved Temporarily";
                        break;

                    case 304:
                        response.StatusDescription = "Not Modified";
                        break;

                    case 400:
                        response.StatusDescription = "Bad Request";
                        break;

                    case 401:
                        response.StatusDescription = "Unauthorized";
                        break;

                    case 403:
                        response.StatusDescription = "Forbidden";
                        break;

                    case 404:
                        response.StatusDescription = "Not Found";
                        break;

                    case 405:
                        response.StatusDescription = "Method Not Allowed";
                        break;

                    case 429:
                        response.StatusDescription = "Too Many Requests";
                        break;

                    case 500:
                        response.StatusDescription = "Internal Server Error";
                        break;

                    case 501:
                        response.StatusDescription = "Not Implemented";
                        break;

                    case 503:
                        response.StatusDescription = "Service Unavailable";
                        break;

                    default:
                        response.StatusDescription = "Unknown Status";
                        break;
                }

                #endregion

                #region Response-Headers

                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.ContentType = req.ContentType;

                if (resp.Headers != null && resp.Headers.Count > 0)
                {
                    foreach (KeyValuePair<string, string> curr in resp.Headers)
                    {
                        response.AddHeader(curr.Key, curr.Value);
                    }
                }

                #endregion

                #region Handle-HEAD-Request

                if (req.Method == HttpMethod.HEAD)
                {
                    resp.Data = null;
                    resp.DataStream = null;
                    resp.ContentLength = 0;
                }

                #endregion

                #region Send-Response

                Stream output = response.OutputStream;

                try
                {
                    if (resp.Data != null && resp.Data.Length > 0)
                    {
                        responseLength = resp.Data.Length;
                        response.ContentLength64 = responseLength;
                        output.Write(resp.Data, 0, (int)responseLength);
                    }
                    else if (resp.DataStream != null && resp.ContentLength > 0)
                    {
                        responseLength = resp.ContentLength;
                        response.ContentLength64 = resp.ContentLength;

                        long bytesRemaining = resp.ContentLength;

                        while (bytesRemaining > 0)
                        {
                            int bytesRead = 0;
                            byte[] buffer = new byte[StreamReadBufferSize];

                            if (bytesRemaining >= StreamReadBufferSize) bytesRead = resp.DataStream.Read(buffer, 0, StreamReadBufferSize);
                            else bytesRead = resp.DataStream.Read(buffer, 0, (int)bytesRemaining);

                            output.Write(buffer, 0, bytesRead);
                            bytesRemaining -= bytesRead;
                        }

                        resp.DataStream.Close();
                        resp.DataStream.Dispose();
                    }
                    else
                    {
                        response.ContentLength64 = 0;
                    }
                }
                catch (Exception)
                {
                    throw;
                    // Console.WriteLine("Outer exception");; 
                }
                finally
                {
                    output.Flush();
                    output.Close();

                    if (response != null) response.Close();
                }

                #endregion

                return;
            }
            catch (Exception)
            {
                // Console.WriteLine("Outer exception");
                throw;
                //return;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }




        //private HttpResponse DefaultRouteProcessor(HttpListenerContext context, HttpRequest request)
        //{
        //    if (context == null) throw new ArgumentNullException(nameof(context));
        //    if (request == null) throw new ArgumentNullException(nameof(request));
        //    HttpResponse ret = DefaultRoute(request);
        //    if (ret == null)
        //    {
        //        ret = new HttpResponse(request, 500, null, "application/json", Encoding.UTF8.GetBytes("Unable to generate response"));
        //        return ret;
        //    }

        //    return ret;
        //}

    }
}
