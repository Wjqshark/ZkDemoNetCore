using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.recipes.@lock;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;

namespace Zookeeper.Test
{
    class Program
    {
        private static ZookeeperClient m_client;

        private static string m_ClientName = "Client0";

        static async Task Main(string[] args)
        {

            if (args != null && args.Length > 0)
            {
                m_ClientName = args[0];
            }

            Console.WriteLine($"当前运行{m_ClientName}");

            m_client = NewClient();

            if (m_client != null)
            {
                Console.WriteLine($"{m_ClientName}连接成功");
                await UpdateOnlineState();
                await ClientWatcher();
                await GroupMsgWatcher();
                await PrivateMsgWatcher();
            }


            //CreateClientTree();
            //CreateMessageTree();


            bool isExit = false;


            while (!isExit)
            {
                var command = Console.ReadLine();

                if (command == "exit")
                {
                    isExit = true;
                }
                else
                {
                    await ExcuteCommand(command);
                }


            }

            await ExitCommand();





            //if (await m_client.ExistsAsync("/Client"))
            //{
            //    List<string> childList = m_client.GetChildrenAsync("/Client").Result.ToList();

            //    if (childList.Count > 0)
            //    {
            //        Console.WriteLine($"目前已经连接{childList.Count}个节点");
            //        foreach (var child in childList)
            //        {
            //            Console.Write($"{child}\t");
            //        }
            //        Console.Write($"\r\n");
            //    }
            //}

            //if (await m_client.ExistsAsync("/Client"))
            //{
            //    //Fast create temporary nodes

            //    bool flag = false;

            //    int index = 0;

            //    while (!flag)
            //    {
            //        if (await m_client.ExistsAsync($"/Client/Client{index}") == false)
            //        {
            //            var data = Encoding.UTF8.GetBytes("2020");
            //            await m_client.CreateRecursiveAsync($"/Client/Client{index}", data);

            //            m_ClientName = $"/Client/Client{index}";
            //            Console.WriteLine($"写入/Client/Client{index} 2020");
            //            flag = true;
            //            break;
            //        }
            //        index++;
            //    }
            //}
            //else
            //{
            //    var data = Encoding.UTF8.GetBytes("2020");
            //    await client.CreateRecursiveAsync("/Client/Client0", data);
            //    Console.WriteLine("写入/Client/Client0 2020");
            //}

            ////if (await client.ExistsAsync("/year"))
            ////{
            ////    IEnumerable<byte> data1 = await client.GetDataAsync("/year");
            ////    string str1 = Encoding.UTF8.GetString(data1.ToArray());
            ////    Console.WriteLine($"获取/year 值为 {str1}");
            ////}


            //Console.ReadLine();
            //await client.DeleteRecursiveAsync(m_ClientName);
            //Console.WriteLine($"{m_ClientName} 已经移除");

        }


        /// <summary>
        /// 新建Client
        /// </summary>
        /// <returns></returns>
        private static ZookeeperClient NewClient()
        {
            ZookeeperClient client = new ZookeeperClient(new ZookeeperClientOptions("192.168.1.14:2181")
            {
                BasePath = "/", //default value
                ConnectionTimeout = TimeSpan.FromSeconds(10), //default value
                SessionTimeout = TimeSpan.FromSeconds(600),
                OperatingTimeout = TimeSpan.FromSeconds(60), //default value
                ReadOnly = false, //default value
                SessionId = 0, //default value
                SessionPasswd = null, //default value
                EnableEphemeralNodeRestore = true //default value
            });


            //判断是否连接成功
            if (!client.WaitUntilConnected(TimeSpan.FromSeconds(10)))
            {
                Console.WriteLine($"未连接成功");
                Console.ReadLine();
                return null;
            }
            return client;
        }


        /// <summary>
        /// 创建Client节点树
        /// </summary>
        /// <returns></returns>
        private static async Task CreateClientTree()
        {
            if (m_client != null)
            {
                //移除Client所有节点
                await m_client.DeleteRecursiveAsync("/Client");
                var data = Encoding.UTF8.GetBytes("OffLine");
                await m_client.CreateRecursiveAsync("/Client/Client0", data);
                await m_client.CreateRecursiveAsync("/Client/Client1", data);
                await m_client.CreateRecursiveAsync("/Client/Client2", data);
            }
        }


