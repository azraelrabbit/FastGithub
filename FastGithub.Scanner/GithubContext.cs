﻿using System;
using System.Net;
using System.Threading;

namespace FastGithub.Scanner
{
    /// <summary>
    /// Github扫描上下文
    /// </summary>
    sealed class GithubContext : DomainAddress, IEquatable<GithubContext>
    {
        /// <summary>
        /// 获取或设置是否可用
        /// </summary>
        public bool Available { get; set; }

        /// <summary>
        /// 设置取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// 获取扫描历史信息
        /// </summary>
        public GithubContextHistory History { get; } = new();


        /// <summary>
        /// Github扫描上下文
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        public GithubContext(string domain, IPAddress address)
            : this(domain, address, CancellationToken.None)
        {
        }

        /// <summary>
        /// Github扫描上下文
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="address"></param>
        /// <param name="cancellationToken"></param>
        public GithubContext(string domain, IPAddress address, CancellationToken cancellationToken)
            : base(domain, address)
        {
            this.CancellationToken = cancellationToken;
        }

        public bool Equals(GithubContext? other)
        {
            return base.Equals(other);
        }

        public override string ToString()
        {
            return $"{this.Domain} {{Address={this.Address}, AvailableRate={this.History.AvailableRate}, AvgElapsed={this.History.AvgElapsed}}}";
        }
    }
}
