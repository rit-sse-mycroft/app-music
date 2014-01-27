using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Expected arguments in the form speechrecognizer host port");
                return;
            }
            Console.WriteLine("Spotify Username: ");
            string username = Console.ReadLine();
            Console.WriteLine("Spotify Password: ");
            string password = GetPassword();
            var client = new MusicClient(username, password);
            client.Connect(args[0], args[1]);
        }

        static string GetPassword()
        {
            string pwd = "";
            while (true)
            {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd = pwd.Substring(0, pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pwd += i.Key;
                    Console.Write("*");
                }
            }
            return pwd;
        }

    }
}
