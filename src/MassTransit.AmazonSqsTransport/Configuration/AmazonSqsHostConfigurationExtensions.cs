namespace MassTransit.AmazonSqsTransport.Configuration
{
    using System;
    using Configurators;
    using Definition;


    public static class AmazonSqsHostConfigurationExtensions
    {
        /// <summary>
        ///     Configure a AmazonSQS host using the configuration API
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="hostAddress">The URI host address of the AmazonSQS host (amazonsqs://region)</param>
        /// <param name="configure"></param>
        public static IAmazonSqsHost Host(this IAmazonSqsBusFactoryConfigurator configurator, Uri hostAddress, Action<IAmazonSqsHostConfigurator> configure)
        {
            if (hostAddress == null)
                throw new ArgumentNullException(nameof(hostAddress));

            var hostConfigurator = new AmazonSqsHostConfigurator(hostAddress);

            configure(hostConfigurator);

            return configurator.Host(hostConfigurator.Settings);
        }

        /// <summary>
        /// Configure a AmazonSQS host with a host name and virtual host
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="hostName">The host name of the broker</param>
        /// <param name="configure">The configuration callback</param>
        public static IAmazonSqsHost Host(this IAmazonSqsBusFactoryConfigurator configurator, string hostName, Action<IAmazonSqsHostConfigurator> configure)
        {
            return configurator.Host(new UriBuilder("amazonsqs", hostName).Uri, configure);
        }

        /// <summary>
        /// Declare a ReceiveEndpoint using a unique generated queue name. This queue defaults to auto-delete
        /// and non-durable. By default all services bus instances include a default receiveEndpoint that is
        /// of this type (created automatically upon the first receiver binding).
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="host"></param>
        /// <param name="configure"></param>
        [Obsolete("The host parameter is no longer required, and can be removed")]
        public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, IAmazonSqsHost host,
            Action<IAmazonSqsReceiveEndpointConfigurator> configure = null)
        {
            configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
        }

        /// <summary>
        /// Declare a ReceiveEndpoint using a unique generated queue name. This queue defaults to auto-delete
        /// and non-durable. By default all services bus instances include a default receiveEndpoint that is
        /// of this type (created automatically upon the first receiver binding).
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="host"></param>
        /// <param name="definition"></param>
        /// <param name="configure"></param>
        [Obsolete("The host parameter is no longer required, and can be removed")]
        public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, IAmazonSqsHost host, IEndpointDefinition definition,
            Action<IAmazonSqsReceiveEndpointConfigurator> configure = null)
        {
            configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
        }

        /// <summary>
        /// Declare a ReceiveEndpoint using a unique generated queue name. This queue defaults to auto-delete
        /// and non-durable. By default all services bus instances include a default receiveEndpoint that is
        /// of this type (created automatically upon the first receiver binding).
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="configure"></param>
        public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, Action<IAmazonSqsReceiveEndpointConfigurator> configure = null)
        {
            configurator.ReceiveEndpoint(new TemporaryEndpointDefinition(), DefaultEndpointNameFormatter.Instance, configure);
        }

        /// <summary>
        /// Declare a ReceiveEndpoint using a unique generated queue name. This queue defaults to auto-delete
        /// and non-durable. By default all services bus instances include a default receiveEndpoint that is
        /// of this type (created automatically upon the first receiver binding).
        /// </summary>
        /// <param name="configurator"></param>
        /// <param name="definition"></param>
        /// <param name="configure"></param>
        public static void ReceiveEndpoint(this IAmazonSqsBusFactoryConfigurator configurator, IEndpointDefinition definition,
            Action<IAmazonSqsReceiveEndpointConfigurator> configure = null)
        {
            configurator.ReceiveEndpoint(definition, DefaultEndpointNameFormatter.Instance, configure);
        }
    }
}
