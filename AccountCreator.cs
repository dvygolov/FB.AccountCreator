﻿using Newtonsoft.Json;
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

        public void Create(string bm, string businessName, string accName, string currency, string zone,
            int cnt = 1, bool useRandomValues = true)
        {
            var houseNum = new Random().Next(1, 101);
            var r = new Random().Next(0, _streets.Count);
            var strAndZip = _streets[r];
            var street = strAndZip.Split('-')[1];
            var zip = strAndZip.Split('-')[0];

            for (int i = 0; i < cnt; i++)
            {
                var request = new RestRequest($"{bm}/adaccount", Method.POST);
                request.AddParameter("access_token", _accessToken);
                request.AddParameter("end_advertiser", "NONE");
                request.AddParameter("media_agency", "NONE");
                request.AddParameter("partner", "NONE");
                request.AddParameter("currency", currency);
                request.AddParameter("timezone_id", zone);
                request.AddParameter("name", $"{accName}{i + 1}");
                request.AddParameter("access_token", _accessToken);
                var response = _restClient.Execute(request);
                var json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json,true);
                var accId = json["id"].ToString();
                Console.WriteLine($"Создан аккаунт с id {accId}");

                Console.WriteLine($"Добавляем текущего пользователя админом...");
                request = new RestRequest($"{bm}/business_users", Method.GET);
                request.AddQueryParameter("access_token", _accessToken);
                response = _restClient.Execute(request);
                json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json,true);
                string userId=string.Empty;
                if (json["data"].Count() > 1)
                {
                    Console.WriteLine("В бизнес менеджере найдено несколько пользователей.");
                    Console.WriteLine("Какому раздать права на этот аккаунт?");
                    int j=1;
                    var users=json["data"].ToList();
                    foreach(var u in users)
                    {
                        Console.WriteLine($"{j}.{u["name"]}");
                        j++;
                    }
                    Console.Write("Выбор:");
                    var ind=int.Parse(Console.ReadLine())-1;
                    userId=users[ind]["id"].ToString();
                }

                request = new RestRequest($"{accId}/assigned_users", Method.POST);
                request.AddParameter("access_token", _accessToken);
                request.AddParameter("user", userId);
                request.AddParameter("tasks", "[\"ANALYZE\",\"ADVERTISE\",\"MANAGE\"]");
                response = _restClient.Execute(request);
                json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json,true);
                Console.WriteLine($"Текущий пользователь добавлен!");

                Console.WriteLine($"Снимаем НДС...");
                //снимаем проклятие НДС
                request = new RestRequest($"{accId}", Method.POST);
                request.AddParameter("access_token", _accessToken);
                request.AddParameter("business_info",
                    "{\"business_name\":\"" + businessName + "\",\"business_street\":\"" + street + " " + houseNum + "\",\"business_city\":\"Минск\",\"business_state\":\"BY\",\"business_zip\":\"" + zip + "\",\"business_country_code\":\"BY\"}");
                response = _restClient.Execute(request);
                json = (JObject)JsonConvert.DeserializeObject(response.Content);
                ErrorChecker.HasErrorsInResponse(json,true);
                Console.WriteLine($"НДС снят!");
            }
        }
    }
}