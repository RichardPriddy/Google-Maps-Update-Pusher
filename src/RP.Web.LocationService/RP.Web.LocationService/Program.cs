using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.SelfHost;
using RP.Web.LocationService.Controllers;
using RP.Web.LocationService.Core;
using RP.Web.LocationService.Models;

namespace RP.Web.LocationService
{
    using System;
    using System.Data.SqlClient;

    public class Program
    {
        private static string connectionString = @"Data Source=(local)\SQLExpress;Initial Catalog=LocationTracker;Integrated Security=True";
        private static string database = "LocationTracker";
        private static string table = "LocationLog";
        private static string schema = "dbo";

        private static SqlDependencyEx _sqlDependency;
        
        private static int _purgeTracker = 0;

        public static void Main(string[] args)
        {
            // Ensure we have permissions for SQL dependancy.
            try
            {
                SqlClientPermission perm = new SqlClientPermission(System.Security.Permissions.PermissionState.Unrestricted);
                perm.Demand();
            }
            catch
            {
                throw new ApplicationException("No permission");
            }

            var config = new HttpSelfHostConfiguration("http://localhost:22222");
            config.Routes.MapHttpRoute("API Default", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });
            config.TransferMode = System.ServiceModel.TransferMode.Streamed;
            config.EnableCors(new EnableCorsAttribute("http://localhost:22223", "", ""));

            using (var server = new HttpSelfHostServer(config))
            {
                server.OpenAsync().Wait();

                _sqlDependency = new SqlDependencyEx(connectionString, database, table, schema, SqlDependencyEx.NotificationTypes.Insert);
                _sqlDependency.Start();
                _sqlDependency.TableChanged += FerryLocationUpdated;

                Console.ReadKey();

                _sqlDependency.Stop();
            }
        }

        private static void FerryLocationUpdated(object sender, SqlDependencyEx.TableChangedEventArgs e)
        {
            // Get the new PointId
            var xElement = e.Data.Element("inserted");
            if (xElement == null)
            {
                return;
            }

            var dataRow = xElement.Element("row");
            var pointId = int.Parse(dataRow.Element("Id").Value);
            var latidude = decimal.Parse(dataRow.Element("Latitude").Value);
            var longitude = decimal.Parse(dataRow.Element("Longitude").Value);
            var pointName = dataRow.Element("PointerName").Value;
            
            // Output the point's details.
            Console.WriteLine("{2} location: {0}, {1}", latidude, longitude, pointName);

            // Create the ferry location object.
            var pointLocation = new PointerLocation
            {
                Lattitude = latidude.ToString(CultureInfo.InvariantCulture),
                Longitude = longitude.ToString(CultureInfo.InvariantCulture),
                PointerName = pointName
            };

            // Push that location to the subscribers.
            PointerController.PushPointerData(pointLocation);

            // Make sure we purge the number of subscribers every hour.
            if (DateTime.Now.Hour != _purgeTracker)
            {
                _purgeTracker = DateTime.Now.Hour;
                PointerController.Clear();
            }
        }
    }
}
