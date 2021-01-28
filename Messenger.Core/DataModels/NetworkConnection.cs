﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger.Core
{
    // delegate for handing response information in events
    public delegate void ResponseHandler(Response response);

    /// <summary>
    /// A class that represents a client to server connection. 
    /// Sends requests to the server and returns responses 
    /// to the client and takes care of handling events from view models.
    /// </summary>
    public class NetworkConnection : Notifier
    {
        #region Public Events

        // wakes up if server granted the request and sent the data after login
        public event ResponseHandler SignInDone;

        // wakes up if sign in failed
        public event ResponseHandler SignInFail;

        #endregion

        #region Public Properties

        // try if are we now waiting for an answer to some of our requests
        public bool WaitingResponse { get; set; }

        // current tcp connection 
        public TcpClient TcpClient { get; set; }

        // current user if the client is authorized 
        public User User { get; set; }

        // true if authorized
        public bool Authorized
        {
            get => User != null;
            set => Authorized = value;
        }

        // all requests from view models go to the 
        // end of this queue for further processing
        public Queue<Request> RequestQueue { get; set; }

        #endregion

        #region Private Properties

        // stream between Client and server
        NetworkStream NetworkStream { get; set; } 

        #endregion

        #region Constructor

        public NetworkConnection()
        {
            // start action in thread pool
            Task.Run(() =>
            {
                // this method should work throughout the 
                // entire connection to the server
                _ = ProcessAsync()
                        // run in parallel in a different synchronization context
                        .ConfigureAwait(continueOnCapturedContext: false);
            });
        }

        #endregion

        #region Public Methods

        // processing this newtwork connection
        public async Task ProcessAsync()
        {
            // create new request queue
            RequestQueue = new Queue<Request>();

            // creating current tcp client
            TcpClient = new TcpClient();

            // connecting to server async
            await TcpClient.ConnectAsync("127.0.0.1", 1300);

            // get newtwork stream of communication with the server
            NetworkStream = TcpClient.GetStream();

            #region Subscribe On View Model Events

            // adding new request in queue
            IoC.Get<SignInViewModel>().SignInFromViewModel += (
                ss, request) => RequestQueue.Enqueue(request);

            #endregion

            // main loop of process method, represents connection to server
            while (true)
            {
                // if we don't wait a response from server and have request
                if (!WaitingResponse
                    && RequestQueue.Count > 0)
                {
                    WaitingResponse = true;

                    // remuve and return object at the beginning of the Request Queue
                    var firstQueueRequest = RequestQueue.Dequeue();

                    // send server request
                    await WriteToStreamAsyncAndSerialize(firstQueueRequest);
                }

                // if we send to server request and waiting response
                if (WaitingResponse)
                {
                    // wait server response
                    var response = await ReadFromStreamAsyncAndDeserialize();

                    // todo: response reaction action
                    CheckResponseTypeAndWakeUpEvents(response);

                    // we dont wait response and ready to send new requests
                    WaitingResponse = false;
                }
            }
        }

        // reaction to server response according to response type
        public void CheckResponseTypeAndWakeUpEvents(Response response)
        {
            switch (response.ServerResponseType)
            {
                case ServerResponse.SignInFailed:
                    {
                        // wakes event
                        SignInFail(response);

                        break;
                    }

                case ServerResponse.SignUpFailed:
                    break;

                case ServerResponse.SendAllInformation:
                    {
                        // create an instance of this user
                        this.User = new User();

                        // set data about this user received from the server
                        this.User = response.UserInfo;

                        // collect the data received from the server 
                        // in a relevant form and send it to the view model
                        SendChatsToViewModel(response.Chats);

                        // logically, we do not have a server response 
                        // about a successful login, it immediately sends data
                        SignInDone(response);
                        break;
                    }

                case ServerResponse.SendNewMessages:
                    break;

                case ServerResponse.Error:
                    break;

                default:
                    break;
            }
        }

        // send chats and messages that came 
        // from the server to the view model
        public void SendChatsToViewModel(IList<Chat> chats)
        {
            // create a list for local work in a method
            var items = new List<ChatListItemViewModel>();

            // create dialogs according to the content of chats
            foreach (var chat in chats)
            {
                // objects that form a list of bubble messages for current chat
                var bubbleMessagesList = new ObservableCollection<ChatMessageListItemViewModel>();

                // collect a collection of messages for this dialog
                foreach (var chatMessage in chat.Messages)
                {
                    // if the author of the message is me
                    if (chatMessage.AuthorUser.Id == this.User.Id)
                    {
                        // add to the end of dialog new message bubble
                        bubbleMessagesList.Add(
                        new ChatMessageListItemViewModel
                        {
                            Message = chatMessage.Text,

                            // initials from first letters of ...
                            ProfileInitials = 
                                // ... first name and 
                                chatMessage.AuthorUser.FirstName.First().ToString()
                                // ... of last name
                                + chatMessage.AuthorUser.LastName.First().ToString(),

                            ProfilePictureRGB = "63c439",
                            SenderName = chatMessage.AuthorUser.Nick,
                            MessageSentTime = chatMessage.CreatedAt,
                            IsSelected = false,
                            AnchorVisibility = true,
                            ShowProfilePicture = false,
                            ImAuthor = true
                        });
                    }

                    // if the author of the message is not me
                    else
                    {
                        // add to the end of dialog new message bubble
                        bubbleMessagesList.Add(
                        new ChatMessageListItemViewModel
                        {
                            Message = chatMessage.Text,

                            // initials from first letters of ...
                            ProfileInitials =
                                // ... first name and 
                                chatMessage.AuthorUser.FirstName.First().ToString()
                                // ... of last name
                                + chatMessage.AuthorUser.LastName.First().ToString(),

                            ProfilePictureRGB = "c46339",
                            SenderName = chatMessage.AuthorUser.Nick,
                            MessageSentTime = chatMessage.CreatedAt,
                            IsSelected = false,
                            AnchorVisibility = true,
                            ShowProfilePicture = false,
                            ImAuthor = false
                        });
                    }
                }

                // add a new chat with messages to Chat List Item
                items.Add(
                    new ChatListItemViewModel
                    {
                        // put the name of my interlocutor in the title
                        Name =
                            chat.OwnerUser.Id == this.User.Id ?
                            chat.MemberUser.Nick : chat.OwnerUser.Nick,

                        // put the first letter of the nickname 
                        // of my interlocutor in the initials
                        ProfileInitials =
                            chat.OwnerUser.Id == this.User.Id ?
                            chat.MemberUser.Nick.First().ToString()
                            : chat.OwnerUser.Nick.First().ToString(),

                        // set background color of initials
                        ProfilePictureRGB = "c46339",

                        // put the last message from this chat
                        Message = chat.Messages.Last().Text,

                        // create bubbles messages
                        CurrentChatMessageList = new ChatMessageListViewModel
                        {
                            // put the the nickname 
                            // of my interlocutor in the title
                            DisplayTitle =
                                chat.OwnerUser.Id == this.User.Id ?
                                chat.MemberUser.Nick : chat.OwnerUser.Nick,

                            // add messages in chat
                            Items = bubbleMessagesList
                        }
                    });
            }

            // get the items from chat list view model and set updated data
            // IoC.Get<ChatListViewModel>().Items = items;
        }

        #endregion

        #region Stream Heplers

        /// <summary>
        /// Deserializes the request received from the user 
        /// and provides the <see cref="Request"/> object.
        /// </summary>
        /// <returns></returns>
        private async Task<Response> ReadFromStreamAsyncAndDeserialize()
        {
            // create empty buffer
            byte[] buffer = new byte[4096];

            var receiveStringBuilder = new StringBuilder();

            // current position in the NetworkStream
            int streamPosition = 0;

            do
            {
                // async read from stream
                streamPosition = await NetworkStream.ReadAsync(buffer, 0, buffer.Length)
                        // run in parallel in a different synchronization context
                        .ConfigureAwait(continueOnCapturedContext: false);

                // encoding one character at a time and appending 
                // it to the incoming receive string
                receiveStringBuilder.Append(
                    Encoding.UTF8.GetString(buffer, 0, streamPosition));

            } while (NetworkStream.DataAvailable);

            // convert bytes to string
            var jsonString = receiveStringBuilder.ToString();

            // deserialize json string to request
            var response = JsonConvert.DeserializeObject<Response>(jsonString);

            return response;
        }

        /// <summary>
        /// Serializes and sends <see cref="Request"/> 
        /// to the <see cref="NetworkStream"/> to server
        /// </summary>
        /// <param name="serverResponse">Object of server Response</param>
        /// <returns></returns>
        public async Task<bool> WriteToStreamAsyncAndSerialize(Request myNewRequest)
        {
            // boolean result of this task
            var tcs = new TaskCompletionSource<bool>();
            
            /*string jsonString = $"Hello! Password:{myNewRequest.UserInitiator.Password}, Login:{myNewRequest.UserInitiator.Email}";*/

            try
            {
                // serialize object to json string
                string jsonString = JsonConvert.SerializeObject(myNewRequest);

                // encoding string to bytes
                byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

                // async write bytes in network stream
                await NetworkStream.WriteAsync(jsonBytes, 0, jsonBytes.Length)
                        // run in parallel in a different synchronization context
                        .ConfigureAwait(continueOnCapturedContext: false);

                tcs.SetResult(true);
            }
            catch
            {
                tcs.SetResult(false);
            }

            // we cannot send other requests until the server responds to our request
            WaitingResponse = true;

            return await tcs.Task;
        }

        #endregion
    } 
}
