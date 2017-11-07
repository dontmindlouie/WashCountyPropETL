using System;
using System.Collections.Generic;

namespace WashCountyPropETL.Models
{
    public partial class WashCountyProp
    {
        public int PropertyId { get; set; }
        public int? StagingId { get; set; }
        public string Source { get; set; }
        public string TaxLotId { get; set; }
        public DateTime? ExtractDateTime { get; set; }
        public string SiteAddress { get; set; }
        public string PropAcctId { get; set; }
        public string PropClass { get; set; }
        public string NeighCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public DateTime? SaleDate { get; set; }
        public string SaleInstr { get; set; }
        public string SaleDeed { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? RollDate { get; set; }
        public string TaxCode { get; set; }
        public decimal? MarketLandValue { get; set; }
        public decimal? MarketBldgValue { get; set; }
        public decimal? SpecialMarketValue { get; set; }
        public decimal? TaxableAssessedValue { get; set; }
        public string Legal { get; set; }
        public string LotSize { get; set; }
        public int? BldgArea { get; set; }
        public short? YearBuilt { get; set; }

        public virtual WashCountyPropStaging Staging { get; set; }
    }
}
