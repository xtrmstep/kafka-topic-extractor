﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Kafker.Csv
{
    public interface ICsvFileReader : IDisposable
    {
        Task<JObject> ReadLineAsync(CancellationToken cancellationToken);
    }
}