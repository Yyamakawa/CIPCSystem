﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;

namespace CentralInterProcessCommunicationServer
{
    public class RemoteHostServer
    {
        #region private field
        private UDP_PACKETS_CLIANT.UDP_PACKETS_CLIANT client;
        private UDP_PACKETS_CODER.UDP_PACKETS_ENCODER enc;
        private UDP_PACKETS_CODER.UDP_PACKETS_DECODER dec;

        private const int myPort = Definitions.REMOTEHOSTSERVER_PORT;

        private const int max_connect_num = 200;

        public DebugWindow debugwindow { set; get; }
        public TerminalConnection.TerminalConnection terminalconnection { set; get; }

        #region server
        /// <summary>
        /// 現在保持しているリモートホストおよびそのタスクのリスト
        /// </summary>
        private List<CentralInterProcessCommunicationServer.RemoteHost> List_remotehost;
        private int id_port = Definitions.PORT_START_NUMBER;
        #endregion

        /// <summary>
        /// 現在保持しているポートのリスト
        /// </summary>
        private List<int> LstPort;
        private Task mytask;
        
        #endregion

        #region propaty
        public List<CentralInterProcessCommunicationServer.RemoteHost> List_RemoteHost
        {
            get 
            {
                return this.List_remotehost;
            }
        }
        public int server_port 
        {
            get 
            {
                return myPort;
            }
        }

        public int host_num 
        {
            get 
            {
                return LstPort.Count;
            }
        }
        public int Num_Port
        {
            get
            {
                return this.List_remotehost.Count;
            }
        }
        #endregion

        #region constructer
        public RemoteHostServer() 
        {
            try
            {
                client = new UDP_PACKETS_CLIANT.UDP_PACKETS_CLIANT(myPort);
            }
            catch (Exception ex) 
            {
                while (true)
                {
                    myDialog dialog = new myDialog(ex.Message);
                    if (dialog.ShowDialog() == true)
                    {
                        break;
                    }
                }
            }

            try
            {
                this.enc = new UDP_PACKETS_CODER.UDP_PACKETS_ENCODER();
                this.dec = new UDP_PACKETS_CODER.UDP_PACKETS_DECODER();
                this.LstPort = new List<int>();

                mytask = new Task(() => this.MAIN_TASK());
                mytask.Start();
            }
            catch (Exception ex) 
            {
                while (true)
                {
                    myDialog dialog = new myDialog(ex.Message);
                    if (dialog.ShowDialog() == true)
                    {
                        break;
                    }
                }
            }
            try
            {
                #region create remotehost servers list and one remotehost server
                this.List_remotehost = new List<RemoteHost>();
                #endregion
            }
            catch(Exception ex)
            {
                while (true)
                {
                    myDialog dialog = new myDialog(ex.Message);
                    if (dialog.ShowDialog() == true)
                    {
                        break;
                    }
                }
            }
        }
        #endregion

        private void MAIN_TASK()
        {
            try
            {
                while (true)
                {
                    int id_port = 0;
                    bool Is_Connected = false;
                    update(ref id_port, ref Is_Connected);

                    this.id_port = id_port;
                    if (Is_Connected)
                    {
                        this.List_remotehost.Add(new RemoteHost(id_port, this.debugwindow, this.terminalconnection));
                    }
                    else
                    {
                        this.List_remotehost.RemoveAll(atPort);
                    }
                }
                throw new Exception("RemoteHostServerが停止しました．");
            }
            catch (Exception ex)
            {
                debugwindow.DebugLog ="["+ this.ToString() +"]"+ ex.Message;
                Thread.Sleep(100);
                mytask = new Task(() => this.MAIN_TASK());
                mytask.Start();
            }

        }

        private bool atPort(RemoteHost obj)
        {
            if (int.Parse(obj.remotePort) == id_port)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }




        #region methods

