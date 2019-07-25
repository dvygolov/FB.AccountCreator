using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FB.AccountCreator
{
    public class Navigator
    {
        private string _accessToken;
        private readonly RestClient _restClient;
        private readonly Dictionary<string,string> _accByNameDict = new Dictionary<string, string>();
        public Navigator(string accessToken, string apiAddress)
        {
            _accessToken = accessToken;
            _restClient = new RestClient(apiAddress);
        }

        public string GetFanPageName(string pageId)
        {
            var request = new RestRequest(pageId, Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "name,is_published,access_token");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            return json["name"].ToString();
        }

        public string GetFanPageBackedInsagramAccount(string pageId)
        {
            var request = new RestRequest(pageId, Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "name,page_backed_instagram_accounts");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            return json["page_backed_instagram_accounts"]["data"][0]["id"].ToString();
        }

        public string SelectFanPage()
        {
            var request = new RestRequest($"me/accounts", Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "name,is_published,access_token");
            request.AddQueryParameter("type", "page");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            var pages = json["data"].ToList();
            for (int i = 0; i < pages.Count; i++)
            {
                var p = pages[i];
                Console.WriteLine($"{i + 1}. {p["name"]} - {p["is_published"]}");
            }

        PageStart:
            int index;
            bool goodRes;
            do
            {
                Console.Write("Выберите страницу, введя её номер, и нажмите Enter:");
                var readIndex = Console.ReadLine();
                goodRes = int.TryParse(readIndex, out index);
                index--;
                if (index < 0 || index > pages.Count - 1) goodRes = false;
            }
            while (!goodRes);

            var selectedPage = pages[index];

            if (!bool.Parse(selectedPage["is_published"].ToString()))
            {
                Console.Write($"Страница {selectedPage["name"]} не опубликована! Опубликовать?");
                if (YesNoSelector.ReadAnswerEqualsYes())
                {
                    //Страница не опубликована! Пытаемся опубликовать
                    request = new RestRequest(selectedPage["id"].ToString(), Method.POST);
                    request.AddParameter("access_token", selectedPage["access_token"].ToString());
                    request.AddParameter("is_published", "true");
                    response = _restClient.Execute(request);
                    var publishJson = (JObject)JsonConvert.DeserializeObject(response.Content);
                    if (publishJson["error"] != null)
                    {
                        //невозможно опубликовать страницу, вероятно, она забанена!
                        Console.WriteLine($"Страница {selectedPage["name"]} не опубликована и, вероятно, забанена!");
                        goto PageStart;
                    }
                    else
                    {
                        //уведомим пользователя, что мы опубликовали страницу после снятия с публикации
                        Console.WriteLine($"Страница {selectedPage["name"]} была заново опубликована после снятия с публикации!");
                        return selectedPage["id"].ToString();
                    }
                }
                else
                    goto PageStart;
            }
            return selectedPage["id"].ToString();
        }

        public string SelectBusinessManager()
        {
            var bms = GetAllBms();

            for (int i = 0; i < bms.Count; i++)
            {
                var bm = bms[i];
                Console.WriteLine($"{i + 1}. {bm["name"]}");
            }

            bool goodRes;
            int index;
            do
            {
                Console.Write("Выберите БМ, введя его номер, и нажмите Enter:");
                var readIndex = Console.ReadLine();
                goodRes = int.TryParse(readIndex, out index);
                if (index > bms.Count) goodRes = false;
            }
            while (!goodRes);
            return bms[index-1]["id"].ToString();
        }


        public string GetAdAccountsBusinessManager(string acc)
        {
            var req = new RestRequest($"act_{acc}", Method.GET);
            req.AddQueryParameter("access_token", _accessToken);
            req.AddQueryParameter("fields", "business");
            var resp = _restClient.Execute(req);
            var respJson = (JObject)JsonConvert.DeserializeObject(resp.Content);
            ErrorChecker.HasErrorsInResponse(respJson, true);
            return respJson["business"]["id"].ToString();
        }

        public string SelectAdAccount(string bmid, bool includeBanned = false)
        {
            var accounts = GetBmsAdAccounts(bmid, includeBanned);

            for (int i = 0; i < accounts.Count; i++)
            {
                var acc = accounts[i];
                Console.WriteLine($"{i + 1}. {acc["name"]}");
            }

            int index;
            bool goodRes;
            do
            {
                Console.Write("Выберите РК, введя его номер, и нажмите Enter:");
                var readIndex = Console.ReadLine();
                goodRes = int.TryParse(readIndex, out index);
                if (index > accounts.Count - 1) goodRes = false;
            }
            while (!goodRes);
            return accounts[index]["id"].ToString();
        }

        public string GetAdAccountByName(string name)
        {
            if (_accByNameDict.Count == 0)
            {
                var bms = GetAllBms();
                foreach (var bm in bms)
                {
                    var adAccounts = GetBmsAdAccounts(bm["id"].ToString(),true);
                    adAccounts.ForEach(acc => _accByNameDict.Add(acc["name"].ToString(), acc["id"].ToString()));
                }
            }
            if (_accByNameDict.ContainsKey(name))
                return _accByNameDict[name];
            return string.Empty;
        }

        public List<JToken> GetAllBms()
        {
            var request = new RestRequest($"me/businesses", Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            var bms = json["data"].ToList();
            return bms;
        }

        public List<JToken> GetBmsAdAccounts(string bmid, bool includeBanned = false)
        {
            var request = new RestRequest($"{bmid}/owned_ad_accounts", Method.GET);
            request.AddQueryParameter("access_token", _accessToken);
            request.AddQueryParameter("fields", "name,account_status");
            var response = _restClient.Execute(request);
            var json = (JObject)JsonConvert.DeserializeObject(response.Content);
            var accounts = json["data"].ToList();
            //Исключаем забаненные
            if (!includeBanned) accounts = accounts.Where(acc => acc["account_status"].ToString() != "2").ToList();
            return accounts;
        }

    }
}
