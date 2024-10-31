using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GServer
{
    public class RouteManager
    {
        public ConcurrentBag<Route> Routes { get; private set; }

        public RouteManager() { Routes = new ConcurrentBag<Route>(); }

        public void Add(Route route)
        {
            if (route == null) throw new ArgumentNullException(nameof(route));
            Routes.Add(route);
        }
        //public void Remove(string path)
        //{
        //    path = path.ToLower();
        //    if (!path.StartsWith("/")) path = "/" + path;

        //    var routesToRemove = new List<Route>();

        //    foreach (var route in Routes)
        //    {
        //        if (route.Path.IsMatch(path))
        //        {
        //            routesToRemove.Add(route); // Mark for removal
        //        }
        //    }

        //    foreach (var route in routesToRemove)
        //    {
        //        Routes = new ConcurrentBag<Route>(Routes.Where(r => r != route));
        //    }
        //}
        public bool Exists(string path)
        {
            try
            {
                path = path.ToLower();
                if (!path.StartsWith("/")) path = "/" + path;

                foreach (var item in Routes)
                {
                    if (item.Path.IsMatch(path))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception)
            {

                throw;
                return false;
            }
        }


        public Func<HttpRequest, HttpResponse> Match(HttpMethod method, string path)
        {
            try
            {
                path = path.ToLower();
                if (!path.StartsWith("/")) path = "/" + path;

                foreach (var item in Routes)
                {
                    if (item.Method == method && item.Path.IsMatch(path))
                    {
                        return item.Handler;
                    }
                }
                return null;
            }
            catch (Exception)
            {

                throw;
                return null;
            }
        }
    }
}