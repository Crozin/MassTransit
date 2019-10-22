namespace MassTransit.Pipeline.Pipes
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using GreenPipes;
    using SendPipeSpecifications;


    public class SendPipe :
        ISendPipe
    {
        public static readonly ISendPipe Empty = new SendPipe(new SendPipeSpecification());
        readonly ConcurrentDictionary<Type, IMessagePipe> _outputPipes;
        readonly ISendPipeSpecification _specification;

        public SendPipe(ISendPipeSpecification specification)
        {
            _specification = specification;
            _outputPipes = new ConcurrentDictionary<Type, IMessagePipe>();
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateScope("sendPipe");

            foreach (var outputPipe in _outputPipes.Values)
            {
                outputPipe.Probe(scope);
            }
        }

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        Task ISendContextPipe.Send<T>(SendContext<T> context)
        {
            return GetPipe<T>().Send(context);
        }

        IMessagePipe GetPipe<T>()
            where T : class
        {
            return _outputPipes.GetOrAdd(typeof(T), x => new MessagePipe<T>(_specification.GetMessageSpecification<T>()));
        }


        interface IMessagePipe :
            IProbeSite
        {
            Task Send<T>(SendContext<T> context)
                where T : class;
        }


        class MessagePipe<TMessage> :
            IMessagePipe
            where TMessage : class
        {
            readonly Lazy<IMessageSendPipe<TMessage>> _output;
            readonly IMessageSendPipeSpecification<TMessage> _specification;

            public MessagePipe(IMessageSendPipeSpecification<TMessage> specification)
            {
                _specification = specification;

                _output = new Lazy<IMessageSendPipe<TMessage>>(CreateFilter);
            }

            Task IMessagePipe.Send<T>(SendContext<T> context)
            {
                return _output.Value.Send((SendContext<TMessage>)context);
            }

            public void Probe(ProbeContext context)
            {
                _output.Value.Probe(context);
            }

            IMessageSendPipe<TMessage> CreateFilter()
            {
                IPipe<SendContext<TMessage>> messagePipe = _specification.BuildMessagePipe();

                return new MessageSendPipe<TMessage>(messagePipe);
            }
        }
    }
}
