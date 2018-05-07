using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Net.NetworkInformation;
using System.Timers;
using System.Security.Permissions;
using System.Linq;

namespace Open_Internet_Watchdog {
    class Program {

        //For hiding console window
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        //file names
        static string path_for_files = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Open Internet Watchdog\\";
        static string config_file = path_for_files + "config.xml";
        static string connections_file = path_for_files + "connections.csv";
        static string log_file = path_for_files + "log.txt";
        static string offline_log = path_for_files + "offline_log.txt";

        static bool online = true;
        static DateTime offline_since;

        static Timer aTimer;
        static Timer offlineTimer;
        static string[][] connections;
        static string[][] connections_always;

        static void Main(string[] args) {
            bool debug, speedtest;
            int connection_time;
            //for hiding console
            var handle = GetConsoleWindow();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(@"   ____                     _____       _                       _    __          __   _       _         _             ");
            Console.WriteLine(@"  / __ \                   |_   _|     | |                     | |   \ \        / /  | |     | |       | |            ");
            Console.WriteLine(@" | |  | |_ __   ___ _ __     | |  _ __ | |_ ___ _ __ _ __   ___| |_   \ \  /\  / /_ _| |_ ___| |__   __| | ___   __ _ ");
            Console.WriteLine(@" | |  | | '_ \ / _ \ '_ \    | | | '_ \| __/ _ \ '__| '_ \ / _ \ __|   \ \/  \/ / _` | __/ __| '_ \ / _` |/ _ \ / _` |");
            Console.WriteLine(@" | |__| | |_) |  __/ | | |  _| |_| | | | ||  __/ |  | | | |  __/ |_     \  /\  / (_| | || (__| | | | (_| | (_) | (_| |");
            Console.WriteLine(@"  \____/| .__/ \___|_| |_| |_____|_| |_|\__\___|_|  |_| |_|\___|\__|     \/  \/ \__,_|\__\___|_| |_|\__,_|\___/ \__, |");
            Console.WriteLine(@"        | |                                                                                                      __/ |");
            Console.WriteLine(@"        |_|                                                                                                     |___/ ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            if(!Directory.Exists(path_for_files)) {
                create_folder(path_for_files);
                }
            if(!File.Exists(config_file)) {
                debug = true;
                write_config();
                }
            else {
                debug = bool.Parse(read_config("/settings/show_console"));
                }
            int sw;
            sw = (debug) ? 5 : 0; // 5=show, 0=hide
            ShowWindow(handle, sw); // hides the window, when debug is false

            connection_time = int.Parse(read_config("/settings/check_time")) * 1000;

            //reading the connections from the file
            using(var sr = new StreamReader(connections_file)) {
                List<string> ip = new List<string>();
                List<string> domain = new List<string>();
                List<string> priority = new List<string>();
                int always_count = 0;
                int count = 0;
                while(!sr.EndOfStream) {
                    var line = sr.ReadLine();
                    var values = line.Split(';');
                    domain.Add(values[0]);
                    ip.Add(values[1]);
                    priority.Add(values[2]);
                    if(bool.Parse(values[2])) {
                        always_count++;
                        }
                    else {
                        count++;
                        }
                    }
                //TODO: fix it!
                connections = new string[count][];
                connections_always = new string[always_count][];
                for(int i = 0; i < ip.Count; i++) {
                    if(bool.Parse(connections[i][2])) {
                        connections_always[i] = new string[] { domain[i], ip[i], priority[i] };
                        }
                    else {
                        connections[i] = new string[] { domain[i], ip[i], priority[i] };
                        }
                    }
                }

            write_console_message(2, "Programm started");
            SetTimer(connection_time);
            write_console_message(2, "Pressing enter hides the console (programm still runs)");



            Console.WriteLine("test");
            Console.ReadKey();
            if(create_folder(path_for_files)) {
                Console.WriteLine("true");
                }
            else {
                Console.WriteLine("false");
                }
            File.AppendAllText(config_file, "Test");
            Console.ReadKey();
            ShowWindow(handle, 0);
            Console.ReadKey();
            }

