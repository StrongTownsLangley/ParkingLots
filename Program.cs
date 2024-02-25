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

        public static List<Relation> _allRelations;
        public static List<Way> _allWays;
        public static List<Node> _allNodes;


        public enum ParkingLotType
        {
            None,
            Public,
            Private
        }

        public class WayWithNodes
        {
            public WayWithNodes(Way Way)
            {
                this.Way = Way;
                if(Way.Nodes != null)
                {
                    var nodeIds = Way.Nodes.ToList();
                    var nodes = _allNodes
                        .Where(m => m.Id != null && nodeIds.Contains(m.Id.Value))
                        .OrderBy(node => nodeIds.IndexOf(node.Id.Value)) // Preserve Order
                        .ToList();
                    this.PopulatedNodes = nodes;
                } else {
                    this.PopulatedNodes = new List<Node>();
                }
            }
            public Way Way { get; set; }
            public List<Node> PopulatedNodes { get; set; }
        }

        public class ParkingLot
        {
            public List<WayWithNodes> WaysWithNodes { get; set; }
            public ParkingLotType ParkingLotType { get; set; }
        }

        public static ParkingLotType GetParkingType(OsmSharp.Tags.TagsCollectionBase tags)
        {
            if (tags == null)
                return ParkingLotType.None;

            if (tags.ContainsKey("amenity") && tags["amenity"] == "parking")
            {
                if (tags.ContainsKey("parking"))
                    if (tags["parking"] == "underground" || tags["parking"] == "rooftop")
                    {
                        return ParkingLotType.None;
                    }
                if (tags.ContainsKey("access") && tags["access"] == "private")
                    return ParkingLotType.Private;
                return ParkingLotType.Public;
            }
            return ParkingLotType.None;
        }

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

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
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

                Console.WriteLine("Reading All OSM Data Relations...");
                _allRelations = osmStreamSource.Where(m => m.Type == OsmGeoType.Relation).Select(m => (Relation)m).ToList();

                Console.WriteLine("Reading All OSM Data Ways...");
                _allWays = osmStreamSource.Where(m => m.Type == OsmGeoType.Way).Select(m => (Way)m).ToList();

                Console.WriteLine("Reading All OSM Data Nodes...");
                _allNodes = osmStreamSource.Where(m => m.Type == OsmGeoType.Node).Select(m => (Node)m).ToList();

                // Close Stream
            }

                Console.WriteLine("Finding Parking Lot Relations..."); // Some parking lots are multiple ways grouped as relations, with the relation having the parking info                
                var relationParkingWayIds = new List<long>();
                var parkingLots = new List<ParkingLot>();
                foreach (Relation relation in _allRelations)
                {
                        var parkingType = GetParkingType(relation.Tags);
                        if(parkingType != ParkingLotType.None)
                        {                            
                            var waysWithNodes = new List<WayWithNodes>();
                            foreach(var member in relation.Members)
                            {
                                if (member.Type == OsmGeoType.Way)
                                {
                                    var wayInRelation = _allWays.FirstOrDefault(m => m.Id == member.Id);
                                    if (wayInRelation == null)
                                        continue;
                                    waysWithNodes.Add(new WayWithNodes(wayInRelation));
                                    relationParkingWayIds.Add(member.Id);
                                }
                            }
                            var parkingLot = new ParkingLot()
                            {
                                WaysWithNodes = waysWithNodes,
                                ParkingLotType = parkingType
                            };

                            parkingLots.Add(parkingLot);

                        }                                    
                }
                Console.WriteLine("Finding Parking Ways...");


                foreach (var way in _allWays)
                {
                        var tags = way.Tags;
                        ParkingLotType parkingType = ParkingLotType.None;
                        if (way.Id != null && relationParkingWayIds.Contains(way.Id.Value))
                        {
                            // Way already found as part of a relation
                            continue;
                        }
                        else
                        {
                            // Way not as part of relation
                            parkingType = GetParkingType(tags);

                            if (parkingType != ParkingLotType.None)
                            {
                                var parkingLot = new ParkingLot()
                                {
                                    WaysWithNodes = new List<WayWithNodes>() { new WayWithNodes(way) },
                                    ParkingLotType = parkingType
                                };

                                parkingLots.Add(parkingLot);

                            }
                        }
                }
                Console.WriteLine(parkingLots.Count() + " surface parking lots found.");


            #endregion

            #region Output JSON
            // Output the identified parking lots
     
            // Configure JSON serialization settings
            var geoJsonPublicLots = new
            {
                type = "FeatureCollection",
                features = new List<object>(),
                info = new List<object>()
            };
            var geoJsonPrivateLots = new
            {
                type = "FeatureCollection",
                features = new List<object>(),
                info = new List<object>()
            };

            
            foreach (var parkingLot in parkingLots)
            {
                if(parkingLot.WaysWithNodes.Count() == 1)
                {
                    // Polygon
                    var parkingLotWay = parkingLot.WaysWithNodes[0];                    

                    var parkingLotPoints = new List<double[]>();
                    foreach (Node node in parkingLotWay.PopulatedNodes)
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
                
                    if(parkingLot.ParkingLotType == ParkingLotType.Public)
                        geoJsonPublicLots.features.Add(feature);                
                    else
                        geoJsonPrivateLots.features.Add(feature);                
                } else {
                    // MultiPolygon
                    var parkingLotPolygons = new List<List<double[]>>();
                    foreach(var parkingLotWay in parkingLot.WaysWithNodes)
                    {
                        var parkingLotPoints = new List<double[]>();
                        foreach (Node node in parkingLotWay.PopulatedNodes)
                        {
                            double[] point = { node.Longitude ?? 0, node.Latitude ?? 0 };
                            parkingLotPoints.Add(point);
                        }

                        parkingLotPolygons.Add(parkingLotPoints);
                    }
                    var feature = new
                    {
                        type = "Feature",
                        properties = new { },
                        geometry = new
                        {
                            type = "MultiPolygon",
                            coordinates = new[]
                            {
                                parkingLotPolygons.ToArray()
                            }
                        }
                    };

                    if (parkingLot.ParkingLotType == ParkingLotType.Public)
                        geoJsonPublicLots.features.Add(feature);
                    else
                        geoJsonPrivateLots.features.Add(feature);       
                }
            }
            
            // Write GeoJSON to file
            Console.WriteLine("Writing JSON 'parkinglots.json' file...");            
            File.WriteAllText(Path.Combine(outputFolder, "publicParkingLots.json"), Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonPublicLots, Formatting.Indented));
            File.WriteAllText(Path.Combine(outputFolder, "privateParkingLots.json"), Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonPrivateLots, Formatting.Indented));
                
            #endregion

            #region Write Map HTML
            // Replace {AVGLAT}, {AVGLON}, {DATALIST}
            // taxLevel.addData(data);
            var website = File.ReadAllText("pl.template.html");

            website = website.Replace("{AVGLAT}", ((minLatitude + maxLatitude) / 2).ToString());
            website = website.Replace("{AVGLON}", ((minLongitude + maxLongitude) / 2).ToString());


            string websiteStatic = website.Replace("{DATALIST}", 
@"publicParkingLots.addData(" + Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonPublicLots) + @");
 privateParkingLots.addData(" + Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonPrivateLots) + @");
");

            string websiteDynamic = website.Replace("{DATALIST}",
@"             fetch('publicParkingLots.json')
                .then(response => response.json())
                .then(data => publicParkingLots.addData(data))
                .catch(error => console.error('Error loading GeoJSON file:', error));

             fetch('privateParkingLots.json')
                .then(response => response.json())
                .then(data => privateParkingLots.addData(data))
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