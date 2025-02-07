namespace GameStore.Exceptions
{
    public class GameNotFoundException : Exception
    {
        public GameNotFoundException() : base("The specified game was not found.")
        {
        }

        public GameNotFoundException(string message) : base(message)
        {
        }

        public GameNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
