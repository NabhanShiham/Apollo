using Microsoft.AspNetCore.Identity;

namespace Apollo.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Book> Books {get; set;} = new List<Book>();
    }
}