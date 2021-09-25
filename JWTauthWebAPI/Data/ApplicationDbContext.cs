using JWTauthWebAPI.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JWTauthWebAPI.Data
{
    public class ApplicationDbContext :DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options):base(options)
        {

        }
        public virtual DbSet<UserAccount> UserAccounts { get; set; }
        public virtual DbSet<Product> Products { get; set; }
    }
}
