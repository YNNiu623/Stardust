﻿using System.Collections.Concurrent;
using NewLife;
using NewLife.Model;
using NewLife.Remoting;
using Stardust.Data.Monitors;
using Stardust.Monitors;

namespace Stardust.Server.Services;

public class MonitorService
{
    private ConcurrentDictionary<Int32, WebHookActor> _actors = new();

    public void WebHook(AppTracer app, TraceModel model)
    {
        if (app == null || model == null) return;
        if (app.WebHook.IsNullOrEmpty()) return;

        // 创建Actor
        var actor = _actors.GetOrAdd(app.ID, k => new WebHookActor { App = app });

        // 发送消息
        actor.App = app;
        actor.Tell(model);
    }

    class WebHookActor : Actor
    {
        public AppTracer App { get; set; }

        private ApiHttpClient _client;
        private String _server;
        private String _action;

        private ApiHttpClient GetClient()
        {
            var addr = App.WebHook;
            if (addr.IsNullOrEmpty()) return null;

            if (_client != null)
            {
                if (_server == addr) return _client;
            }

            if (addr.IsNullOrEmpty()) return null;

            _client = new ApiHttpClient(addr);

            _server = addr;

            var uri = new Uri(addr);
            _action = uri.AbsolutePath;

            return _client;
        }

        protected override async Task ReceiveAsync(ActorContext context, CancellationToken cancellationToken)
        {
            if (context.Message is not TraceModel model) return;

            var client = GetClient();
            if (client == null) return;

            await client.PostAsync<Object>(_action, model);
        }
    }
}