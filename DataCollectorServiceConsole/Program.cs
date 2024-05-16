using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace DataCollectorServiceConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var service = new DataCollectorService.Service();
            ServiceBase[] servicesToRun = new ServiceBase[] { service };

            if (Environment.UserInteractive)
            {
                Console.CancelKeyPress += (x, y) => service.Stop();
                service.Start();

                Console.WriteLine("Служба запущена. Нажмите любую кнопку для ее остановки");

                Console.ReadKey();
                service.Stop();
                Console.WriteLine("Служба остановлена");
            }
            else
            {
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
