﻿using Microsoft.WindowsAzure.Storage;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Votus.Core.Domain;
using Votus.Core.Infrastructure.Azure.Caching;
using Votus.Core.Infrastructure.Azure.ServiceBus;
using Votus.Core.Infrastructure.Azure.Storage;
using Votus.Core.Infrastructure.Caching;
using Votus.Core.Infrastructure.Configuration;
using Votus.Core.Infrastructure.Data;
using Votus.Core.Infrastructure.EventSourcing;
using Votus.Core.Infrastructure.Logging;
using Votus.Core.Infrastructure.Queuing;
using Votus.Core.Infrastructure.Serialization;

namespace Votus.Core
{
    public class CoreInjectionModule : NinjectModule
    {
        #region Constants & Variables

        public const string CommandsQueueName            = "Commands";
        public const string AggregateRootEventsTopicPath = "AggregateRootEvents";

        private readonly ConfigurationInjectionModule _configurationInjectionModule;

        #endregion

        #region Constructors

        public
        CoreInjectionModule()
            : this(
                new ConfigurationInjectionModule(
                    typeof(EnvironmentVariableRepository),
                    typeof(ProjectConfigFileRepository)
                )
            )
        {
        }

        public 
        CoreInjectionModule(
            ConfigurationInjectionModule configInjectionModule)
        {
            _configurationInjectionModule = configInjectionModule;
        }

        #endregion

        public
        override 
        void 
        Load()
        {
            Kernel.Load(_configurationInjectionModule);

            Bind<ILog>()
                .To<DotNetTraceLogger>()
                .InSingletonScope();

            Bind<ISerializer>()
                .To<NewtonsoftJsonSerializer>()
                .InSingletonScope();

            Bind<CloudStorageAccount>()
                .ToMethod(ctx =>
                    ctx.Kernel
                        .Get<ApplicationSettings>()
                        .AppCloudStorageAccount)
                .InSingletonScope();

            Bind<ICache>()
                .ToMethod(ctx => {
                    var config = ctx.Kernel.Get<ApplicationSettings>();

                    return new AzureDistributedCache(
                        config.AzureCacheServiceName,
                        config.AzureCacheServicePrimaryAccessKey
                        );
                })
                .InSingletonScope();

            Bind<IQueue>()
                .ToMethod(ConfigureQueue)
                .InSingletonScope();

            Bind<IEventBus>()
                .ToMethod(ctx => 
                    new AzureEventBus(
                        connectionString: ctx.Kernel.Get<ApplicationSettings>().AzureServiceBusConnectionString,
                        topicPath:        AggregateRootEventsTopicPath
                    )
                )
                .InSingletonScope();

            Bind<IPartitionedRepository>()
                .ToMethod(ctx => CreatePartitionedRepo(ctx, name: "EventStoreEvents"))
                .WhenInjectedInto<EventStore>()
                .InSingletonScope();

            Bind(typeof(IVersioningRepository<>))
                .To<AggregateRootRepository>()
                .InSingletonScope();

            Bind<IKeyValueRepository>()
                .ToMethod(ctx => new BlobKeyValueRepository(containerName: "view-cache"))
                .InSingletonScope();

            Bind<QueueManager>()
                .ToSelf()
                .InSingletonScope();

            Bind<Ideas>()
                .ToSelf()
                .InSingletonScope();
        }

        public 
        static 
        IPartitionedRepository 
        CreatePartitionedRepo(
            IContext    ctx,
            string      name)
        {
            return new AzureTableRepository(
                ctx.Kernel
                    .Get<CloudStorageAccount>()
                    .CreateCloudTableClient()
                    .GetTableReference(name)
            );
        }

        private 
        static
        IQueue
        ConfigureQueue(
            IContext ctx)
        {
            var settings = ctx.Kernel.Get<ApplicationSettings>();

            return new ServiceBusQueue(
                settings.AzureServiceBusConnectionString, 
                CommandsQueueName
            );
        }
    }
}