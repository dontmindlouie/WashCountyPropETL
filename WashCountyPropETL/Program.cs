using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using WashCountyPropETL.Models;

namespace WashCountyPropETL
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Start Washington County Property");
            Console.ReadLine();

            //Possible TaxLotIDs
            var possibleSearchTerms = InitializeSearchTerms();

            //Verify TaxLotIDs
            List<string> taxLotIDResult = VerifyID(possibleSearchTerms);

            //Extract Property Data
            var propertyData = ExtractPropertyData(taxLotIDResult);

            //Save Property Data
            using(var context = new RealEstatePropContext())
            {
                context.WashCountyPropStaging.Add(new WashCountyPropStaging {
                    TaxLotId = propertyData[0]["taxLotID"]
                    , SiteAddress = propertyData[0]["SiteAddress"]
                    , PropAcctId = propertyData[0]["PropertyID"]
                    , PropClass = propertyData[0]["PropertyClass"]
                    , NeighCode = propertyData[0]["NeighCode"]
                    , LatLong = propertyData[0]["LatLong"]
                    , SaleDate = propertyData[0]["SaleDate"]
                    , SaleInstr = propertyData[0]["SaleInstr"]
                    , SaleDeed = propertyData[0]["SaleDeed"]
                    , SalePrice = propertyData[0]["SalePrice"]
                    , RollDate = propertyData[0]["RollDate"]
                    , TaxCode = propertyData[0]["TaxCode"]
                    , MarketLandValue = propertyData[0]["MarketLandValue"]
                    , MarketBldgValue = propertyData[0]["MarketBuildingValue"]
                    , SpecialMarketValue = propertyData[0]["SpecialMarketValue"]
                    , TaxableAssessedValue = propertyData[0]["TaxableAssessedValue"]
                    , Legal = propertyData[0]["TaxableAssessedValue"]
                    , LotSize = propertyData[0]["LotSize"]
                    , BldgArea = propertyData[0]["BldgSqFt"]
                    , YearBuilt = propertyData[0]["YearBuilt"]
                    });
                context.SaveChanges();
            }

            Console.WriteLine("ETL End");
            Console.ReadLine();
        }
        public static List<List<string>> InitializeSearchTerms()
        {
            List<List<string>> searchTerms = new List<List<string>>
            {
                new List<string> {"1", "2", "3" }
                , new List<string> {"N", "S" }
                , new List<string> {"0", "1", "2", "3", "4", "5", "6" }
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
            var searchTerm = possibleTaxLots[0][0];
            var validTaxLots = new List<string>();

            string rawHtmlIDList = SearchValidTaxlotIDs(searchTerm).Result;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtmlIDList);
            HtmlNodeCollection taxLotIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table//tr/td/table//tr//td//a");
            var rawValidTaxLotIDs = taxLotIDNode.Select(node => node.InnerText).ToList();
            bool resultCap = false;
            resultCap = rawHtmlIDList.Contains("Search exceeded the maximum return limit.");
            validTaxLots.AddRange(rawValidTaxLotIDs);
            return validTaxLots;
        }
        public static async Task<string> SearchValidTaxlotIDs(string searchTerm)
        {
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
            var PropertiesInfo = new List<Dictionary<string, string>>() { };
            //for (var i = 0; i < validTaxLotIDs.Count(); i++){ //Loop through extracting property data
            
                HtmlWeb hw = new HtmlWeb();
                HtmlDocument htmlDoc = hw.Load("http://washims.co.washington.or.us/GIS/index.cfm?id=30&sid=3&IDValue=" + validTaxLotIDs[0]);
                HtmlNodeCollection siteAddressNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[2]/td[2]");
                var siteAddress = siteAddressNode.Select(node => node.InnerText);
                HtmlNodeCollection propertyIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[4]/td[2]");
                var propertyID = propertyIDNode.Select(node => node.InnerText);
                HtmlNodeCollection propertyClassNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[5]/td[2]/text()");
                var propertyClass = propertyClassNode.Select(node => node.InnerText);
                HtmlNodeCollection neighCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[6]/td[2]");
                var neighCode = neighCodeNode.Select(node => node.InnerText);
                HtmlNodeCollection latLongNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[3]//tr[7]/td[2]");
                var latLong = latLongNode.Select(node => node.InnerText);
                HtmlNodeCollection saleDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[1]");
                var saleDate = saleDateNode.Select(node => node.InnerText);
                HtmlNodeCollection saleInstrNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[2]");
                var saleInstr = saleInstrNode.Select(node => node.InnerText);
                HtmlNodeCollection saleDeedNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[3]");
                var saleDeed = saleDeedNode.Select(node => node.InnerText);
                HtmlNodeCollection salePriceNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[4]//tr[3]/td[4]");
                var salePrice = salePriceNode.Select(node => node.InnerText);
                HtmlNodeCollection rollDateNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[2]/td[2]");
                var rollDate = rollDateNode.Select(node => node.InnerText);
                HtmlNodeCollection taxCodeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[3]/td[2]");
                var taxCode = taxCodeNode.Select(node => node.InnerText);
                HtmlNodeCollection marketLandValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[4]/td[2]");
                var marketLandValue = marketLandValueNode.Select(node => node.InnerText);
                HtmlNodeCollection marketBldgValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[5]/td[2]");
                var marketBldgValue = marketBldgValueNode.Select(node => node.InnerText);
                HtmlNodeCollection specialMarketValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[6]/td[2]");
                var specialMarketValue = specialMarketValueNode.Select(node => node.InnerText);
                HtmlNodeCollection marketTotalValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[7]/td[2]");
                var marketTotalValue = marketTotalValueNode.Select(node => node.InnerText);
                HtmlNodeCollection taxableAssessedValueNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[8]/td[2]");
                var taxableAssessedValue = taxableAssessedValueNode.Select(node => node.InnerText);
                HtmlNodeCollection legalNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[9]/td[2]");
                var legal = legalNode.Select(node => node.InnerText);
                HtmlNodeCollection lotSizeNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[10]/td[2]");
                var lotSize = lotSizeNode.Select(node => node.InnerText);
                HtmlNodeCollection bldgSqFtNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[11]/td[2]");
                var bldgSqFt = bldgSqFtNode.Select(node => node.InnerText);
                HtmlNodeCollection yearBuiltNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table[3]//tr/td[3]/table[5]//tr[12]/td[2]");
                var yearBuilt = yearBuiltNode.Select(node => node.InnerText);

                var PropertyInfo = new Dictionary<string, string>() {
                    {"taxLotID", validTaxLotIDs[0] }
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
                    , {"TaxCode", rollDate.ElementAt(0)}
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
            //}
            return PropertiesInfo;
        }

    }
}
