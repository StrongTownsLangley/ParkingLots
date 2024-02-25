# Parking Lot Mapping Tool
This tool takes an area of the map specified as minimum and maximum longitudes and latitudes, downloads the map data from [Open Street Map](https://openstreetmap.org), and generates an interactive parking lot map based on that data.
![parkinglot_map](https://github.com/StrongTownsLangley/ParkingLots/assets/160652425/b000c58a-0aa9-43aa-87bf-b91d4155ed00)

## Demo
This tool was used to compile the map at https://strongtownslangley.org/maps?parking-map

## Download
You can download the tool from the releases page: https://github.com/StrongTownsLangley/ParkingLots/releases/

## How to Use
This tool works best with good comprehensive open street map data, this varies from place to place and you can contribute to the project yourself if data is missing.

```
Usage: pl.exe -min-longitude=* -min-latitude=* -max-longitude=* -max-latitude=* [output-folder="json"]
```

Viewable maps are generated in the output folder:
- **website.static.html** can be opened as a local file in a brower but is a larger file as it contains all the layers data.
- **website.dynamic.html** will not work as a local file and must run on a local or remote webserver, but is smaller as it loads the parkinglots.json file generated by the tool dynamically. This is the preferred method for deployment on the internet and allows you to customize the page and simply update the json files seperately in future.

**NOTE: As the area is defined by a rectangle region, parking lots from outside your city may be included. In a future version it may be possible to specify a specific city or region instead of lat/long coords.**

### Why is this useful?
Looking at a map of parking lots allows us to see where areas of valuable land are being essentially "wasted" on space for cars.

Parking lots generate little economic activity and as such are discouraged by [Strong Towns](https://strongtowns.org) in favour of more productive land use.

### How does this tool know where parking lots are?
The tool relies on user submitted and other generated data as part of the Open Street Map project which includes parking lot areas. It uses the Overpass API to download OSM (Open Street Map) Data of specified region, and then searches for all parking lots, excluding roof-top and underground parking.

In the OSM data, parking lots are defined as a "way" (which represents a collection of nodes which define the parking lot area) with the tag *amenity* defined as ***parking***:
```
 <way id="100001" visible="true" version="1">
  <nd ref="000001"/>
  <nd ref="000002"/>
  ...
  <tag k="amenity" v="parking"/>
  <tag k="parking" v="surface"/>
  ...
 </way>
```

Multipolygon Parking Lots (a large shapes with other shapes inside "cut out") are defined as a "relation", which is a group of ways, the relation in this case has the tag *amenity* defined as ***parking*** instead of the child way elements:
```
 <relation id="200001" visible="true" version="2">
  <member type="way" ref="100001" role="outer"/>
  <member type="way" ref="100002" role="inner"/>
  ...
  <tag k="amenity" v="parking"/>
  <tag k="parking" v="surface"/>
  <tag k="type" v="multipolygon"/>
  ...
 </way>
```

If the parking tag is set to *underground* or *rooftop* it is not included in the results.

Private parking lots have the following tag:
```
  <tag k="access" v="private"/>
```
Public parking lots either have no access tag or the access tag set to "customers" or "public", but no distinction is made here on the map. Only "private" is recognized.

## Other Similar Projects
- wordpipelines's Overpass Turbo script was used as a reference for this project: https://overpass-turbo.eu/s/1ddZ - thank you!
  
## Credits and Contributing

This tool was programmed by James Hansen (james@strongtownslangley.org)

[OSMSharp](https://github.com/OsmSharp) is used to read and parse the OSM data.

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) is used to write JSON files and data.

[Leaflet](https://github.com/Leaflet/Leaflet) is used to display the map.

The project is a Visual Studio 2013 project in C# .NET 4.5.

## Help

If you encounter any issues while using it or have suggestions for improvement, please [open an issue](https://github.com/StrongTownsLangley/ParkingLots/issues) on the GitHub repository. Pull requests are also welcome.

I can be found on Strong Towns Local Conversation discord, you can also get help in the #🧮do-the-math channel on the Strong Towns Langley discord: https://discord.gg/MuAn3cFd8J

## License

This program is released under the [Apache 2.0 License](https://github.com/StrongTownsLangley/ParkingLots/blob/main/LICENSE). If you use it for your website or project, please provide credit to **Strong Towns Langley** and preferably link to this GitHub.
