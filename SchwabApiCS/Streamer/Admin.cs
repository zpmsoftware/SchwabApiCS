// <copyright file="Admin.cs" company="ZPM Software Inc">
// Copyright © 2024 ZPM Software Inc. All rights reserved.
// This Source Code is subject to the terms MIT Public License
// </copyright>

using System;
using Newtonsoft.Json;
using System.ComponentModel;
using static SchwabApiCS.SchwabApi;
using static SchwabApiCS.Streamer.StreamerRequests;
using System.Security.Authentication;
using System.Windows.Controls;
using static SchwabApiCS.Streamer.LevelOneEquity;
using static SchwabApiCS.Streamer.ResponseMessage;
using static SchwabApiCS.Streamer;

namespace SchwabApiCS
{
    public partial class Streamer
    {
        public class AdminClass : ServiceClass
        {
            public AdminClass(Streamer streamer)
                : base(streamer, Streamer.Services.ADMIN)
            {
            }

            internal override void ProcessResponse(ResponseMessage.Response r)
            {
                switch (r.command)
                {
                    case "LOGIN":
                        if (r.content.code == 0)
                        {
                            streamer.isLoggedIn = true;
                            while (streamer.requestQueue.Count > 0)
                            {
                                streamer.websocket.Send(streamer.requestQueue[0]);
                                streamer.requestQueue.RemoveAt(0);
                            }
                        }
                        else
                            throw new Exception("streamer login failed.");
                        break;

                    default:
                        break;
                }
            }

            internal override void Notify(NotifyMessage.Notify notify)
            {
                switch (notify.content.code)
                {
                    case 30: // Stop streaming due to empty subscription
                        streamer.isLoggedIn = false; // start queueing any requests, sending a new request will reopen and login automatically.
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
