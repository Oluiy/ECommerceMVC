namespace tryout.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = "";
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
}
