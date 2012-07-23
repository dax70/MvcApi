using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcApi.Test
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public static UserModel[] Users = new UserModel[] 
        { 
            new UserModel{ Id = 1, Name ="Joe" }, 
            new UserModel{ Id = 2, Name = "Jane" }, 
            new UserModel{ Id = 3, Name = "Jim" } ,
            new UserModel{ Id = 4, Name = "Maria" }    
   
        };
    }
}