        /// <summary>
        /// 创建Message节点树
        /// </summary>
        /// <returns></returns>
        private static async Task CreateMessageTree()
        {
            if (m_client != null)
            {
                //移除Message所有节点
                await m_client.DeleteRecursiveAsync("/Message");
                var data = Encoding.UTF8.GetBytes("");
                await m_client.CreateRecursiveAsync("/Message/GroupMessage", data);
                await m_client.CreateRecursiveAsync("/Message/PrivateMessage/Client0", data);
                await m_client.CreateRecursiveAsync("/Message/PrivateMessage/Client1", data);
                await m_client.CreateRecursiveAsync("/Message/PrivateMessage/Client2", data);
            }
        }



        static private async Task ExcuteCommand(string commandStr)
        {
            //查询在线状态
            if (commandStr == "state")
            {
                await OutputOnlineState();
            }
            //发送群组消息
            else if (commandStr.StartsWith("groupmsg "))
            {
                string msg = commandStr.Replace("groupmsg ", "");

                await SendGroupMessage(msg);
            }
            //发送私有消息
            else if (commandStr.StartsWith("privatemsg "))
            {
                string str = commandStr.Replace("privatemsg ", "");
                var result = str.Split(" ");

                if (result.Length >= 2)
                {
                    await SendPrivateMessage(result[0], result[1]);
                }

            }
        }


        #region 在线状态

        /// <summary>
        /// 更新该节点上线状态
        /// </summary>
        /// <returns></returns>
        private static async Task UpdateOnlineState()
        {
            if (m_client != null)
            {
                string path = $"/Client/{m_ClientName}";
                var data = Encoding.UTF8.GetBytes("OnLine");
                if (await m_client.ExistsAsync(path))
                {
                    await m_client.SetDataAsync(path, data);
                }
                else
                {
                    await m_client.CreateRecursiveAsync(path, data);
                }
            }
        }

        /// <summary>
        /// 输出在线状态
        /// </summary>
        /// <returns></returns>
        private static async Task OutputOnlineState()
        {
            if (m_client != null)
            {
                var childs = await m_client.GetChildrenAsync("/Client");
                foreach (var child in childs)
                {
                    string childpath = $"/Client/{child}";
                    if (await m_client.ExistsAsync(childpath))
                    {
                        var data1 = await m_client.GetDataAsync(childpath);
                        string onlineresult = Encoding.UTF8.GetString(data1.ToArray());
                        string result = $"{child}状态是{onlineresult}";
                        Console.WriteLine(result);
                    }
                }
            }
        }


        #endregion

        #region Watcher
        /// <summary>
        /// 检测Client上下线
        /// </summary>
        /// <returns></returns>
        private static async Task ClientWatcher()
        {
            if (m_client != null)
            {
                var childs = await m_client.GetChildrenAsync("/Client");
                foreach (var child in childs)
                {
                    string childpath = $"/Client/{child}";
                    if (await m_client.ExistsAsync(childpath))
                    {
                        await m_client.SubscribeDataChange(childpath, (ct, args) =>
                        {
                            IEnumerable<byte> currentData = args.CurrentData;
                            string onlineresult = Encoding.UTF8.GetString(currentData.ToArray());
                            string path = args.Path;
                            string client = path.Replace("/Client/", "");
                            Watcher.Event.EventType eventType = args.Type;

                            string result = $"通知:{client}状态是{onlineresult}";
                            Console.WriteLine(result);

                            return Task.CompletedTask;
                        });
                    }
                }
            }
        }

        private static List<string> groupMsgList = new List<string>();

