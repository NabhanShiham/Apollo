using System.ComponentModel.DataAnnotations;

namespace Apollo.Models
{
    public class BookViewModel
    {
        [Required(ErrorMessage = "Book name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;
        
        [Display(Name = "Book Cover Photo")]
        public IFormFile? PhotoFile { get; set; }
    }    
}
