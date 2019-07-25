using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FB.AccountCreator
{
    public class TimeZones
    {
        private Dictionary<string,string> _timezones;
        public TimeZones()
        {
            _timezones=File.ReadAllLines("Timezones.txt").ToDictionary(l=>l.Split('-')[0],l=>l.Split('-')[1]);
        }

        public string GetTimeZoneCodeByIndex(int i)
        {
            var name=_timezones.Keys.ToList()[i-1];
            return _timezones[name];
        }

        public void PrintTimeZoneNames()
        {
            int i=1;
            foreach(var tzn in _timezones.Keys)
            {
                System.Console.WriteLine($"{i}.{tzn}");
                i++;
            }
        }
    }
}
