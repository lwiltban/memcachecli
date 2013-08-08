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
        private static Logger log = LogManager.GetCurrentClassLogger();

        private string ServerName { get; set; }
        public string Operation { get; set; }
        public string Pattern { get; set; }

        static void Main(string[] args)
        {
            CLI cli = null;
            if (args.Length <= 1)
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
                log.Error("Invalid arguments");
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
            switch (Operation.ToLower())
            {
                case "delete":
                    rc = DoDelete();
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
            Console.WriteLine("MemcacheCLI <memcacheserver> <operation> <pattern>"); 
            Console.WriteLine("Operation is currently on delete, pattern is the prefix of the cache keys to delete");
            System.Environment.Exit(1);
        }



        private int DoDelete()
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
                log.Error("DoDelete", ex);
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
                            if (IsDeleteable(item))
                            {
                                log.Info(string.Format("Deleteable item {0}", item.Name));
                                current.Delete(item.Name);
                                log.Info(string.Format("Deleted item {0}", item.Name));
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

        private bool IsDeleteable(Item item)
        {
            bool rc = true;
            try
            {                
                Match match = Regex.Match(item.Name, Pattern);
                rc = match.Success;
            }
            catch (Exception ex)
            {
                log.Error("IsDeleteable", ex);
                rc = false;
            }
            return rc;
        }
    }
}
