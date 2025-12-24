using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Apollo.Data.Interfaces;

namespace Apollo.Entities
{
    public class Book : IHasTimeStamps
    {
        [Key]
        public int Id {get; set;}
        [Required]
        [StringLength(100)]
        public string Name {get; set;} = string.Empty;
        [Required]
        [StringLength(500)]
        public string Description {get; set;} = string.Empty;
        public string PhotoPath {get; set;} = string.Empty;
        public bool IsBorrowed {get; set;}
        public DateTime CreatedAt {get; set;}
        public DateTime UpdatedAt {get; set;}
        [Required]
        public string OwnerId {get; set;} = string.Empty;
        [ForeignKey(nameof(OwnerId))]
        public ApplicationUser? Owner {get; set;}
    }
}

