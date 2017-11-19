using Hangfire;
using System;

// https://stackoverflow.com/a/44477843/1672458

namespace VainBotDiscord.Utils
{
    public class HangfireActivator : JobActivator
    {
        readonly IServiceProvider _provider;

        public HangfireActivator(IServiceProvider provider)
        {
            _provider = provider;
        }

        public override object ActivateJob(Type jobType) => _provider.GetService(jobType);
    }
}
