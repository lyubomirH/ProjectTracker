using System.ComponentModel.DataAnnotations;
using ProjectTracker.Data.Constants;

namespace ProjectTracker.Web.ViewModels.Auth
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(DataConstants.User.MaxFirstNameLength, MinimumLength = DataConstants.User.MinFirstNameLength,
            ErrorMessage = "First name must be between {2} and {1} characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(DataConstants.User.MaxLastNameLength, MinimumLength = DataConstants.User.MinLastNameLength,
            ErrorMessage = "Last name must be between {2} and {1} characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string Role { get; set; } = RoleNames.Developer;
    }
}