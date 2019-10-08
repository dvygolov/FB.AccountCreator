using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FB.AccountCreator
{
    public class AccountCreator
    {
        private string _accessToken;
        private List<string> _streets;
        private RestClient _restClient;

        public AccountCreator(string apiAddress, string accessToken)
        {
            _accessToken = accessToken;
            _restClient = new RestClient(apiAddress);
            _streets = File.ReadAllLines("Addresses.txt").ToList();
        }

        public void Create(string bm, string businessName, string accName, string currency, string zone, int cnt)
        {
            var houseNum = new Random().Next(1, 101);
            var r = new Random().Next(0, _streets.Count);
            var strAndZip = _streets[r];
            var street = strAndZip.Split('-')[1];
            var zip = strAndZip.Split('-')[0];
            string userId = string.Empty;

            for (int i = 0; i < cnt; i++)
            {
                var newAccName = accName.EndsWith('#') ?
                    $"{accName.TrimEnd('#')}{i + 2}" :
                    $"{accName}{i + 1}";
                var request = new RestRequest($"{bm}/adaccount", Method.POST);
                request.AddParameter("access_token", _accessToken);
                request.AddParameter("end_advertiser", "NONE");
                request.AddParameter("media_agency", "NONE");
                request.AddParameter("partner", "NONE");
                request.AddParameter("currency", currency);
                request.AddParameter("timezone_id", zone);
                request.AddParameter("name", newAccName);
                request.AddParameter("access_token", _accessToken);
                var response = _restClient.Execute(request);
                var json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json, true);
                var accId = json["id"].ToString();
                Console.WriteLine($"Создан аккаунт {newAccName} с id {accId}");

                Console.WriteLine($"Добавляем текущего пользователя админом...");
                if (userId == string.Empty)
                {
                    request = new RestRequest($"{bm}/business_users", Method.GET);
                    request.AddQueryParameter("access_token", _accessToken);
                    response = _restClient.Execute(request);
                    json = (JObject)JsonConvert.DeserializeObject(response.Content);
                    ErrorChecker.HasErrorsInResponse(json, true);
                    var users = json["data"].ToList();
                    if (users.Count > 1)
                    {
                        Console.WriteLine("В бизнес менеджере найдено несколько пользователей.");
                        Console.WriteLine("Какому раздать права на этот аккаунт?");
                        int j = 1;
                        foreach (var u in users)
                        {
                            Console.WriteLine($"{j}.{u["name"]}");
                            j++;
                        }
                        Console.Write("Выбор:");
                        var ind = int.Parse(Console.ReadLine()) - 1;
                        userId = users[ind]["id"].ToString();
                    }
                    else
                        userId =users[0]["id"].ToString();
                }

                request = new RestRequest($"{accId}/assigned_users", Method.POST);
                request.AddParameter("access_token", _accessToken);
                request.AddParameter("user", userId);
                request.AddParameter("tasks", "[\"ANALYZE\",\"ADVERTISE\",\"MANAGE\"]");
                response = _restClient.Execute(request);
                json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json, true);
                Console.WriteLine($"Текущий пользователь добавлен!");

                Console.WriteLine($"Снимаем НДС...");
                //снимаем проклятие НДС и убираем уведомления
                request = new RestRequest($"{accId}", Method.POST);
                request.AddParameter("access_token", _accessToken);
				request.AddParameter("is_notifications_enabled", "false");
                request.AddParameter("business_info",
                    "{\"business_name\":\"" + businessName + "\",\"business_street\":\"" + street + " " + houseNum + "\",\"business_city\":\"Минск\",\"business_state\":\"BY\",\"business_zip\":\"" + zip + "\",\"business_country_code\":\"BY\"}");
                response = _restClient.Execute(request);
                json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json, true);
                Console.WriteLine($"НДС снят!");
            }
        }
    }
}
