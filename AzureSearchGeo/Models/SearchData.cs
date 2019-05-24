using System.Collections;

namespace AzureSearchGeo.Models
{
    public static class GlobalVariables
    {
        public static int ResultsPerPage
        {
            get
            {
                return 3;
            }
        }
        public static int MaxPageRange
        {
            get
            {
                return 5;
            }
        }

        public static int PageRangeDelta
        {
            get
            {
                return 2;
            }
        }
    }

    public class SearchData
    {
        public SearchData()
        {
            hotels = new ArrayList();
        }

        [System.ComponentModel.DataAnnotations.Key]

        // The text to search for in the hotels data.
        public string searchText { get; set; }

        // The total number of results found for the search text.
        public int resultCount { get; set; }

        // The list of hotels to display in the current page.
        public ArrayList hotels;

        // The current page being displayed.
        public int currentPage { get; set; }

        // The total number of pages of results.
        public int pageCount { get; set; }

        public int leftMostPage { get; set; }
        public int pageRange { get; set; }

        // Used when page numbers, or next or prev buttons, have been selected.
        public string paging { get; set; }

        // For Geo queries
        public string lat { get; set; }
        public string lon { get; set; }
        public string radius { get; set; }

        public void AddHotel(string name, string desc, double rate, string bedoption, string[] tags, double distanceInMiles)
        {
            // Populate a new Hotel class, but only with the data that has been provided.
            Hotel hotel = new Hotel();
            hotel.HotelName = name;
            hotel.Description = desc;
            hotel.Tags = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                hotel.Tags[i] = new string(tags[i]);
            }
            hotel.distanceInKilometers = distanceInMiles;

            // Create just a single room for the hotel, containing the sample rate and room description.
            Room room = new Room();
            room.BaseRate = rate;
            room.BedOptions = bedoption;

            hotel.Rooms = new Room[1];
            hotel.Rooms[0] = room;

            hotels.Add((object)hotel);
        }
        public Hotel getHotel(int index)
        {
            Hotel h = (Hotel)hotels[index];
            return h;
        }

        public string getHotelDescription(int index)
        {
            Hotel h = (Hotel)hotels[index];
            // Combine the tag data into a comma-delimited string
            string tagData = string.Join(", ", h.Tags);
            string fullDescription = h.Description;

            if (tagData.Length > 0)
            {
                fullDescription += "\nHighlights: " + tagData;
            }

            // Get the sample room data and combine into one string.
            var firstLine = "Distance: " + h.distanceInKilometers.ToString("0.##") + " Km.";
            firstLine += " Sample room: ";
            firstLine += h.Rooms[0].BedOptions;
            firstLine += " $" + h.Rooms[0].BaseRate;
            firstLine += "\n" + fullDescription;
            return firstLine;
        }
    }
}

