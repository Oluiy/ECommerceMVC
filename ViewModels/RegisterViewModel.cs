using System.ComponentModel.DataAnnotations;
namespace tryout.ViewModels;

public class RegisterViewModel
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-20);
}
