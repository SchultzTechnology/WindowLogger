namespace WindowLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                // Clear the console if output is redirected to a console window
                if (!Console.IsOutputRedirected)
                {
                    Console.Clear();
                }

                // Get open windows
                var windows = WindowsAutomation.GetOpenWindows();

                // Print windows
                Console.WriteLine($"Open Windows (Updated: {DateTime.Now})\n");
                foreach (var window in windows)
                {
                    Console.WriteLine($"Title: {window.Title}, Class: {window.ClassName}, Handle: {window.Handle}");
                }

                // Wait for 1 second before refreshing
                Thread.Sleep(1000);
            }
        }
    }
}
