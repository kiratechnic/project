using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GService
{
    class RouteManager
    {
        public ConcurrentBag<Route> Routes;       

        public RouteManager () {}

        public void Add(Route route){
            if (route == null) throw new ArgumentNullException(nameof(route));
            Routes.Add(route);
        }


        public bool Exists(string path){

            path = path.ToLower();
            if (!path.StartsWith("/")) path = "/" + path; 
            foreach (var curr in Routes)
                {
                    if (curr.IsDirectory)
                    {
                        if (path.StartsWith(curr.Path.ToLower())) return true;
                    }
                    else
                    {
                        if (path.Equals(curr.Path.ToLower())) return true;
                    }
                }


            return false;
        }












    }
}