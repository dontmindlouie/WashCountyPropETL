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
                if (resultCap == false & searchFailure == false) 
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
            for (var i = 0; i < validTaxLotIDs.Count()-1; i++)
            { 
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument htmlDoc = hw.Load("http://washims.co.washington.or.us/GIS/index.cfm?id=30&sid=3&IDValue=" + validTaxLotIDs[i]);
                HtmlNodeCollection taxLotIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[3]/td[2]");
                var taxLotID = taxLotIDNode.Select(node => node.InnerText);
                HtmlNodeCollection siteAddressNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[2]/td[2]");
                var siteAddress = siteAddressNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection propertyIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[4]/td[2]");
                var propertyID = propertyIDNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection propertyClassNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[5]/td[2]/text()");
                var propertyClass = propertyClassNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection neighCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[6]/td[2]");
                var neighCode = neighCodeNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection latLongNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[7]/td[2]");
                var latLong = latLongNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection saleDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[1]");
                var saleDate = saleDateNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection saleInstrNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[2]");
                var saleInstr = saleInstrNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection saleDeedNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[3]");
                var saleDeed = saleDeedNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection salePriceNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[4]");
                var salePrice = salePriceNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection rollDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[2]/td[2]");
                var rollDate = rollDateNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection taxCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[3]/td[2]");
                var taxCode = taxCodeNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection marketLandValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[4]/td[2]");
                var marketLandValue = marketLandValueNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection marketBldgValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[5]/td[2]");
                var marketBldgValue = marketBldgValueNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection specialMarketValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[6]/td[2]");
                var specialMarketValue = specialMarketValueNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection marketTotalValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[7]/td[2]");
                var marketTotalValue = marketTotalValueNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection taxableAssessedValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[8]/td[2]");
                var taxableAssessedValue = taxableAssessedValueNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection legalNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[9]/td[2]");
                var legal = legalNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection lotSizeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[10]/td[2]");
                var lotSize = lotSizeNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection bldgSqFtNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[11]/td[2]");
                var bldgSqFt = bldgSqFtNode.Select(node => node.InnerText).DefaultIfEmpty("");
                HtmlNodeCollection yearBuiltNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[12]/td[2]");
                var yearBuilt = yearBuiltNode.Select(node => node.InnerText).DefaultIfEmpty("");

                var PropertyInfo = new Dictionary<string, string>() {
                    {"taxLotID", taxLotID.ElementAt(0) }
                    , {"SiteAddress", siteAddress.ElementAt(0) }
                    , {"PropertyID", propertyID.ElementAt(0) }
                    , {"PropertyClass", propertyClass.ElementAt(0) }
                    , {"NeighCode", neighCode.ElementAt(0) }
                    , {"LatLong", latLong.ElementAt(0) }
                    , {"SaleDate", saleDate.ElementAt(0) }
                    , {"SaleInstr", saleInstr.ElementAt(0) }
                    , {"SaleDeed", saleDeed.ElementAt(0) }
                    , {"SalePrice", salePrice.ElementAt(0) }
                    , {"RollDate", rollDate.ElementAt(0) }
                    , {"TaxCode", taxCode.ElementAt(0)}
                    , {"MarketLandValue", marketLandValue.ElementAt(0) }
                    , {"MarketBuildingValue", marketBldgValue.ElementAt(0) }
                    , {"SpecialMarketValue", specialMarketValue.ElementAt(0) }
                    , {"MarketTotalValue", marketTotalValue.ElementAt(0) }
                    , {"TaxableAssessedValue", taxableAssessedValue.ElementAt(0) }
                    , {"Legal", legal.ElementAt(0) }
                    , {"LotSize", lotSize.ElementAt(0) }
                    , {"BldgSqFt", bldgSqFt.ElementAt(0) }
                    , {"YearBuilt", yearBuilt.ElementAt(0) }
                };
                PropertiesInfo.Add(PropertyInfo);

                Thread.Sleep(10); //wait to prevent DDOSing
                Console.WriteLine($"Extract Data Time : {DateTime.Now}");
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
