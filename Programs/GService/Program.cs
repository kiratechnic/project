using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;



//
//(https?:\/\/)?([a-zA-Z0-9\-]+\.[a-zA-Z]{2,})
//(\/[^\s]*)?
//(\?[^\s]*)?
//




namespace GService
{
    class Program
    {
        public static void Main()
        {
            string urlPattern = @"^(https?:\/\/)?([a-zA-Z0-9\-]+\.[a-zA-Z]{2,})(\/[^\s]*)?(\?[^\s]*)?$";

            Regex regex = new Regex(urlPattern);

            // Примеры URL для проверки
            string[] urls = {
                "https://example.com/path/to/resource?param1=value1&param2=value2",
                "http://www.example.com/?search=test",
                "https://example.com",
                "ftp://example.com/resource",
                "https://example.com/path?param=value&otherParam=otherValue"
            };

            foreach (var url in urls)
            {
                bool isValid = regex.IsMatch(url);
                Console.WriteLine($"URL: {url} - Valid: {isValid}");
            }
        }

/*


        private void StartLocalServer(CancellationToken token)
        {
            Task.Run(() => AcceptConnections(token), token);
            TerminatorLocalServer.WaitOne();
        }
        //private void StartServer(CancellationToken token)
        //{
        //    Task.Run(() => AcceptConnections(token), token);
        //    Terminator.WaitOne();
        //}

        private void AcceptConnections(CancellationToken token)
        {
            try
            {
                localTcpListener.Start();

                while (true)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        if (token.IsCancellationRequested) throw new OperationCanceledException();

                        var context = c as TcpClient;
                        try
                        {

                        }
                        catch (System.Exception e)
                        {

                            throw;
                        }



                    }, localTcpListener.AcceptSocket());
                }
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
            }
        }

*/



        //static HttpResponse GetHelloRoute(HttpRequest req)
        //{
        //    return new HttpResponse(req, 200, null, "text/plain", Encoding.UTF8.GetBytes("hello static route!"));
        //}

        //static HttpResponse GetWorldRoute(HttpRequest req)
        //{
        //    return new HttpResponse(req, 200, null, "text/plain", Encoding.UTF8.GetBytes("world static route!"));
        //}

        //static HttpResponse PostDataRoute(HttpRequest req)
        //{
        //    return new HttpResponse(req, 200, null, req.ContentType, req.ContentLength, req.Data);
        //}


        //static void Main()
        //{
        //    Server s = new Server("127.0.0.1", 9000, false, DefaultRoute);


        //    s.StaticRoutes.Add(HttpMethod.GET, "/hello/", GetHelloRoute);
        //    s.StaticRoutes.Add(HttpMethod.GET, "/world/", GetWorldRoute);
        //    s.StaticRoutes.Add(HttpMethod.POST, "/data/", PostDataRoute);

        //    s.DynamicRoutes.Add(HttpMethod.GET, new Regex("^/foo/\\d+$"), GetFooWithId);
        //    s.DynamicRoutes.Add(HttpMethod.GET, new Regex("^/foo/(.*?)/(.*?)/?$"), GetFooMultipleChildren);
        //    s.DynamicRoutes.Add(HttpMethod.GET, new Regex("^/foo/(.*?)/?$"), GetFooOneChild);
        //    s.DynamicRoutes.Add(HttpMethod.GET, new Regex("^/foo/?$"), GetFoo);
        //    Console.WriteLine("Press ENTER to exit");
        //    Console.ReadLine();
        //}

        //static HttpResponse GetFooWithId(HttpRequest req)
        //{
        //    return ResponseBuilder(req, "hello from the GET /foo with ID dynamic route!");
        //}

        //static HttpResponse GetFooMultipleChildren(HttpRequest req)
        //{
        //    return ResponseBuilder(req, "hello from the GET /foo with multiple children dynamic route!");
        //}

        //static HttpResponse GetFooOneChild(HttpRequest req)
        //{
        //    return ResponseBuilder(req, "hello from the GET /foo with one child dynamic route!");
        //}

        //static HttpResponse GetFoo(HttpRequest req)
        //{
        //    return ResponseBuilder(req, "hello from the GET /foo dynamic route!");
        //}

        //static HttpResponse DefaultRoute(HttpRequest req)
        //{
        //    return ResponseBuilder(req, "hello from the default route!");
        //}

        //static HttpResponse ResponseBuilder(HttpRequest req, string text)
        //{
        //    return new HttpResponse(req, 200, null, "text/plain", Encoding.UTF8.GetBytes(text));
        //}





    }

    /*
            private void AcceptConnections(CancellationToken token)
            {
                try
                {
                    foreach (string curr in _ListenerHostnames)
                    {
                        string prefix = null;
                        if (_ListenerSsl) prefix = "https://" + curr + ":" + _ListenerPort + "/";
                        else prefix = "http://" + curr + ":" + _ListenerPort + "/";
                        _HttpListener.Prefixes.Add(prefix);
                    }

                    _HttpListener.Start();

                    while (_HttpListener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        { 
                            if (token.IsCancellationRequested) throw new OperationCanceledException();

                            var context = c as HttpListenerContext;

                            try
                            {
                                // при проблеме парса http запроса
                                HttpRequest req = new HttpRequest(context, ReadInputStream);
                                if (req == null)
                                {
                                    HttpResponse resp = new HttpResponse(req, 500, null, "text/plain", "Unable to parse HTTP request");
                                    SendResponse(context, req, resp);
                                    return;
                                }

                                // Check access control
                                if (!AccessControl.Permit(req.SourceIp))
                                { 
                                    context.Response.Close();
                                    return;
                                } 

                                // Process OPTIONS request
                                if (req.Method == HttpMethod.OPTIONS
                                    && OptionsRoute != null)
                                { 
                                    OptionsProcessor(context, req);
                                    return;
                                }

                                // Send to handler
                                Task.Run(() =>
                                {
                                    HttpResponse resp = null;
                                    Func<HttpRequest, HttpResponse> handler = null;

                                    // Check content routes
                                    if (req.Method == HttpMethod.GET
                                        || req.Method == HttpMethod.HEAD)
                                    { 
                                        if (ContentRoutes.Exists(req.RawUrlWithoutQuery))
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

                                    // Return
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
                                                headers = Common.AddToDict(curr.Key, curr.Value, headers); 
                                        }

                                        SendResponse(context, req, resp);

                                        return;
                                    } 
                                }); 
                            }
                            catch (Exception)
                            {

                            }
                            finally
                            {

                            }
                        }, _HttpListener.GetContext());
                    }
                } 
                catch (OperationCanceledException)
                {
                    // do nothing
                }
                catch (Exception)
                { 
                    throw;
                }
                finally
                { 
                }
            }




            public Func<HttpRequest, HttpResponse> Match(HttpMethod method, string path)
            {
                if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

                path = path.ToLower();
                if (!path.StartsWith("/")) path = "/" + path;
                if (!path.EndsWith("/")) path = path + "/";

                lock (_Lock)
                {
                    StaticRoute curr = _Routes.FirstOrDefault(i => i.Method == method && i.Path == path);
                    if (curr == null || curr == default(StaticRoute))
                    {
                        return null;
                    }
                    else
                    {
                        return curr.Handler;
                    }
                }
            }

    */

}