using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiPlayerLib;
using System.ServiceModel;

namespace MultiPlayerService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost servHost = null;
            try
            {
                //// Address
                //servHost = new ServiceHost(typeof(DrawInfo),
                //new Uri("net.tcp://localhost:12000/MultiPlayerLib/"));
                //// Service contract and binding
                //servHost.AddServiceEndpoint(typeof(IDrawInfo), new NetTcpBinding(), "DrawInfo");

                // Create the service host 
                servHost = new ServiceHost(typeof(DrawInfo));
                // Manage the service’s life cycle
                servHost.Open();
                Console.WriteLine("Service started. Press a key to quit.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.ReadKey();
                if (servHost != null)
                    servHost.Close();
            }
        }
    }
}
