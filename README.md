# Parking Lot Mapping Tool
This tool takes an area of the map specified as minimum and maximum longitudes and latitudes, downloads the map data from [Open Street Map](https://openstreetmap.org), and generates an interactive parking lot map.

![parkinglog_map](https://github.com/StrongTownsLangley/ParkingLots/assets/160652425/0d7f359e-e7d8-45ca-8eba-bfb35202a40e)

## Demo
This tool was used to compile the map at https://strongtownslangley.org/maps?parking-map

## Download
You can download the tool from the releases page: https://github.com/StrongTownsLangley/ParkingLots/releases/

## How to Use
This tool works best with good comprehensive open street map data, this varies from place to place and you can contribute to the project yourself if data is missing.

```
Usage: pl.exe -min-longitude=* -min-latitude=* -max-longitude=* -max-latitude=* output-folder="json"
```

Viewable maps are generated in the output folder:
- **website.static.html** can be opened as a local file in a brower but is a larger file as it contains all the layers data.
- **website.dynamic.html** will not work as a local file and must run on a local or remote webserver, but is smaller as it loads the parkinglots.json file generated by the tool dynamically. This is the preferred method for deployment on the internet and allows you to customize the page and simply update the parkinglots.json file seperately in future.

### Why is this useful and Why?
Looking at a map of parking lots allows us to see where areas of valuable land are being essentially "wasted" on space for cars.

Parking lots generate little economic activity and as such are discouraged by [Strong Towns](https://strongtowns.org) in favour of more productive land use.

### What does this tool do?
- Uses Overpass API to download OSM (Open Street Map) Data of specified region.
- Searches for all parking lots, excluding roof-top and underground parking.
- Generates JSON data suitable for a Leaflet map layer.
- Outputs website with map for viewing.

**NOTE: As the area is defined by a rectangle region, parking lots from outside your city may be included. In a future version it may be possible to specify a specific city or region instead of lat/long coords.**

## Other Similar Projects
- wordpipelines's Overpass Turbo script was used as a reference for this project: https://overpass-turbo.eu/s/1ddZ - thank you!
  
## Credits and Contributing

This tool was programmed by James Hansen (james@strongtownslangley.org)

[OSMSharp](https://github.com/OsmSharp) is used to read and parse the OSM data.

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) is used to write JSON files and data.

[Leaflet](https://github.com/Leaflet/Leaflet) is used to display the map.

The project is a Visual Studio 2013 project in C# .NET 4.5.

If you encounter any issues while using it or have suggestions for improvement, please [open an issue](https://github.com/StrongTownsLangley/ParkingLots/issues) on the GitHub repository. Pull requests are also welcome.

## License

This program is released under the [Apache 2.0 License](https://github.com/StrongTownsLangley/ParkingLots/blob/main/LICENSE). If you use it for your website or project, please provide credit to **Strong Towns Langley** and preferably link to this GitHub.