        /// <summary>
        /// 检测群组消息
        /// </summary>
        /// <returns></returns>
        private static async Task GroupMsgWatcher()
        {
            if (m_client != null)
            {
                await m_client.SubscribeChildrenChange("/Message/GroupMessage", async (ct, args) =>
                {
                    List<string> currentChildrens = args.CurrentChildrens.ToList();
                    string path = args.Path;
                    Watcher.Event.EventType eventType = args.Type;

                    var newadded = currentChildrens.Where(t => !groupMsgList.Contains(t)).ToList();

                    foreach (var newMsg in newadded)
                    {
                        string newpath = $"/Message/GroupMessage/{newMsg}";

                        if (await m_client.ExistsAsync(newpath))
                        {
                            var data = await m_client.GetDataAsync(newpath);
                            string msg = Encoding.UTF8.GetString(data.ToArray());

                            string result = $"{msg},RecieveTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
                            Console.WriteLine(result);
                        }
                    }

                    groupMsgList.AddRange(newadded);

                    var needdelete = groupMsgList.Where(t => !currentChildrens.Contains(t)).ToList();

                    foreach (var delete in needdelete)
                    {
                        groupMsgList.Remove(delete);
                    }

                });
            }
        }

        /// <summary>
        /// 检测私有消息
        /// </summary>
        /// <returns></returns>
        private static async Task PrivateMsgWatcher()
        {
            if (m_client != null)
            {
                await m_client.SubscribeChildrenChange($"/Message/PrivateMessage/{m_ClientName}", async (ct, args) =>
                {
                    List<string> currentChildrens = args.CurrentChildrens.ToList();
                    string path = args.Path;
                    Watcher.Event.EventType eventType = args.Type;

                    foreach (var newMsg in currentChildrens)
                    {
                        string newpath = $"/Message/PrivateMessage/{m_ClientName}/{newMsg}";

                        if (await m_client.ExistsAsync(newpath))
                        {
                            var data = await m_client.GetDataAsync(newpath);
                            string msg = Encoding.UTF8.GetString(data.ToArray());

                            string result = $"{msg},RecieveTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
                            Console.WriteLine(result);

                            await m_client.DeleteAsync(newpath);
                        }
                    }

                });
            }
        }


        #endregion



        #region Message
        /// <summary>
        /// 发送群组消息
        /// </summary>
        /// <returns></returns> 
        private static async Task SendGroupMessage(string msg)
        {
            if (m_client != null)
            {
                string path = $"/Message/GroupMessage";
                if (!await m_client.ExistsAsync(path))
                {
                    var data0 = Encoding.UTF8.GetBytes("");
                    await m_client.CreateRecursiveAsync(path, data0);
                }

                Guid groupGuid = Guid.NewGuid();
                string groupMessagePath = $"/Message/GroupMessage/GM_{groupGuid.ToString()}";

                string outputMsg = $"GM: From:{m_ClientName},SendTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},Msg:{msg}";


                var data = Encoding.UTF8.GetBytes(outputMsg);

                //创建短暂节点
                await m_client.CreateEphemeralAsync(groupMessagePath, data);
            }
        }

        /// <summary>
        /// 发送私有消息
        /// </summary>
        /// <returns></returns> 
        private static async Task SendPrivateMessage(string client, string msg)
        {
            if (m_client != null)
            {
                string path = $"/Message/PrivateMessage/{client}";
                if (!await m_client.ExistsAsync(path))
                {
                    var data0 = Encoding.UTF8.GetBytes("");
                    await m_client.CreateRecursiveAsync(path, data0);
                }

                Guid groupGuid = Guid.NewGuid();
                string privateMessagePath = $"/Message/PrivateMessage/{client}/PM_{groupGuid.ToString()}";

                string outputMsg = $"PM_From:{m_ClientName},SendTime:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},Msg:{msg}";

                var data = Encoding.UTF8.GetBytes(outputMsg);

                //创建短暂节点
                await m_client.CreateEphemeralAsync(privateMessagePath, data);
            }
        }


        #endregion





        /// <summary>
        /// 下线退出
        /// </summary>
        /// <returns></returns>
        private static async Task ExitCommand()
        {
            if (m_client != null)
            {
                string path = $"/Client/{m_ClientName}";
                var data = Encoding.UTF8.GetBytes("OffLine");
                if (await m_client.ExistsAsync(path))
                {
                    await m_client.SetDataAsync(path, data);
                }
                else
                {
                    await m_client.CreateRecursiveAsync(path, data);
                }

                m_client.Dispose();
                m_client = null;

                Console.WriteLine($"{m_ClientName}已经关闭");
            }

        }

    }
}
