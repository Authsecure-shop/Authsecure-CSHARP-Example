using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

public class api
{
    public string name { get; set; }
    public string ownerid { get; set; }
    public string secret { get; set; }
    public string version { get; set; }
    public string sessionid { get; set; }
    public response_structure response { get; set; }
    public user_data_structure user_data { get; set; }

    public api(string name, string ownerid, string secret, string version)
    {
        this.name = name;
        this.ownerid = ownerid;
        this.secret = secret;
        this.version = version;
        this.sessionid = null;
    }

    private async Task<T> DeserializeJsonAsync<T>(string json)
    {
        using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(ms);
        }
    }

    public async Task<bool> InitApiAsync()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                string url = $"https://authsecure.shop/api/initv2.php?name={name}&ownerid={ownerid}&type=init&secret={secret}&version={version}";
              
                string responseStr = await client.GetStringAsync(url);
                if (responseStr == "AuthSecure_Invalid")
                {
                    error("Application not found or invalid credentials!");
                }

                var obj = await DeserializeJsonAsync<response_structure>(responseStr);

                if (obj.success)
                {
                    
                    string serverVersion = obj.appinfo?.GetType().GetProperty("version")?.GetValue(obj.appinfo)?.ToString();
                    if (serverVersion != version)
                    {
                        error("Version mismatch! Please update the application.");
                    }

                    sessionid = obj.sessionid;
                    return true;
                }
                else
                {
                    error("Unknown error during initialization");
                }
            }
            catch (Exception ex)
            {
               
                Environment.Exit(1);
            }
        }
        return false;
    }
    public static void error(string message)
    {
        string folder = @"Logs", file = Path.Combine(folder, "ErrorLogs.txt");

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        if (!File.Exists(file))
        {
            using (FileStream stream = File.Create(file))
            {
                File.AppendAllText(file, DateTime.Now + " > This is the start of your error logs file");
            }
        }

        File.AppendAllText(file, DateTime.Now + $" > {message}" + Environment.NewLine);

        System.Windows.MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        Environment.Exit(1);
    }









    public async Task<response_structure> RegisterAsync(string username, string password, string license, string email = null)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            if (!await InitApiAsync())
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            string url = $"https://authsecure.shop/api/register.php?username={username}&password={password}&license={license}&email={email}&hwid={hwid}&sessionid={sessionid}&name={name}&ownerid={ownerid}";
            string responseStr = await client.GetStringAsync(url);

            response = await DeserializeJsonAsync<response_structure>(responseStr);

            if (response.success)
            {
                var info = response.info;
                user_data = new user_data_structure
                {
                    username = info.username,
                    subscription = info.subscription,
                    expiration = info.expiry, // Unix timestamp
                    ip = info.ip,
                    hwid = info.hwid,
                    CreationDate = DateTimeOffset.FromUnixTimeSeconds(info.createdate).DateTime,
                    LastLoginDate = DateTimeOffset.FromUnixTimeSeconds(info.lastlogin).DateTime,
                    timeleft = info.timeleft,
                    subscriptions = new List<subscription_structure>
                    {
                        new subscription_structure
                        {
                            key = license,
                            subscription = info.subscription,
                            expiration = info.expiry
                        }
                    }
                };
            }

            return response;
        }
    }
    private long _expirationUnix;
    public long expiration
    {
        get => _expirationUnix;
        set
        {
            _expirationUnix = value;
            ExpirationDate = DateTimeOffset.FromUnixTimeSeconds(value).DateTime;
        }
    }
    public DateTime ExpirationDate { get; private set; }


    public async Task<response_structure> LoginAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            if (!await InitApiAsync())
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            string url = $"https://authsecure.shop/api/login.php?username={username}&pass={password}&hwid={hwid}&sessionid={sessionid}&name={name}&ownerid={ownerid}";
            string responseStr = await client.GetStringAsync(url);

            response = await DeserializeJsonAsync<response_structure>(responseStr);

            if (response.success)
            {
                var info = response.info;
                user_data = new user_data_structure
                {
                    username = info.username,
                    subscription = info.subscription,
                    expiration = info.expiry,
                    ip = info.ip,
                    hwid = info.hwid,
                    CreationDate = DateTimeOffset.FromUnixTimeSeconds(info.createdate).DateTime,
                    LastLoginDate = DateTimeOffset.FromUnixTimeSeconds(info.lastlogin).DateTime,
                    timeleft = info.timeleft,
                    subscriptions = new List<subscription_structure>
                    {
                        new subscription_structure
                        {
                            key = "", // no key on login
                            subscription = info.subscription,
                            expiration = info.expiry
                        }
                    }
                };

            }

            return response;
        }
    }

    public int expirydaysleft()
    {
        if (user_data == null) return 0;
        return (int)Math.Ceiling(user_data.timeleft / 86400.0); // seconds to days
    }
}

[DataContract]
public class response_structure
{
    [DataMember] public bool success { get; set; }
    [DataMember] public string message { get; set; }
    [DataMember] public string ownerid { get; set; }
    [DataMember] public info_structure info { get; set; }
    [DataMember] public string sessionid { get; set; }
    [DataMember] public appinfo_structure appinfo { get; set; }

}

[DataContract]
public class appinfo_structure
{
    [DataMember] public string name { get; set; }
    [DataMember] public string version { get; set; }
}


[DataContract]
public class info_structure
{
    [DataMember] public string username { get; set; }
    [DataMember] public string subscription { get; set; }
    [DataMember] public long expiry { get; set; }
    [DataMember] public string ip { get; set; }
    [DataMember] public string hwid { get; set; }
    [DataMember] public long createdate { get; set; }
    [DataMember] public long lastlogin { get; set; }
    [DataMember] public long timeleft { get; set; }
}

[DataContract]
public class user_data_structure
{
    [DataMember] public string username { get; set; }
    [DataMember] public string subscription { get; set; }
    [DataMember] public long expiration { get; set; }
    [DataMember] public string ip { get; set; }
    [DataMember] public string hwid { get; set; }
    [DataMember] public DateTime CreationDate { get; set; }
    [DataMember] public DateTime LastLoginDate { get; set; }
    [DataMember] public long timeleft { get; set; }
    [DataMember] public List<subscription_structure> subscriptions { get; set; }
}

[DataContract]
public class subscription_structure
{
    [DataMember] public string key { get; set; }
    [DataMember] public string subscription { get; set; }

    // Yeh original Unix timestamp rahega (DB/JSON ke liye)
    [DataMember]
    public long expiration { get; set; }

    // Yeh auto convert ho kar hamesha DateTime dega
    public DateTime ExpirationDate
    {
        get
        {
            return DateTimeOffset.FromUnixTimeSeconds(expiration).ToLocalTime().DateTime;
        }
    }
}
