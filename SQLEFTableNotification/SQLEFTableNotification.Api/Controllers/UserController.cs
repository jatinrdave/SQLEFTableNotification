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

namespace SQLEFTableNotification.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService<UserViewModel, User> _userService;
        public UserController(UserService<UserViewModel, User> userService)
        {
            _userService = userService;
        }

        //get all
        [Authorize]
        [HttpGet]
        public IEnumerable<UserViewModel> GetAll()
        {
            var test = _userService.DoNothing();
            var items = _userService.GetAll();
            return items;
        }

        //get by predicate example
        //get all active by username
        [Authorize]
        [HttpGet("GetActiveByFirstName/{firstname}")]
        public IActionResult GetActiveByFirstName(string firstname)
        {
            var items = _userService.Get(a => a.IsActive && a.FirstName == firstname);
            return Ok(items);
        }

        //get one
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var item = _userService.GetOne(id);
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
        public IActionResult Create([FromBody] UserViewModel user)
        {
            if (user == null)
                return BadRequest();

            var id = _userService.Add(user);
            return Created($"api/User/{id}", id);  //HTTP201 Resource created
        }

        //update
        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UserViewModel user)
        {
            if (user == null || user.Id != id)
                return BadRequest();

            int retVal = _userService.Update(user);
            if (retVal == 0)
                return StatusCode(304);  //Not Modified
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //412 Precondition Failed  - concurrency
            else
                return Accepted(user);
        }

        //delete
        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            int retVal = _userService.Remove(id);
            if (retVal == 0)
                return NotFound();  //Not Found 404
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //Precondition Failed  - concurrency
            else
                return NoContent();   	     //No Content 204
        }

    }
}