        public static void write_conn_log(bool online) {
            if(online) {
                TimeSpan temp = DateTime.Now - offline_since;
                File.AppendAllText(offline_log, "[Online] Went online:  " + DateTime.Now + "\n");
                write_log("[Online] Went online:  " + DateTime.Now);
                int minutes = (temp.Days * 24 * 60) + (temp.Hours * 60) + (temp.Minutes);
                File.AppendAllText(offline_log, "[Online] You was offline for " + minutes + "min " + temp.Seconds + "sec" + "\n");
                write_log("[Time] You was offline for " + temp + "min");
                offline_since = DateTime.MinValue;
                offlineTimer.Stop();
                offlineTimer.Dispose();
                }
            else {
                File.AppendAllText(offline_log, "[Offline] Went offline:  " + DateTime.Now + "\n");
                write_log("[Offline] Went offline:  " + DateTime.Now);
                offline_since = DateTime.Now;
                int time;
                XmlDocument doc = new XmlDocument();
                doc.Load(config_file);
                XmlNode node = doc.DocumentElement.SelectSingleNode("/settings/offline_check_time");
                time = int.Parse(node.InnerText) * 1000;
                SetTimer_offline(time);
                }
            }

        public static void write_log(string message) {
            File.AppendAllText(log_file, DateTime.Now + " - " + message + "\n");
            }

        public static void SetTimer(int timer) {
            aTimer = new Timer(timer);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            timed_event();
            }
        public static void SetTimer_offline(int timer) {
            offlineTimer = new Timer(timer);
            offlineTimer.Elapsed += OnTimedEvent_offline;
            offlineTimer.AutoReset = true;
            offlineTimer.Enabled = true;
            }
        private static void OnTimedEvent_offline(Object source, ElapsedEventArgs e) {
            timed_event();
            }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e) {
            timed_event();
            }

        public static void timed_event() {

            bool _online = false;
            for(int i = 0; i < connections_always.Length; i++) {
                if(connection_test(connections_always[i][0], connections_always[i][1])) {
                    _online = true;
                    }
                }
            if(!_online) {
                Random rnd = new Random();
                string[][] random_connections = connections.OrderBy(x => rnd.Next()).ToArray();
                for(int i = 0; i < random_connections.Length; i++) {
                    if(connection_test(random_connections[i][0], random_connections[i][1])) {
                        _online = true;
                        break;
                        }
                    }
                }
            if(!_online && online) {
                write_conn_log(false);
                online = _online;
                write_console_message(4, "You have no connection to the Internet!");
                }
            else if(_online && !online) {
                online = _online;
                write_conn_log(true);
                }
            }


