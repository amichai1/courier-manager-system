internal partial class Program
{
    private static void Main(string[] args)
    {
        Welcome4661();
        Welcomeyyyy();
        Console.ReadKey();
    }

    static partial void Welcomeyyyy();
    private static void Welcome4661()
    {
        Console.WriteLine("Hello, World!");
        Console.Write("Enter your name:\n");
        System.String name = Console.ReadLine() ?? "Guest";
        Console.WriteLine($"{name}, Welcome to my first console application!");
    }
}