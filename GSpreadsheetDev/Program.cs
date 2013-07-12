using Google.GData.Client;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;


namespace GSpreadsheetDev
{
    class Program
    {
        //http://www.larimer.org/databases/api.htm
        //https://developers.google.com/google-apps/spreadsheets/#working_with_cell-based_feeds
        //https://developers.google.com/google-apps/spreadsheets/#working_with_cell-based_feeds
        //http://www.larimer.org/webservices/PropertyInformation.cfc?wsdl
        static void Main(string[] args)
        {
            string userName = "";
            string password = "";

            // spreadsheet service and auth
            SpreadsheetsService _service = new SpreadsheetsService("MySpreadsheetService");
            _service.setUserCredentials(userName, password);

            // new spreadsheet query
            SpreadsheetQuery query = new SpreadsheetQuery();
            query.Title = "Foothills Green Pool Association"; // will find the spreadsheet using title

            // make the request
            SpreadsheetFeed feed = _service.Query(query);

            // get spreadsheet 
            SpreadsheetEntry spreadsheet = (SpreadsheetEntry)feed.Entries[0];

            // request the spreadsheets worksheet feeds
            WorksheetFeed wsFeeds = spreadsheet.Worksheets;

            // target worksheet
            WorksheetEntry worksheet = (WorksheetEntry)wsFeeds.Entries[2];

            // need request url for the list feed
            AtomLink listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);

            // fetch a list
            ListQuery listQuery = new ListQuery(listFeedLink.HRef.ToString());
            ListFeed listFeed = _service.Query(listQuery);

            // loop through each entry and update the legal cell for each row
            foreach (ListEntry entry in listFeed.Entries)
            {
                // get the property from larimer
                string parcel = "";
                string legal = "";
                Console.WriteLine(entry.Elements[1].Value);
                // look at each element in entry
                foreach (ListEntry.Custom element in entry.Elements)
                {
                    if (element.LocalName == "parcelnumber")
                    {
                        parcel = element.Value.Trim();
                        legal = GetLegal(parcel).Trim();
                    }

                    if (!string.IsNullOrEmpty(parcel))
                    {
                        // get property info
                    }

                    if (element.LocalName == "legal")
                    {
                        element.Value = legal;
                    }

                    if (element.LocalName == "subdivision")
                    {
                        element.Value = GetSubdivision(legal);
                    }

                    if (element.LocalName == "filing")
                    {
                        element.Value = GetFiling(legal);
                    }
                }

                entry.Update();
            }

            Console.WriteLine("Done, press any key");
            Console.ReadKey();
        }

        private static string GetLegal(string parcel)
        {
            PropertyInformation property = new PropertyInformation().GetPropertyInfo("", parcel);
            //PropertyInfo property = new PropertyInformation().GetPropertyInfo("", parcel);
            if (!string.IsNullOrEmpty(property.legal))
            {
                return property.legal;
            }
            return "";
        }

        private static string GetSubdivision(string legal)
        {
            foreach (var item in Subdivisions())
            {
                if (legal.ToLower().Contains(item.ToLower()))
                {
                    return item;
                }

                if (legal.ToLower().Contains("village w"))
                {
                    return "Village West";
                }

                if (legal.ToLower().Contains("village \"west\""))
                {
                    return "Village West";
                }

                if (legal.ToLower().Contains("village west4"))
                {
                    return "Village West";
                }
            }
            return "";
        }

        private static string GetFiling(string legal)
        {
            foreach (var item in Filings())
            {
                if (legal.ToLower().Contains(item))
                {
                    return item;
                }
            }
            return "";
        }

        private static List<string> Filings()
        {
            var list = new List<string>();
            list.Add("1st");
            list.Add("2nd");
            list.Add("3rd");
            list.Add("4th");
            list.Add("5th");
            list.Add("6th");
            list.Add("7th");
            list.Add("8th");
            list.Add("9th");
            list.Add("10th");
            return list;
        }
        private static List<string> Subdivisions()
        {
            var list = new List<string>();
            list.Add("Village West");
            list.Add("Foothills Village");
            list.Add("Foothills Green");
            list.Add("Aspen Knolls");
            list.Add("HillPond");
            list.Add("OTHER");
            return list;

        }


    }

    // this is just a stub until the api is back
    public class PropertyInformation
    {
        public string parcel { get; set; }
        public string legal { get; set; }
        public PropertyInformation GetPropertyInfo(string scheduleNum, string parcelNum)
        {
            return new PropertyInformation()
            {
                parcel = parcelNum,
                legal = "Lot 20 Foothills Green 1st"
            };
        }
    }

}
