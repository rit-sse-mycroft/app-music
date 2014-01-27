using SpotiFire;
using System;
using System.Collections.Generic;
using System.IO;
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
            
            Spotify.CreateSession(File.ReadAllBytes("spotify_appkey.key"), "cache", "settings", "Spotifire");
            Spotify.Task.Wait();
            Session session = Spotify.Task.Result;
            Console.WriteLine("Spotify Username: ");
            string username = Console.ReadLine().Trim();
            Console.WriteLine("Spotify Password: ");
            string password = GetPassword();
            var task = session.Login(username, password, false);
            task.Wait();
            var result = task.Result;
            if (result == Error.OK)
            {
                Console.WriteLine("Successfully Logged In!");
                var client = new MusicClient(session);
                client.Connect(args[0], args[1]);
            }
            else
            {
                Console.WriteLine("Invalid login information.");
            }
            
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
                    pwd += i.KeyChar;
                    Console.Write("*");
                }
            }
            return pwd;
        }

    }
}
