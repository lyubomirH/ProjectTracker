using System.ComponentModel.DataAnnotations;
using ProjectTracker.Data.Constants;

namespace ProjectTracker.Web.ViewModels.WorkItems
{
    public class WorkItemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(DataConstants.WorkItem.MaxTitleLength, MinimumLength = DataConstants.WorkItem.MinTitleLength,
            ErrorMessage = "Title must be between {2} and {1} characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(DataConstants.WorkItem.MaxDescriptionLength,
            ErrorMessage = "Description cannot exceed {1} characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Priority is required")]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "ToDo";

        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        [Display(Name = "Assign To")]
        public string? AssigneeId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Range(DataConstants.WorkItem.MinEstimatedHours, DataConstants.WorkItem.MaxEstimatedHours,
            ErrorMessage = "Estimated hours must be between {1} and {2}")]
        [Display(Name = "Estimated Hours")]
        public int? EstimatedHours { get; set; }

        [Display(Name = "Actual Hours")]
        public int? ActualHours { get; set; }
    }
}