namespace Monq.Core.HttpClientExtensions.Tests.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is Service s)
            {
                return s.Id == Id && s.Name == Name;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() | Name.GetHashCode();
        }
    }
}
