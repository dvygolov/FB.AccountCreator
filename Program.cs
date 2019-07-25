using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

namespace FB.AccountCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            var accessToken = config.GetValue<string>("access_token");
            var apiAddress = config.GetValue<string>("fbapi_address");
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Не указан access_token!");
                return;
            }

            var nav = new Navigator(accessToken, apiAddress);
            var bm = nav.SelectBusinessManager();
            Console.Write("Введите количество аккаунтов, которое нужно создать:");
            var count = int.Parse(Console.ReadLine());
            var existing = nav.GetBmsAdAccounts(bm, true);
            var nameRegex = "^(.*?1)$";
            string accName;
            if (existing.Count == 1 && Regex.IsMatch(existing[0]["name"].ToString(), nameRegex))
            {
                Console.WriteLine("В БМ найден один акк с именем, оканчивающимся на 1, будет заюзано это имя");
                accName = existing[0]["name"].ToString().Replace('1','#');
            }
            else
            {
                Console.Write("Введите имя аккаунта:");
                accName = Console.ReadLine();
            }
            Console.Write("Введите название компании (2 слова, для НДС):");
            var businessName = Console.ReadLine();
            if (count > 1)
                Console.WriteLine("Будет создано несколько акков, поэтому к имени будет приписано число!");
            Console.WriteLine("Выберите валюту аккаунта");
            Console.WriteLine("1.RUB");
            Console.WriteLine("2.USD");
            Console.Write("Выбор:");
            var cur = Console.ReadLine() == "1" ? "RUB" : "USD";
            Console.WriteLine("Выберите часовой пояс:");
            var tz = new TimeZones();
            tz.PrintTimeZoneNames();
            Console.Write("Выбор:");
            var zone = tz.GetTimeZoneCodeByIndex(int.Parse(Console.ReadLine()));
            Console.WriteLine("Начинаем процедуру создания аккаунта...");
            var arc = new AccountCreator(apiAddress, accessToken);
            arc.Create(bm, businessName, accName, cur, zone, count);
            Console.WriteLine("Аккаунт(ы) создан(ы). Лейте в плюс, гайз!");
        }
    }
}
