using DataCrawlers;
using DataCrawlers.Services;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlightRadar24Crawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            int counter = 0;

            MyScheduler.IntervalInHours(17, 50, 1,
                () =>
                {
                    DateTime localDate = DateTime.Now;
                    Console.WriteLine("------------------------");
                    Console.WriteLine("Starting in: ");
                    Console.WriteLine(localDate);
                    counter++;
                    Console.WriteLine("Index: " + counter);
                    Console.WriteLine("------------------------");
                    //Weather --> TLV
                    fetchWeatherDataAsync("32.005532", "34.885411");
                    Console.WriteLine("------------------------");
                    //Weather --> MAD
                    fetchWeatherDataAsync("40.529342", "-3.648067");
                    Console.WriteLine("------------------------");
                });
            MyScheduler.IntervalInHours(16, 50, 24,
                 () =>
                 {
                     //Flights -->Gets all flights from TLV -> MAD
                     Console.WriteLine("------------------------");
                     _ = fetchFlightDataAsync();
                 });
            Console.ReadLine();
        }


        //Weather from OpenWeatherAPI
        private static void fetchWeatherDataAsync(string lat, string lon)
        {
            string html = string.Empty;
            string url = @"http://api.openweathermap.org/data/2.5/weather?lat=" + lat + "&lon=" + lon + "&appid=e3dfb30ec102a94a22bf516451f98e80";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }

            jsonParser(html);
        }


        private static void jsonParser(string html) {

            JObject JSON = JObject.Parse(html);
            Weather weather = null;
            weather = JsonConvert.DeserializeObject<Weather>(JSON.ToString());
            double unixTime = Convert.ToDouble(weather.TimeStamp);
            DateTime realTime = UnixTimeStampToDateTime(unixTime);

            string lat = weather.coord.lat;
            string lon = weather.coord.lon;
            string windSpeed = weather.Wind.speed;
            string windDirection = "0";

            try
            {
                 windDirection = weather.Wind.deg;
            }
            catch
            {
                 windDirection = "0";
            }
            string season = getSeason(realTime);

                var document = new BsonDocument
            {
                {"City" ,weather.City == "Qiryat Ono" ? "TLV" : "MAD" },
                {"TimeStamp", weather.TimeStamp },
                {"Date", realTime },
                {"Lat" , lat},
                {"Long", lon},
                {"WindSpeed", windSpeed},
                {"WindDirection", windDirection},
                {"Season", season}
            };
            
            string url = "mongodb+srv://AdielPerez:Ap308101062@adieltest-nhck7.azure.mongodb.net/test?retryWrites=true&w=majority";
            MongoClient dbClient = new MongoClient(url);

            var database = dbClient.GetDatabase("AdielTest");
            var collection = database.GetCollection<BsonDocument>("Weathers");

            collection.InsertOne(document);

           
            if (weather.City == "Qiryat Ono")
            {
                Console.WriteLine("Weather for TLV Sent Successfuly");
            }
            else
            {
                Console.WriteLine("Weather for MAD Sent Successfuly");
            }
            
        }
    

        private static string getSeason(DateTime realTime)
        {
            if (realTime.Month == 12 || realTime.Month == 1 ||
                realTime.Month == 2)
            {
                return "Winter";
            }
            if (realTime.Month == 3 || realTime.Month == 4 ||
                realTime.Month == 5)
            {
                return "Spring";
            }
            if (realTime.Month == 6 || realTime.Month == 7 ||
                realTime.Month == 8)
            {
                return "Summer";
            }
            if (realTime.Month == 9 || realTime.Month == 10 ||
                realTime.Month == 11)
            {
                return "Autumn";
            }
            return "";
        }


        //Flights from Flight Radar 24
        private static async Task fetchFlightDataAsync()
        {
            string urlForFlights = ("https://www.flightradar24.com/data/flights/nh962");
            var httpClient = new HttpClient();
            var htmlForFlight = await httpClient.GetStringAsync(urlForFlights);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlForFlight);
            string data = htmlDocument.ParsedText;
            retrieveFlights(data);
        }

        //Get HTML --> Flight DIV
        private static void retrieveFlights(string data)
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);
            string yesterdayDate = yesterday.Day.ToString() + " " + GetMonthName(yesterday.Month).ToString() + " " + yesterday.Year.ToString();
            string allDataSpecific = data;
            const string endIndexKey = "Flightradar24 AB";
            bool flightFlag = allDataSpecific.Contains(yesterdayDate);
            int numOfFlightsPerDay = Regex.Matches(allDataSpecific, yesterdayDate).Count;
            string[] flight = new string[numOfFlightsPerDay];

            if (flightFlag)
            {
                int index = allDataSpecific.IndexOf(yesterdayDate);
                int endIndex = allDataSpecific.IndexOf(endIndexKey);

                for (int i = 0; i < numOfFlightsPerDay / 2; i++)
                {
                    flight[i] = allDataSpecific.Substring(index, 1000);
                    parseFlight(flight[i], numOfFlightsPerDay);
                    index += 1107;
                }
            }
        }

        //Flight DIV --> Parse Flights
        private static void parseFlight(string flight, int numOfFlightsPerDay)
        {
            int stdIndex = 0;
            int atdIndex = 0;
            int staIndex = 0;
            int fromIndex = 0;
            int toIndex = 0;
            int landedIndex = 0;

            string STD = "";
            string ATD = "";
            string STA = "";
            string FROM = "";
            string TO = "";
            string Landed = "";

            double STDts = 0;
            double ATDts = 0;
            double STAts = 0;
            double Landedts = 0;

            for (int i = 0; i < numOfFlightsPerDay/2; i++)
            {
                if (flight.Contains("Landed"))
                {
                    landedIndex = flight.IndexOf("Landed");
                    Landed = flight.Substring(landedIndex + 6, 6);
                    DateTime parsedDate = DateTime.Parse(Landed);
                    parsedDate.ToUniversalTime();
                    Landedts = getTimedStamp(parsedDate);
                }
                if (flight.Contains("STD"))
                {
                    stdIndex = flight.IndexOf("STD");
                    STD = flight.Substring(stdIndex + 84, 5);
                    DateTime parsedDate = DateTime.Parse(STD);
                    parsedDate.ToUniversalTime();
                    STDts = getTimedStamp(parsedDate);
                }

                if (flight.Contains("ATD"))
                {
                    atdIndex = flight.IndexOf("ATD");
                    ATD = flight.Substring(atdIndex + 84, 5);
                    DateTime parsedDate = DateTime.Parse(ATD);
                    parsedDate.ToUniversalTime();
                    ATDts = getTimedStamp(parsedDate);

                }

                if (flight.Contains("STA"))
                {
                    staIndex = flight.IndexOf("STA");
                    STA = flight.Substring(staIndex + 84, 5);
                    DateTime parsedDate = DateTime.Parse(STA);
                    parsedDate.ToUniversalTime();
                    STAts = getTimedStamp(parsedDate);
                }

                if (flight.Contains("FROM"))
                {
                    fromIndex = flight.IndexOf("FROM");
                    FROM = flight.Substring(fromIndex + 37, 8).Trim();
                }

                if (flight.Contains("TO"))
                {
                    toIndex = flight.IndexOf("TO");
                    TO = flight.Substring(toIndex + 34, 7).Trim();
                }


                if (!string.IsNullOrEmpty(STD))
                {
                    var document = new BsonDocument
                    {
                        {"From" ,FROM },
                        {"To", TO },
                        {"Date", STDts },
                        {"Flight Time", Math.Abs(Landedts - ATDts) },
                        {"Scheduled Time Departure", STDts },
                        {"Scheduled Time Arrival", STAts },
                        {"Actual Time Departure", ATDts }
                    };

                    string url = "mongodb+srv://AdielPerez:Ap308101062@adieltest-nhck7.azure.mongodb.net/test?retryWrites=true&w=majority";
                    MongoClient dbClient = new MongoClient(url);

                    var database = dbClient.GetDatabase("AdielTest");
                    var collection = database.GetCollection<BsonDocument>("Flights");

                    collection.InsertOne(document);

                    Console.WriteLine("Flight TLV --> Madrid Sent Successfuly");
                
                }

            }

        }

        //Get Month by name
        private static object GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "Jan";
                case 2:
                    return "Feb";
                case 3:
                    return "Mar";
                case 4:
                    return "Apr";
                case 5:
                    return "May";
                case 6:
                    return "Jun";
                case 7:
                    return "Jul";
                case 8:
                    return "Aug";
                case 9:
                    return "Sep";
                case 10:
                    return "Oct";
                case 11:
                    return "Nov";
                case 12:
                    return "Dec";
                default:
                    Console.WriteLine("Error With DATE");
                    return month;
            }
        }


        //Get Timed stamp in Milliseconds
        private static long getTimedStamp(DateTime time)
        {
            var yesterday = time.AddDays(-1);
            long unixTime = ((DateTimeOffset)yesterday).ToUnixTimeMilliseconds();

            return unixTime;
        }
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }

}