using AdapterBase.Extensions;
using AdapterBase.Models.Hotel;
using AdapterBase.Models.Hotel.Common;
using AdapterBase.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TDXLambda.Config;
using AdapterBase.Models.Common;
using TDXLambda.Extensions;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Bookindotcom.Model;

namespace Bookindotcom
{
    public class HotelSearch
    {
        private readonly ILogger<HotelSearch> _logger;
        public HotelSearch(ILogger<HotelSearch> logger)
        {
            _logger = logger;
        }
        public async Task<HotelSearchResponseScreenDTO> Execute(HotelSearchRequestScreenDTO request)
        {

            var response = new HotelSearchResponseScreenDTO();
            HotelSearchRes objHotelListRes = new HotelSearchRes();
            var checkin = request.Request.CheckInDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var checkout = request.Request.CheckOutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            StringBuilder RoomInfo = new StringBuilder();
            
            #region  RoomInfo
            int icount = 1;
            foreach (var room in request.Request.Rooms)
            {
                
                var adults = room.PaxInfo.Where(p => p.Type.EqualsIgnoreCase("ADT")).Select(p => p.Quantity).FirstOrDefault();
                if (adults > 1)
                {
                    RoomInfo.Append("&room" + icount++ + "=");
                    for (int i = 0; i < adults; i++)
                    {
                        RoomInfo.Append("A,");

                    }
                }
                if (room.PaxInfo.Where(x => x.Type.EqualsIgnoreCase("CHD")).Select(x => x.Age).Any())
                {
                    var children_ages = new List<int>(room.PaxInfo.Where(x => x.Type.EqualsIgnoreCase("CHD")).Select(x => x.Age).ToList());

                    RoomInfo.Append(string.Join(",", children_ages.ToArray()));
                }
                else
                {
                    RoomInfo = RoomInfo.Remove(RoomInfo.Length - 1, 1);
                }
                
            }
            #endregion

            var data = request.Context.Parameters.Val("WSUrl") + "/hotelAvailability?checkin=" + checkin + "&checkout=" + checkout + "&city_ids="
                       + request.Request.LocationCode + "&guest_country=" + request.Request.Nationality + RoomInfo + "&extras=room_details,hotel_details,hotel_amenities,payment_terms,room_amenities,room_policies,add_cheapest_breakfast_rate,add_cheapest_breakfast_rate";


            #region Invoke Supplier Service

            Log.LogRequest(data);

            var httpResponse = await HttpRepository.Instance.ExecuteRequest(new DXHttpRequest()
            {
                Url = data,
                Headers = new Dictionary<string, string>()
                {
                    {"Content-Type","application/json"},
                    {"Authorization", request.Context.Parameters.Val("Auth")}
                },
                RequestType = "GET",
                Timeout = -1,
                Body = string.Empty
            });

            Log.LogResponse(httpResponse.Body);

            #endregion Invoke Supplier Service

            objHotelListRes = JsonConvert.DeserializeObject<HotelSearchRes>(httpResponse.Body);


            if (objHotelListRes?.result != null && objHotelListRes?.result?.Count() > 0)
            {
                response.Response = new HotelSearchResponseWrapper();
                response.Response.Hotels = new List<HotelSearchData>();

                foreach (Result objHotel in objHotelListRes.result)
                {
                    HotelSearchData objHotelSearchData = new HotelSearchData
                    {
                        Code = Convert.ToString(objHotel?.hotel_id),
                        Token = Convert.ToString(Guid.NewGuid()),
                        Status = "Available",
                        Provider = request.Context.Providers.FirstOrDefault(),
                        Vendor = request.Context.Providers.FirstOrDefault(),
                        CheckInDate = request.Request.CheckInDate,
                        CheckOutDate = request.Request.CheckOutDate

                    };
                    objHotelSearchData.Amount = Convert.ToDecimal(objHotel.price);
                    objHotelSearchData.Address = objHotel?.address;
                    objHotelSearchData.CurrencyCode = objHotel?.hotel_currency_code;
                    objHotelSearchData.Name = objHotel?.hotel_name;
                    var untiltime = objHotel?.checkin_time?.until;
                    if(!string.IsNullOrEmpty(untiltime))
                    {
                        objHotelSearchData.CheckInTime = objHotel?.checkin_time.from + "TO" + untiltime;
                    }
                    else
                    { 
                    objHotelSearchData.CheckInTime = objHotel?.checkin_time.from;
                    }
                    objHotelSearchData.Rating = Convert.ToDecimal(objHotel.stars);
                    objHotelSearchData.Zip = objHotel?.postcode;
                    objHotelSearchData.CountryCode = objHotel?.country.ToUpper();
                    string freeCan= (objHotel?.rooms?.FirstOrDefault()?.refundable_until);
                    if(!string.IsNullOrEmpty(freeCan))
                    { 
                    string ext = freeCan.Substring(0, freeCan.LastIndexOf(" +"));
                    objHotelSearchData.FreeCancellationDate =  DateTime.Parse(ext.Replace(" ","T"));
                    }
                    if (objHotel?.rooms?.FirstOrDefault()?.deal_tagging != null)
                    {
                        objHotelSearchData.HasSpecialDeal = true;
                        
                    }

                    objHotelSearchData.DisplayRateInfo = new List<RateInfo>()
                    {
                        new RateInfo()
                           {
                             Amount = Convert.ToDecimal(objHotel?.price),
                             CurrencyCode = objHotel?.hotel_currency_code,
                             Description = "BASEPRICE",
                             Purpose = "1"
                            },
                        new RateInfo()
                           {
                             Amount = Convert.ToDecimal(objHotel?.price),
                             CurrencyCode =objHotel?.hotel_currency_code,
                             Description = "TOTALAMOUNT",
                             Purpose = "10"
                            }
                       
                    };
                    response.Response.Hotels.Add(objHotelSearchData);

                }
            }

                return response;
        }



    }
}