        /// <summary>
        /// サーバがアクセスされるまで待機し、アクセスされた場合そのメッセージを検査し、その情報に従って
        /// ポートを割り振る、もしくは終了します。
        /// </summary>
        /// <returns></returns>
        private void update(ref int Port, ref bool isConnect) 
        {
            try
            {
                this.client.Close();
                this.client = null;
                Thread.Sleep(100);
                this.client = new UDP_PACKETS_CLIANT.UDP_PACKETS_CLIANT(myPort);
                this.debugwindow.DebugLog = "[RemoteHostServer]操作受付を開始します．ポート：" + this.server_port;
                while (true)
                {
                    if (this.client.Received_Data != null)
                    {
                        dec.Source = this.client.Received_Data;
                        break;
                    }
                    Thread.Sleep(10);
                }
                
                Thread.Sleep(100);
                switch (dec.get_int()) 
                {
                    case Definitions.CONNECTION_DEMANDS:
                        isConnect = true;
                        break;
                    case Definitions.CONNECTION_END:
                        isConnect = false;
                        break;
                    default:
                        this.debugwindow.DebugLog = "要求された信号に対応するコマンドが存在しません。リモートホスト側のコネクトの設定を確認してください。";
                        break;
                }

                if (isConnect)
                {
                    this.connect(ref Port);
                }
                else 
                {
                    this.disconnect(dec.get_int());
                    try
                    {
                        this.terminalconnection.Tcp_Send();
                    }
                    catch (Exception ex)
                    {
                        debugwindow.DebugLog = "[" + this.ToString() + "]" + ex.Message;
                    }
                }

                
            }catch(Exception ex)
            {
                MessageBox.Show( ex.Message + "ポートを割り振るサーバのアップデートでエラーが発生しました。");
            }
        }

        private void connect(ref int port) 
        {
            try
            {
                for (int current_port = Definitions.PORT_START_NUMBER; current_port < Definitions.PORT_START_NUMBER + max_connect_num; current_port++)
                {
                    if (!this.LstPort.Contains(current_port))
                    {
                        port = current_port;
                        this.LstPort.Add(current_port);
                        
                        this.enc = new UDP_PACKETS_CODER.UDP_PACKETS_ENCODER();
                        this.enc += Definitions.CONNECTION_OK;
                        this.enc += current_port;
                        this.client.Send(this.enc.data);

                        this.debugwindow.DebugLog = "[RemoteHostServer]接続要求がきました．解放ポートを通知します．ポート番号：" + port.ToString();
                        break;
                    }
                }
            }catch(Exception ex)
            {
                debugwindow.DebugLog = ex.Message + "コネクトの段階でエラーが発生しました。";
            }
        }

        private int destroyport;
        private void disconnect(int port) 
        {
            try 
            {
                this.debugwindow.DebugLog = "[RemoteHostServer]切断要求がきました．ポート番号：" + port.ToString();
                destroyport = port;
                this.LstPort.Remove(port);
                this.List_remotehost.RemoveAll(check_remotehosts);
                
                this.enc = new UDP_PACKETS_CODER.UDP_PACKETS_ENCODER();
                this.enc += Definitions.CONNECTION_END;
                this.client.Send(this.enc.data);
            }
            catch (Exception ex)
            {
                debugwindow.DebugLog = ex.Message + "接続を終了する段階でエラーが発生しました。";
            }
        }

        /// <summary>
        /// ポートを指定して強制的にリモートホストを削除します
        /// </summary>
        /// <param name="port"></param>
        public void ShutDownDisconnect(int port)
        {
            try
            {
                this.debugwindow.DebugLog = "[RemoteHostServer]強制切断します．ポート番号：" + port.ToString();
                destroyport = port;
                this.LstPort.Remove(port);
                this.List_remotehost.RemoveAll(check_remotehosts);

                this.terminalconnection.Tcp_Send();
            }
            catch (Exception ex)
            {
                debugwindow.DebugLog = ex.Message + "接続を終了する段階でエラーが発生しました。";
            }
        }

        private bool check_remotehosts(RemoteHost obj)
        {
            if (obj.ID == destroyport)
            {
                obj.disconnect();
                return true;
            }
            else 
            {
                return false;
            }
        }


        #region ListAdd
        CollectionViewSource view = new CollectionViewSource();
        public ObservableCollection<Client> Clients = new ObservableCollection<Client>();
        public void Listupdate( ListView LISTVIEW)
        {

            Clients.Clear();
            if (this.List_remotehost.Count >= 0)
            {
                for (int i = 0; i < this.List_remotehost.Count; i++)
                {
                    Clients.Add(new Client()
                    {
                        Name = this.List_remotehost[i].Name,
                        Port = this.List_remotehost[i].ID,
                        remoteIP = this.List_remotehost[i].remoteIP,
                        remotePort = this.List_remotehost[i].remotePort,
                        Connect_Name = this.List_remotehost[i].Connect_Name,
                        FPS = this.List_remotehost[i].Fps,
                        mode = this.List_remotehost[i].Connection_Mode.ToString()
                    });
                }
            }
            view.Source = Clients;
            LISTVIEW.DataContext = view;
        }
        #endregion
        #endregion
    }
    public class Client
    {
        public string Name { set; get; }
        public int Port { set; get; }
        public string remoteIP { set; get; }
        public string remotePort { set; get; }
        public string Connect_Name { set; get; }
        public int FPS { set; get; }
        public string mode { set; get; }
    }
}
