using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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

    // Initialize API session
    public async Task<bool> InitApiAsync()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                string url = $"https://authsecure.shop/api/initv2.php?name={name}&ownerid={ownerid}&type=init&secret={secret}&version={version}";
                string responseStr = await client.GetStringAsync(url);

                JObject obj = JObject.Parse(responseStr);
                if ((bool)obj["success"])
                {
                    sessionid = obj["sessionid"].ToString();
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    // 🔑 Register user
    public async Task<response_structure> RegisterAsync(string username, string password, string license, string email = null)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            bool initSuccess = await InitApiAsync();
            if (!initSuccess)
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string url = $"https://authsecure.shop/api/register.php?username={username}&password={password}&license={license}&email={email}&hwid={hwid}&sessionid={sessionid}&name={name}&ownerid={ownerid}";
                string responseStr = await client.GetStringAsync(url);

                JObject obj = JObject.Parse(responseStr);
                response = obj.ToObject<response_structure>();

                if (response.success)
                {
                    user_data = new user_data_structure
                    {
                        username = (string)obj["info"]["username"],
                        subscription = (string)obj["info"]["subscription"],
                        expiration = (long)obj["info"]["expiry"],
                        ip = (string)obj["info"]["ip"],
                        hwid = (string)obj["info"]["hwid"],
                        CreationDate = DateTimeOffset.FromUnixTimeSeconds((long)obj["info"]["createdate"]).DateTime,
                        LastLoginDate = DateTimeOffset.FromUnixTimeSeconds((long)obj["info"]["lastlogin"]).DateTime,
                        timeleft = (long)obj["info"]["timeleft"]
                    };

                    user_data.subscriptions = new List<subscription_structure>
                {
                    new subscription_structure
                    {
                        key = license,
                        subscription = user_data.subscription,
                        expiration = user_data.expiration
                    }
                };
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception("Register Error: " + ex.Message);
            }
        }
    }


    // Login user
    public async Task<response_structure> LoginAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            bool initSuccess = await InitApiAsync();
            if (!initSuccess)
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string url = $"https://authsecure.shop/api/login.php?username={username}&pass={password}&hwid={hwid}&sessionid={sessionid}&name={name}&ownerid={ownerid}";
                string responseStr = await client.GetStringAsync(url);

                JObject obj = JObject.Parse(responseStr);
                response = obj.ToObject<response_structure>();

                if (response.success)
                {
                    user_data = new user_data_structure
                    {
                        username = (string)obj["info"]["username"],
                        subscription = (string)obj["info"]["subscription"],
                        expiration = (long)obj["info"]["expiry"],
                        ip = (string)obj["info"]["ip"],
                        hwid = (string)obj["info"]["hwid"],
                        CreationDate = DateTimeOffset.FromUnixTimeSeconds((long)obj["info"]["createdate"]).DateTime,
                        LastLoginDate = DateTimeOffset.FromUnixTimeSeconds((long)obj["info"]["lastlogin"]).DateTime,
                        timeleft = (long)obj["info"]["timeleft"]
                    };

                    user_data.subscriptions = new List<subscription_structure>
                    {
                        new subscription_structure
                        {
                            key = "", // no key on login
                            subscription = user_data.subscription,
                            expiration = user_data.expiration
                        }
                    };
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception("Login Error: " + ex.Message);
            }
        }
    }

    // Remaining days
    public int expirydaysleft()
    {
        if (user_data == null) return 0;
        return (int)Math.Ceiling(user_data.timeleft / 86400.0); // seconds to days
    }
}

// Response structure
public class response_structure
{
    public bool success { get; set; }
    public string message { get; set; }
    public string ownerid { get; set; }
    public JObject info { get; set; }
}

// User data
public class user_data_structure
{
    public string username { get; set; }
    public string subscription { get; set; }

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

    public string ip { get; set; }
    public string hwid { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastLoginDate { get; set; }
    public long timeleft { get; set; }

    public List<subscription_structure> subscriptions { get; set; }
}

// Subscription
public class subscription_structure
{
    public string key { get; set; }
    public string subscription { get; set; }

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
}