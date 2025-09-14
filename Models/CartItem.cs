namespace tryout.Models;

public class CartItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public int CustomerId { get; set; }   // optional if you persist per user
}
