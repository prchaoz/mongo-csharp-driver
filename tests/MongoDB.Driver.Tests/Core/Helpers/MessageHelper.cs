/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Moq;

namespace MongoDB.Driver.Core.Helpers
{
    internal sealed class MessageHelper
    {
        private static readonly DatabaseNamespace __defaultDatabaseNamespace = new("foo");
        private static readonly CollectionNamespace __defaultCollectionNamespace = new(__defaultDatabaseNamespace, "bar");

        public static CollectionNamespace DefaultCollectionNamespace
        {
            get { return __defaultCollectionNamespace; }
        }

        public static DatabaseNamespace DefaultDatabaseNamespace
        {
            get { return __defaultDatabaseNamespace; }
        }

        public static QueryMessage BuildQuery(
            BsonDocument query = null,
            BsonDocument fields = null,
            int requestId = 0,
            CollectionNamespace collectionNamespace = null,
            int skip = 0,
            int batchSize = 0,
            bool noCursorTimeout = false,
            bool partialOk = false,
            bool tailableCursor = false,
            bool awaitData = false,
            bool oplogReplay = false)
        {
#pragma warning disable 618
            return new QueryMessage(
                requestId: requestId,
                collectionNamespace: collectionNamespace ?? __defaultCollectionNamespace,
                query: query ?? new BsonDocument(),
                fields: fields,
                queryValidator: NoOpElementNameValidator.Instance,
                skip: skip,
                batchSize: batchSize,
                secondaryOk: false,
                partialOk: partialOk,
                noCursorTimeout: noCursorTimeout,
                tailableCursor: tailableCursor,
                awaitData: awaitData,
                oplogReplay: oplogReplay);
#pragma warning restore 618
        }

        public static QueryMessage BuildCommand(
            BsonDocument command,
            int requestId = 0,
            DatabaseNamespace databaseNamespace = null)
        {
            if (databaseNamespace == null)
            {
                databaseNamespace = __defaultDatabaseNamespace;
            }

#pragma warning disable 618
            return new QueryMessage(
                requestId: requestId,
                collectionNamespace: databaseNamespace.CommandCollection,
                query: command,
                fields: null,
                queryValidator: NoOpElementNameValidator.Instance,
                skip: 0,
                batchSize: 0,
                secondaryOk: false,
                partialOk: false,
                noCursorTimeout: false,
                oplogReplay: false,
                tailableCursor: false,
                awaitData: false);
#pragma warning restore 618
        }

        public static CommandResponseMessage BuildCommandResponse(
            RawBsonDocument command,
            int requestId = 0,
            int responseTo = 0,
            bool moreToCome = false)
        {
            return new CommandResponseMessage(
                new CommandMessage(
                requestId: requestId,
                responseTo: responseTo,
                sections: new[] { new Type0CommandMessageSection<RawBsonDocument>(command, RawBsonDocumentSerializer.Instance) },
                moreToCome: moreToCome));
        }

        public static QueryMessage BuildGetLastError(
            WriteConcern writeConcern,
            int requestId = 0,
            DatabaseNamespace databaseNamespace = null)
        {
            var command = writeConcern.ToBsonDocument();
            command.InsertAt(0, new BsonElement("getLastError", 1));
            return BuildCommand(command, requestId, databaseNamespace);
        }

        public static ReplyMessage<T> BuildQueryFailedReply<T>(
            BsonDocument queryFailureDocument,
            int responseTo = 0)
        {
            return new ReplyMessage<T>(
                false,
                0,
                false,
                null,
                1,
                true,
                queryFailureDocument,
                0,
                responseTo,
                BsonSerializer.SerializerRegistry.GetSerializer<T>(),
                0);
        }

        public static ReplyMessage<T> BuildReply<T>(
            T document,
            IBsonSerializer<T> serializer = null,
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return BuildReply<T>(
                new[] { document },
                serializer,
                cursorId,
                requestId,
                responseTo,
                startingFrom);
        }

        public static ReplyMessage<T> BuildReply<T>(
            IEnumerable<T> documents,
            IBsonSerializer<T> serializer = null,
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            var documentsList = documents.ToList();
            return new ReplyMessage<T>(
                awaitCapable: true,
                cursorId: cursorId,
                cursorNotFound: false,
                documents: documentsList,
                numberReturned: documentsList.Count,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: serializer ?? new Mock<IBsonSerializer<T>>().Object,
                startingFrom: startingFrom);
        }

        public static ReplyMessage<T> BuildNoDocumentsReturnedReply<T>(
            long cursorId = 0,
            int requestId = 0,
            int responseTo = 0,
            int startingFrom = 0)
        {
            return new ReplyMessage<T>(
                awaitCapable: true,
                cursorId: cursorId,
                cursorNotFound: false,
                documents: new List<T>(),
                numberReturned: 0,
                queryFailure: false,
                queryFailureDocument: null,
                requestId: requestId,
                responseTo: responseTo,
                serializer: new Mock<IBsonSerializer<T>>().Object,
                startingFrom: startingFrom);
        }

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(IEnumerable<MongoDBMessage> requests)
        {
            var docs = new List<BsonDocument>();
            foreach (var request in requests)
            {
                using (var stringWriter = new StringWriter())
                {
                    var encoderFactory = new JsonMessageEncoderFactory(stringWriter, null);

                    request.GetEncoder(encoderFactory).WriteMessage(request);
                    docs.Add(BsonDocument.Parse(stringWriter.GetStringBuilder().ToString()));
                }
            }
            return docs;
        }

        public static List<BsonDocument> TranslateMessagesToBsonDocuments(byte[] bytes)
        {
            return TranslateMessagesToBsonDocuments(TranslateBytesToRequests(bytes));
        }

        public static void WriteResponsesToStream(BlockingMemoryStream stream, params ResponseMessage[] responses)
        {
            lock (stream.Lock)
            {
                var startPosition = stream.Position;
                foreach (var response in responses)
                {
                    var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                    var encoder = response.GetEncoder(encoderFactory);
                    encoder.WriteMessage(response);
                }
                stream.Position = startPosition;
            }
        }

        private static List<RequestMessage> TranslateBytesToRequests(byte[] bytes)
        {
            var requests = new List<RequestMessage>();

            using (var buffer = new ByteArrayBuffer(bytes))
            using (var stream = new ByteBufferStream(buffer))
            {
                int bytesRead = 0;
                while (stream.Length > bytesRead)
                {
                    int startPosition = bytesRead;
                    var length = stream.ReadInt32();
                    stream.ReadInt32(); // requestId
                    stream.ReadInt32(); // responseTo
                    var opCode = (Opcode)stream.ReadInt32();
                    bytesRead += length;
                    stream.Position = startPosition;

                    var encoderFactory = new BinaryMessageEncoderFactory(stream, null);
                    switch (opCode)
                    {
                        case Opcode.Query:
                            requests.Add((RequestMessage)encoderFactory.GetQueryMessageEncoder().ReadMessage());
                            break;
                        default:
                            throw new InvalidOperationException("Unsupported request type.");
                    }
                }
            }

            return requests;
        }
    }
}