        public static bool connection_test(string domain, string ip) {
            string check_data;
            bool domain_check;
            if(domain == "") {
                check_data = ip;
                domain_check = false;
                }
            else {
                check_data = domain;
                domain_check = true;
                }

            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = true;
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            try {
                PingReply reply = pingSender.Send(check_data, timeout, buffer, options);
                if(reply.Status == IPStatus.Success) {
                    if(domain_check) {
                        if(reply.Address.ToString() == ip) {
                            string temp = "Checked Domain: " + domain;
                            write_console_message(3, temp);
                            temp = "Points to: " + reply.Address.ToString() + " Should point to: " + ip;
                            write_console_message(3, temp);
                            return true;
                            }
                        else {
                            string temp = "Checked Domain: " + domain;
                            write_console_message(4, temp);
                            temp = "Points to: " + reply.Address.ToString() + " Should point to: " + ip;
                            write_console_message(4, temp);
                            return false;
                            }
                        }
                    string temp2 = "Checked IP: " + reply.Address.ToString() + " Should be: " + ip;
                    write_console_message(3, temp2);
                    return true;
                    }
                else {
                    string temp2 = "Checked IP: " + reply.Address.ToString() + " Should be: " + ip;
                    write_console_message(4, temp2);
                    return false;
                    }
                }
            catch(Exception) {
                write_console_message(1, "Ping failed. Maybe the enterd domain is Wrong or IP");
                string temp = "Domain: " + domain + " IP: " + ip;
                write_console_message(1, temp);
                return false;
                throw;
                }
            }
        /// <summary>
        /// Writes a message into the console and into the log file
        /// </summary>
        /// <param name="type">1=Error, 2=Info, 3=Online, 4=Offline</param>
        /// <param name="message"></param>
        public static void write_console_message(int type, string message) {
            switch(type) {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[ERROR]  ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(message + "\n");
                    write_log("[ERROR]  " + message);
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("[Info]  ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(message + "\n");
                    write_log("[Info]  " + message);
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write("[Online]  ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(message + "\n");
                    write_log("[Online]  " + message);
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.Write("[Offline]  ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(message + "\n");
                    write_log("[Offline]  " + message);
                    break;
                }
            }

        /// <summary>
        /// Asks the question and waits for a yes/no awnser, to convert this into a boolean
        /// </summary>
        /// <param name="message">The message that get shown in the console</param>
        /// <returns>bool matching the question</returns>
        public static bool bool_check(string message) {
            Console.WriteLine(message);
            string s = Console.ReadLine();
            if(s == "n" || s == "no") {
                return false;
                }
            else if(s == "y" || s == "yes") {
                return true;
                }
            else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Wrong input!");
                Console.ForegroundColor = ConsoleColor.White;
                return bool_check(message);
                }
            }

        /// <summary>
        /// Ask the question and waits for an integer
        /// </summary>
        /// <param name="message">The message that the user get shown</param>
        /// <returns>returns a int</returns>
        public static int int_check(string message) {
            Console.WriteLine(message);
            string s = Console.ReadLine();
            int result;
            if(!int.TryParse(s, out result)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Wrong input!");
                Console.ForegroundColor = ConsoleColor.White;
                return int_check(message);
                }
            return result;
            }

        public static List<List<string>> request_ip() {
            bool run = true;
            List<List<string>> results = new List<List<string>>();
            int i = 0;
            while(run) {
                Console.WriteLine("\nEnter a Domain (optional. Like this (without http://): example.org)");
                string domain = Console.ReadLine();
                string ip;
                if(domain != "") {
                    Console.WriteLine("\nEnter a IP for this Domain (like this: 0.0.0.0 or 1234:5678:9012:3456::2003):");
                    ip = Console.ReadLine();
                    }
                else {
                    Console.WriteLine("\nEnter a IP (like this: 0.0.0.0 or 1234:5678:9012:3456::2003):");
                    ip = Console.ReadLine();
                    }
                bool priority = bool_check("\nShould this IP/Domian always get checked?");
                if(bool_check("\nIs this right (y/n)?\nDomain: " + domain + "\nIP: " + ip)) {
                    results.Add(new List<string>());
                    results[i].Add(domain);
                    results[i].Add(ip);
                    results[i].Add(priority.ToString());
                    if(bool_check("Do you want to add more? (y/n)")) {
                        i++;
                        }
                    else {
                        run = false;
                        }
                    }
                }
            return results;
            }

        public static bool create_folder(string path) {
            FileIOPermission f = new FileIOPermission(FileIOPermissionAccess.Read, path);
            f.AddPathList(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, path);
            try {
                if(!Directory.Exists(path)) {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    }
                f.Demand();
                return true;
                }
            catch(Exception) {
                return false;
                throw;
                }
            }

        public static string read_config(string config_name) {
            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(config_file);
                XmlNode node = doc.DocumentElement.SelectSingleNode(config_name);
                return node.InnerText;
                }
            catch(Exception) {
                write_console_message(1, "Program paused");
                write_console_message(1, "The config file is not quite right. Please check the config file or delete it to create a new one");
                if(bool_check("\nDo you want to create a new config file? (y/n)")) {
                    write_config();
                    }
                else {
                    write_console_message(2, "Program closed");
                    Environment.Exit(0);
                    }
                throw;
                }
            }

        public static void write_config() {
            bool speedtest, show_console, write_log;
            List<List<string>> ip_check;
            int speedtest_time, check_time, offline_time;
            show_console = bool_check("\nShow Console on startup? (y/n)");
            write_log = bool_check("\nShould only Errors get written in Log file? (y/n)");
            speedtest = bool_check("\nDo you want to use speedtest? (y/n)");
            if(!speedtest) {
                speedtest_time = 0;
                }
            else {
                speedtest_time = int_check("\nHow often do you want to run a Speedtest? (in whole seconds)\nNote: Don't pick a to short time. Recommended: 3600 seconds (1 Hour)");
                }
            check_time = int_check("\nHow often do you want to test if you connected to the internet? (in whole seconds)\nRecommended:300 seconds (5 Minutes)");
            offline_time = int_check("\nHow often do you want to test if you reconnected to the internet? (in whole seconds)\nRecommended:60 secound");
            Console.WriteLine("\nYou can use a Domain or an IP Address to check if you're connected to the internet. The Domain is optional, but when you enter one, you also need to enter a Matching IP Adress. The IP Adress is needed to check if the DNS gave you the right IP. Some Routers might point to themselfs, if no internet connection is present. You also can enter more IP Adresses, if one might be offline");
            ip_check = request_ip();

            new XDocument(
                new XElement("settings",
                    new XElement("show_console", show_console),
                    new XElement("write_only_errors", write_log),
                    new XElement("speedtest", speedtest),
                    new XElement("speed_time", speedtest_time),
                    new XElement("check_time", check_time),
                    new XElement("offline_check_time", offline_time)
                )
            )
            .Save(config_file);
            using(StreamWriter sw =
             new StreamWriter(connections_file)) {
                for(int i = 0; i < ip_check.Count; i++) {
                    sw.WriteLine(ip_check[i][0] + ";" + ip_check[i][1] + ";" + ip_check[i][2]);
                    }
                }

            }

        }
    }
