﻿namespace MassTransit.AmazonSqsTransport.Configuration
{
    using Amazon.Runtime;
    using Amazon.SimpleNotificationService;
    using Amazon.SQS;
    using Transport;
    using Transports;


    public interface IAmazonSqsHostConfigurator
    {
        /// <summary>
        /// Sets the accessKey for the connection to AmazonSQS/AmazonSNS
        /// </summary>
        /// <param name="accessKey"></param>
        void AccessKey(string accessKey);

        /// <summary>
        /// Sets the secretKey for the connection to AmazonSQS/AmazonSNS
        /// </summary>
        /// <param name="secretKey"></param>
        void SecretKey(string secretKey);

        /// <summary>
        /// Sets the credentials for the connection to AmazonSQS/AmazonSNS
        /// This is an alternative to using AccessKey() and SecretKey()
        /// See https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/T_Amazon_Runtime_AWSCredentials.htm for usages
        /// </summary>
        /// <param name="credentials"></param>
        void Credentials(AWSCredentials credentials);

        /// <summary>
        /// Sets the default config for the connection to AmazonSQS
        /// </summary>
        /// <param name="config"></param>
        void Config(AmazonSQSConfig config);

        /// <summary>
        /// Sets the default config for the connection to AmazonSNS
        /// </summary>
        /// <param name="config"></param>
        void Config(AmazonSimpleNotificationServiceConfig config);

        /// <summary>
        /// Specifies a method used to determine if a header should be copied to the transport message
        /// </summary>
        /// <param name="allowTransportHeader"></param>
        void AllowTransportHeader(AllowTransportHeader allowTransportHeader);
    }
}
