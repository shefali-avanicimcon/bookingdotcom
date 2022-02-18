using AdapterBase.Models.Base;
using AdapterBase.Models.Common;
using AdapterBase.Models.Hotel;
using AdapterBase.Repositories;
using Bookindotcom.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using TDXLambda.Config;
using TDXLambda.Extensions;

namespace Bookindotcom
{
   public class HotelReservation
    {
        public async Task<HotelReservationResponseScreenDTO> Execute(HotelReservationRequestScreenDTO request)
        {
            var response = new HotelReservationResponseScreenDTO();
            return response;
            HotelRes objBookingRequest = new HotelRes();

            objBookingRequest.block_quantities = request.Request.RoomGroups[0].Rooms.Count.ToString();
            objBookingRequest.incremental_prices = request.Request.RoomGroups[0].GroupAmount.ToString();

            if (request.Request.RoomGroups.Count > 0 && request.Request.RoomGroups[0].Rooms.Count > 0)
            {
               
                foreach (var room in request.Request.RoomGroups[0].Rooms)
                {

                    objBookingRequest.hotel_id = request.Request.Code;
                    objBookingRequest.checkin = request.Request.CheckInDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    objBookingRequest.checkout = request.Request.CheckOutDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    objBookingRequest.block_ids = room.RoomTypeCode;
                    foreach (TravellerDetails objTravellerDetails in room?.TravellerDetails)
                    {

                        objBookingRequest.booker_firstname = objTravellerDetails?.Details?.FirstName;
                        objBookingRequest.booker_lastname = objTravellerDetails?.Details?.LastName;
                        objBookingRequest.booker_email = objTravellerDetails?.Details?.ContactInformation?.Email;
                        objBookingRequest.booker_telephone = objTravellerDetails.Details.ContactInformation.PhoneNumber;
                        objBookingRequest.booker_country = objTravellerDetails.Details.Location.Country;
                    }

                }
            }
            objBookingRequest.extras = "hotel_contact_info";
            var data = JsonConvert.SerializeObject(objBookingRequest);


            #region Invoke Supplier Service

            Log.LogRequest(data);

            var httpResponse = await HttpRepository.Instance.ExecuteRequest(new DXHttpRequest()
            {
                Url = request.Context.Parameters.Val("BookingUrl"),
                Headers = new Dictionary<string, string>()
                {
                    {"Content-Type","application/json"},
                    {"Authorization", request.Context.Parameters.Val("Auth")}
                },
                RequestType = "POST",
                Timeout = -1,
                Body = data
            });

            Log.LogResponse(httpResponse.Body);

            #endregion Invoke Supplier Service

            var jResponse = JObject.Parse(httpResponse.Body);

            if (jResponse != null)
            {

                #region Success Response

                if (jResponse != null)
                {
                    response.BookingReferenceNo = Convert.ToString(jResponse["result.reservation_id"]);
                    response.BookingStatus = "BOOKED";
                }
                else
                {
                    response.Errors = new List<ScreenError>()
                    {
                        new ScreenError()
                        {
                            Code = "201",
                            Message = "booking_id is missing in Supplier Response."
                        }
                    };
                }

                #endregion Success Response
            }


        }

    }
}
