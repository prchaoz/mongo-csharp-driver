/* Copyright 2013-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs after a message has been sent.
    /// </summary>
    public struct ConnectionSentMessagesEvent : IEvent
    {
        private readonly ConnectionId _connectionId;
        private readonly TimeSpan _networkDuration;
        private readonly TimeSpan _serializationDuration;
        private readonly int _length;
        private readonly long? _operationId;
        private readonly int _requestId;
        private readonly DateTime _timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionSentMessagesEvent" /> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="requestIds">The request ids.</param>
        /// <param name="length">The length.</param>
        /// <param name="networkDuration">The duration of time spent on the network.</param>
        /// <param name="serializationDuration">The duration of time spent serializing the messages.</param>
        /// <param name="operationId">The operation identifier.</param>
        [Obsolete("Support for sending multiple messages has been removed, use the constructor with single requestId instead.")]
        public ConnectionSentMessagesEvent(ConnectionId connectionId, IReadOnlyList<int> requestIds, int length, TimeSpan networkDuration, TimeSpan serializationDuration, long? operationId)
        {
            _connectionId = connectionId;
            _requestId = requestIds.Single();
            _length = length;
            _networkDuration = networkDuration;
            _serializationDuration = serializationDuration;
            _operationId = operationId;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionSentMessagesEvent" /> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="requestId">The request id.</param>
        /// <param name="length">The length.</param>
        /// <param name="networkDuration">The duration of time spent on the network.</param>
        /// <param name="serializationDuration">The duration of time spent serializing the messages.</param>
        /// <param name="operationId">The operation identifier.</param>
        public ConnectionSentMessagesEvent(ConnectionId connectionId, int requestId, int length, TimeSpan networkDuration, TimeSpan serializationDuration, long? operationId)
        {
            _connectionId = connectionId;
            _requestId = requestId;
            _length = length;
            _networkDuration = networkDuration;
            _serializationDuration = serializationDuration;
            _operationId = operationId;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _connectionId.ServerId.ClusterId; }
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the duration of time it took to send the message.
        /// </summary>
        public TimeSpan Duration
        {
            get { return _networkDuration + _serializationDuration; }
        }

        /// <summary>
        /// Gets the duration of time spent on the network.
        /// </summary>
        public TimeSpan NetworkDuration
        {
            get { return _networkDuration; }
        }

        /// <summary>
        /// Gets the operation identifier.
        /// </summary>
        public long? OperationId
        {
            get { return _operationId; }
        }

        /// <summary>
        /// Gets the duration of time spent serializing the messages.
        /// </summary>
        public TimeSpan SerializationDuration
        {
            get { return _serializationDuration; }
        }

        /// <summary>
        /// Gets the combined length of the messages.
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
        }

        /// <summary>
        /// Gets the request ids.
        /// </summary>
        [Obsolete($"Support for sending multiple messages has been removed, use {nameof(RequestId)} instead.")]
        public IReadOnlyList<int> RequestIds
        {
            get { return [_requestId]; }
        }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _connectionId.ServerId; }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        // explicit interface implementations
        EventType IEvent.Type => EventType.ConnectionSentMessages;
    }
}
