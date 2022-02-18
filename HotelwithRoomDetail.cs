using AdapterBase.Models.Common;
using AdapterBase.Models.Hotel;
using AdapterBase.Models.Hotel.Common;
using AdapterBase.Repositories;
using Bookindotcom.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDXLambda.Config;
using TDXLambda.Extensions;

namespace Bookindotcom
{
    public class HotelwithRoomDetail
    {
        private readonly ILogger<HotelwithRoomDetail> _logger;
        public HotelwithRoomDetail(ILogger<HotelwithRoomDetail> logger)
        {
            _logger = logger;
        }

        public async Task<HotelDetailsResponseScreenDTO> Execute(HotelDetailsRequestScreenDTO request)
        {
            #region Prepare Supplier Request
            var checkin = request.Request.CheckInDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var checkout = request.Request.CheckOutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var Hotelcode = request.Request.Code;

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

            var data = request.Context.Parameters.Val("WSUrl") + "hotelAvailability?checkin=" + checkin + "&checkout=" + checkout + "&guest_country=" 
                + request.Request.Nationality+"&hotel_ids=" + Hotelcode + RoomInfo + "&extras=room_details,hotel_details,hotel_amenities,payment_terms,room_amenities,room_policies,add_cheapest_breakfast_rate,add_cheapest_breakfast_rate";

            #endregion Prepare Supplier Request

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

            var response = new HotelDetailsResponseScreenDTO();
            response.Response = new HotelDetailsData<HotelDetailsRoom>();
            HotelSearchRes objHoteldetail = new HotelSearchRes();
            objHoteldetail = JsonConvert.DeserializeObject<HotelSearchRes>(httpResponse.Body);
            if (objHoteldetail.result.FirstOrDefault().rooms.Count() > 1)
            {
                response.Response.Code = objHoteldetail.result.FirstOrDefault().hotel_id.ToString();
                response.Response.Name = request?.Request?.Name;
                response.Response.Token = Convert.ToString(Guid.NewGuid());
                response.Response.Status = "Available";
                response.Response.CurrencyCode = objHoteldetail.result.FirstOrDefault().hotel_currency_code;
                response.Response.Provider = request.Context.Providers.FirstOrDefault();
                response.Response.Vendor = request.Context.Providers.FirstOrDefault();
                response.Response.CheckInDate = request.Request.CheckInDate;
                response.Response.CheckOutDate = request.Request.CheckOutDate;
                response.Response.CountryCode = objHoteldetail?.result?.FirstOrDefault()?.country.ToUpper();
                response.Response.Address = objHoteldetail?.result.FirstOrDefault()?.address;
                response.Response.Zip = objHoteldetail?.result?.FirstOrDefault()?.postcode;
                var untiltime = objHoteldetail?.result?.FirstOrDefault()?.checkin_time?.until;
                if (!string.IsNullOrEmpty(untiltime))
                {
                    response.Response.CheckInTime = objHoteldetail.result.FirstOrDefault()?.checkin_time.from + "TO" + untiltime;
                }
                else
                {
                    response.Response.CheckInTime = objHoteldetail.result.FirstOrDefault()?.checkin_time.from;
                }

                //response.Response.Config = new List<KeyValue<string, string>>();
                //response.Response.Config.Add(new KeyValue<string, string>()
                //{

                //    Key = "availRQ",
                //    Value = data

                //});

                List<string> HotelAmenities = (objHoteldetail?.result.FirstOrDefault()?.hotel_amenities);
                response.Response.Amenities = new List<Amenity>();
                foreach (var objAmenity in HotelAmenities)
                {
                    response.Response.Amenities.Add(new Amenity()
                    {
                        Name = objAmenity
                    });

                }

            }
            response.Response.RoomGroups = new List<RoomGroup<HotelDetailsRoom>>();
            int iGroupId = 1;
            int iRoomCnt = 0;
            RoomGroup<HotelDetailsRoom> objRoomGroup = new RoomGroup<HotelDetailsRoom>()
            {
                Rooms = new List<HotelDetailsRoom>()
            };

            objRoomGroup.GroupId = (iGroupId++).ToString();
            objRoomGroup.GroupAmount = Convert.ToDecimal(objHoteldetail.result.FirstOrDefault().price);
            foreach (var room in objHoteldetail.result.FirstOrDefault().rooms)
            {
               
                List<HotelRoomBoardType> listBoardTypes = new List<HotelRoomBoardType>();
                List<BreakdownRateInfo> listBreakDownRateInfo = new List<BreakdownRateInfo>();
                iRoomCnt++;
                HotelDetailsRoom objHotelDetailsRoom = new HotelDetailsRoom();

                objHotelDetailsRoom.Name = room.room_name;
                objHotelDetailsRoom.RoomTypeCode = room.block_id.ToString();
                objHotelDetailsRoom.RoomId = room.room_id.ToString();
                objHotelDetailsRoom.Status = "available";
                objHotelDetailsRoom.Features = new List<Amenity>();
                List<string> RoomAmenities = room?.room_amenities;
                 foreach (var objRoomAmenity in RoomAmenities)
                    {
                        objHotelDetailsRoom.Features.Add(new Amenity()
                        {
                            Type = "Room",
                            Name = objRoomAmenity
                        });
                    }
                    objHotelDetailsRoom.PaxInfo = new List<HotelPaxInfo>()
                {
                    new HotelPaxInfo()
                      {
                         Type = "ADT",
                         Quantity = room.adults

                      }
                };
                if (room?.children?.Count() > 0)
                {
                    foreach (var chdAge in room?.children)
                    {
                        objHotelDetailsRoom?.PaxInfo.Add(new HotelPaxInfo()
                        {
                            Type = "CHD",
                            Age = Convert.ToString(chdAge).Int(),
                            Quantity = 1
                        });
                    }
                }

                objHotelDetailsRoom.Amount = Convert.ToDecimal(room.price);
                objHotelDetailsRoom.DisplayRateInfo = new List<RateInfo>()
                  {
                      new RateInfo()
                       {
                          Amount = Convert.ToDecimal(room.price),
                          CurrencyCode =  objHoteldetail.result.FirstOrDefault().hotel_currency_code,
                          Description = "BASEPRICE",
                          Purpose = "1"
                       },
                       new RateInfo()
                       {
                          Amount = Convert.ToDecimal(room.price),
                          CurrencyCode =  objHoteldetail.result.FirstOrDefault().hotel_currency_code,
                          Description = "TOTALAMOUNT",
                          Purpose = "10"
                       },

                  };

                objHotelDetailsRoom.Flags = new Dictionary<string, bool>()
                   {
                                    { "isRefundable",room.refundable }
                   };
                objHotelDetailsRoom.AvailabilityCount = room.num_rooms_available_at_this_price;
                objHotelDetailsRoom.SequenceNumber = iRoomCnt;
                if (room.deal_tagging != null)
                {
                    objHotelDetailsRoom.HasSpecialDeal = true;
                    objHotelDetailsRoom.SpecialDealDescription = room?.deal_tagging?.deal_name;
                }
                objHotelDetailsRoom.Quantity = 1;
                objHotelDetailsRoom.Policies= new List<Policy>();
                var roomPolicy = new Policy();
                roomPolicy.Type = "Cancellation";
                roomPolicy.Name = "Cancellation Policy";
                roomPolicy.Description = "prepayment_description:"+ room?.payment_terms?.prepayment_description+"Note:"+ room?.payment_terms?.name+ "cancellation_description:" + room?.payment_terms?.cancellation_description;
                objHotelDetailsRoom.Policies.Add(roomPolicy);
                 if(!String.IsNullOrEmpty(room?.refundable_until))
                 {
                    var roomPolicys = new Policy();
                    roomPolicys.DateCriteria = new DateInfo();
                    roomPolicys.Type = "Cancellation";
                    string ext = room.refundable_until.Substring(0, room.refundable_until.LastIndexOf(" +"));
                    roomPolicys.DateCriteria.StartDate = Convert.ToDateTime(request.Request.CheckInDate);
                    roomPolicys.DateCriteria.EndDate = DateTime.Parse(ext.Replace(" ", "T"));

                    bool IsCurrentPolicy = GetIsCurrentPolicy(Convert.ToDateTime(ext.Replace(" ", "T")), Convert.ToDateTime(request.Request.CheckInDate));
                    roomPolicys.Name = "Cancellation Policy";
                    roomPolicys.Criteria = new List<KeyValue<string, string>>();
                    roomPolicys.Criteria.Add(new KeyValue<string, string>()
                    {
                        Key = "CancellationChargeValue",
                        Value = room.price.ToString()
                    });
                    roomPolicys.Criteria.Add(new KeyValue<string, string>()
                    {
                        Key = "CancellationChargeType",
                        Value ="Amount"
                    });
                    roomPolicys.Criteria.Add(new KeyValue<string, string>()
                    {
                        Key = "CancellationChargeCurrency",
                        Value = objHoteldetail.result.FirstOrDefault().hotel_currency_code
                    });
                    roomPolicys.Criteria.Add(new KeyValue<string, string>()
                    {
                        Key = "CancellationChargeUnit",
                        Value = "Days"
                    });
                    roomPolicys.Criteria.Add(new KeyValue<string, string>()
                    {
                        Key = "CancellationDeadline",
                        Value = Convert.ToString(GetDeadLine(DateTime.Parse(ext.Replace(" ", "T")), Convert.ToDateTime(request.Request.CheckInDate)))
                    });

                    objHotelDetailsRoom.Policies.Add(roomPolicys);
                }
                
                objRoomGroup.Rooms.Add(objHotelDetailsRoom);
                

               
            }
            response.Response.RoomGroups.Add(objRoomGroup);

            return response;


        }
        public static bool GetIsCurrentPolicy(DateTime dtPolicyStartDate, DateTime dtCheckInDate)
        {
            bool bIsCurrentPolicy = false;
            try
            {
                int deadLineDays = ((TimeSpan)dtCheckInDate.Date.Subtract(dtPolicyStartDate.Date)).Days;
                int currentDaysDiff = ((TimeSpan)dtCheckInDate.Date.Subtract(DateTime.Today.Date)).Days;

                if (currentDaysDiff <= deadLineDays)
                    bIsCurrentPolicy = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return bIsCurrentPolicy;
        }

        public static int GetDeadLine(DateTime dtPolicyStartDate, DateTime dtCheckInDate)
        {
            int deadLineDays;
            try
            {
                deadLineDays = ((TimeSpan)dtCheckInDate.Date.Subtract(dtPolicyStartDate.Date)).Days;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return deadLineDays;
        }


    }

}
