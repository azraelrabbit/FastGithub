﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Scanner.LookupProviders
{
    /// <summary>
    /// ipaddress.com的域名与ip关系提供者
    /// </summary>
    [Service(ServiceLifetime.Singleton, ServiceType = typeof(IGithubLookupProvider))]
    sealed class IPAddressComProvider : IGithubLookupProvider
    {
        private readonly IOptionsMonitor<IPAddressComProviderOptions> options;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<IPAddressComProvider> logger;
        private readonly Uri lookupUri = new("https://www.ipaddress.com/ip-lookup");

        /// <summary>
        /// 获取排序
        /// </summary>
        public int Order => default;

        /// <summary>
        /// ipaddress.com的域名与ip关系提供者
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public IPAddressComProvider(
            IOptionsMonitor<IPAddressComProviderOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<IPAddressComProvider> logger)
        {
            this.options = options;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        /// <summary>
        /// 查找域名与ip关系
        /// </summary>
        /// <param name="domains"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DomainAddress>> LookupAsync(IEnumerable<string> domains, CancellationToken cancellationToken)
        {
            var setting = this.options.CurrentValue;
            if (setting.Enable == false)
            {
                return Enumerable.Empty<DomainAddress>();
            }

            var httpClient = this.httpClientFactory.CreateClient(nameof(FastGithub));
            var result = new HashSet<DomainAddress>();
            foreach (var domain in domains)
            {
                try
                {
                    var addresses = await this.LookupAsync(httpClient, domain, cancellationToken);
                    foreach (var address in addresses)
                    {
                        result.Add(new DomainAddress(domain, address));
                    }
                }
                catch (Exception)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    this.logger.LogWarning($"ipaddress.com无法解析{domain}");
                }
            }
            return result;
        }

        /// <summary>
        /// 反查ip
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private async Task<List<IPAddress>> LookupAsync(HttpClient httpClient, string domain, CancellationToken cancellationToken)
        {
            var keyValue = new KeyValuePair<string?, string?>("host", domain);
            var content = new FormUrlEncodedContent(Enumerable.Repeat(keyValue, 1));
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = lookupUri,
                Content = content
            };

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var match = Regex.Match(html, @"(?<=<h1>IP Lookup : )\d+\.\d+\.\d+\.\d+", RegexOptions.IgnoreCase);

            if (match.Success && IPAddress.TryParse(match.Value, out var address))
            {
                return new List<IPAddress> { address };
            }

            var prefix = Regex.Escape("type=\"radio\" value=\"");
            var matches = Regex.Matches(html, @$"(?<={prefix})\d+\.\d+\.\d+\.\d+", RegexOptions.IgnoreCase);
            var addressList = new List<IPAddress>();
            foreach (Match item in matches)
            {
                if (IPAddress.TryParse(item.Value, out address))
                {
                    addressList.Add(address);
                }
            }
            return addressList;
        }
    }
}
