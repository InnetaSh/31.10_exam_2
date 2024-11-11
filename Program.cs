namespace _31._10_exam_2
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var server = new Server();
            await server.Listen();

            //Console.ReadKey();
        }
    }
}
