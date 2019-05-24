using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureSearchGeo.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

using System.Collections.Generic;



namespace AzureSearchGeo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static SearchServiceClient CreateSearchServiceClient(IConfigurationRoot configuration)
        {
            string searchServiceName = configuration["SearchServiceName"];
            string queryApiKey = configuration["SearchServiceQueryApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(queryApiKey));
            return serviceClient;
        }

        private static SearchServiceClient _serviceClient;
        private static ISearchIndexClient _indexClient;
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;


        private const double milesToKilometers = 1.60934d;
        private double DistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                double theta = lon1 - lon2;
                double dist = Math.Sin(Deg2rad(lat1)) * Math.Sin(Deg2rad(lat2)) + Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) * Math.Cos(Deg2rad(theta));
                dist = Math.Acos(dist);
                dist = Rad2deg(dist);
                dist = dist * 60 * 1.1515 * milesToKilometers;
                /*
                if (unit == 'K')
                {
                    dist = dist * 1.609344;
                }
                else if (unit == 'N')
                {
                    dist = dist * 0.8684;
                }*/
                return (dist);
            }
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts decimal degrees to radians             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double Deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  This function converts radians to decimal degrees             :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double Rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        public async Task<ActionResult> Geo(SearchData model)
        {
            try
            {
                // Use static variables to set up the configuration and Azure service and index clients, for efficiency.
                _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                _configuration = _builder.Build();

                _serviceClient = CreateSearchServiceClient(_configuration);
                _indexClient = _serviceClient.Indexes.GetClient("hotels");

                // Calcuate the range of current page results.
                int page = 0;

                if (model.paging != null && model.paging == "next")
                {
                    // Increment the page
                    page = (int)TempData["page"] + 1;

                    // Recover the geo info
                    model.lat = (string)TempData["lat"];
                    model.lon = (string)TempData["lon"];
                    model.radius = (string)TempData["radius"];

                    // Recover the search text.
                    model.searchText = TempData["searchfor"].ToString();
                } else
                {
                    // First call. Check for valid lat/lon/radius input
                    if (model.lat == null)
                    {
                        model.lat = "0";
                    }
                    if (model.lon == null)
                    {
                        model.lon = "0";
                    }
                    if (model.radius == null)
                    {
                        // Find everything.
                        model.radius = "21000";
                    }
                }

                // Call suggest API and return results
                SearchParameters sp = new SearchParameters()
                {
                    // "Location" must be capitalized to match index schema.
                    // Distance (the radius) is in kilometers.
                    // Point order is Longitude then Latitude.
                    Filter = $"geo.distance(Location, geography'POINT({model.lon} {model.lat})') le {model.radius}",

                    // If order by is not specified, the returned order is simply as the data is found.
                    // As the user has specified a distance, seems right to return the data in distance order.
                    OrderBy = new[] { $"geo.distance(Location, geography'POINT({model.lon} {model.lat})') asc" },

                    // Must return the Location to calculate the distance.
                    Select = new[] { "HotelName", "Description", "Tags", "Rooms", "Location" },
                    SearchMode = SearchMode.All,
                };

                DocumentSearchResult<Hotel> results = await _indexClient.Documents.SearchAsync<Hotel>(model.searchText, sp);

                if (results.Results == null)
                {
                    model.resultCount = 0;
                }
                else
                {
                    // Record the total number of results.
                    model.resultCount = (int)results.Results.Count;

                    int start = page * GlobalVariables.ResultsPerPage;
                    int end = Math.Min(model.resultCount, (page + 1) * GlobalVariables.ResultsPerPage);
                    double distanceInKm;

                    for (int i = start; i < end; i++)
                    {
                        distanceInKm = DistanceInKm(double.Parse(model.lat), double.Parse(model.lon), results.Results[i].Document.Location.Latitude, results.Results[i].Document.Location.Longitude);

                        // Check for hotels with no room data provided.
                        if (results.Results[i].Document.Rooms.Length > 0)
                        {
                            // Add a hotel with sample room data (an example of a "complex type").
                            model.AddHotel(results.Results[i].Document.HotelName,
                                 results.Results[i].Document.Description,
                                 (double)results.Results[i].Document.Rooms[0].BaseRate,
                                 results.Results[i].Document.Rooms[0].BedOptions,
                                 results.Results[i].Document.Tags,
                                 distanceInKm);
                        }
                        else
                        {
                            // Add a hotel with no sample room data.
                            model.AddHotel(results.Results[i].Document.HotelName,
                                results.Results[i].Document.Description,
                                0d,
                                "No room data provided",
                                results.Results[i].Document.Tags,
                                distanceInKm);
                        }
                    }

                    // Ensure Temp data is stored for the next call.
                    TempData["page"] = page;
                    TempData["lat"] = model.lat;
                    TempData["lon"] = model.lon;
                    TempData["radius"] = model.radius;
                    TempData["searchfor"] = model.searchText;
                }
            }
            catch
            {
                return View("Error", new ErrorViewModel { RequestId = "2" });
            }
            return View("Index", model);
        }

        public async Task<ActionResult> NextGeo(SearchData model)
        {
            model.paging = "next";
            await Geo(model);

            List<string> hotels = new List<string>();
            for (int n = 0; n < model.hotels.Count; n++)
            {
                hotels.Add(model.getHotel(n).HotelName);
                hotels.Add(model.getHotelDescription(n));
            }
            return new JsonResult(hotels);
        }
    }
}


