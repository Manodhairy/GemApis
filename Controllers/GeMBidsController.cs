using GemApi.DTOs.Request;
using GemApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GemApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeMBidsController : ControllerBase
    {
        private readonly IGeMBidService _service;

        public GeMBidsController(IGeMBidService service)
        {
            _service = service;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetBids([FromQuery] BidFilterRequestDto request)
        {
            var result = await _service.GetBidsAsync(request);
            return Ok(result);
        }

        // GET api/gembids/filters
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters([FromQuery] BidFilterRequestDto request)
        {
            var result = await _service.GetFiltersAsync(request);
            return Ok(result);
        }

        // GET api/gembids/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var result = await _service.GetDashboardAsync();
            return Ok(result);
        }

        // GET api/gembids/GEM/2024/B/1234567
        // Catch-all route because GeM bid numbers contain slashes.
        [HttpGet("{*bidNumber}")]
        public async Task<IActionResult> GetBidDetails(string bidNumber)
        {
            var result = await _service.GetBidDetailsAsync(bidNumber);
            if (result == null)
                return NotFound(new { message = $"Bid '{bidNumber}' was not found." });

            return Ok(result);
        }
    }
}