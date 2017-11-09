using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using WashCountyPropETL.Models;

namespace WashCountyPropETL
{
    class Program
    {
        static void Main(string[] args)
        {
            //Welcome to my real estate property web data scraper
            Console.WriteLine("Washington County Property ETL"); 
            
            //Initialize all possible search terms
            var possibleSearchTerms = InitializeSearchTerms();

            //Verify the taxlot IDs by searching through each possible search term
            List<string> taxLotIDResult = VerifyID(possibleSearchTerms);

            //Extract the property data with the verified taxlot IDs
            var propertyData = ExtractPropertyData(taxLotIDResult);

            //Save property data to the database
            SavePropertyData(propertyData);

            Console.WriteLine("ETL End");
            Console.ReadLine();
        }
        public static List<List<string>> InitializeSearchTerms()
        {
            List<List<string>> searchTerms = new List<List<string>>
            {
                new List<string> {"1", "2", "3" }
                , new List<string> {"N", "S" }
                , new List<string> {"1", "2", "3", "4", "5", "6" }
                , new List<string> {"0", "1", "2", "3" }
                , new List<string> {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
                , new List<string> {"0", "A", "B", "C", "D" }
                , new List<string> {"0", "A", "B", "C", "D" }
                , new List<string> {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
                , new List<string> {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }
            };
            return searchTerms;
        }
        public static List<string> VerifyID(List<List<string>> possibleTaxLots)
        {
            string startTaxLot = "1";
            Console.WriteLine("Starting TaxLotID (Default = 1): ");
            startTaxLot = Console.ReadLine();
            
            int loopcount = 1;
            Console.WriteLine("Enter number of verification loops (Default = 1): ");
            loopcount = Convert.ToInt32(Console.ReadLine());

            int group = startTaxLot.Count()-1;
            int subgroup = possibleTaxLots[group].IndexOf(Convert.ToString(startTaxLot.Last()));
            var validTaxLots = new List<string>();
            bool resultCap = false;
            bool searchFailure = false;
            string searchTerm = startTaxLot;
            //Traverse the county website's property search API to figure out which taxlots are valid
            for (int i = 0; i < loopcount; i++)
            {
                Thread.Sleep(10); //prevent DDOSing
                string rawHtmlIDList = SearchValidTaxlotIDs(searchTerm).Result;
                resultCap = rawHtmlIDList.Contains("Search exceeded the maximum return limit.");
                searchFailure = rawHtmlIDList.Contains("No Records Found");
                Console.WriteLine($"search term: {searchTerm}");
                //Console.WriteLine($"search failure: {searchFailure} | resultCap: {resultCap}");

                //Valid search results: verify and record taxLot IDs
                if (resultCap == false & searchFailure == false ) 
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(rawHtmlIDList);
                    HtmlNodeCollection taxLotIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table//tr/td/table//tr//td//a");
                    var rawValidTaxLotIDs = taxLotIDNode.Select(node => node.InnerText).ToList();
                    validTaxLots.AddRange(rawValidTaxLotIDs);
                    Console.WriteLine($"VerifyID Time : {DateTime.Now} at possibleTaxLots");
                }
                //Excessive number of results: step out
                if (resultCap == true) 
                {
                    group++;
                    subgroup = 0;
                    searchTerm = String.Concat(searchTerm, possibleTaxLots[group][subgroup]);
                }
                //End of possible search term subgroup: step in & step up
                else if (subgroup == possibleTaxLots[group].Count() - 1) 
                {
                    while (searchTerm.Last() == Convert.ToChar(possibleTaxLots[group].Last()))
                    {
                        searchTerm = searchTerm.Remove(searchTerm.Count() - 1);
                        group--;
                    }
                    subgroup = possibleTaxLots[group].IndexOf(Convert.ToString(searchTerm.Last()));
                    searchTerm = searchTerm.Remove(searchTerm.Count() - 1);
                    subgroup++;
                    searchTerm = String.Concat(searchTerm, possibleTaxLots[group][subgroup]);
                }
                //Empty result: step up
                else if (searchFailure == true)
                {
                    searchTerm = searchTerm.Remove(searchTerm.Count() - 1);
                    subgroup++;
                    searchTerm = String.Concat(searchTerm, possibleTaxLots[group][subgroup]);
                }
                //Valid search results: step up
                else if (resultCap == false & searchFailure == false)
                {
                    searchTerm = searchTerm.Remove(searchTerm.Count() - 1);
                    subgroup++;
                    searchTerm = String.Concat(searchTerm, possibleTaxLots[group][subgroup]);
                }
                //else { Console.WriteLine("TaxlotID verification failed"); }
            }
            return validTaxLots;
        }
        public static async Task<string> SearchValidTaxlotIDs(string searchTerm)
        {
            //RESTful API to automate search function
            var responseString = "";
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    {"theTLNO", searchTerm }
                    , {"theAddress", "" }
                    , {"theRnum", "" }
                    , {"OwnerName", "" }
                    , {"btnSubmit", "Search" }
                };
                client.BaseAddress = new Uri(@"http://washims.co.washington.or.us");
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(@"/GIS/index.cfm?id=20&sid=2", content);
                responseString = await response.Content.ReadAsStringAsync();
            }
            return responseString;
        }
        public static List<Dictionary<string, string>> ExtractPropertyData(List<string> validTaxLotIDs)
        {
            //HTMLAgilityPack and XPath to extract detailed property data
            var PropertiesInfo = new List<Dictionary<string, string>>() { };
            string taxLotID;
            string siteAddress;
            string propertyID;
            string propertyClass;
            string neighCode;
            string latLong;
            string saleDate;
            string saleInstr;
            string saleDeed;
            string salePrice;
            string rollDate;
            string taxCode;
            string marketLandValue;
            string marketBldgValue;
            string specialMarketValue;
            string marketTotalValue;
            string taxableAssessedValue;
            string legal;
            string lotSize;
            string bldgSqFt;
            string yearBuilt;

            for (var i = 0; i < validTaxLotIDs.Count()-1; i++)
            { 
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument htmlDoc = hw.Load("http://washims.co.washington.or.us/GIS/index.cfm?id=30&sid=3&IDValue=" + validTaxLotIDs[i]);
                HtmlNodeCollection taxLotIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[3]/td[2]");
                if (taxLotIDNode.Select(node => node.InnerText).ElementAtOrDefault(0) != " &nbsp;")
                {
                    taxLotID = taxLotIDNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    Console.WriteLine($"TaxLot: {taxLotID}");
                    HtmlNodeCollection siteAddressNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[2]/td[2]");
                    if (siteAddressNode == null) { siteAddress = "N/A"; }
                    else
                    {
                        siteAddress = siteAddressNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection propertyIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[4]/td[2]");
                    if (propertyIDNode == null) { propertyID = "N/A"; }
                    else
                    {
                        propertyID = propertyIDNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection propertyClassNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[5]/td[2]/text()");
                    if (propertyClassNode == null) { propertyClass = "N/A"; }
                    else
                    {
                        propertyClass = propertyClassNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection neighCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[6]/td[2]");
                    if (neighCodeNode == null) { neighCode = "N/A"; }
                    else
                    {
                        neighCode = neighCodeNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection latLongNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[7]/td[2]");
                    if (latLongNode == null) { latLong = "N/A"; }
                    else
                    {
                        latLong = latLongNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection saleDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[1]");
                    if (saleDateNode == null) { saleDate = "N/A"; }
                    else
                    {
                        saleDate = saleDateNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection saleInstrNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[2]");
                    if (saleInstrNode == null) { saleInstr = "N/A"; }
                    else
                    {
                        saleInstr = saleInstrNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection saleDeedNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[3]");
                    if (saleDeedNode == null) { saleDeed = "N/A"; }
                    else
                    {
                        saleDeed = saleDeedNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection salePriceNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[4]");
                    if (salePriceNode == null) { salePrice = "N/A"; }
                    else
                    {
                        salePrice = salePriceNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection rollDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[2]/td[2]");
                    if (rollDateNode == null) { rollDate = "N/A"; }
                    else
                    {
                        rollDate = rollDateNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection taxCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[3]/td[2]");
                    if (taxCodeNode == null) { taxCode = "N/A"; }
                    else
                    {
                        taxCode = taxCodeNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection marketLandValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[4]/td[2]");
                    if (marketLandValueNode == null) { marketLandValue = "N/A"; }
                    else
                    {
                        marketLandValue = marketLandValueNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection marketBldgValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[5]/td[2]");
                    if (marketBldgValueNode == null) { marketBldgValue = "N/A"; }
                    else
                    {
                        marketBldgValue = marketBldgValueNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection specialMarketValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[6]/td[2]");
                    if (specialMarketValueNode == null) { specialMarketValue = "N/A"; }
                    else
                    {
                        specialMarketValue = specialMarketValueNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection marketTotalValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[7]/td[2]");
                    if (marketTotalValueNode == null) { marketTotalValue = "N/A"; }
                    else
                    {
                        marketTotalValue = marketTotalValueNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection taxableAssessedValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[8]/td[2]");
                    if (taxableAssessedValueNode == null) { taxableAssessedValue = "N/A"; }
                    else
                    {
                        taxableAssessedValue = taxableAssessedValueNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection legalNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[9]/td[2]");
                    if (legalNode == null) { legal = "N/A"; }
                    else
                    {
                        legal = legalNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection lotSizeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[10]/td[2]");
                    if (lotSizeNode == null) { lotSize = "N/A"; }
                    else
                    {
                        lotSize = lotSizeNode.Select(node => node.InnerText).ElementAtOrDefault(0);
                    }
                    HtmlNodeCollection bldgSqFtNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[11]/td[2]");
                    if (bldgSqFtNode == null) { bldgSqFt = "N/A"; }
                    else{ bldgSqFt = bldgSqFtNode.Select(node => node.InnerText).ElementAtOrDefault(0); }
                    HtmlNodeCollection yearBuiltNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[12]/td[2]");
                    if (yearBuiltNode == null) { yearBuilt = "N/A"; }
                    else { yearBuilt = yearBuiltNode.Select(node => node.InnerText).ElementAtOrDefault(0); }

                    var PropertyInfo = new Dictionary<string, string>() {
                    {"taxLotID", taxLotID }
                    , {"SiteAddress", siteAddress }//.ElementAt(0) }
                    , {"PropertyID", propertyID }//.ElementAt(0) }
                    , {"PropertyClass", propertyClass }//.ElementAt(0) }
                    , {"NeighCode", neighCode }//.ElementAt(0) }
                    , {"LatLong", latLong }//.ElementAt(0) }
                    , {"SaleDate", saleDate }//.ElementAt(0) }
                    , {"SaleInstr", saleInstr }//.ElementAt(0) }
                    , {"SaleDeed", saleDeed }//.ElementAt(0) }
                    , {"SalePrice", salePrice }//.ElementAt(0) }
                    , {"RollDate", rollDate }//.ElementAt(0) }
                    , {"TaxCode", taxCode }//.ElementAt(0)}
                    , {"MarketLandValue", marketLandValue }//.ElementAt(0) }
                    , {"MarketBuildingValue", marketBldgValue }//.ElementAt(0) }
                    , {"SpecialMarketValue", specialMarketValue }//.ElementAt(0) }
                    , {"MarketTotalValue", marketTotalValue }//.ElementAt(0) }
                    , {"TaxableAssessedValue", taxableAssessedValue }//.ElementAt(0) }
                    , {"Legal", legal }//.ElementAt(0) }
                    , {"LotSize", lotSize }//.ElementAt(0) }
                    , {"BldgSqFt", bldgSqFt }//.ElementAt(0) }
                    , {"YearBuilt", yearBuilt }//.ElementAt(0) }
                };
                    PropertiesInfo.Add(PropertyInfo);
                    Thread.Sleep(10); //wait to prevent DDOSing
                    Console.WriteLine($"Extract Data Time : {DateTime.Now}");
                };
                }
            return PropertiesInfo;
        }
        public static void SavePropertyData(List<Dictionary<string, string>> propertyDataSave)
        {
            //Entity Framework Core to save property data into SQL server database
            using (var context = new RealEstatePropContext())
            {
                for (var i = 0; i < propertyDataSave.Count; i++)
                {
                    context.WashCountyPropStaging.Add(new WashCountyPropStaging
                    {
                        TaxLotId = propertyDataSave[i]["taxLotID"],
                        Source = "WashingtonCountyWebsite",
                        SiteAddress = propertyDataSave[i]["SiteAddress"],
                        ExtractDateTime = DateTime.Now,
                        PropAcctId = propertyDataSave[i]["PropertyID"],
                        PropClass = propertyDataSave[i]["PropertyClass"],
                        NeighCode = propertyDataSave[i]["NeighCode"],
                        LatLong = propertyDataSave[i]["LatLong"],
                        SaleDate = propertyDataSave[i]["SaleDate"],
                        SaleInstr = propertyDataSave[i]["SaleInstr"],
                        SaleDeed = propertyDataSave[i]["SaleDeed"],
                        SalePrice = propertyDataSave[i]["SalePrice"],
                        RollDate = propertyDataSave[i]["RollDate"],
                        TaxCode = propertyDataSave[i]["TaxCode"],
                        MarketLandValue = propertyDataSave[i]["MarketLandValue"],
                        MarketBldgValue = propertyDataSave[i]["MarketBuildingValue"],
                        SpecialMarketValue = propertyDataSave[i]["SpecialMarketValue"],
                        TaxableAssessedValue = propertyDataSave[i]["TaxableAssessedValue"],
                        Legal = propertyDataSave[i]["Legal"],
                        LotSize = propertyDataSave[i]["LotSize"],
                        BldgArea = propertyDataSave[i]["BldgSqFt"],
                        YearBuilt = propertyDataSave[i]["YearBuilt"]
                    });
                }
                context.SaveChanges();
            }
        }
    }
}
