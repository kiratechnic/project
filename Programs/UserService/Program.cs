using System;
using System.Net;
using System.Text.RegularExpressions;

namespace UserService
{
    internal class Program
    {
        static void Main()
        {
            // Регулярное выражение для метода GET
            //Regex getPattern = new Regex("^(GET)\\s+.+?/foo/\\d+$");
            string itemPath = "/foo/\\d+";
            string pattern = $"^([A-Z]+)\\s+(?:.+?)?({itemPath})$";
            Regex getPattern = new Regex(pattern);

            // Регулярное выражение для любого метода
            //string anyMethodPattern = @"^(GET|POST|PUT|DELETE)\s+.+?/(about)/?$";

            // Примеры строк для проверки
            string[] testStrings = {
            "GET www.example.org:2000/foo/232",
            " example.org:2000/foo/232",
            "GET http://127.0.0.1:2000/////foo/232",
            "GET ////foo/232",
            "GET www.example.org/foo",
            "GET localhost:2000/foo/",
            "PUT www.example.org/foo",
            "DELETE www.example.org:45443/about",
            "GET localhost:2000/other/232",
            "POST www.example.org/other/232"
        };

            Console.WriteLine($"Проверка  @{getPattern.ToString().Substring(1, getPattern.ToString().Length - 2)}@   на метод GET:");
            foreach (var testString in testStrings)
            {
                if (getPattern.IsMatch(testString))
                {
                    Console.WriteLine($"Совпадение: {testString}");
                }
                else
                {

                    Console.WriteLine($" {testString}");
                }

            }

            //Console.WriteLine("\nПроверка на любой метод:");
            //foreach (var testString in testStrings)
            //{
            //    if (Regex.IsMatch(testString, anyMethodPattern))
            //    {
            //        Console.WriteLine($"Совпадение: {testString}");
            //    }
            //}
        }
    }
}