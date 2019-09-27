﻿// Copyright 2007-2018 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, either express or implied. See the License for the
// specific language governing permissions and limitations under the License.
namespace MassTransit.RabbitMqTransport.Topology.Topologies
{
    using System;
    using System.Collections.Generic;
    using Builders;
    using Configurators;
    using Entities;
    using MassTransit.Topology;
    using MassTransit.Topology.Topologies;
    using Settings;
    using Specifications;
    using Util;


    public class RabbitMqMessagePublishTopology<TMessage> :
        MessagePublishTopology<TMessage>,
        IRabbitMqMessagePublishTopologyConfigurator<TMessage>
        where TMessage : class
    {
        readonly ExchangeConfigurator _exchange;
        readonly IList<IRabbitMqMessagePublishTopology> _implementedMessageTypes;
        readonly IRabbitMqPublishTopology _publishTopology;
        readonly IMessageTopology<TMessage> _messageTopology;
        readonly IList<IRabbitMqPublishTopologySpecification> _specifications;

        public RabbitMqMessagePublishTopology(IRabbitMqPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology,
            IMessageExchangeTypeSelector<TMessage> exchangeTypeSelector)
        {
            _publishTopology = publishTopology;
            _messageTopology = messageTopology;
            ExchangeTypeSelector = exchangeTypeSelector;

            var exchangeName = messageTopology.EntityName;
            var exchangeType = exchangeTypeSelector.GetExchangeType(exchangeName);

            var temporary = TypeMetadataCache<TMessage>.IsTemporaryMessageType;

            var durable = !temporary;
            var autoDelete = temporary;

            _exchange = new ExchangeConfigurator(exchangeName, exchangeType, durable, autoDelete);

            _implementedMessageTypes = new List<IRabbitMqMessagePublishTopology>();
            _specifications = new List<IRabbitMqPublishTopologySpecification>();
        }

        IMessageExchangeTypeSelector<TMessage> ExchangeTypeSelector { get; }

        public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
        {
            var exchangeHandle = builder.ExchangeDeclare(_exchange.ExchangeName, _exchange.ExchangeType, _exchange.Durable, _exchange.AutoDelete,
                _exchange.ExchangeArguments);

            if (builder.Exchange != null)
                builder.ExchangeBind(builder.Exchange, exchangeHandle, "", new Dictionary<string, object>());
            else
                builder.Exchange = exchangeHandle;

            for (int i = 0; i < _specifications.Count; i++)
                _specifications[i].Apply(builder);

            foreach (IRabbitMqMessagePublishTopology configurator in _implementedMessageTypes)
                configurator.Apply(builder);
        }

        public override bool TryGetPublishAddress(Uri baseAddress, out Uri publishAddress)
        {
            publishAddress = GetSendSettings().GetSendAddress(baseAddress);
            return true;
        }

        public SendSettings GetSendSettings()
        {
            return new RabbitMqSendSettings(_exchange.ExchangeName, _exchange.ExchangeType, _exchange.Durable, _exchange.AutoDelete);
        }

        public BrokerTopology GetBrokerTopology()
        {
            var builder = new PublishEndpointBrokerTopologyBuilder(_publishTopology.BrokerTopologyOptions);

            Apply(builder);

            return builder.BuildBrokerTopology();
        }

        public Exchange Exchange => _exchange;

        bool IExchangeConfigurator.Durable
        {
            set => _exchange.Durable = value;
        }

        bool IExchangeConfigurator.AutoDelete
        {
            set => _exchange.AutoDelete = value;
        }

        string IExchangeConfigurator.ExchangeType
        {
            set => _exchange.ExchangeType = value;
        }

        void IExchangeConfigurator.SetExchangeArgument(string key, object value)
        {
            _exchange.SetExchangeArgument(key, value);
        }

        void IExchangeConfigurator.SetExchangeArgument(string key, TimeSpan value)
        {
            _exchange.SetExchangeArgument(key, value);
        }

        public string AlternateExchange
        {
            set => _exchange.SetExchangeArgument(RabbitMQ.Client.Headers.AlternateExchange, value);
        }

        public void BindQueue(string exchangeName, string queueName, Action<IQueueBindingConfigurator> configure = null)
        {
            if (string.IsNullOrWhiteSpace(exchangeName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

            var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

            var specification = new ExchangeToQueueBindingPublishTopologySpecification(exchangeName, exchangeType, queueName);

            configure?.Invoke(specification);

            _specifications.Add(specification);
        }

        public void BindAlterateExchangeQueue(string exchangeName, string queueName = null, Action<IQueueBindingConfigurator> configure = null)
        {
            BindQueue(exchangeName, queueName, configure);

            AlternateExchange = exchangeName;
        }

        public void AddImplementedMessageConfigurator<T>(IRabbitMqMessagePublishTopologyConfigurator<T> configurator, bool direct)
            where T : class
        {
            var adapter = new ImplementedTypeAdapter<T>(configurator, direct);

            _implementedMessageTypes.Add(adapter);
        }


        class ImplementedTypeAdapter<T> :
            IRabbitMqMessagePublishTopology
            where T : class
        {
            readonly IRabbitMqMessagePublishTopologyConfigurator<T> _configurator;
            readonly bool _direct;

            public ImplementedTypeAdapter(IRabbitMqMessagePublishTopologyConfigurator<T> configurator, bool direct)
            {
                _configurator = configurator;
                _direct = direct;
            }

            public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
            {
                if (_direct)
                {
                    var implementedBuilder = builder.CreateImplementedBuilder();

                    _configurator.Apply(implementedBuilder);
                }
            }
        }
    }
}
