using AdapterBase.Models.Common;
using AdapterBase.Models.Hotel;
using AdapterBase.Models.Hotel.Common;
using AdapterBase.Repositories;
using Bookindotcom.Model;
using Newtonsoft.Json;
using System.Linq;
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
    public class CheckHotelAvail
    {
        public async Task<HotelAvailabilityResponseScreenDTO> Execute(HotelAvailabilityRequestScreenDTO request)
        {

            var checkin = request.Request.CheckInDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var checkout = request.Request.CheckOutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var Hotelcode = request.Request.Code;
            var paxCount = request.Request.RoomGroups.SelectMany(x => x.Rooms).SelectMany(y => y.PaxInfo).Sum(z => z.Quantity);

            var data = request.Context.Parameters.Val("WSUrl") + "blockAvailability?checkin=" + checkin + "&checkout=" + checkout
               + "&hotel_ids=" + Hotelcode + "&show_test=" + request.Context.Parameters.Val("show_test") + "&guest_qty=" + paxCount + "&guest_cc=nl"
            + "&extras=facilities,all_prices,additional_room_info,cancellation_info,mealplans,important_information";


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

            var response = new HotelAvailabilityResponseScreenDTO();
            response.Response = new HotelDetailsData<HotelDetailsRoom>();
            CheckHotelavail objCheckHotelavail = new CheckHotelavail();
            objCheckHotelavail = JsonConvert.DeserializeObject<CheckHotelavail>(httpResponse.Body);
            response.Response.Status = "Available";
            // string currency = objCheckHotelavail.result.FirstOrDefault().hotel_currency_code;
            response.Response.Code = request.Request.Code;
            response.Response.Name = request.Request.Name;
            response.Response.Token = Convert.ToString(Guid.NewGuid());
            //response.Response.CurrencyCode = currency;
            response.Response.MetaIncluded = false;
            response.Response.Provider = request.Context.Providers.FirstOrDefault();
            response.Response.Vendor = request.Context.Providers.FirstOrDefault();
            response.Response.CheckInDate = request.Request.CheckInDate;
            response.Response.CheckOutDate = request.Request.CheckOutDate;
            response.Response.RoomGroups = new List<RoomGroup<HotelDetailsRoom>>();
            int iGroupId = 1;
            int iRoomCnt = 0;
            RoomGroup<HotelDetailsRoom> objRoomGroup = new RoomGroup<HotelDetailsRoom>()
            {
                Rooms = new List<HotelDetailsRoom>()
            };
            decimal totalAmount = 0.0M;
            objRoomGroup.GroupId = (iGroupId++).ToString();
            objRoomGroup.GroupAmount= totalAmount;
            var lstroom = request.Request.RoomGroups.SelectMany(x => x.Rooms).Select(y => y.RoomTypeCode).ToList();
            foreach (var RoomAvailId in lstroom)
            {

                foreach (var room in objCheckHotelavail.result.FirstOrDefault().block)
                {
                    if (room.block_id.ToString() == RoomAvailId.ToString())
                    {
                        List<HotelRoomBoardType> listBoardTypes = new List<HotelRoomBoardType>();
                        List<BreakdownRateInfo> listBreakDownRateInfo = new List<BreakdownRateInfo>();
                        iRoomCnt++;
                        HotelDetailsRoom objHotelDetailsRoom = new HotelDetailsRoom();

                        objHotelDetailsRoom.Name = room.name;
                        objHotelDetailsRoom.RoomTypeCode = room.block_id.ToString();
                        objHotelDetailsRoom.RoomId = room.room_id.ToString();
                        objHotelDetailsRoom.Status = "available";
                        objHotelDetailsRoom.Features = new List<Amenity>();
                        List<string> RoomAmenities = room?.facilities;
                        foreach (var objRoomAmenity in RoomAmenities)
                        {
                            objHotelDetailsRoom.Features.Add(new Amenity()
                            {
                                Type = "Room",
                                Name = objRoomAmenity
                            });
                        }
                        //objHotelDetailsRoom.PaxInfo = new List<HotelPaxInfo>()
                        //{
                        //    new HotelPaxInfo()
                        //      {
                        //         Type = "ADT",
                        //         Quantity = room.adults
                        // }
                        //};
                        //if (room?.children?.Count() > 0)
                        //{
                        //    foreach (var chdAge in room?.children)
                        //    {
                        //        objHotelDetailsRoom?.PaxInfo.Add(new HotelPaxInfo()
                        //        {
                        //            Type = "CHD",
                        //            Age = Convert.ToString(chdAge).Int(),
                        //            Quantity = 1
                        //        });
                        //    }
                        //}
                        totalAmount = totalAmount + Convert.ToDecimal(room.min_price.price);
                        objHotelDetailsRoom.Amount = Convert.ToDecimal(room.min_price.price);
                        objHotelDetailsRoom.DisplayRateInfo = new List<RateInfo>()
                         {
                          new RateInfo()
                           {
                              Amount = Convert.ToDecimal(room.min_price.price),
                              CurrencyCode =  room.min_price.currency,
                              Description = "BASEPRICE",
                              Purpose = "1"
                           },
                           new RateInfo()
                           {
                              Amount = Convert.ToDecimal(room.min_price.price),
                              CurrencyCode =  room.min_price.currency,
                              Description = "TOTALAMOUNT",
                              Purpose = "10"
                           },

                         };

                        objHotelDetailsRoom.Flags = new Dictionary<string, bool>()
                           {
                                            { "isRefundable",room.refundable }
                           };
                        //objHotelDetailsRoom.AvailabilityCount = room.num_rooms_available_at_this_price;
                        objHotelDetailsRoom.SequenceNumber = iRoomCnt;
                        if (room.deal_tagging != null)
                        {
                            objHotelDetailsRoom.HasSpecialDeal = true;
                            objHotelDetailsRoom.SpecialDealDescription = room?.deal_tagging.ToString();
                        }
                        objHotelDetailsRoom.Quantity = 1;
                        objHotelDetailsRoom.Policies = new List<Policy>();
                        var roomPolicy = new Policy();
                        roomPolicy.Type = "Cancellation";
                        roomPolicy.Name = "Cancellation Policy";
                        roomPolicy.Description = "Important_Information:" + objCheckHotelavail.result.FirstOrDefault().important_information;
                        objHotelDetailsRoom.Policies.Add(roomPolicy);
                        foreach (var roompolicy in room.cancellation_info)
                        {
                            if (!String.IsNullOrEmpty(roompolicy?.currency))
                            {
                                var roomPolicys = new Policy();
                                roomPolicys.DateCriteria = new DateInfo();
                                roomPolicys.Type = "Cancellation";
                                roomPolicys.DateCriteria.StartDate = DateTime.Parse(roompolicy.from.Replace(" ", "T"));
                                roomPolicys.DateCriteria.EndDate = DateTime.Parse(roompolicy.until.Replace(" ", "T"));

                                bool IsCurrentPolicy = GetIsCurrentPolicy(Convert.ToDateTime(roompolicy.until.Replace(" ", "T")), Convert.ToDateTime(request.Request.CheckInDate));
                                roomPolicys.Name = "Cancellation Policy";
                                roomPolicys.Criteria = new List<KeyValue<string, string>>();
                                roomPolicys.Criteria.Add(new KeyValue<string, string>()
                                {
                                    Key = "CancellationChargeValue",
                                    Value = roompolicy.fee.ToString()
                                });
                                roomPolicys.Criteria.Add(new KeyValue<string, string>()
                                {
                                    Key = "CancellationChargeType",
                                    Value = "Amount"
                                });
                                roomPolicys.Criteria.Add(new KeyValue<string, string>()
                                {
                                    Key = "CancellationChargeCurrency",
                                    Value = roompolicy.currency
                                });
                                roomPolicys.Criteria.Add(new KeyValue<string, string>()
                                {
                                    Key = "CancellationChargeUnit",
                                    Value = "Days"
                                });
                                roomPolicys.Criteria.Add(new KeyValue<string, string>()
                                {
                                    Key = "CancellationDeadline",
                                    Value = Convert.ToString(GetDeadLine(DateTime.Parse(roompolicy.from.Replace(" ", "T")), Convert.ToDateTime(request.Request.CheckInDate)))
                                });

                                objHotelDetailsRoom.Policies.Add(roomPolicys);
                            }
                        }
                        objRoomGroup.Rooms.Add(objHotelDetailsRoom);
                    }

                }
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
