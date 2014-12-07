﻿using Ninject;
using Ninject.Modules;
using System;
using Votus.Core;
using Votus.Core.Domain;
using Votus.Core.Infrastructure.Azure.ServiceBus;
using Votus.Core.Infrastructure.Data;
using Votus.Core.Infrastructure.EventSourcing;
using Votus.Web.Areas.Api.Controllers;
using Votus.Web.Areas.Api.ViewManagers;
using WebApi.OutputCache.Core.Cache;

namespace Votus.Web.Areas.Api
{
    public class ApiDependencyInjectionModule : NinjectModule
    {
        public override void Load()
        {
            // Configure all the core dependencies...
            Kernel.Load(new[] { new CoreInjectionModule() });

            // Bind the view caches...
            Bind<IPartitionedRepository>()
                .ToMethod(ctx =>
                    // TODO: This should be stored in BLOB or DocumentDb storage...
                    CoreInjectionModule.CreatePartitionedRepo(ctx, "IdeasByTimeDescending")) 
                .InSingletonScope();

            Bind<IPartitionedRepository>()
                .ToMethod(ctx =>
                    CoreInjectionModule.CreatePartitionedRepo(ctx, "RecentTestEntities")) 
                .WhenInjectedInto<InfrastructureTestingController>()
                .InSingletonScope();

            Bind<IPartitionedRepository>()
                .ToMethod(ctx =>
                    CoreInjectionModule.CreatePartitionedRepo(ctx, "RecentTestEntities")) 
                .WhenInjectedInto<TestEntityRepository>()
                .InSingletonScope();

            // Configure the web api output caching provider
            Bind<IApiOutputCache>()
                .To<ApiOutputCachingProvider>()
                .InSingletonScope();
            
            // Bind all the events to their handlers...
            BindEvent<IdeaCreatedEvent      >(Kernel.Get<IdeasByTimeDescendingViewManager>().HandleAsync);
            BindEvent<IdeaCreatedEvent      >(Kernel.Get<IdeaByIdViewManager             >().HandleAsync);
            BindEvent<GoalCreatedEvent      >(Kernel.Get<Ideas                    >().HandleAsync); // TODO: Bind in Core
            BindEvent<TaskCreatedEvent      >(Kernel.Get<Ideas                    >().HandleAsync); // TODO: Bind in Core
            BindEvent<TaskCreatedEvent      >(Kernel.Get<TaskByIdViewManager             >().HandleAsync);
            BindEvent<TaskVotedCompleteEvent>(Kernel.Get<TaskByIdViewManager             >().HandleAsync);
            BindEvent<TaskVotedCompleteEvent>(Kernel.Get<TasksByIdeaViewManager          >().HandleAsync);
            BindEvent<TaskAddedToIdeaEvent  >(Kernel.Get<TasksByIdeaViewManager          >().HandleAsync);
            BindEvent<GoalAddedToIdeaEvent  >(Kernel.Get<GoalsByIdeaViewManager          >().HandleAsync);
            BindEvent<TestEntityCreatedEvent>(Kernel.Get<RecentTestEntitiesViewManager   >().HandleAsync);
        }

        public
        void BindEvent<TEvent>(
            Func<TEvent, System.Threading.Tasks.Task> handler)
        {
            Bind<IEventProcessor>()
                .ToMethod(ctx => 
                    new ServiceBusSubscriptionProcessor<TEvent>(
                        serviceBusConnectionString: ctx.Kernel.Get<ApplicationSettings>().AzureServiceBusConnectionString,
                        topicPath:                  CoreInjectionModule.AggregateRootEventsTopicPath,
                        asyncEventHandler:          handler
                    )
                )
                .InSingletonScope();
        }
    }
}