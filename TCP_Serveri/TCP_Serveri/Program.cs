
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

namespace MultiServer
{


    class Program
    {

        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];


        static void Main()
        {
            Console.Title = "Server";
            SetupServer();
            Console.ReadLine(); // When we press enter close everything
            CloseAllSockets();
        }
        private static void SetupServer()
        {
            IPAddress myIP = IPAddress.Parse("127.1.0.1");
            Console.WriteLine("Setting up server...");
            serverSocket.Bind(new IPEndPoint(myIP, PORT));
            serverSocket.Listen(0);
            serverSocket.BeginAccept(AcceptCallback, null);
            Console.WriteLine("Server setup complete");
        }

        /// <summary>
        /// Close all connected client (we do not need to shutdown the server socket as its connections
        /// are already closed with the clients).
        /// </summary>
        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            serverSocket.Close();
        }


        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);
            Console.WriteLine("Client connected, waiting for request...");
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Console.WriteLine("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                current.Close();
                clientSockets.Remove(current);
                return;
            }


            byte[] recBuf = new byte[received];
            Array.Copy(buffer, recBuf, received);
            string text = Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Text: " + text);




            if (text.ToLower() == "get time") // Client requested time
            {
                Console.WriteLine("Text is a get time request");
                byte[] data = Encoding.ASCII.GetBytes(DateTime.Now.ToLongTimeString());
                current.Send(data);
                Console.WriteLine("Time sent to client");
            }

            else if (text.ToLower() == "ooop create file") // client wants to create file
            {
                Console.WriteLine("Text is a create file request");
                string path = @"C:\Users\jupol\OneDrive\Desktop\projekti rrjeta";
                using (FileStream fs = File.Create(path)) ;
                byte[] data = Encoding.ASCII.GetBytes("The file has been created");
                current.Send(data);
            }

            else if (text.ToLower() == "ooop delete file") // client wants to delete file
            {
                Console.WriteLine("Text is a delete file request");
                string path = @"C:\Users\jupol\OneDrive\Desktop\projekti rrjeta";
                if (File.Exists(path))
                {
                    File.Delete(path);
                    byte[] data = Encoding.ASCII.GetBytes("The file has been deleted");
                    current.Send(data);
                }
            }
            else if (text.Contains("read") == true) //any client can read file
            {
                Console.WriteLine("Text is a read file request");
                string text1 = System.IO.File.ReadAllText(@"C:\Users\jupol\OneDrive\Desktop\projekti rrjeta");
                byte[] data1 = Encoding.ASCII.GetBytes(text1);
                current.Send(data1);
            }
            else if (text.ToLower() == "ooop write file") //client wants to write file
            {
                Console.WriteLine("Text is a write file request");
                string createText = "Hello and Welcome" + Environment.NewLine;
                File.WriteAllText(@"C:\Users\jupol\OneDrive\Desktop\projekti rrjeta", createText);
                byte[] data2 = Encoding.ASCII.GetBytes(createText);
                current.Send(data2);
            }
            else if (text.ToLower() == "ooop open file")
            {
                Console.WriteLine("Text is a open file request");
                Process p = new Process();
                ProcessStartInfo pi = new ProcessStartInfo();
                pi.UseShellExecute = true;
                pi.FileName = @"C:\Users\jupol\OneDrive\Desktop\projekti rrjeta";
                p.StartInfo = pi;
                try
                {
                    p.Start();
                }
                catch (Exception Ex)
                {
                    //MessageBox.Show(Ex.Message);
                }
                byte[] data3 = Encoding.ASCII.GetBytes(pi.FileName);
                current.Send(data3);
            }
            else if (text.ToLower() == "exit") // Client wants to exit gracefully
            {
                // Always Shutdown before closing
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Client disconnected");
                return;
            }
            else
            {
                Console.WriteLine("Text is an invalid request");
                byte[] data = Encoding.ASCII.GetBytes("Invalid request");
                current.Send(data);
                Console.WriteLine("Warning Sent");

            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);
        }
    }
}