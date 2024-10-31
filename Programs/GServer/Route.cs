using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GServer
{
    public class Route
    {
        #region Public-Members

        public RouteType RouteType { get; private set; }
        private HttpMethod _method;
        public HttpMethod Method 
        { 
            get {
                //if (RouteType != RouteType.Content) { return _method; }
                //throw new InvalidOperationException("Method is not available for this route type");
                return _method;
            }
            private set { _method = value; } 
        }      
        private string[] _parameters;
        public string[] Parameters 
        { 
            get {
                if (RouteType != RouteType.Parameterized) { return _parameters; }
                throw new InvalidOperationException("Parameters is not available for this route type"); 
            }
            private set { _parameters = value; } 
        }
        public Regex Path { get; private set; } 
        public Func<HttpRequest, HttpResponse> Handler { get; private set; }
        //private bool? _isDirectory; 
        //public bool IsDirectory {
        //    get { return RouteType == RouteType.Content ? _isDirectory.GetValueOrDefault() : false; } 
        //    private set { _isDirectory = value; } 
        //}
        
        #endregion

        #region Constructors

        public Route() {}

        private Route(RouteType routeType, HttpMethod method, Regex path, Func<HttpRequest, HttpResponse> handler) {
            RouteType = routeType;
            Method = method;
            Path = path;
            Handler = handler;
        }
        private Route(RouteType routeType, HttpMethod method, Regex path, Func<HttpRequest, HttpResponse> handler, string[] parameters) {
            RouteType = routeType;
            Method = method;
            Parameters = parameters;
            Path = path;
            Handler = handler;
        }
        //private Route(RouteType routeType, Regex path, bool isDirectory) {
        //    RouteType = routeType;
        //    Path = path;
        //    IsDirectory = isDirectory;
        //}

        #endregion

        #region Build-Routes

        //public static Route BuildContentRoute(string path, bool isDirectory) {
        //    return new Route(RouteType.Content, new Regex($"^{path}$"), isDirectory); 
        //}

        public static Route BuildDefaultRoute(HttpMethod method, string path, Func<HttpRequest, HttpResponse> handler)
        {
            return new Route(RouteType.Default, method, new Regex($"^{path}$"), handler);
        }
        public static Route BuildDynamicRoute(HttpMethod method, Regex path, Func<HttpRequest, HttpResponse> handler) {
            return new Route(RouteType.Dynamic, method, path, handler);
        }

        public static Route BuildParameterizedRoute(HttpMethod method, Regex path, Func<HttpRequest, HttpResponse> handler, string[] parameters) {
            return new Route(RouteType.Parameterized, method, path, handler, parameters); 
        }

        public static Route BuildStaticRoute(HttpMethod method, string path, Func<HttpRequest, HttpResponse> handler) {
            return new Route(RouteType.Static, method, new Regex($"^{path}$"), handler);
        }

        #endregion
    }
}
