namespace MassTransit.EntityFrameworkCoreIntegration.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Automatonymous;
    using EntityFrameworkCoreIntegration;
    using GreenPipes;
    using Mappings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Design;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Shared;
    using TestFramework.Sagas;


    namespace ContainerTests
    {
        using System.Linq;
        using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;


        [TestFixture(typeof(SqlServerTestDbParameters))]
        [TestFixture(typeof(SqlServerResiliencyTestDbParameters))]
        [TestFixture(typeof(PostgresTestDbParameters))]
        public class Using_optimistic_concurrency<T> :
            EntityFrameworkTestFixture<T, TestInstanceDbContext>
            where T : ITestDbParameters, new()
        {
            [OneTimeSetUp]
            public async Task SetUp()
            {
                await using var context = new TestInstanceContextFactory().CreateDbContext(DbContextOptionsBuilder);

                await context.Database.MigrateAsync();
            }

            [OneTimeTearDown]
            public async Task TearDown()
            {
                await using var context = new TestInstanceContextFactory().CreateDbContext(DbContextOptionsBuilder);

                await context.Database.EnsureDeletedAsync();
            }

            [Test]
            public async Task Should_work_as_expected()
            {
                Task<ConsumeContext<TestStarted>> started = ConnectPublishHandler<TestStarted>();
                Task<ConsumeContext<TestUpdated>> updated = ConnectPublishHandler<TestUpdated>();

                var correlationId = NewId.NextGuid();
                var testKey = NewId.NextGuid().ToString();

                await InputQueueSendEndpoint.Send(new StartTest
                {
                    CorrelationId = correlationId,
                    TestKey = testKey
                });

                await started;

                await InputQueueSendEndpoint.Send(new UpdateTest
                {
                    TestId = correlationId,
                    TestKey = testKey
                });

                await updated;
            }

            readonly IServiceProvider _provider;

            public Using_optimistic_concurrency()
            {
                // add new migration by calling
                // dotnet ef migrations add --context "TestInstanceDbContext" Init  -v

                _provider = new ServiceCollection()
                    .AddMassTransit(ConfigureRegistration)
                    .AddScoped<PublishTestStartedActivity>()
                    .BuildServiceProvider();
            }

            protected void ConfigureRegistration<T>(IRegistrationConfigurator<T> configurator)
            {
                configurator.AddSagaStateMachine<TestStateMachineSaga, TestInstance>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ConcurrencyMode = ConcurrencyMode.Optimistic;

                        r.AddDbContext<DbContext, TestInstanceDbContext>(ApplyBuilderOptions);
                    });

                configurator.AddBus(provider => BusControl);
            }

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                configurator.UseMessageRetry(r => r.Immediate(5));
                configurator.UseInMemoryOutbox();
                configurator.ConfigureSaga<TestInstance>(_provider);
            }
        }


        [TestFixture(typeof(SqlServerTestDbParameters))]
        [TestFixture(typeof(SqlServerResiliencyTestDbParameters))]
        [TestFixture(typeof(PostgresTestDbParameters))]
        public class Using_pessimistic_concurrency<TParameters> :
            EntityFrameworkTestFixture<TParameters, TestInstanceDbContext>
            where TParameters : ITestDbParameters, new()
        {
            [OneTimeSetUp]
            public async Task SetUp()
            {
                await using var context = new TestInstanceContextFactory().CreateDbContext(DbContextOptionsBuilder);

                await context.Database.MigrateAsync();
            }

            [OneTimeTearDown]
            public async Task TearDown()
            {
                await using var context = new TestInstanceContextFactory().CreateDbContext(DbContextOptionsBuilder);

                await context.Database.EnsureDeletedAsync();
            }

            [Test]
            public async Task Should_work_as_expected()
            {
                Task<ConsumeContext<TestStarted>> started = ConnectPublishHandler<TestStarted>();
                Task<ConsumeContext<TestUpdated>> updated = ConnectPublishHandler<TestUpdated>();

                var correlationId = NewId.NextGuid();
                var testKey = NewId.NextGuid().ToString();

                await InputQueueSendEndpoint.Send(new StartTest
                {
                    CorrelationId = correlationId,
                    TestKey = testKey
                });

                await started;

                await InputQueueSendEndpoint.Send(new UpdateTest
                {
                    TestId = correlationId,
                    TestKey = testKey
                });

                await updated;
            }

            readonly IServiceProvider _provider;

            public Using_pessimistic_concurrency()
            {
                _provider = new ServiceCollection()
                    .AddMassTransit(ConfigureRegistration)
                    .AddScoped<PublishTestStartedActivity>()
                    .BuildServiceProvider();
            }

            protected void ConfigureRegistration<T>(IRegistrationConfigurator<T> configurator)
            {
                configurator.AddSagaStateMachine<TestStateMachineSaga, TestInstance>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.AddDbContext<DbContext, TestInstanceDbContext>(ApplyBuilderOptions);

                        r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                        if (DbContextOptionsBuilder.Options.Extensions.Any(x => x is NpgsqlOptionsExtension))
                            r.LockStatementProvider = new PostgresLockStatementProvider(false);
                        else
                            r.LockStatementProvider = new SqlServerLockStatementProvider(false);
                    });

                configurator.AddBus(provider => BusControl);
            }

            protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
            {
                configurator.UseMessageRetry(r => r.Immediate(5));

                // TODO figure out why Postgres locking isn't working :(
                if (DbContextOptionsBuilder.Options.Extensions.Any(x => x is NpgsqlOptionsExtension))
                    configurator.UseInMemoryOutbox();

                configurator.ConfigureSaga<TestInstance>(_provider);
            }
        }


        public class TestInstance :
            SagaStateMachineInstance
        {
            public Guid CorrelationId { get; set; }

            public string CurrentState { get; set; }
            public string Key { get; set; }
        }


        class TestInstanceMap :
            SagaClassMap<TestInstance>
        {
            protected override void Configure(EntityTypeBuilder<TestInstance> entity, ModelBuilder model)
            {
                base.Configure(entity, model);

                entity.Property(x => x.CurrentState);
                entity.Property(x => x.Key);

                entity.HasKey(p => p.Key);
            }
        }


        public class TestInstanceDbContext :
            SagaDbContext
        {
            public TestInstanceDbContext(DbContextOptions options)
                : base(options)
            {
            }

            protected override IEnumerable<ISagaClassMap> Configurations
            {
                get { yield return new TestInstanceMap(); }
            }
        }


        public class TestInstanceContextFactory :
            IDesignTimeDbContextFactory<TestInstanceDbContext>
        {
            public TestInstanceDbContext CreateDbContext(string[] args)
            {
                // used only for database update and migrations. Since IDesignTimeDbContextFactory is icky,
                // we only support command line tools for SQL Server, so use SQL Server if you need to do
                // migrations.

                var optionsBuilder = new SqlServerTestDbParameters().GetDbContextOptions(typeof(TestInstanceDbContext));

                return new TestInstanceDbContext(optionsBuilder.Options);
            }

            public TestInstanceDbContext CreateDbContext(DbContextOptionsBuilder optionsBuilder)
            {
                return new TestInstanceDbContext(optionsBuilder.Options);
            }
        }


        public class TestStateMachineSaga :
            MassTransitStateMachine<TestInstance>
        {
            public TestStateMachineSaga()
            {
                InstanceState(x => x.CurrentState);

                Event(() => Updated, x =>
                {
                    x.CorrelateById(m => m.Message.TestId);
                    x.OnMissingInstance(i => i.Fault());
                });

                Initially(
                    When(Started)
                        .Then(context => context.Instance.Key = context.Data.TestKey)
                        .Activity(x => x.OfInstanceType<PublishTestStartedActivity>())
                        .TransitionTo(Active));

                During(Active,
                    When(Updated)
                        .Publish(context => new TestUpdated
                        {
                            CorrelationId = context.Instance.CorrelationId,
                            TestKey = context.Instance.Key
                        })
                        .TransitionTo(Done)
                        .Finalize());

                SetCompletedWhenFinalized();
            }

            public State Active { get; private set; }
            public State Done { get; private set; }

            public Event<StartTest> Started { get; private set; }
            public Event<UpdateTest> Updated { get; private set; }
        }


        public class UpdateTest
        {
            public Guid TestId { get; set; }
            public string TestKey { get; set; }
        }


        public class PublishTestStartedActivity :
            Activity<TestInstance>
        {
            readonly ConsumeContext _context;

            public PublishTestStartedActivity(ConsumeContext context)
            {
                _context = context;
            }

            public void Probe(ProbeContext context)
            {
                context.CreateScope("publisher");
            }

            public void Accept(StateMachineVisitor visitor)
            {
                visitor.Visit(this);
            }

            public async Task Execute(BehaviorContext<TestInstance> context, Behavior<TestInstance> next)
            {
                await _context.Publish(new TestStarted
                {
                    CorrelationId = context.Instance.CorrelationId,
                    TestKey = context.Instance.Key
                }).ConfigureAwait(false);

                await next.Execute(context).ConfigureAwait(false);
            }

            public async Task Execute<T>(BehaviorContext<TestInstance, T> context, Behavior<TestInstance, T> next)
            {
                await _context.Publish(new TestStarted
                {
                    CorrelationId = context.Instance.CorrelationId,
                    TestKey = context.Instance.Key
                }).ConfigureAwait(false);

                await next.Execute(context).ConfigureAwait(false);
            }

            public Task Faulted<TException>(BehaviorExceptionContext<TestInstance, TException> context, Behavior<TestInstance> next)
                where TException : Exception
            {
                return next.Faulted(context);
            }

            public Task Faulted<T, TException>(BehaviorExceptionContext<TestInstance, T, TException> context, Behavior<TestInstance, T> next)
                where TException : Exception
            {
                return next.Faulted(context);
            }
        }
    }
}
