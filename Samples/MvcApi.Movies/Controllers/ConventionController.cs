using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcApi.Movies.Models;

namespace MvcApi.Movies.Controllers
{
    public class ConventionController : ApiController
    {

        // GET /convention
        public IQueryable<User> Get()
        {
            return users.AsQueryable();
        }

        // GET /convention/5
        public User Get(int id)
        {
            return users.First(u => u.Id == id);
        }

        // GET /convention/new
        public User New()
        {
            // seed view so that validation works, not needed on ajax views.
            return new User();
        }

        // POST /convention
        public User Post(User user)
        {
            // echo entity with Id populated and can create location.
            user.Id = users.Length; // Simulate Id gen.
            return user;
        }

        // PUT /convention/5
        public User Put(User user)
        {
            return user;
        }

        // DELETE /convention/5
        public void Delete(int id)
        {
        }

        private static User[] users = new User[] 
        { 
            new User{ Id = 1, Name = "Joe", Email = "joe@yahoo.com" }, 
            new User{ Id = 2, Name = "Jane", Email = "jane@gmail.com" }, 
            new User{ Id = 3, Name = "Jim", Email = "james@hotmail.com" } ,
            new User{ Id = 4, Name = "Maria", Email= "maria@me.com" }    
   
        };
    }
}
