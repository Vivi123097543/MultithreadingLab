namespace ComputerLibrary
{
    public class Computer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Brand { get; set; }

        public Computer()
        {
        }

        public Computer(int id, string name, string brand)
        {
            Id = id;
            Name = name;
            Brand = brand;
        }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}, Brand: {Brand}";
        }
    }
}
