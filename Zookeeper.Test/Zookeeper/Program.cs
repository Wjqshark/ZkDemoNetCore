using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Zookeeper
{
    class Program
    {


        #region privates

        private static bool m_JreOk = false;
        private static bool m_JdkOk = false;

        private static string m_JreVersion = "";
        private static string m_JdkVersion = "";

        private static string runningPath = "";


        private static string jrePath = @"jre.bat";
        private static string jdkPath = @"jdk.bat";


        private static string zkPath = @"apache-zookeeper-3.5.6-bin";
        private static string zkBinPath = @"apache-zookeeper-3.5.6-bin\bin";
        private static string zkConfPath = @"apache-zookeeper-3.5.6-bin\conf";
        private static string zkServerPath = @"apache-zookeeper-3.5.6-bin\bin\zkServer.cmd";

        #endregion



        static async Task Main(string[] args)
        {

            #region 设置路径

            runningPath = AppDomain.CurrentDomain.BaseDirectory;

            jrePath = Path.Combine(runningPath, jrePath);
            jdkPath = Path.Combine(runningPath, jdkPath);

            zkPath = Path.Combine(runningPath, zkPath);
            zkBinPath = Path.Combine(runningPath, zkBinPath);
            zkConfPath = Path.Combine(runningPath, zkConfPath);
            zkServerPath = Path.Combine(runningPath, zkServerPath);


            #endregion

            Console.WriteLine("开始检测运行环境");

            m_JreOk = await CheckJREEnvirment();

            if (m_JreOk)
            {
                Console.WriteLine($"JRE环境Ok，JRE版本{m_JreVersion}");
            }
            else
            {
                Console.WriteLine($"正在静默安装JRE");
                m_JreOk = await SetupJreSliently();
            }

            //m_JdkOk = await CheckJDKEnvirment();

            //if (m_JdkOk)
            //{
            //    Console.WriteLine($"JDK环境Ok，JDK版本{m_JdkVersion}");
            //}
            //else
            //{
            //    Console.WriteLine($"JDK环境不具备，准备静默安装JDK");
            //}

            if (m_JreOk)
            {

                m_JreOk = await CheckJREEnvirment();

                string javaHome = null;
                while (string.IsNullOrEmpty(javaHome))
                {

                    Console.WriteLine($"JAVA_HOME 等待设置中...");
                    javaHome = await GetJavaHome(m_JreVersion);

                    //睡5秒等待Jre全部完成
                    if (string.IsNullOrEmpty(javaHome))
                    {
                        Thread.Sleep(5000);
                    }
                }

                if (!string.IsNullOrEmpty(javaHome))
                {
                    bool setOK = SetSysEnvironmentByName("JAVA_HOME", javaHome);
                    if (setOK)
                    {
                        Console.WriteLine($"已设置 JAVA_HOME : {javaHome}");
                        RunZookeeperServer();
                    }
                }


            }




            Console.ReadLine();
        }


        #region 注册表检测

        /// <summary>
        /// 检测JRE环境
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> CheckJREEnvirment()
        {
            bool flag = false;
            var openSoftwareSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (openSoftwareSubKey != null)
            {
                RegistryKey JavaSoftKey = openSoftwareSubKey.OpenSubKey("JavaSoft");
                if (JavaSoftKey != null)
                {
                    RegistryKey jreKey = JavaSoftKey.OpenSubKey("Java Runtime Environment");

                    if (jreKey != null)
                    {
                        var version = jreKey.GetValue("CurrentVersion");
                        if (version != null)
                        {
                            m_JreVersion = version.ToString();
                            flag = true;
                        }
                    }

                }
            }


            return flag;
        }

        /// <summary>
        /// 检测JDK环境
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> CheckJDKEnvirment()
        {
            bool flag = false;
            var openSoftwareSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (openSoftwareSubKey != null)
            {
                RegistryKey JavaSoftKey = openSoftwareSubKey.OpenSubKey("JavaSoft");
                if (JavaSoftKey != null)
                {
                    RegistryKey jdkKey = JavaSoftKey.OpenSubKey("JDK");

                    if (jdkKey != null)
                    {
                        var version = jdkKey.GetValue("CurrentVersion");
                        if (version != null)
                        {
                            m_JdkVersion = version.ToString();
                            flag = true;
                        }
                    }

                }
            }


            return flag;
        }

        /// <summary>
        /// 获取JavaHOME
        /// </summary>
        /// <returns></returns>
        private static async Task<string> GetJavaHome(string version)
        {
            string javahome = "";
            var openSoftwareSubKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (openSoftwareSubKey != null)
            {
                RegistryKey JavaSoftKey = openSoftwareSubKey.OpenSubKey("JavaSoft");
                if (JavaSoftKey != null)
                {
                    RegistryKey jreKey = JavaSoftKey.OpenSubKey("Java Runtime Environment");

                    if (jreKey != null)
                    {
                        var jreversionKey = jreKey.OpenSubKey(m_JreVersion);
                        if (jreversionKey != null)
                        {

                            var result = jreversionKey.GetValue("JavaHome");

                            if (result != null)
                            {
                                javahome = result.ToString();
                            }
                        }
                    }

                }
            }
            return javahome;
        }

        #endregion

        #region 环境变量

        /// <summary>
        /// 获取系统环境变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string GetSysEnvironmentByName(string name)
        {
            string result = string.Empty;
            try
            {
                result = System.Environment.GetEnvironmentVariable(name);
            }
            catch (Exception ee)
            {
                Console.WriteLine($"Error:GetSysEnvironmentByName : {ee.ToString()}");
                return null;
            }
            return result;
        }


        /// <summary>
        /// 设置系统环境变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool SetSysEnvironmentByName(string key, string value)
        {
            bool result = false;
            try
            {
                System.Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Machine);
                result = true;
            }
            catch (Exception ee)
            {
                Console.WriteLine($"Error:SetSysEnvironmentByName : {ee.ToString()}");
            }

            return result;
        }



        #endregion


        #region 静默安装

        private static async Task<bool> SetupJreSliently()
        {
            bool result = false;
            try
            {
                Console.WriteLine("安装JRE中...");
                using (Process p = new Process())
                {
                    ProcessStartInfo pStartInfo = new ProcessStartInfo();
                    pStartInfo.FileName = jrePath;
                    pStartInfo.Verb = "runas";
                    pStartInfo.Arguments = "";
                    pStartInfo.UseShellExecute = false;
                    p.StartInfo = pStartInfo;
                    p.Start();
                    p.WaitForExit();
                    p.Close();
                    Console.WriteLine("JRE安装完成");
                    result = true;
                }

            }
            catch (Exception ee)
            {
                Console.WriteLine($"Error:SetupJreSliently : {ee.ToString()}");
            }

            return result;
        }

        #endregion


        private static void RunZookeeperServer()
        {
            Console.WriteLine("正在启动ZookeeperServer...");
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = zkServerPath;
            process.StartInfo.Verb = "runas";
            process.StartInfo.Arguments = "";
            process.StartInfo.UseShellExecute = false;
            process.Start();
        }







    }
}
