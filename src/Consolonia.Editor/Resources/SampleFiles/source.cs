internal class CSharp
{
    public static List<int> BubbleSort(List<int> xs)
    {
        List<int> acc = xs.ToList();
        List<int> last;
        do
        {
            last = acc.ToList();
            acc = PassList(last.ToList());
        } while (!acc.SequenceEqual(last));

        return acc;
    }

    public static List<int> PassList(List<int> xs)
    {
        if (xs.Count() <= 1)
            return xs;
        int x0 = xs[0];
        int x1 = xs[1];
        if (x1 < x0)
        {
            xs.RemoveAt(1);
            return new List<int> { x1 }.Concat(PassList(xs)).ToList();
        }

        xs.RemoveAt(0);
        return new List<int> { x0 }.Concat(PassList(xs)).ToList();
    }

    public static void ErrorAndExit()
    {
        Console.WriteLine(
            "Usage: please provide a list of at least two integers to sort in the format \"1, 2, 3, 4, 5\"");
        Environment.Exit(1);
    }

    //public static void Main(string[] args)
    //{
    //    if (args.Length != 1)
    //        ErrorAndExit();
    //    try
    //    {
    //        var xs = args[0].Split(',').Select(i => Int32.Parse(i.Trim())).ToList();
    //        if (xs.Count() <= 1)
    //            ErrorAndExit();
    //        var sortedXs = BubbleSort(xs);
    //        Console.WriteLine(string.Join(", ", sortedXs));
    //    }
    //    catch
    //    {
    //        ErrorAndExit();
    //    }
    //}
}