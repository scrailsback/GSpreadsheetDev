using Google.GData.Client;
using Google.GData.Spreadsheets;
using System;
using System.Collections.Generic;
using GSpreadsheetDev.LarimerParcelInfo;


namespace GSpreadsheetDev
{
    class Program
    {
        //http://www.larimer.org/databases/api.htm
        //https://developers.google.com/google-apps/spreadsheets/#working_with_cell-based_feeds
        //https://developers.google.com/google-apps/spreadsheets/#working_with_cell-based_feeds
        //http://www.larimer.org/webservices/PropertyInformation.cfc?wsdl

        public static SpreadsheetsService _service = new SpreadsheetsService("MySpreadsheetService");
        public static string userName = "";
        public static string password = "";

        static void Main()
        {
            
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
            ListFeed parcelList = _service.Query(listQuery);


            // fetch list of members
            WorksheetEntry memberWorksheet = (WorksheetEntry)wsFeeds.Entries[0];
            AtomLink memberListFeedLink = memberWorksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
            ListQuery memberListQuery = new ListQuery(memberListFeedLink.HRef.ToString());
            ListFeed memberList = _service.Query(memberListQuery);

            // loop through each entry and update the legal cell for each row
            int i = 0;
            foreach (ListEntry entry in parcelList.Entries)
            {
                i++;
                PropertyInfo property = new PropertyInformation().GetPropertyInfo("", entry.Elements[1].Value.ToString());
                Parcel parcel = new Parcel(property, memberList);

                Console.WriteLine("{0} => {1}, {2}", i,  parcel.Number, parcel.IsMember);

                // look at each element in entry
                foreach (ListEntry.Custom element in entry.Elements)
                {
                    
                    if (element.LocalName == "legal")
                    {
                        element.Value = parcel.Legal;
                    }

                    if (element.LocalName == "subdivision")
                    {
                        element.Value = parcel.Subdivision;
                    }

                    if (element.LocalName == "filing")
                    {
                        element.Value = parcel.Filing;
                    }

                    if (element.LocalName == "ismember")
                    {
                        element.Value = parcel.IsMember == true ? "1" : "0";
                    }

                    if (element.LocalName == "grantsigned")
                    {
                        element.Value = parcel.GrantSigned;
                    }

                    if (element.LocalName == "grantfiled")
                    {
                        element.Value = parcel.GrantFiled;
                    }

                    if (element.LocalName == "memberid")
                    {
                        element.Value = parcel.MemberId;
                    }

                    if (element.LocalName == "annexmarkercolor")
                    {
                        var color = "";
                        if (parcel.IsMember)
                        {
                            color = "small_blue";
                        }
                        else
                        {
                            color = parcel.IsAnnexed ? "small_red" : "small_yellow";
                        }
                         
                         element.Value = color;
                    }

                }

                entry.Update();
            }

            Console.WriteLine("Done, press any key");
            Console.ReadKey();
        }
    }

