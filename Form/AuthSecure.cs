using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;



public class api
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    // Import the required Atom Table functions from kernel32.dll
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern ushort GlobalAddAtom(string lpString);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern ushort GlobalFindAtom(string lpString);
   
    public string name, ownerid, version, path, seed;
    /// <summary>
    /// Set up your application credentials in order to use AuthSecure
    /// </summary>
    /// <param name="name">Application Name</param>
    /// <param name="ownerid">Your OwnerID, found in your account settings.</param>
    /// <param name="version">Application Version, if version doesnt match it will open the download link you set up in your application settings and close the app, if empty the app will close</param>
    public api(string name, string ownerid, string secret, string version, string path = null)
    {
        if (ownerid.Length != 11)
        {

            Process.Start("https://authsecure.shop");
            Thread.Sleep(2000);
          
            Log("API", "Application not setup correctly. Please watch the YouTube video for setup.");
        }

        this.name = name;

        this.ownerid = ownerid;

        this.version = version;

        this.secret = secret;

        this.path = path;
        
    }

    public user_data_structure user_data { get; set; }
    public string secret { get; set; }
    public string sessionid { get; set; }
    public response_structure response { get; set; }
    public getvar_structure response_var { get; set; }


    [DataContract]
    public class GlobalVarResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "data")]
        public GlobalVarData Data { get; set; }
    }

    [DataContract]
    public class GlobalVarData
    {
        [DataMember(Name = "variable_key")]
        public string VariableKey { get; set; }

        [DataMember(Name = "variable_value")]
        public string VariableValue { get; set; }
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
                // API URL
                string url = $"https://authsecure.shop/api/initv2.php?name={name}&ownerid={ownerid}&type=init&secret={secret}&version={version}";

                // API response
                string responseStr = await client.GetStringAsync(url);

                // Deserialize JSON
                var obj = await DeserializeJsonAsync<response_structure>(responseStr);

                if (obj == null)
                {
                    Log("Init Failed", "Invalid server response");
                    Environment.Exit(0); // terminate 
                }

                if (!obj.success)
                {
                    string reason = obj.message ?? "Initialization failed";
                    Log("Init Failed", reason);
                    Environment.Exit(0); // terminate 
                }

                // double check optional
                string serverVersion = obj.appinfo?.GetType().GetProperty("version")?.GetValue(obj.appinfo)?.ToString();
                if (serverVersion != version)
                {
                    Log("Init Failed", "Version mismatch! Please update the application.");
                    Environment.Exit(0); // terminate 
                }
                sessionid = obj.sessionid;
                return true;
            }
            catch (Exception ex)
            {
                Log("Init Failed", "Connection error: " + ex.Message);
                Environment.Exit(0); // terminate
            }
        }

        return false; // normally never reach here
    }

    // 🔹 Fetch global variable
    public async Task<string> var(string variableKey)
    {
        if (string.IsNullOrEmpty(sessionid))
            if (!await InitApiAsync()) throw new Exception("Failed to initialize API session.");

         HttpClient client = new HttpClient();
        try
        {
            string url = $"https://authsecure.shop/api/globalvariablefeach.php?sessionid={sessionid}&app_name={name}&ownerid={ownerid}&action=view&key={variableKey}";
            var responseStream = await client.GetStreamAsync(url);

            var serializer = new DataContractJsonSerializer(typeof(GlobalVarResponse));
            GlobalVarResponse obj = (GlobalVarResponse)serializer.ReadObject(responseStream);
            response = new response_structure { success = obj.Success, message = obj.Message };

            if (obj.Success)
                return obj.Data.VariableValue;
            else
                return null;
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show("Error fetching variable: " + ex.Message);
            return null;
        }
    }

   

    // ✅ Simple Log Writer
    private void Log(string title, string message)
    {
        string logPath = "init_logs.txt"; // file banegi same folder me
        string logMsg = $"[{DateTime.Now}] {title} -> {message}{Environment.NewLine}";
        File.AppendAllText(logPath, logMsg);
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

    public async Task setvar(string variableName, string variableData)
    {
        try
        {
             var client = new HttpClient();

            string url = $"https://authsecure.shop/api/user_var.php?" +
                         $"sessionid={sessionid}&" +
                         $"username={Uri.EscapeDataString(user_data.username)}&" +
                         $"variable_name={Uri.EscapeDataString(variableName)}&" +
                         $"variable_data={Uri.EscapeDataString(variableData)}&" +
                         $"action=create"; // या "update" भी कर सकते हैं

            string responseStr = (await client.GetStringAsync(url))?.Trim();

             var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr));
            var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
            response_var = (getvar_structure)serializer.ReadObject(ms);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error setting variable: " + ex.Message);
        }
    }

    // API call method
    public async Task<getvar_structure> GetVarAsync(string variableName)
    {
        var client = new HttpClient();
        string url = $"https://authsecure.shop/api/global_var.php?sessionid={sessionid}&username={user_data.username}&key={variableName}&action=view";

        string responseStr = await client.GetStringAsync(url);

        using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr)))
        {
            var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
            return (getvar_structure)serializer.ReadObject(ms);
        }
    }

    public async Task getvar(string variableName)
    {
       
            var client = new HttpClient();
            string url = $"https://authsecure.shop/api/user_var.php?sessionid={sessionid}&username={user_data.username}&variable_name={variableName}&action=view";
            string responseStr = await client.GetStringAsync(url);
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr));
            var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
            response_var = (getvar_structure)serializer.ReadObject(ms);
        
       
       
    }


    public int expirydaysleft()
    {
        if (user_data == null) return 0;
        return (int)Math.Ceiling(user_data.timeleft / 86400.0); // seconds to days
    }
}

[DataContract]
public class user_var_response
{
    [DataMember] public string username { get; set; }
    [DataMember] public string variable_name { get; set; }
    [DataMember] public string variable_data { get; set; }
}

[DataContract]
public class getvar_structure
{
    [DataMember] public bool success { get; set; }
    [DataMember] public string message { get; set; }
    [DataMember] public user_var_response response { get; set; }
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

   
    [DataMember]
    public long expiration { get; set; }

    
    public DateTime ExpirationDate
    {
        get
        {
            return DateTimeOffset.FromUnixTimeSeconds(expiration).ToLocalTime().DateTime;
        }
    }
}
