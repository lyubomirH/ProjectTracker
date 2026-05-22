using System.ComponentModel.DataAnnotations;
using ProjectTracker.Data.Constants;

namespace ProjectTracker.Web.ViewModels.Projects
{
    public class ProjectFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(DataConstants.Project.MaxNameLength, MinimumLength = DataConstants.Project.MinNameLength,
            ErrorMessage = "Project name must be between {2} and {1} characters")]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(DataConstants.Project.MaxDescriptionLength,
            ErrorMessage = "Description cannot exceed {1} characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";
    }
}