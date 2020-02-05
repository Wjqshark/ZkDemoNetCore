using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;
using Rabbit.Zookeeper;
using Rabbit.Zookeeper.Implementation;

namespace Zookeeper.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IZookeeperClient client = new ZookeeperClient(new ZookeeperClientOptions("192.168.1.14:2181")
            {
                BasePath = "/", //default value
                ConnectionTimeout = TimeSpan.FromSeconds(10), //default value
                SessionTimeout = TimeSpan.FromSeconds(20), //default value
                OperatingTimeout = TimeSpan.FromSeconds(60), //default value
                ReadOnly = false, //default value
                SessionId = 0, //default value
                SessionPasswd = null, //default value
                EnableEphemeralNodeRestore = true //default value
            });


            var data = Encoding.UTF8.GetBytes("2020");

            if (!await client.ExistsAsync("/year"))
            {
                //Fast create temporary nodes
                await client.CreateEphemeralAsync("/year", data);
                Console.WriteLine("写入/year 2020");
            }

            if (await client.ExistsAsync("/year"))
            {
                IEnumerable<byte> data1 = await client.GetDataAsync("/year");
                string str1 = Encoding.UTF8.GetString(data1.ToArray());
                Console.WriteLine($"获取/year 值为 {str1}");
            }

            Console.ReadLine();


        }
    }
}
