namespace GameStore.Models.Entities
{
    public class ShoppingCart
    {
        public Guid Id { get; set; }
        public LinkedList<Game> Games { get; set; } = new LinkedList<Game>();
        public required ApplicationUser Customer { get; set; }

        public decimal TotalPrice
        {
            get
            {
                decimal totalPrice = 0;
                foreach (var game in Games)
                {
                    totalPrice += game.Price;
                }
                return totalPrice;
            }
        }

        public int Count
        {
            get
            {
                return Games.Count;
            }
        }
    }
}
