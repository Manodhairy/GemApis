using GemApi.DTOs;
using GemApi.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GemApi.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidsController : ControllerBase
    {

        private readonly IBidService _service;

        public BidsController(IBidService service)
        {
            _service = service;
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterBids(
            [FromQuery] BidFilterDto filter)
        {
            var data =
                await _service.FilterBidsAsync(filter);

            var totalRecords =
                await _service.GetFilteredCountAsync(filter);

            return Ok(new
            {
                totalRecords,
                pageNumber = filter.PageNumber,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling(
                    totalRecords /
                    (double)filter.PageSize),
                data
            });
        }
    }
}
        