    public class Parcel
    {
        public string Number { get; set; }
        public string Owner { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string Legal { get; set; }
        public string Subdivision { get; set; }
        public string Filing { get; set; }
        public string GrantSigned { get; set; }
        public string GrantFiled { get; set; }
        public string MemberId { get; set; }
        public bool IsMember { get; set; }
        public bool IsAnnexed { get; set; }
        public string MarkerColor { get; set; }

        public Parcel() { }

        public Parcel(PropertyInfo p, ListFeed memberList)
        {
            this.Number = p.parcelNumber;
            this.Owner = p.ownerName1;
            this.Address = p.address;
            this.City = p.city;
            this.Zip = p.zipCode;
            this.Legal = p.legal;
            this.Subdivision = !string.IsNullOrWhiteSpace(p.subdivDescr) ? p.subdivDescr : GetSubdivision();
            this.Filing = GetFiling();
            this.IsAnnexed = GetAnnex();
            Membership m = GetLienInfo(memberList);
            this.IsMember = !string.IsNullOrWhiteSpace(m.MemberId) ? true : false;
            this.GrantFiled = !string.IsNullOrWhiteSpace(m.GrantFiled) ? m.GrantSigned : "";
            this.GrantSigned =  !string.IsNullOrWhiteSpace(m.GrantSigned) ? m.GrantSigned : "";
            this.MemberId = !string.IsNullOrWhiteSpace(m.MemberId) ? m.MemberId : "";
        }

        private string GetFiling()
        {
            if (string.IsNullOrWhiteSpace(this.Legal)) {
                return "";
            }

            foreach(var item in Filings()) {
                if (Legal.ToLower().Contains(item.ToLower())) {
                    return item.ToString();
                }
            }
            return "";
        }

        private bool GetAnnex()
        {
            // Foothills Village First Filing, 
            // Foothills Green First Filing, 
            // Village West Sixth Filing, 
            // Village West Seventh Filing, 
            // Village West Eighth Filing, 
            // Village West Ninth Filing, 
            // Aspen Knolls First Filing, 
            // Hill Pond on Spring Creek Second Filing
            // Village West Fifth Filing which are located on 
	        // - Independence Road,  
	        // - Independence Court, 
            // - and the east side of Constitution Avenue between Independence Road and Winfield Drive 

            if (this.Subdivision.ToLower().Contains("foothills village"))
            {
                return this.Legal.ToLower().Contains("1st");
            }

            if (this.Subdivision.ToLower().Contains("foothills green"))
            {
                return this.Legal.ToLower().Contains("1st");
            }

            if (this.Subdivision.ToLower().Contains("village west"))
            {
                if (this.Legal.ToLower().Contains("5th"))
                {
                    if (this.Address.ToLower().Contains("independence rd"))
                    {
                        return true;
                    }

                    if (this.Address.ToLower().Contains("independence ct"))
                    {
                        return true;
                    }

                    if (this.Address.ToLower().Contains("constitution ave"))
                    {
                        string[] d = this.Address.Split(' ');
                        int n = Int32.Parse(d[0]);
                        return n % 2 == 0;
                    }
                }
                
                
                if (this.Legal.ToLower().Contains("6th"))
                {
                    return true;
                }

                if (this.Legal.ToLower().Contains("7th"))
                {
                    return true;
                }

                if (this.Legal.ToLower().Contains("8th"))
                {
                    return true;
                }

                if (this.Legal.ToLower().Contains("9th"))
                {
                    return true;
                }                
            }

            if (this.Legal.ToLower().Contains("aspen knolls"))
            {
                return true;
            }

            if (this.Legal.ToLower().Contains("hillpond"))
            {
                return true;
            }
            return false;
        }

        private string[] Filings()
        {
            return new string[] { "1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th", "11th", "12th", "13th", "14th", "15th" };
        }

        private string[] Subdivisions()
        {
            return new string[] { "Foothills Green", "Foothills Village", "Foothills West", "Hill Pond", "Peck Minor", "Aspen Knolls" };
        }

        private string GetSubdivision()
        {
            if (this.Legal.ToLower().Contains("foothills green"))
            {
                return "FOOTHILLS GREEN";
            }

            if (this.Legal.ToLower().Contains("foothills village"))
            {
                return "FOOTHILLS VILLAGE";
            }

            if (this.Legal.ToLower().Contains("foothills west"))
            {
                return "FOOTHILLS WEST";
            }

            if (this.Legal.ToLower().Contains("hllpond"))
            {
                return "HILL POND";
            }

            if (this.Legal.ToLower().Contains("aspen"))
            {
                return "ASPEN KNOLLS";
            }
            return "";
        }

        private Membership GetLienInfo(ListFeed memberList)
        {
            Membership m = new Membership();
            foreach (ListEntry entry in memberList.Entries)
            {
                if (entry.Elements[8].Value.ToString().Replace("-","") == this.Number)
                {
                    m.GrantFiled = entry.Elements[6].Value.ToString();
                    m.GrantSigned = entry.Elements[5].Value.ToString();
                    m.MemberId = entry.Elements[0].Value.ToString();
                }
            }
            return m;
        }
    }

    public class Membership
    {
        public string MemberId { get; set; }
        public string GrantSigned { get; set; }
        public string GrantFiled { get; set; }
        public Membership() { }
    }
}
