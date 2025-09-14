namespace tryout.Models;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";   // Base64
    public string PasswordSalt { get; set; } = "";   // Base64
    public string PhoneNumber { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Order>? Orders { get; set; }
    public ICollection<RefreshToken>? RefreshTokens { get; set; }
}
