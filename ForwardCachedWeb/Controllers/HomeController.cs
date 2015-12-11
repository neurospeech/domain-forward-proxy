using ForwardCachedWeb.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ForwardCachedWeb.Controllers
{
    public class HomeController : Controller
    {

        private static object lockObject = new object();

        StringWriter logger = new StringWriter();


        // GET: Home
        public async Task<ActionResult> Index(string all)
        {

            using (HttpClientHandler ch = new HttpClientHandler())
            {
                ch.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
                ch.AllowAutoRedirect = true;


                using (HttpClient client = new HttpClient(ch))
                {



                    //bool isSecure = Request.IsSecureConnection;


                    var msg = BuildRequest();

                    //if(isSecure){
                    //    builder.Scheme = "https";
                    //}

                    var r = await client.SendAsync(msg);

                    logger.WriteLine();

                    var resp = await HttpResponseActionResult.New(logger,r);

                    // log....
                    lock (lockObject)
                    {
                        DateTime today = DateTime.Today;
                        String path = String.Format(MvcApplication.LogPath + "/log-{0}.txt", today.ToString("yyyy-MM-dd"));
                        System.IO.File.AppendAllText(path, logger.GetStringBuilder().ToString());

                    }

                    return resp;
                }
            }
        }

        protected override void OnException(ExceptionContext filterContext)
        {



            base.OnException(filterContext);
        }


        HttpRequestMessage BuildRequest()
        {
            UriBuilder builder = new UriBuilder(Request.Url);
            string host = builder.Host;
            ProxyHost phost = null;
            if (!ProxyHost.Hosts.TryGetValue(host, out phost)) {
                throw new UnauthorizedAccessException();
            }
            builder.Host = phost.Target;

            logger.WriteLine(builder.Uri);

            //builder.Scheme = "https";
            //builder.Port = 443;

            var msg = new HttpRequestMessage(GetMethod(Request.HttpMethod), builder.Uri);

            // transfer all headers 
            foreach (string item in Request.Headers.Keys)
            {
                string value = Request.Headers[item];
                msg.Headers.Add(item, value);
                logger.WriteLine("{0}={1}",item,value);
            }

            msg.Headers.Accept.Clear();
            msg.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
            msg.Headers.AcceptEncoding.Clear();
            msg.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            msg.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));


            if (Request.ContentLength > 0)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Request.InputStream.CopyTo(ms);

                    msg.Content = new ByteArrayContent(ms.ToArray());
                }
            }

            return msg;
        }

        private HttpMethod GetMethod(string p)
        {
            if (string.IsNullOrWhiteSpace(p))
                return HttpMethod.Get;
            p = p.ToLower();
            switch (p)
            {
                case "post":
                    return HttpMethod.Post;
                case "put":
                    return HttpMethod.Put;
                case "options":
                    return HttpMethod.Options;
                case "delete":
                    return HttpMethod.Delete;
                case "head":
                    return HttpMethod.Head;
                case "trace":
                    return HttpMethod.Trace;
                default:
                    break;
            }
            return HttpMethod.Get;
        }

        private void SetCache(HttpCachePolicyBase cache, System.Net.Http.Headers.CacheControlHeaderValue cacheIn)
        {
            if (cacheIn.Public)
            {
                logger.WriteLine("Cache.Public");
                cache.SetCacheability(HttpCacheability.Public);
            }
            if (cacheIn.Private)
            {
                logger.WriteLine("Cache.Private");
                cache.SetCacheability(HttpCacheability.Private);
            }
            if (cacheIn.MaxAge != null)
            {
                logger.WriteLine("Cache.MaxAge");
                cache.SetMaxAge(cacheIn.MaxAge.Value);
            }

        }
    }

    public class HttpResponseActionResult : ActionResult
    {

        public static async Task<HttpResponseActionResult> New(StringWriter logger, HttpResponseMessage msg)
        {
            var s = await msg.Content.ReadAsByteArrayAsync();
            return new HttpResponseActionResult
            {
                logger = logger,
                Content = s,
                ResponseMessage = msg
            };
        }

        public StringWriter logger;

        public HttpResponseMessage ResponseMessage { get; set; }

        public byte[] Content { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            var Response = context.HttpContext.Response;

            Response.StatusCode = (int)ResponseMessage.StatusCode;
            Response.StatusDescription = ResponseMessage.ReasonPhrase;
            Response.TrySkipIisCustomErrors = true;

            logger.WriteLine("{0} {1}", Response.StatusCode, Response.StatusDescription);

            if (Response.StatusCode == 200)
            {
                SetCache(Response.Cache, ResponseMessage.Headers.CacheControl);
            }
            else
            {
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }

            var content = ResponseMessage.Content;
            var val = content.Headers.Expires;
            if (val != null)
            {
                Response.ExpiresAbsolute = val.Value.UtcDateTime;
            }

            Response.ContentType = content.Headers.ContentType.ToString();

            foreach (var item in ResponseMessage.Headers)
            {
                String value = string.Join(";", item.Value);
                Response.Headers.Set(item.Key, value);
                logger.WriteLine("{0}={1}", item.Key, value);

            }

            if (ResponseMessage.Headers.Location != null)
            {
                Response.RedirectLocation = ResponseMessage.Headers.Location.ToString();
            }
            else
            {
                if (Content != null)
                {
                    Response.OutputStream.Write(Content, 0, Content.Length);
                }
            }
        }

        private void SetCache(HttpCachePolicyBase cache, System.Net.Http.Headers.CacheControlHeaderValue cacheIn)
        {
            if (cacheIn == null)
                return;

            if (cacheIn.Public)
            {
                cache.SetCacheability(HttpCacheability.Public);
            }
            if (cacheIn.Private)
            {
                cache.SetCacheability(HttpCacheability.Private);
            }
            if (cacheIn.MaxAge != null)
            {
                cache.SetMaxAge(cacheIn.MaxAge.Value);
            }

        }
    }
}