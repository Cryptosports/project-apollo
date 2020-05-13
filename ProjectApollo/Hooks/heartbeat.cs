﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Project_Apollo.Registry;
using Newtonsoft.Json;
using System.IO;
using static Project_Apollo.Registry.APIRegistry;

namespace Project_Apollo.Hooks
{
    public class heartbeat
    {
        public struct heartbeat_ReplyData
        {
            public string status;
            public Dictionary<string, string> data;
        }
        [APIPath("/api/v1/user/heartbeat", "PUT", true)]
        public ReplyData Heartbeat(IPAddress remoteIP, int remotePort, List<string> arguments, string body, string method, Dictionary<string, string> Headers)
        {
            ReplyData _reply = new ReplyData();

            Heartbeat_Memory mem = Heartbeat_Memory.GetHeartbeat();
            if (mem.Contains(remoteIP.ToString())) mem.Set(remoteIP.ToString(), Guid.NewGuid().ToString());
            else mem.Add(remoteIP.ToString(), Guid.NewGuid().ToString());
            heartbeat_ReplyData hbrd = new heartbeat_ReplyData();
            hbrd.status = "success";
            hbrd.data = new Dictionary<string, string>();
            hbrd.data.Add("session_id", mem.Get(remoteIP.ToString()));
            _reply.Status = 200;
            _reply.Body = JsonConvert.SerializeObject(hbrd);


            return _reply;
        }
    }

    public class Heartbeat_Memory
    {
        private static readonly object _lock = new object();
        public static Heartbeat_Memory GetHeartbeat()
        {
            lock (_lock)
            {

                if (!File.Exists("presence.json"))
                {
                    Heartbeat_Memory hm = new Heartbeat_Memory();
                    return hm;
                }
                string js = File.ReadAllText("presence.json");
                return (Heartbeat_Memory)JsonConvert.DeserializeObject<Heartbeat_Memory>(js);
            }
        }
        private static readonly object retr = new object();
        public Dictionary<string, string> _pres = new Dictionary<string, string>();
        public bool Contains(string Key)
        {
            return _pres.ContainsKey(Key);
        }

        public void Add(string Key,string Val)
        {
            _pres.Add(Key, Val);
            Commit();
        }

        public void Rem(string Key)
        {
            _pres.Remove(Key);
            Commit();
        }

        public void Set(string Key,string Val)
        {
            _pres[Key] = Val;
            Commit();
        }

        public string Get(string Key)
        {
            return _pres[Key];
        }

        public void Commit()
        {
            lock (_lock)
            {

                File.WriteAllText("presence.json", JsonConvert.SerializeObject(this));
            }
        }

    }
}
