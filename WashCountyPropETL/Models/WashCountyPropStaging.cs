using System;
using System.Collections.Generic;

namespace WashCountyPropETL.Models
{
    public partial class WashCountyPropStaging
    {
        public WashCountyPropStaging()
        {
            WashCountyProp = new HashSet<WashCountyProp>();
        }

        public int StagingId { get; set; }
        public DateTime? ExtractDateTime { get; set; }
        public string Source { get; set; }
        public string TaxLotId { get; set; }
        public string SiteAddress { get; set; }
        public string PropAcctId { get; set; }
        public string PropClass { get; set; }
        public string NeighCode { get; set; }
        public string LatLong { get; set; }
        public string SaleDate { get; set; }
        public string SaleInstr { get; set; }
        public string SaleDeed { get; set; }
        public string SalePrice { get; set; }
        public string RollDate { get; set; }
        public string TaxCode { get; set; }
        public string MarketLandValue { get; set; }
        public string MarketBldgValue { get; set; }
        public string SpecialMarketValue { get; set; }
        public string TaxableAssessedValue { get; set; }
        public string Legal { get; set; }
        public string LotSize { get; set; }
        public string BldgArea { get; set; }
        public string YearBuilt { get; set; }

        public virtual ICollection<WashCountyProp> WashCountyProp { get; set; }
    }
}
