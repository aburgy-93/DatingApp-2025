using System;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

// Testing class for error codes
public class BuggyController(DataContext context) : BaseApiController
{
    [Authorize]
    [HttpGet("auth")]
    // Tests unauthorized errors
    public ActionResult<string> GetAuth() {
        return "secret text";
    }

     [HttpGet("not-found")]
     // Tests for not found errors
    public ActionResult<AppUser> GetNotFound() {
        var thing = context.Users.Find(-1);

        if(thing == null) return NotFound();

        return thing;
    }

     [HttpGet("server-error")]
     // Tests for server errors
    public ActionResult<AppUser> GetServerError() {
        var thing = context.Users.Find(-1) ?? throw new Exception("A bad thing has happened");

        return thing;
    }

     [HttpGet("bad-request")]
     // Tests for bad requests
    public ActionResult<string> GetBadRequest() {

        return BadRequest("This was not a good request");
    }
}
