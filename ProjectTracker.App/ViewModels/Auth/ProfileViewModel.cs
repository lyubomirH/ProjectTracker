using System.ComponentModel.DataAnnotations;
using ProjectTracker.Data.Constants;

namespace ProjectTracker.Web.ViewModels.Auth
{
    public class ProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        [StringLength(DataConstants.User.MaxFirstNameLength, MinimumLength = DataConstants.User.MinFirstNameLength)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(DataConstants.User.MaxLastNameLength, MinimumLength = DataConstants.User.MinLastNameLength)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Avatar URL")]
        [Url]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Department")]
        [StringLength(DataConstants.User.MaxDepartmentLength)]
        public string? Department { get; set; }

        [Display(Name = "Job Title")]
        [StringLength(DataConstants.User.MaxJobTitleLength)]
        public string? JobTitle { get; set; }

        [Display(Name = "Bio")]
        [StringLength(DataConstants.User.MaxBioLength)]
        [DataType(DataType.MultilineText)]
        public string? Bio { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}