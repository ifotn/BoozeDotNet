using System.ComponentModel.DataAnnotations;

namespace BoozeDotNet.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // parent refs.  this is a junction table to reconcile the many-to-many orders-products relationship
        public Order Order { get; set; }
        public Product Product { get; set; }
    }
}
