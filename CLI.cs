using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MemcacheCLI.Models;
using NLog;

namespace MemcacheCLI
{
    class CLI
    {
        enum OpEnum
        {
            noop = 0,
            delete = 1,
            list = 2
        }

        private static Logger log = LogManager.GetCurrentClassLogger();

        private string ServerName { get; set; }
        private string Operation { get; set; }
        private OpEnum Op { get; set; } 
        private string Pattern { get; set; }

        static void Main(string[] args)
        {
            CLI cli = null;
            if (args.Length < 1)
            {
                cli = new CLI();
            }
            else
            {
                cli = new CLI(args);
            }
            int rc = cli.DoWork();

            System.Environment.Exit(rc);
        }

        public CLI(string[] args)
        {
            if (args.Length >= 3)
            {
                ServerName = args[0];
                Operation = args[1];
                Pattern = args[2];
            }
            else
            {
                Console.WriteLine(string.Format("Invalid arguments \"{0}\"", String.Join(",", args.Select(s => s))));
                log.Error(string.Format("Invalid arguments \"{0}\"", String.Join(",", args.Select(s => s))));
                Usage();

            }
        }

        public CLI()
        {
            ServerName = ConfigurationManager.AppSettings["MemcacheServer"];
            Operation = ConfigurationManager.AppSettings["Operation"];
            Pattern = ConfigurationManager.AppSettings["Pattern"];
        }

        public int DoWork()
        {
            log.Info("MemcacheCLI DoWork begin");
            log.Trace(string.Format("Args: {0} {1} {2}", ServerName, Operation, Pattern));

            int rc = 0;
            Op = OpEnum.noop;

            switch (Operation.ToLower())
            {
                case "delete":
                    Op = OpEnum.delete;
                    rc = DoServer();
                    break;

                case "list":
                    Op = OpEnum.list;
                    //Console.WriteLine(string.Format("{0} Items:", ServerName));
                    rc = DoServer();
                    break;

                default:
                    log.Error(string.Format("Unimplemented operation {0}", Operation));
                    rc = -1;
                    break;

            }
            log.Info(string.Format("MemcacheCLI end {0}", rc));

            return rc;
        }

        public void Usage()
        {
            Console.WriteLine("MemcacheCLI usage");
            Console.WriteLine("MemcacheCLI <memcacheserver:port> <operation> <pattern>"); 
            Console.WriteLine("Operation is currently delete or list, pattern is the regular expression of the cache keys to see if valid");
            System.Environment.Exit(1);
        }



        private int DoServer()
        {
            int rc = 0;
            try
            {
                string[] address = ServerName.Split(':');
                var name = string.Format("{0}", address[0]);
                var serverObj = new Server { ServerID = 0, Name = name, IPAddress = name, Port = Int32.Parse(address[1]) };

                return Server(serverObj);

            }
            catch (Exception ex)
            {
                log.Error("DoServer", ex);
                rc = 1;
            }

            return rc;
        }

        private int Server(Server current)
        {
            int rc = 0;
            try
            {
                if (current != null && current.getSlabs())
                {
                    return Slabs(current);
                }
            }
            catch (Exception ex)
            {
                log.Error("Server", ex);
                rc = 2;
            }
            return rc;
        }

        private int Slabs(Server current)
        {
            int rc = 0;
            try
            {
                if (current != null && current.Slabs.Values != null)
                {
                    foreach (Slab slab in current.Slabs.Values)
                    {
                        if (slab.getItems())
                        {
                            rc |= Items(current, slab);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Slabs", ex);
                rc = 3;
            }
            return rc;
        }

        private int Items(Server current, Slab slab)
        {
            int rc = 0;
            try
            {
                if (slab != null)
                {
                    Dictionary<string, Item> items = slab.Items;
                    if (items != null)
                    {
                        foreach (Item item in items.Values)
                        {
                            if (IsValid(item))
                            {
                                log.Trace(string.Format("Found valid item {0}", item.Name));
                                DoOperation(current, item);                                
                            }
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Items", ex);
                rc = 4;
            }
            return rc;
        }

        private bool IsValid(Item item)
        {
            bool rc = true;
            try
            {
                Match match = Regex.Match(item.Name, Pattern);
                rc = match.Success;
            }
            catch (Exception ex)
            {
                log.Error("IsValid", ex);
                rc = false;
            }
            return rc;
        }

        private bool DoOperation(Server current, Item item)
        {
            bool rc = true;
            try
            {
                switch (Op)
                {
                    case OpEnum.delete:
                        rc = current.Delete(item.Name);
                        log.Info(string.Format("Deleted item {0}", item.Name));
                        break;

                    case OpEnum.list:
                        Console.WriteLine(item.Name);
                        break;

                    default:
                        log.Error(string.Format("Invalid operation {0}", Op));
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error("DoOperation", ex);
            }
            return rc;
        }
    }
}
