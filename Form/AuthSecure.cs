using System;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;



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




#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private async Task<T> DeserializeJsonAsync<T>(string json)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(ms);
        }
    }

    /// <summary>
    /// Checks if the current session is validated or not
    /// </summary>
    public async Task<bool> Init()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                // 🔹 Prepare POST data (form fields)
                var postData = new Dictionary<string, string>
                {
                    ["type"] = "init",
                    ["name"] = name,
                    ["ownerid"] = ownerid,
                    ["secret"] = secret,
                    ["version"] = version
                };

                // 🔹 Convert to form-url encoded content
                var content = new FormUrlEncodedContent(postData);

                // 🔹 Send POST request
                var response = await client.PostAsync("https://authsecure.shop/post/initv2.php", content);

                // 🔹 Read the response
                string responseStr = await response.Content.ReadAsStringAsync();

                // 🔹 Deserialize JSON
                var obj = await DeserializeJsonAsync<response_structure>(responseStr);

                if (obj == null)
                {
                    Log("Init Failed", "Invalid server response");
                    Environment.Exit(0);
                }

                if (!obj.success)
                {
                    string reason = obj.message ?? "Initialization failed";
                    Log("Init Failed", reason);
                    Environment.Exit(0);
                }

                // 🔹 Check version
                string serverVersion = obj.appinfo?.GetType().GetProperty("version")?.GetValue(obj.appinfo)?.ToString();
                if (serverVersion != version)
                {
                    Log("Init Failed", "Version mismatch! Please update the application.");
                    Environment.Exit(0);
                }

                // 🔹 Save session ID
                sessionid = obj.sessionid;
                return true;
            }
            catch (Exception ex)
            {
                Log("Init Failed", "Connection error: " + ex.Message);
                Environment.Exit(0);
            }
        }

        return false; // should never reach here
    }




    /// <summary>
    /// Registers the user using a license and gives the user a subscription that matches their license level
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="pass">Password</param>
    /// <param name="key">License key</param>
    public async Task<response_structure> register(string username, string password, string license, string email = null)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            if (!await Init())
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            // Prepare POST data
            var postData = new Dictionary<string, string>
            {
                ["type"] = "register",
                ["username"] = username,
                ["pass"] = password,       // match PHP $_POST['pass']
                ["license"] = license,
                ["email"] = email ?? "",
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var content = new FormUrlEncodedContent(postData);

            // Send POST request
            var httpResponse = await client.PostAsync("https://authsecure.shop/post/register.php", content);
            var responseStr = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize JSON response
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


    // ✅ Simple Log Writer
    private void Log(string title, string message)
    {
        string logPath = "init_logs.txt"; // file banegi same folder me
        string logMsg = $"[{DateTime.Now}] {title} -> {message}{Environment.NewLine}";
        File.AppendAllText(logPath, logMsg);
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

    /// <summary>
    /// Authenticates the user using their username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="pass">Password</param>
    public async Task<response_structure> login(string username, string password)
    {
        if (string.IsNullOrEmpty(sessionid))
        {
            if (!await Init())
                throw new Exception("Failed to initialize API session.");
        }

        string hwid = WindowsIdentity.GetCurrent().User.Value;

        using (HttpClient client = new HttpClient())
        {
            // Prepare POST data
            var postData = new Dictionary<string, string>
            {
                ["type"] = "login",
                ["username"] = username,
                ["pass"] = password,
                ["hwid"] = hwid,
                ["sessionid"] = sessionid,
                ["name"] = name,
                ["ownerid"] = ownerid
            };

            var content = new FormUrlEncodedContent(postData);

            // Send POST request
            var responseMsg = await client.PostAsync("https://authsecure.shop/post/login.php", content);
            string responseStr = await responseMsg.Content.ReadAsStringAsync();

            // Deserialize JSON response
            response = await DeserializeJsonAsync<response_structure>(responseStr);

            // If login successful, set user data
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
    /// <summary>
    /// Use Buttons from AuthSecure Customer Panel
    /// </summary>
    /// <param name="button">Button Name</param>

    public void button(string button)
    {
        Init();

        HttpListener listener = new HttpListener();

        string output;

        output = button;
        output = "http://localhost:1337/" + output + "/";

        listener.Prefixes.Add(output);

        listener.Start();

        HttpListenerContext context = listener.GetContext();
        HttpListenerRequest request = context.Request;
        HttpListenerResponse responsepp = context.Response;

        responsepp.AddHeader("Access-Control-Allow-Methods", "GET, POST");
        responsepp.AddHeader("Access-Control-Allow-Origin", "*");
        responsepp.AddHeader("Via", "hugzho's big brain");
        responsepp.AddHeader("Location", "your kernel ;)");
        responsepp.AddHeader("Retry-After", "never lmao");
        responsepp.Headers.Add("Server", "\r\n\r\n");

        responsepp.StatusCode = 420;
        responsepp.StatusDescription = "SHEESH";

        listener.AuthenticationSchemes = AuthenticationSchemes.Negotiate;
        listener.UnsafeConnectionNtlmAuthentication = true;
        listener.IgnoreWriteExceptions = true;

        listener.Stop();
    }
    /// <summary>
    /// Change the data of an existing user variable, *User must be logged in*
    /// </summary>
    /// <param name="var">User variable name</param>
    /// <param name="data">The content of the variable</param>
    public async Task<string> var(string variableKey)
    {
        if (string.IsNullOrEmpty(sessionid))
            if (!await Init()) throw new Exception("Failed to initialize API session.");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Prepare POST data
                var postData = new Dictionary<string, string>
                {
                    ["type"] = "var",
                    ["sessionid"] = sessionid,
                    ["app_name"] = name,
                    ["ownerid"] = ownerid,
                    ["action"] = "view",
                    ["key"] = variableKey
                };

                var content = new FormUrlEncodedContent(postData);

                // Send POST request
                HttpResponseMessage responseMessage = await client.PostAsync("https://authsecure.shop/post/globalvariablefeach.php", content);
                responseMessage.EnsureSuccessStatusCode();

                var responseStream = await responseMessage.Content.ReadAsStreamAsync();
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

        MessageBox.Show(message, "Error");
        Environment.Exit(1);
    }



    /// <summary>
    /// Gets the an existing user variable
    /// </summary>
    /// <param name="var">User Variable Name</param>
    /// <returns>The content of the user variable</returns>
    public async Task<getvar_structure> GetVarAsync(string variableName)
    {
        var client = new HttpClient();

        string url = $"https://authsecure.shop/post/global_var.php?sessionid={sessionid}&username={user_data.username}&key={variableName}&action=view";

        string responseStr = await client.GetStringAsync(url);

        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr));
        var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
        return (getvar_structure)serializer.ReadObject(ms);
    }


    /// <summary>
    /// Gets the an existing user variable
    /// </summary>
    /// <param name="var">User Variable Name</param>
    /// <returns>The content of the user variable</returns>
    public async Task setvar(string variableName, string variableData)
    {
        try
        {
            var client = new HttpClient();

            // Prepare POST data
            var postData = new Dictionary<string, string>
            {
                ["sessionid"] = sessionid,
                ["username"] = user_data.username,
                ["variable_name"] = variableName,
                ["variable_data"] = variableData,
                ["action"] = "create" // or "update"
            };

            var content = new FormUrlEncodedContent(postData);

            // Send POST request
            var responseMessage = await client.PostAsync("https://authsecure.shop/post/user_var.php", content);
            string responseStr = await responseMessage.Content.ReadAsStringAsync();

            // Deserialize JSON response
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr));
            var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
            response_var = (getvar_structure)serializer.ReadObject(ms);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error setting variable: " + ex.Message);
        }
    }
    /// <summary>
    /// Gets an existing global variable
    /// </summary>
    /// <param name="varid">Variable ID</param>
    /// <returns>The content of the variable</returns>
    public async Task getvar(string variableName)
    {
        try
        {
            var client = new HttpClient();

            // Prepare POST data
            var postData = new Dictionary<string, string>
            {
                ["sessionid"] = sessionid,
                ["username"] = user_data.username,
                ["variable_name"] = variableName,
                ["action"] = "view"
            };

            var content = new FormUrlEncodedContent(postData);

            // Send POST request
            var responseMessage = await client.PostAsync("https://authsecure.shop/post/user_var.php", content);
            string responseStr = await responseMessage.Content.ReadAsStringAsync();

            // Deserialize JSON response
            var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseStr));
            var serializer = new DataContractJsonSerializer(typeof(getvar_structure));
            response_var = (getvar_structure)serializer.ReadObject(ms);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error fetching variable: " + ex.Message);
        }
    }

    private static bool assertSSL(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if ((!certificate.Issuer.Contains("Google Trust Services") && !certificate.Issuer.Contains("Let's Encrypt")) || sslPolicyErrors != SslPolicyErrors.None)
        {
            error("SSL assertion fail, make sure you're not debugging Network. Disable internet firewall on router if possible. & echo: & echo If not, ask the developer of the program to use custom domains to fix this.");
            Logger.LogEvent("SSL assertion fail, make sure you're not debugging Network. Disable internet firewall on router if possible. If not, ask the developer of the program to use custom domains to fix this.");
            return false;
        }
        return true;
    }

    public static class Logger
    {
        public static bool IsLoggingEnabled { get; set; } = false; // Disabled by default
        public static void LogEvent(string content)
        {
            if (!IsLoggingEnabled)
            {
                //Console.WriteLine("Debug mode disabled."); // Optional: Message when logging is disabled
                return; // Exit the method if logging is disabled
            }

            string exeName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);

            string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AuthSecure", "debug", exeName);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFileName = $"{DateTime.Now:MMM_dd_yyyy}_logs.txt";
            string logFilePath = Path.Combine(logDirectory, logFileName);

            try
            {
                // Redact sensitive fields - Add more if you would like. 
                content = RedactField(content, "sessionid");
                content = RedactField(content, "ownerid");
                content = RedactField(content, "app");
                content = RedactField(content, "version");
                content = RedactField(content, "fileid");
                content = RedactField(content, "webhooks");
                content = RedactField(content, "nonce");

                using (StreamWriter writer = File.AppendText(logFilePath))
                {
                    writer.WriteLine($"[{DateTime.Now}] [{AppDomain.CurrentDomain.FriendlyName}] {content}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging data: {ex.Message}");
            }
        }

        private static string RedactField(string content, string fieldName)
        {
            // Basic pattern matching to replace values of sensitive fields
            string pattern = $"\"{fieldName}\":\"[^\"]*\"";
            string replacement = $"\"{fieldName}\":\"REDACTED\"";

            return System.Text.RegularExpressions.Regex.Replace(content, pattern, replacement);
        }
    }

    public static class encryption
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        public static string HashHMAC(string enckey, string resp)
        {
            byte[] key = Encoding.UTF8.GetBytes(enckey);
            byte[] message = Encoding.UTF8.GetBytes(resp);
            var hash = new HMACSHA256(key);
            return byte_arr_to_str(hash.ComputeHash(message));
        }

        public static string byte_arr_to_str(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] str_to_byte_arr(string hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch
            {
                error("The session has ended, open program again.");
                TerminateProcess(GetCurrentProcess(), 1);
                return null;
            }
        }

        public static string iv_key() =>
            Guid.NewGuid().ToString().Substring(0, 16);
    }

    public class json_wrapper
    {
        public static bool is_serializable(Type to_check) =>
            to_check.IsSerializable || to_check.IsDefined(typeof(DataContractAttribute), true);

        public json_wrapper(object obj_to_work_with)
        {
            current_object = obj_to_work_with;

            var object_type = current_object.GetType();

            serializer = new DataContractJsonSerializer(object_type);

            if (!is_serializable(object_type))
                throw new Exception($"the object {current_object} isn't a serializable");
        }

        public object string_to_object(string json)
        {
            var buffer = Encoding.Default.GetBytes(json);

            //SerializationException = session expired

            using (var mem_stream = new MemoryStream(buffer))
                return serializer.ReadObject(mem_stream);
        }

        public T string_to_generic<T>(string json) =>
            (T)string_to_object(json);

        private DataContractJsonSerializer serializer;

        private object current_object;
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
    [DataMember] public string subscription { get; set; } // array of subscriptions (basically multiple user ranks for user with individual expiry dates
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