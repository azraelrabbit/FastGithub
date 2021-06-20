using FastGithub.Scanner.ScanMiddlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner
{
    /// <summary>
    /// github扫描服务
    /// </summary>
    [Service(ServiceLifetime.Singleton)]
    sealed class GithubScanService
    {
        private readonly GithubLookupFacotry lookupFactory;
        private readonly GithubContextCollection scanResults;
        private readonly ILogger<GithubScanService> logger;

        private readonly InvokeDelegate<GithubContext> fullScanDelegate;
        private readonly InvokeDelegate<GithubContext> resultScanDelegate;


        private  List<AvaDomain> _avaliableGithubs=   new List<AvaDomain>();

        /// <summary>
        /// github扫描服务
        /// </summary>
        /// <param name="lookupFactory"></param>
        /// <param name="scanResults"></param>
        /// <param name="appService"></param>
        /// <param name="logger"></param>
        public GithubScanService(
            GithubLookupFacotry lookupFactory,
            GithubContextCollection scanResults,
            IServiceProvider appService,
            ILogger<GithubScanService> logger)
        {
            this.lookupFactory = lookupFactory;
            this.scanResults = scanResults;
            this.logger = logger;

            this.fullScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<ConcurrentMiddleware>()
                .Use<StatisticsMiddleware>()
                .Use<TcpScanMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();

            this.resultScanDelegate = new PipelineBuilder<GithubContext>(appService, ctx => Task.CompletedTask)
                .Use<StatisticsMiddleware>()
                .Use<HttpsScanMiddleware>()
                .Build();
        }

        /// <summary>
        /// 扫描所有的ip
        /// </summary>
        /// <returns></returns>
        public async Task ScanAllAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("完整扫描开始..");
            var domainAddresses = await this.lookupFactory.LookupAsync(cancellationToken);

            var scanTasks = domainAddresses
                .Select(item => new GithubContext(item.Domain, item.Address, cancellationToken))
                .Select(ctx => ScanAsync(ctx));

            var results = await Task.WhenAll(scanTasks);
            var successCount = results.Count(item => item);
            this.logger.LogInformation($"完整扫描结束，成功{successCount}条共{results.Length}条");


            async Task<bool> ScanAsync(GithubContext context)
            {
                await this.fullScanDelegate(context);
                if (context.Available == true)
                {
                    this.scanResults.Add(context);
                }
                return context.Available;
            }
        }

        /// <summary>
        /// 扫描曾经扫描到的结果
        /// </summary>
        /// <returns></returns>
        public async Task ScanResultAsync()
        {
            this.logger.LogInformation("结果扫描开始..");

            var results = this.scanResults.ToArray();
            var contexts = results
                .OrderBy(item => item.Domain)
                .ThenByDescending(item => item.History.AvailableRate)
                .ThenBy(item => item.History.AvgElapsed);

            foreach (var context in contexts)
            {
                await this.resultScanDelegate(context);
            }

            //var avaliables=contexts.Where(p => p.Available).ToList();

            //avaliables.Where(p=>p.)


          

            var avaliables = contexts.Where(item => item.History.AvailableRate > 0d);
                //.OrderByDescending(item => item.History.AvailableRate)
                //.ThenBy(item => item.History.AvgElapsed);

            var domains = avaliables.Select(p => p.Domain).Distinct().ToList();

            foreach (var domain in domains)
            {
                var dl=avaliables.Where(p => p.Domain == domain).ToList() .OrderByDescending(item => item.History.AvailableRate)
                    .ThenBy(item => item.History.AvgElapsed);

                var da = dl.FirstOrDefault();

                if (da != null)
                {
                    if (_avaliableGithubs.Exists(p => p.Domain == da.Domain))
                    {
                        var ei = _avaliableGithubs.FirstOrDefault(p => p.Domain == da.Domain);
                        ei.IpAddress = da.Address;
                    }
                    else
                    {
                        _avaliableGithubs.Add(new AvaDomain()
                        {
                            Domain = da.Domain,
                            IpAddress = da.Address
                        });
                    }
                }
            }

            //show up domain hosts
            var sb = new StringBuilder();
            sb.AppendLine("=========the github hosts==========");

            foreach (var ag in _avaliableGithubs)
            {
                sb.AppendLine($"{ag.IpAddress.ToString()}  {ag.Domain}");
            }

            sb.AppendLine("=========the github hosts end======");

            this.logger.LogInformation(sb.ToString());

            this.logger.LogInformation($"结果扫描结束，共扫描{results.Length}条记录");
        }
    }

    internal class AvaDomain
    {
        public string Domain { get; set; }

        public IPAddress IpAddress { get; set; }
    }
}
