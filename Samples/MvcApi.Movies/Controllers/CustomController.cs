#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcApi.Movies.Models;
#endregion

namespace MvcApi.Movies.Controllers
{
    public class CustomController : ApiController
    {

        // GET /custom
        [HttpGet]
        public IEnumerable<User> List()
        {
            return users;
        }

        // GET /custom/5
        [HttpGet]
        public User Show(int id)
        {
            return users.First(u => u.Id == id);
        }

        // GET /custom/new
        //[HttpGet]
        public User New()
        {
            // seed view so that validation works, not needed on ajax views.
            return new User();
        }

        // POST /custom
        [HttpPost]
        public User Create(User user)
        {
            // echo entity with Id populated and can create location.
            return user;
        }

        // PUT /custom/5
        [HttpPut]
        public void Put(int id, User user)
        {
        }

        // DELETE /custom/5
        [HttpDelete]
        public void Delete(int id)
        {
        }

        private static User[] users = new User[] 
        { 
            new User{ Id = 1, Name ="Joe" }, 
            new User{ Id = 2, Name = "Jane" }, 
            new User{ Id = 3, Name = "Jim" }    
        };
    }
}
