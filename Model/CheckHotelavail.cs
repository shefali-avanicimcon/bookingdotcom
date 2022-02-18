using System;
using System.Collections.Generic;
using System.Text;

namespace Bookindotcom.Model
{
   public class CheckHotelavail
    {
        public Meta meta { get; set; }
        public List<Result> result { get; set; }
    }
   
    public class RackRate
    {
        public double price { get; set; }
        public string currency { get; set; }
    }

    public class ExtraChargesBreakdown
    {
    }

    public class MinPrice
    {
        public string currency { get; set; }
        public double price { get; set; }
        public ExtraChargesBreakdown extra_charges_breakdown { get; set; }
    }

    public class IncrementalPrice
    {
        public string currency { get; set; }
        public double price { get; set; }
        public ExtraChargesBreakdown extra_charges_breakdown { get; set; }
    }

    public class Block
    {
        public string block_id { get; set; }
        public string room_description { get; set; }
        public bool lunch_included { get; set; }
        public string mealplan_description { get; set; }
        public int max_adults { get; set; }
        public bool refundable { get; set; }
        public bool is_flash_deal { get; set; }
        public RackRate rack_rate { get; set; }
        public string taxes { get; set; }
        public MinPrice min_price { get; set; }
        public List<CancellationInfo> cancellation_info { get; set; }
        public double room_surface_in_m2 { get; set; }
        public bool breakfast_included { get; set; }
        public object deal_tagging { get; set; }
        public bool half_board { get; set; }
        public int room_id { get; set; }
        public List<IncrementalPrice> incremental_price { get; set; }
        public bool deposit_required { get; set; }
        public string photos { get; set; }
        public bool dinner_included { get; set; }
        public string refundable_until { get; set; }
        public int max_occupancy { get; set; }
        public string name { get; set; }
        public List<string> facilities { get; set; }
        public bool full_board { get; set; }
        public double room_surface_in_feet2 { get; set; }
        public bool all_inclusive { get; set; }
    }
    public class CancellationInfo
    {
        public string until { get; set; }
        public string currency { get; set; }
        public string from { get; set; }
        public double fee { get; set; }
        public string timezone { get; set; }
    }


}
