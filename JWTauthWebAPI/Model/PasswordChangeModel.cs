﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JWTauthWebAPI.Model
{
    public class PasswordChangeModel
    {
        public int UserID { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
    }
}
