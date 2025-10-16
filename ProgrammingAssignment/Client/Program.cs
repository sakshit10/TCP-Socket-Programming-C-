using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SocketProgrammingAssignment
{
    public class TCPClient
    {
        private readonly string _host;
        private readonly int _port;
        private readonly byte[] _encryptionKey;
        private readonly bool _encryptionEnabled;
        private TcpClient _client;
        private NetworkStream _stream;

        public TCPClient(string host = "127.0.0.1", int port = 5555, byte[] encryptionKey = null)
        {
            _host = host;
            _port = port;
            _encryptionKey = encryptionKey;
            _encryptionEnabled = encryptionKey != null;
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

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port);
                _stream = _client.GetStream();

                string encryptionStatus = _encryptionEnabled ? "ENABLED" : "DISABLED";
                Console.WriteLine($"[CONNECTED] Connected to server at {_host}:{_port}");
                Console.WriteLine($"[ENCRYPTION] {encryptionStatus}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Connection failed: {ex.Message}");
                return false;
            }
        }

        public async Task SendRequestAsync(string message)
        {
            try
            {
                string encryptedMsg = EncryptMessage(message);
                byte[] data = Encoding.UTF8.GetBytes(encryptedMsg);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();

                Console.WriteLine($"\n[SENT] {message}");
                Console.WriteLine("[RECEIVING] Waiting for server responses...\n");

                int responseCount = 0;
                byte[] buffer = new byte[1024];

                _stream.ReadTimeout = 2000;

                while (true)
                {
                    try
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead == 0)
                            break;

                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        string response = DecryptMessage(receivedMessage);
                        if (!string.IsNullOrEmpty(response))
                        {
                            responseCount++;
                            Console.WriteLine($"[RESPONSE {responseCount}] {response}");

                            if (response == "EMPTY" || response.Contains("Invalid format"))
                            {
                                break;
                            }
                        }
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }

                if (responseCount == 0)
                {
                    Console.WriteLine("[INFO] No response received from server");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        public async Task InteractiveModeAsync()
        {
            Console.WriteLine("\n============================================================");
            Console.WriteLine("TCP CLIENT - INTERACTIVE MODE");
            Console.WriteLine("============================================================");
            Console.WriteLine("\nFormat: SetX-Key (e.g., SetA-Two)");
            Console.WriteLine("Available Sets: SetA, SetB, SetC, SetD, SetE");
            Console.WriteLine("Type 'quit' or 'exit' to close connection\n");

            while (true)
            {
                try
                {
                    Console.Write("[INPUT] Enter request: ");
                    string input = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    if (input.ToLower() == "quit" || input.ToLower() == "exit" || input.ToLower() == "q")
                    {
                        Console.WriteLine("[INFO] Closing connection...");
                        break;
                    }

                    await SendRequestAsync(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    break;
                }
            }
        }

        public void Close()
        {
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("[DISCONNECTED] Connection closed");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("TCP CLIENT - SOCKET PROGRAMMING ASSIGNMENT");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            string host = "127.0.0.1";
            int port = 5555;

            TCPClient client = new TCPClient(host, port);

            if (!await client.ConnectAsync())
            {
                return;
            }

            try
            {
                await client.InteractiveModeAsync();
            }
            finally
            {
                client.Close();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}