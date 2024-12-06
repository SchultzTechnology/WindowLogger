using System;
using WindowsAutomation;

namespace WindowLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            var windows = WindowsAutomation.GetOpenWindows();
            foreach (var window in windows)
            {
                Console.WriteLine($"Title: {window.Title}, Handle: {window.Handle}");
            }
        }
    }
}