namespace DatingApp.API.Models
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;

    public class Role : IdentityRole<int>
    {
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}