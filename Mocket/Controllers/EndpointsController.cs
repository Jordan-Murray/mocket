using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mocket.Models;
using Mocket.Services;

namespace Mocket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EndpointsController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;

        public EndpointsController(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MockApiEndpoint>>> Get()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var endpoints = await _cosmosDbService.GetItemsAsync<MockApiEndpoint>($"SELECT * FROM c WHERE c.userId = '{userId}'");
            return Ok(endpoints);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MockApiEndpoint>> Get(string id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var endpoint = await _cosmosDbService.GetItemAsync<MockApiEndpoint>(id);
            if (endpoint == null || endpoint.UserId != userId)
            {
                return NotFound();
            }
            return Ok(endpoint);
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] MockApiEndpoint item)
        {
            item.Id = Guid.NewGuid().ToString();
            item.UserId = User.FindFirst("UserId")?.Value;
            await _cosmosDbService.AddItemAsync(item);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(string id, [FromBody] MockApiEndpoint item)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (id != item.Id || item.UserId != userId)
            {
                return BadRequest();
            }
            await _cosmosDbService.UpdateItemAsync(id, item);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var userId = User.FindFirst("UserId")?.Value;
            var endpoint = await _cosmosDbService.GetItemAsync<MockApiEndpoint>(id);
            if (endpoint == null || endpoint.UserId != userId)
            {
                return NotFound();
            }
            
            await _cosmosDbService.DeleteItemAsync<MockApiEndpoint>(id);
            return NoContent();
        }
    }
}
