using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace JWTauthWebAPI.Model
{
    public class UserAccount :BaseModel
    {
        public int UserAccountId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordSalt { get; set; }
        public int HashIteration { get; set; }
        public int HashLength { get; set; }
        [NotMapped]
        public string Token { get; set; }
    }
}
