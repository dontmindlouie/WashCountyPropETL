using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;

namespace WashCountyPropETL
{
    class ETLclasses
    {
        public Tuple<IEnumerable<string>, bool> SearchID(string searchTerm)
        {
            string rawHtmlIDList = SearchValidTaxlotIDs(searchTerm).Result;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtmlIDList);
            HtmlNodeCollection taxLotIDNode = htmlDoc.DocumentNode.SelectNodes("/html/body/table//tr/td/table//tr//td//a");
            var validTaxlotIDs = taxLotIDNode.Select(node => node.InnerText);
            Console.WriteLine("First valid taxlot ID of thi search: " + validTaxlotIDs.ElementAt(0));
            Console.WriteLine("Count of valid taxlot IDs of this search: " + validTaxlotIDs.Count());
            bool resultCap = false;
            resultCap = rawHtmlIDList.Contains("Search exceeded the maximum return limit.");
            Console.WriteLine("Result Capacity Reached: " + resultCap.ToString());
            return new Tuple<IEnumerable<string>, bool>(validTaxlotIDs, resultCap);
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
    }
}
