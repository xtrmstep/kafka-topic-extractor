﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Kafker.Commands;
using Kafker.Configurations;
using Kafker.Helpers;
using Kafker.Kafka;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly:InternalsVisibleTo("Kafker.Tests")]

namespace Kafker
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var configuration = CreateConfiguration(environment);

            var kafkerSettings = configuration.GetSection(nameof(KafkerSettings)).Get<KafkerSettings>();

            var services = new ServiceCollection()
                .AddSingleton<IConsumerFactory, ConsumerFactory>()
                .AddSingleton<IProducerFactory, ProducerFactory>()
                .AddSingleton<IFileTagProvider, FileTagProvider>()
                .AddSingleton<IExtractCommand, ExtractCommand>()
                .AddSingleton<ICreateCommand, CreateCommand>()
                .AddSingleton<IConvertCommand, ConvertCommand>()
                .AddSingleton<IListCommand, ListCommand>()
                .AddSingleton<IEmitCommand, EmitCommand>()
                .AddSingleton(PhysicalConsole.Singleton)
                .AddSingleton(kafkerSettings)
                .BuildServiceProvider();

            using var app = new CommandLineApplication
            {
                Name = "kafker",
                Description = "CLI to extract Kafka topic with JSON events to CSV file"
            };
            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);

            app.Command("extract", p =>
            {
                p.Description = "Extract a topic to CSV file using existing configuration";
                
                var topicArg = p.Option("-t|--topic <TOPIC>", "File name with topic configuration", CommandOptionType.SingleValue).IsRequired();
                var mapArg = p.Option("-m|--map <MAP>", "File name of a file with mapping configuration", CommandOptionType.SingleValue);

                p.OnExecuteAsync(async cancellationToken =>
                {
                    var extractCommand = services.GetService<IExtractCommand>();
                    return await extractCommand.InvokeAsync(cancellationToken, topicArg.Value(), mapArg.Value());
                });
            });

            app.Command("create", p =>
            {
                p.Description = "Create template CFG and MAP files";
                
                var nameArg = p.Option("-t|--topic <TOPIC>", "Template name", CommandOptionType.SingleValue);

                p.OnExecuteAsync(async cancellationToken =>
                {
                    var createTemplateCommand = services.GetService<ICreateCommand>();
                    return await createTemplateCommand.InvokeAsync(cancellationToken, nameArg.Value());
                });
            });
            
            app.Command("list", p =>
            {
                p.Description = "List existing configurations";
                p.OnExecuteAsync(async cancellationToken =>
                {
                    var listCommand = services.GetService<IListCommand>();
                    return await listCommand.InvokeAsync();
                });
            });
            
            app.Command("emit", p =>
            {
                p.Description = "Emit events to a topic using existing configuration";
                
                var topicArg = p.Option("-t|--topic <TOPIC>", "Topic name to which events should be emitted", CommandOptionType.SingleValue).IsRequired();
                var fileName = p.Argument("file", "CSV file name with events").IsRequired();

                p.OnExecuteAsync(async cancellationToken =>
                {
                    var emitCommand = services.GetService<IEmitCommand>();
                    return await emitCommand.InvokeAsync(cancellationToken, topicArg.Value(), fileName.Value);
                });
            });

            app.Command("convert", p =>
            {
                p.Description = "Convert json snapshot to a datatable";
                
                var topicArg = p.Option("-t|--topic <TOPIC>", "File name with topic configuration", CommandOptionType.SingleValue).IsRequired();
                var fileName = p.Argument("file", "CSV file name with events").IsRequired();    
                    
                p.OnExecuteAsync(async cancellationToken =>
                {
                    var convertCommand = services.GetService<IConvertCommand>();
                    return await convertCommand.InvokeAsync(cancellationToken, fileName.Value,topicArg.Value());
                });
            });
            
            app.OnExecuteAsync(async cancellationToken =>
            {
                await PhysicalConsole.Singleton.Error.WriteLineAsync("Specify a command");
                app.ShowHelp();
                return await Task.FromResult(0).ConfigureAwait(false);
            });

            return await app.ExecuteAsync(args).ConfigureAwait(false);
        }

        public static IConfigurationRoot CreateConfiguration(string environment)
        {
            environment ??= "Development";
            var env = environment.ToLowerInvariant();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true);
            var configuration = builder.Build();
            return configuration;
        }
    }
}