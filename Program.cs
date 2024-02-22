using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using OsmSharp;
using OsmSharp.Streams;
using Newtonsoft.Json;


/* STRONG TOWNS LANGLEY PARKING LOT MAPPING TOOL
 * Coded by: James Hansen (james@strongtownslangley.org)
 * https://github.com/StrongTownsLangley/ParkingLots
 * strongtownslangley.org
 */


namespace pl
{
    class Program
    {
        public static double minLongitude = 0;
        public static double minLatitude = 0;
        public static double maxLongitude = 0;
        public static double maxLatitude = 0;
        public static string outputFolder = "json";        

        static void Main(string[] args)
        {
            Console.WriteLine("Strong Towns Langley Parking Lot Mapping Tool");
            Console.WriteLine("https://github.com/StrongTownsLangley/ParkingLots");
            Console.WriteLine("https://strongtownslangley.org");
            Console.WriteLine("Coded by James Hansen (james@strongtownslangley.org)");
            Console.WriteLine("---------------------------------------------------");

            // Read Arguments //
            #region Read Arguments
            int regionCount = 0;
            foreach (var arg in args)
            {
                if (arg.StartsWith("-min-longitude="))
                {
                    double.TryParse(arg.Substring("-min-longitude=".Length), out minLongitude);
                    regionCount++;
                }
                else if (arg.StartsWith("-min-latitude="))
                {
                    double.TryParse(arg.Substring("-min-latitude=".Length), out minLatitude);
                    regionCount++;
                }
                else if (arg.StartsWith("-max-longitude="))
                {
                    double.TryParse(arg.Substring("-max-longitude=".Length), out maxLongitude);
                    regionCount++;
                }
                else if (arg.StartsWith("-max-latitude="))
                {
                    double.TryParse(arg.Substring("-max-latitude=".Length), out maxLatitude);
                    regionCount++;
                }
                else if (arg.StartsWith("-output-folder="))
                {
                    outputFolder = arg.Substring("-output-folder=".Length);
                }
            }
            
            if(regionCount < 4)
            {
                Console.WriteLine("Usage: pl.exe -min-longitude=* -min-latitude=* -max-longitude=* -max-latitude=* [output-folder=\"json\"]");
                return;
            }
            #endregion

            // Define the bounding box
            #region Download OSM            
            string overpassApiUrl = "http://www.overpass-api.de/api/map?bbox=" + minLongitude + "," + minLatitude + "," + maxLongitude + "," + maxLatitude;
            string osmDataFilePath = Path.Combine(outputFolder,"osm_data.osm"); // file to save OSM data
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    Console.WriteLine("Downloading OSM data...");
                    webClient.DownloadFile(overpassApiUrl, osmDataFilePath);
                    Console.WriteLine("OSM data downloaded successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading OSM data: {ex.Message}");
                return;
            }
            #endregion

            #region Identify Surface Parking Lots
            // Read the OSM file
            using (var stream = File.OpenRead(osmDataFilePath))
            {
                // Create an OsmReader
                var osmStreamSource = new OsmSharp.Streams.XmlOsmStreamSource(stream);
                Console.WriteLine("Reading All OSM Data Nodes...");
                List<Node> allNodes = osmStreamSource.Where(m => m.Type == OsmGeoType.Node).Select(m => (Node)m).ToList();
                Console.WriteLine("Finding All Parking Lots...");
                var parkingLots = new Dictionary<Way,List<Node>>();
                int parkingLotCount = 0;
                foreach (var entity in osmStreamSource)
                {
                    if (entity.Type == OsmGeoType.Way) // Do not include Nodes, just Polygons
                    {
                        var parkingLotWay = (Way)entity;

                        var tags = entity.Tags;
                        if (tags == null)
                            continue;
                        if (tags.ContainsKey("amenity") && tags["amenity"] == "parking")
                        {
                            if (tags.ContainsKey("parking"))
                                if (tags["parking"] == "underground" || tags["parking"] == "rooftop")
                                    continue; // Skip Underground and Rooftop Parking


                            var nodeIds = parkingLotWay.Nodes.ToList();
                            var parkingLotNodes = allNodes
                                .Where(m => m.Id != null && nodeIds.Contains(m.Id.Value))
                                .OrderBy(node => nodeIds.IndexOf(node.Id.Value)) // Preserve Order
                                .ToList();

                            parkingLots.Add(parkingLotWay, parkingLotNodes);
                            parkingLotCount++;
                        }
                    }

                }
                Console.WriteLine(parkingLots.Count() + " surface parking lots found.");


            #endregion

            #region Output JSON
            // Output the identified parking lots
     
            // Configure JSON serialization settings
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            var geoJson = new
            {
                type = "FeatureCollection",
                features = new List<object>(),
                info = new List<object>()
            };


            int featureCount = 0;
            foreach (var parkingLot in parkingLots)
            {
                var parkingLotWay = parkingLot.Key;
                var parkingLotNodes = parkingLot.Value;

                var parkingLotPoints = new List<double[]>();
                foreach (Node node in parkingLotNodes)
                {
                    double[] point = { node.Longitude ?? 0, node.Latitude ?? 0 };
                    parkingLotPoints.Add(point);
                }

                var feature = new
                {
                    type = "Feature",
                    properties = new { },
                    geometry = new
                    {
                        type = "Polygon",
                        coordinates = new[]
                        {
                            parkingLotPoints.ToArray()
                        }
                    }
                };
                
                geoJson.features.Add(feature);                
            }
            
            // Write GeoJSON to file
            Console.WriteLine("Writing JSON 'parkinglots.json' file...");
            string geoJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(geoJson);
            File.WriteAllText(Path.Combine(outputFolder, "parkinglots.json"), Newtonsoft.Json.JsonConvert.SerializeObject(geoJson, Formatting.Indented));
                
            #endregion

            #region Write Map HTML
            // Replace {AVGLAT}, {AVGLON}, {DATALIST}
            // taxLevel.addData(data);
            var website = File.ReadAllText("pl.template.html");

            website = website.Replace("{AVGLAT}", ((minLatitude + maxLatitude) / 2).ToString());
            website = website.Replace("{AVGLON}", ((minLongitude + maxLongitude) / 2).ToString());


            string websiteStatic = website.Replace("{DATALIST}", "pl.addData(" + geoJsonString + ");\r\n");
            string websiteDynamic = website.Replace("{DATALIST}",
@"             fetch('parkinglots.json')
                .then(response => response.json())
                .then(data => taxLevel.addData(data))
                .catch(error => console.error('Error loading GeoJSON file:', error));"
);

            var websiteStaticFile = "website.static.html";
            Console.WriteLine("Writing Static Website '" + websiteStaticFile + "' file...");
            File.WriteAllText(Path.Combine(outputFolder, websiteStaticFile), websiteStatic);

            var websiteDynamicFile = "website.dynamic.html";
            Console.WriteLine("Writing Dynamic Website '" + websiteDynamicFile + "' file...");
            File.WriteAllText(Path.Combine(outputFolder, websiteDynamicFile), websiteDynamic);
            #endregion

            Console.WriteLine("Complete!");            
            }
        }
    }
}