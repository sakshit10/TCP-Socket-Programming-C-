using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketProgrammingAssignment
{
    public class TCPServer
    {
        private readonly string _host;
        private readonly int _port;
        private readonly byte[] _encryptionKey;
        private readonly bool _encryptionEnabled;
        private TcpListener _listener;
        private readonly Dictionary<string, List<Dictionary<string, int>>> _serverData;
        private int _activeConnections = 0;

        public TCPServer(string host = "127.0.0.1", int port = 5555, byte[] encryptionKey = null)
        {
            _host = host;
            _port = port;
            _encryptionKey = encryptionKey;
            _encryptionEnabled = encryptionKey != null;

            // Initialize server data structure as per assignment
            _serverData = new Dictionary<string, List<Dictionary<string, int>>>
            {
                { "SetA", new List<Dictionary<string, int>> { new Dictionary<string, int> { { "One", 1 }, { "Two", 2 } } } },
                { "SetB", new List<Dictionary<string, int>> { new Dictionary<string, int> { { "Three", 3 }, { "Four", 4 } } } },
                { "SetC", new List<Dictionary<string, int>> { new Dictionary<string, int> { { "Five", 5 }, { "Six", 6 } } } },
                { "SetD", new List<Dictionary<string, int>> { new Dictionary<string, int> { { "Seven", 7 }, { "Eight", 8 } } } },
                { "SetE", new List<Dictionary<string, int>> { new Dictionary<string, int> { { "Nine", 9 }, { "Ten", 10 } } } }
            };
        }

        private string EncryptMessage(string message)
        {
            if (!_encryptionEnabled) return message;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;
                    aes.GenerateIV();

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ENCRYPTION ERROR] {ex.Message}");
                return null;
            }
        }

        private string DecryptMessage(string encryptedMessage)
        {
            if (!_encryptionEnabled) return encryptedMessage;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(encryptedMessage);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = _encryptionKey;

                    byte[] iv = new byte[aes.IV.Length];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    byte[] cipherText = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipherText, 0, cipherText.Length);

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DECRYPTION ERROR] {ex.Message}");
                return null;
            }
        }

        private int? LookupValue(string key, string subKey)
        {
            if (_serverData.ContainsKey(key))
            {
                var subset = _serverData[key][0];
                if (subset.ContainsKey(subKey))
                {
                    return subset[subKey];
                }
            }
            return null;
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string clientAddress = client.Client.RemoteEndPoint.ToString();
            Interlocked.Increment(ref _activeConnections);
            Console.WriteLine($"[NEW CONNECTION] {clientAddress} connected");
            Console.WriteLine($"[ACTIVE CONNECTIONS] {_activeConnections}");

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        string message = DecryptMessage(receivedMessage);
                        if (string.IsNullOrEmpty(message))
                            continue;

                        Console.WriteLine($"[{clientAddress}] Received: {message}");

                        if (message.Contains("-"))
                        {
                            string[] parts = message.Split('-');
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string subKey = parts[1].Trim();

                                int? value = LookupValue(key, subKey);

                                if (value.HasValue)
                                {
                                    Console.WriteLine($"[{clientAddress}] Found {key}-{subKey} = {value.Value}");

                                    for (int i = 0; i < value.Value; i++)
                                    {
                                        string currentTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                                        string response = currentTime;

                                        string encryptedResponse = EncryptMessage(response);
                                        byte[] responseData = Encoding.UTF8.GetBytes(encryptedResponse);

                                        await stream.WriteAsync(responseData, 0, responseData.Length);
                                        await stream.FlushAsync();

                                        Console.WriteLine($"[{clientAddress}] Sent: {currentTime} ({i + 1}/{value.Value})");

                                        if (i < value.Value - 1)
                                        {
                                            await Task.Delay(1000);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[{clientAddress}] Not found: {key}-{subKey}");
                                    string emptyMsg = EncryptMessage("EMPTY");
                                    byte[] emptyData = Encoding.UTF8.GetBytes(emptyMsg);
                                    await stream.WriteAsync(emptyData, 0, emptyData.Length);
                                }
                            }
                            else
                            {
                                string errorMsg = EncryptMessage("Invalid format. Use: SetX-Key");
                                byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
                                await stream.WriteAsync(errorData, 0, errorData.Length);
                            }
                        }
                        else
                        {
                            string errorMsg = EncryptMessage("Invalid format. Use: SetX-Key");
                            byte[] errorData = Encoding.UTF8.GetBytes(errorMsg);
                            await stream.WriteAsync(errorData, 0, errorData.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {clientAddress}: {ex.Message}");
            }
            finally
            {
                client.Close();
                Interlocked.Decrement(ref _activeConnections);
                Console.WriteLine($"[DISCONNECTED] {clientAddress}");
                Console.WriteLine($"[ACTIVE CONNECTIONS] {_activeConnections}");
            }
        }

        public async Task StartAsync()
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(_host);
                _listener = new TcpListener(ipAddress, _port);
                _listener.Start();

                string encryptionStatus = _encryptionEnabled ? "ENABLED" : "DISABLED";
                Console.WriteLine($"[STARTING] Server is starting on {_host}:{_port}");
                Console.WriteLine($"[ENCRYPTION] {encryptionStatus}");
                Console.WriteLine($"[LISTENING] Server is listening for connections...");

                while (true)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        public void Stop()
        {
            _listener?.Stop();
            Console.WriteLine("[STOPPED] Server stopped");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("TCP SERVER - SOCKET PROGRAMMING ASSIGNMENT");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            string host = "127.0.0.1";
            int port = 5555;

            TCPServer server = new TCPServer(host, port);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n[SHUTTING DOWN] Server is shutting down...");
                server.Stop();
                Environment.Exit(0);
            };

            await server.StartAsync();
        }
    }
}