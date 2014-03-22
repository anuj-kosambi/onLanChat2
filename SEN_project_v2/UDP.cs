﻿#define NUDP
#if UDP
#define UDPConnection
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace SEN_project_v2
{
    public class UDP
    {
        public UdpClient recevingClient = null;
        public UdpClient sendingClient = null;
        public UdpClient rtpReceClient = null;
        public UdpClient rtpSendClient = null;
        public const string Connect = "<#Connect#>";
        public const string RConnect = "<\\#Connect#>";
        public const string Disconnect = "<#Disconnect#>";
        public const string Message = "<#Message#>";
        public const string Videocall = "<#VideoCall>";
        public const string RVideocall = "<\\#VideoCall>";
        public const string AddMember = "<#Add#>";
        public const string RemoveMember = "<#Remove#>";
        private int port;
        private MainWindow window;

        public UDP(int port)
        {
            if (port == (int)MainWindow.Ports.UDP)
            {
                recevingClient = new UdpClient(port);
               
            }
            else if (port == (int)MainWindow.Ports.RTP)
            {
                rtpReceClient = new UdpClient(port);
              
            }
            sendingClient = new UdpClient();
            rtpSendClient = new UdpClient();
            this.port = port;


        }

        public void SendMessageTo(string value, IPAddress ip)
        {
            sendingClient.Connect(new IPEndPoint(ip, port));
            sendingClient.Send(Encoding.ASCII.GetBytes(value), value.Length);
#if UDP
            System.Diagnostics.Debug.WriteLine("UDP:||-----Sending:" + value + " to " + ip.ToString() + "------");
#endif
        }

        public void recevingThread()
        {
            recevingClient.Client.ReceiveBufferSize = 1024 * 1024;

            while (true)
            {


                byte[] data;
                IPEndPoint recevied = new IPEndPoint(IPAddress.Any, port);
                data = recevingClient.Receive(ref recevied);

                string stringData = Encoding.ASCII.GetString(data);
#if UDPConnection
                System.Diagnostics.Debug.WriteLine("UDP||-----Recevied " + stringData + " from " + recevied.Address + " ----");
#endif
 
                  
                #region Connection
                if (stringData.StartsWith(Connect))
                    
                {
                    if(MainWindow.hostIPS.Contains(recevied.Address))
                    {
                        MainWindow.hostIP = recevied.Address;
                        
                    }
                    
                    string[] splits = stringData.Split(new String[] { Connect }, StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Length == 0)
                    {
#if UDPConnection
                        System.Diagnostics.Debug.WriteLine("-----" + recevied.Address + "Does  not Contain PC/Nick Name----");
#endif
                        continue;
                    }

                    SendMessageTo(RConnect + Environment.MachineName, recevied.Address);
 #if UDPConnection
                    System.Diagnostics.Debug.WriteLine("-----Sending:" + RConnect + "------");
#endif
                    User user = new User(recevied.Address, splits[0]);
                    if (UserList.Add(user))
                        window.Dispatcher.Invoke((Action)(() =>
                        {
                            this.window.AddToUserList(user.CreateView());
                        }));

                }
                else if (stringData.StartsWith(RConnect))
                {
                    string[] splits = stringData.Split(new String[] { RConnect }, StringSplitOptions.RemoveEmptyEntries);
                    User user = new User(recevied.Address, splits[0]);
         
                    if (UserList.Add(user))
                        window.Dispatcher.Invoke((Action)(() =>
                        {
                            this.window.AddToUserList(user.CreateView());
                        }));

                }

                else if (stringData.StartsWith(Disconnect))
                {
                    User user = new User(recevied.Address, "Disconnecting..");
                    UserList.Add(user);
                    UserList.Remove(recevied.Address);
                    window.Dispatcher.Invoke((Action)(() =>
                    {
                        this.window.RemoverFromUserList(recevied.Address);
                    }));
                }
                #endregion
                #region VideoCall Connection

                else if (stringData.StartsWith(Videocall))
                {
                  

                    window.Dispatcher.Invoke((Action)(() =>
                    { window.CreateVideoConf(recevied.Address); }));
                    //window.videoConf.Dispatcher.Invoke((Action)(() =>
                    //{
                    //    window.videoConf.statusLabel.Content = "Connected to Host ...";
                    //    window.videoConf.AddUser(recevied.Address);
                    //    window.videoConf.MakeUserPreview(recevied.Address, VideoPreview.Mode.Request);

                    //}));

                }
                else if (stringData.StartsWith(RVideocall))
                {
                    foreach (IPAddress ip in window.videoConf.Users)
                    {
                        SendMessageTo(AddMember + recevied.Address + AddMember, ip);
                    }
                    foreach (IPAddress ip in window.videoConf.Users)
                    {
                        SendMessageTo(AddMember + ip.ToString() + AddMember, recevied.Address);
                    }
                    window.videoConf.Users.Add(recevied.Address);
                    window.videoConf.Dispatcher.Invoke((Action)(() =>
                    {
                        window.videoConf.vp[recevied.Address]._Mode = VideoPreview.Mode.InCall;
                        if (window.videoConf.Users.Count == window.videoConf.requestedUsers.Count)
                            window.videoConf.statusLabel.Content = "Room Created Successfully...";

                    }));
                    //System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap("temp.jpg");
                    //System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    //bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    //foreach (IPAddress ip in window.videoConf.Users)
                    //{
                    //    window.Dispatcher.Invoke((Action)(() =>
                    //        {
                    //            window.rtpClient.rtpSender.Send(ms.GetBuffer());

                               
                    //        }));
                    //}
                    
                }
                else if (stringData.StartsWith(AddMember))
                {
                    string[] splits = stringData.Split(new String[] { AddMember }, StringSplitOptions.RemoveEmptyEntries);
                    if (splits.Length > 0)
                    {
                        window.Dispatcher.Invoke((Action)(() =>
                        {
                           
                 //       if (window.videoConf == null)
                 //           window.CreateVideoConf(recevied.Address);
                        }));
                        window.videoConf.Dispatcher.Invoke((Action)(() =>
                        {
                           
                            window.videoConf.AddUser(IPAddress.Parse(splits[0]));
                            window.videoConf.MakeUserPreview(IPAddress.Parse(splits[0]), VideoPreview.Mode.InCall);
                        }));
                    }


                }
                else if (stringData.StartsWith(RemoveMember))
                {
                    window.videoConf.Dispatcher.Invoke((Action)(() =>
                    {
                        window.videoConf.requestedUsers.Remove(recevied.Address);
                        window.videoConf._stack.Children.Remove(window.videoConf.vp[recevied.Address]);
                        window.videoConf.statusLabel.Content = UserList.Get(recevied.Address).nick + " can't Join :(";
                        if (window.videoConf.Users.Count == window.videoConf.requestedUsers.Count)
                            window.videoConf.statusLabel.Content = "Room Created Successfully With ("+window.videoConf.requestedUsers.Count+")Members ...:D";

                    }));
                }

                #endregion

            }


        }
        public void SetWindow(MainWindow window)
        {
            this.window = window;
        }
  
    }
}