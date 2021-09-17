using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace POC.Console.Sender
{
    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousClient
    {
        // The port number for the remote device.
        private const int port = 11000;

        // ManualResetEvent instances signal completion.
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        // The response from the remote device.
        private static String response = String.Empty;

        private static void StartClient()
        {
            string sMessage = string.Empty;

            string sHeader = string.Empty;
            string msg_length = string.Empty;
            string if_id = string.Empty;
            string msg_key = string.Empty;
            string tr_dt = string.Empty;
            string res_code = string.Empty;
            string res_cnt = string.Empty;
            string reserved = string.Empty;

            string sBody = string.Empty;
            string inq_ymd = string.Empty;
            string bsn_se = string.Empty;

            const string sDelimiter = ";";
            string sJournalSystem = string.Empty;
            string sIP = string.Empty;

            // Connect to a remote device.
            try
            {
                sJournalSystem = Config.GetConfig("ServerMachine");
                sIP = Config.GetConfig("IP");

                // Establish the remote endpoint for the socket.
                // The name of the remote device is "knyang".
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("knyang");
                IPHostEntry ipHostInfo = Dns.GetHostEntry(sIP);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Set message header
                msg_length = "00000220";                  // 8 byte
                if_id = "AGCXX00001";                       // 10 byte
                msg_key = "9394929294929394929391720"; // 25 byte
                tr_dt = "20070801092120";                 //  14 byte
                res_code = "200";                              // 3 byte
                res_cnt = "00000001";                        // 8 byte
                reserved = "00000000000000000000000000000000"; // 32 byte
                sHeader = msg_length + if_id + msg_key + tr_dt + res_code + res_cnt + reserved;

                // Set message body
                inq_ymd = "20070801";                      // 8 byte
                bsn_se = "A";                                    // 1 byte "A"  출퇴근정보, "B" 비상소집정보
                sBody = inq_ymd + sDelimiter + bsn_se;

                // Send test data to the remote device.
                //Send(client, "This is a test<EOF>");
                Send(client, sHeader + sBody);
                sendDone.WaitOne();

                // Receive the response from the remote device.
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the System.Console.
                System.Console.WriteLine("Response received : {0}", response);

                // Release the socket.
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                System.Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket 
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    //state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    Encoding e = System.Text.Encoding.GetEncoding(949);
                    state.sb.Append(e.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                    System.Console.WriteLine(state.ToString());
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            //byte[] byteData = Encoding.ASCII.GetBytes(data);
            Encoding e = System.Text.Encoding.GetEncoding(949);
            byte[] byteData = e.GetBytes(data);

            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                System.Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        public static int Main(String[] args)
        {
            StartClient();
            return 0;
        }
    }
}