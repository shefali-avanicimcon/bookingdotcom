using System;
using System.Collections.Generic;
using System.Text;

namespace Bookindotcom.Model
{
    class HotelSearchRes
    {
        public List<Result> result { get; set; }
        public Meta meta { get; set; }
    }
    public class CheapestBreakfastRate
    {
    }

    public class PaymentTerms
    {
        public string prepayment_description { get; set; }
        public string name { get; set; }
        public string cancellation_description { get; set; }
    }

    public class DealTagging
    {
        public double public_price { get; set; }
        public int discount_percentage { get; set; }
        public string deal_name { get; set; }
    }

    public class Room
    {
        public PaymentTerms payment_terms { get; set; }
        public string room_name { get; set; }
        public int room_type_id { get; set; }
        public bool refundable { get; set; }
        public double price { get; set; }
        public List<string> children { get; set; }
        public string block_id { get; set; }
        public int adults { get; set; }
        public int room_id { get; set; }
        public List<string> room_amenities { get; set; }
        public DealTagging deal_tagging { get; set; }
        public bool deposit_required { get; set; }
        public string refundable_until { get; set; }
        public int num_rooms_available_at_this_price { get; set; }
    }

    public class CheckinTime
    {
        public string from { get; set; }
        public string until { get; set; }
    }

    public class Result
    {
        public string default_language { get; set; }
        public int hotel_id { get; set; }
        public string hotel_name { get; set; }
        public CheapestBreakfastRate cheapest_breakfast_rate { get; set; }
        public string country { get; set; }
        public List<string> hotel_amenities { get; set; }
        public List<Room> rooms { get; set; }
        public string stars { get; set; }
        public bool direct_payment { get; set; }
        public string postcode { get; set; }
        public string hotel_currency_code { get; set; }
        public double price { get; set; }
        public CheckinTime checkin_time { get; set; }
        public string address { get; set; }
        public bool is_flash_deal { get; set; }
        public string hotel_url { get; set; }
        public bool can_pay_now { get; set; }
        public List<Block> block { get; set; }
        public string important_information { get; set; }
    }

    public class Meta
    {
        public string ruid { get; set; }
    }
}
