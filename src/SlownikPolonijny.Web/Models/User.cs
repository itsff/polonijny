using System;
using SlownikPolonijny.Dal;
using System.Collections.Generic;

namespace SlownikPolonijny.Web.Models
{
    public class WebUser : AspNetCore.Identity.Mongo.Model.MongoUser
    {
        public WebUser() : base() { }

        public WebUser(string userName) : base(userName) { }
    }

    public class WebRole : AspNetCore.Identity.Mongo.Model.MongoRole
    {
        public WebRole() : base() { }

        public WebRole(string name) : base(name) { }
    }
}