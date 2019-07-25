using Microsoft.Extensions.Configuration;
using System;

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
            Console.Write("Введите имя аккаунта:");
            var name = Console.ReadLine();
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
            arc.Create(bm, businessName, name, cur, zone, count);
            Console.WriteLine("Аккаунт(ы) создан(ы). Лейте в плюс, гайз!");
        }
    }
}
