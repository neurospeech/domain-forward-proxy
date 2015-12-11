using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ForwardCachedWeb.Core
{
    public class ProxyHost
    {
        public String Host { get; set; }
        public String Target { get; set; }
        public bool LocalCache { get; set; }


        public static Dictionary<string, ProxyHost> Hosts
        {
            get;
            private set;
        }


        public static void Init(string path) {
            JavaScriptSerializer serializer = new JavaScriptSerializer();

            Hosts = serializer
                .Deserialize<ProxyHost[]>(System.IO.File.ReadAllText(path))
                .ToDictionary(x=>x.Host.ToLower(),x=>x);
        }

    }
}
