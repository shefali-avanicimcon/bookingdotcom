using AdapterBase.Models.Hotel;
using AdapterBase.Models.Hotel.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bookindotcom
{
    public class Hotelpolicy
    {
        public async Task<HotelPolicyDetailsResponseScreenDTO> Execute(HotelPolicyDetailsRequestScreenDTO request)
        {
            var response = new HotelPolicyDetailsResponseScreenDTO
            {
                Response = new HotelDetailsData<HotelDetailsRoom>()
            };
            response.Response = request.Request;
            return response;
        }
        }
}
