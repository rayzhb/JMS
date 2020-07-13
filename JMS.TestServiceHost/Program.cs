﻿using JMS.Common.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;

namespace JMS
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            var gatewaycert = new System.Security.Cryptography.X509Certificates.X509Certificate2("d:/test.pfx", "123456");

            ServiceCollection services = new ServiceCollection();

            var gateways = new NetAddress[] {
               new NetAddress{
                    Address = "localhost",
                    Port = 8911
               }
            };
            var msp = new MicroServiceHost(services);
            if (File.Exists("./appsettings.json") == false)
            {
                //本地没有appsettings.json，先从网关拉一个
                msp.GetGatewayShareFile(gateways[0], "test/appsettings.json", "./appsettings.json", gatewaycert);
            }


            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            var configuration = builder.Build();

           
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole(); // 将日志输出到控制台
            });
            services.AddSingleton<IConfiguration>(configuration);

            msp.MapShareFileToLocal(gateways[0] , "test/appsettings.json", "./appsettings.json",(p,p2)=> {
                Console.WriteLine(p + "回调");
            });
            msp.MapShareFileToLocal(gateways[0], "test/appsettings2.json", "./appsettings2.json");


            msp.Register<Controller1>("Controller1");
            msp.Register<Controller2>("Service2");
            msp.Build(8912, gateways)
                .UseTransactionRecorder(o=> {
                    o.TransactionLogFolder = "./tranlogs";
                })
                .UseSSL(c=> {
                    c.GatewayClientCertificate = gatewaycert;
                    //c.ServerCertificate = c.GatewayClientCertificate;
                })
                .Run();
        }
    }
}
