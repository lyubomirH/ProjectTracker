using System.ComponentModel.DataAnnotations;
using ProjectTracker.Data.Constants;

namespace ProjectTracker.Web.ViewModels.Projects
{
    public class ProjectFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(DataConstants.Project.MaxNameLength, MinimumLength = DataConstants.Project.MinNameLength)]
        [Display(Name = "Project Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(DataConstants.Project.MaxDescriptionLength)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;
    }
}