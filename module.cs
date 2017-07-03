using System;
using Microsoft.Azure.Devices.Gateway;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;


namespace MessageBatcher
{
    public class MessageBatcherModule : IGatewayModule
    {
        private string configuration;
        private Broker broker;
        private static int sizeOfCurrentMessageListBytes;
        private static List<OpcUaMessage> messageList = new List<OpcUaMessage>();

        public void Create(Broker broker, byte[] configuration)
        {
            this.configuration = System.Text.Encoding.UTF8.GetString(configuration);
            this.broker = broker;
        }

        public void Destroy()
        {
        }

        public void Receive(Message received_message)
        {
            //Specify the number of OPC UA messages to stuff into one IoT Hub message.
            //Keep in mind IoT Hub messages can be a max of 256KB but billing is based on each message <= 4KB or 4096 bytes
            //The larger the MaxSizeOfIoTHubMessageBytes, the less 'real time' the data as it will take longer to fill the list before 
            //sending it. If your tags update infrequently, using MessageBatcher is probably a bad idea. 
            int MaxSizeOfIoTHubMessageBytes = 4096;  

            int sizeOfCurrentMessageBytes = System.Text.Encoding.UTF8.GetByteCount(System.Text.Encoding.UTF8.GetString(received_message.Content).ToCharArray());

            Console.WriteLine("*** Size of current message in bytes: " + sizeOfCurrentMessageBytes);

            Console.WriteLine("*** Size of message list in bytes: " + sizeOfCurrentMessageListBytes);

            int roomLeftBytes = MaxSizeOfIoTHubMessageBytes - sizeOfCurrentMessageListBytes;

            Console.WriteLine("*** Room left in message list in bytes: " + roomLeftBytes);

            if (sizeOfCurrentMessageBytes < roomLeftBytes)
            { 
                //Message payload is in Json. Deserialize it to an object.
                string msgPayloadInJson = System.Text.Encoding.UTF8.GetString(received_message.Content, 0, received_message.Content.Length);
                OpcUaMessage msgPayload = JsonConvert.DeserializeObject<OpcUaMessage>(msgPayloadInJson);

                //Add it to the list
                messageList.Add(msgPayload);
                Console.WriteLine($"*** Message {messageList.Count} added to message list");

                //Update size of message list
                sizeOfCurrentMessageListBytes = sizeOfCurrentMessageListBytes + sizeOfCurrentMessageBytes;
            }
            else //If no room left, send existing list, reset it, and add current message to list
            {
                Console.WriteLine("*** Message list full!");

                string msgToSendInJson = JsonConvert.SerializeObject(messageList);

                byte[] messagePayloadBytes = System.Text.Encoding.UTF8.GetBytes(msgToSendInJson);

                //Add properties from last message to received
                Dictionary<string, string> messageProperties = received_message.Properties;
                Message messageToPublish = new Message(messagePayloadBytes, messageProperties);

                //Send message and reset byte count
                this.broker.Publish(messageToPublish);
                sizeOfCurrentMessageListBytes = 0;
                messageList.Clear();
                Console.WriteLine("*** Message list sent. Byte count reset to 0");

                //Message payload is in Json. Deserialize it to an object.
                string msgPayloadInJson = System.Text.Encoding.UTF8.GetString(received_message.Content, 0, received_message.Content.Length);
                OpcUaMessage msgPayload = JsonConvert.DeserializeObject<OpcUaMessage>(msgPayloadInJson);

                //Add it to the list
                messageList.Add(msgPayload);
                Console.WriteLine($"*** Message {messageList.Count} added to message list");

                //Update size of message list
                sizeOfCurrentMessageListBytes = sizeOfCurrentMessageListBytes + sizeOfCurrentMessageBytes;
            }
        }
    }

    public class OpcUaMessage
    {
        public string ApplicationUri { get; set; }
        public string DisplayName { get; set; }
        public string NodeId { get; set; }
        public OpcUaValue Value { get; set; }
    }

    public class OpcUaValue
    {
        public string Value { get; set; }
        public string SourceTimestamp { get; set; }
    }

}
