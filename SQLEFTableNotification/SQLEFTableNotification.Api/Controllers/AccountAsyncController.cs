using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog;
using SQLEFTableNotification.Domain;
using SQLEFTableNotification.Domain.Service;
using SQLEFTableNotification.Entity;
using SQLEFTableNotification.Entity.Context;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SQLEFTableNotification.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AccountAsyncController : ControllerBase
    {
        private readonly AccountServiceAsync<AccountViewModel, Account> _accountServiceAsync;
        public AccountAsyncController(AccountServiceAsync<AccountViewModel, Account> accountServiceAsync)
        {
            _accountServiceAsync = accountServiceAsync;
        }


        //get all
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _accountServiceAsync.GetAll();
            return Ok(items);
        }

        //get by predicate example
        //get all active by name
        [Authorize]
        [HttpGet("GetActiveByName/{name}")]
        public async Task<IActionResult> GetActiveByName(string name)
        {
            var items = await _accountServiceAsync.Get(a => a.IsActive && a.Name == name);
            return Ok(items);
        }

        //get one
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _accountServiceAsync.GetOne(id);
            if (item == null)
            {
                Log.Error("GetById({ ID}) NOT FOUND", id);
                return NotFound();
            }

            return Ok(item);
        }

        //add
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountViewModel account)
        {
            if (account == null)
                return BadRequest();

            var id = await _accountServiceAsync.Add(account);
            return Created($"api/Account/{id}", id);  //HTTP201 Resource created
        }

        //update
        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccountViewModel account)
        {
            if (account == null || account.Id != id)
                return BadRequest();

            int retVal = await _accountServiceAsync.Update(account);
            if (retVal == 0)
                return StatusCode(304);  //Not Modified
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //412 Precondition Failed  - concurrency
            else
                return Accepted(account);
        }


        //delete
        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            int retVal = await _accountServiceAsync.Remove(id);
            if (retVal == 0)
                return NotFound();  //Not Found 404
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //Precondition Failed  - concurrency
            else
                return NoContent();   	     //No Content 204
        }
    }
}


