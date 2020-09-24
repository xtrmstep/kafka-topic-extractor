﻿using System.Threading;
using System.Threading.Tasks;
using Kafker.Configurations;
using Kafker.Helpers;
using Kafker.Kafka;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Options;

namespace Kafker.Commands
{
    public class EmitCommand : IEmitCommand
    {
        private readonly IConsole _console;
        private readonly KafkerSettings _settings;
        private readonly IProducerFactory _producerFactory;

        public EmitCommand(IConsole console, KafkerSettings settings, IProducerFactory producerFactory)
        {
            _console = console;
            _settings = settings;
            _producerFactory = producerFactory;
        }

        public async Task<int> InvokeAsync(CancellationToken cancellationToken, string topic, string fileName)
        {
            var cfg = await ExtractorHelper.ReadConfigurationAsync(topic, _settings, _console);
            
            var producedEvents = 0;
            using var topicProducer = _producerFactory.Create(cfg);
            try
            {
                var recordsBuffer = new RecordsBuffer();
                await recordsBuffer.LoadFromFileAsync(fileName);
                var records = recordsBuffer.GetRecords();
                foreach (var record in records)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    await topicProducer.ProduceAsync(record);
                    producedEvents++;
                }
            }
            finally
            {
                await _console.Out.WriteLineAsync($"Produced {producedEvents} events"); 
            }

            return await Task.FromResult(0).ConfigureAwait(false); // ok
        }
    }
}