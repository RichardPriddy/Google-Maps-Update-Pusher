using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using RP.Web.LocationService.Models;

namespace RP.Web.LocationService.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class PointerController : ApiController
    {
        private static readonly ConcurrentQueue<StreamWriter> Streammessage = new ConcurrentQueue<StreamWriter>();

        public HttpResponseMessage Get(HttpRequestMessage request)
        {
            HttpResponseMessage response = request.CreateResponse();
            response.Content = new PushStreamContent((a, b, c) => { OnStreamAvailable(a, b, c); }, "text/event-stream");
            return response;
        }

        public static void OnStreamAvailable(Stream stream, HttpContent headers, TransportContext context)
        {
            StreamWriter streamwriter = new StreamWriter(stream);
            Streammessage.Enqueue(streamwriter);
        }

        public static void PushPointerData(PointerLocation m)
        {
            foreach (var subscriber in Streammessage)
            {
                try
                {
                    subscriber.WriteLine("data:" + JsonConvert.SerializeObject(m) + "\n");
                    subscriber.Flush();
                }
                catch
                {
                }
            }
        }

        public static void Clear()
        {
            StreamWriter ignored;
            while (Streammessage.TryDequeue(out ignored))
            {
                ignored.Close();
            }
        }
    }
}