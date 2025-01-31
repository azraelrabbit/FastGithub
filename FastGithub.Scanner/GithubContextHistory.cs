﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace FastGithub.Scanner
{
    /// <summary>
    /// GithubContext的扫描历史
    /// </summary>
    sealed class GithubContextHistory
    {
        /// <summary>
        /// 最多保存最的近的10条记录
        /// </summary>
        private const int MAX_LOG_COUNT = 10;
        private record ScanLog(bool Available, TimeSpan Elapsed);

        private readonly Queue<ScanLog> scanLogs = new();

        /// <summary>
        /// 获取可用率
        /// </summary>
        /// <returns></returns>
        public double AvailableRate
        {
            get
            {
                if (this.scanLogs.Count == 0)
                {
                    return 0d;
                }

                var availableCount = this.scanLogs.Count(item => item.Available);
                return (double)availableCount / this.scanLogs.Count;
            }
        }


        /// <summary>
        /// 获取平均耗时
        /// </summary>
        /// <returns></returns>
        public TimeSpan AvgElapsed
        {
            get
            {
                var availableCount = 0;
                var availableElapsed = TimeSpan.Zero;

                foreach (var item in this.scanLogs)
                {
                    if (item.Available == true)
                    {
                        availableCount += 1;
                        availableElapsed = availableElapsed.Add(item.Elapsed);
                    }
                }

                return availableCount == 0 ? TimeSpan.MaxValue : availableElapsed / availableCount;
            }
        }

        /// <summary>
        /// 添加记录
        /// </summary>
        /// <param name="available">是否可用</param>
        /// <param name="elapsed">扫描耗时</param>
        public void Add(bool available, TimeSpan elapsed)
        {
            this.scanLogs.Enqueue(new ScanLog(available, elapsed));
            while (this.scanLogs.Count > MAX_LOG_COUNT)
            {
                this.scanLogs.Dequeue();
            }
        }
    }
}
