using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VainBot.Services;

namespace VainBot.SlashCommandModules
{
    public class ReminderSlashCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ReminderService _reminderSvc;
        private readonly ILogger<ReminderSlashCommandModule> _logger;

        private bool _allowImmediateReminder = false;

        public ReminderSlashCommandModule(ReminderService reminderSvc, ILogger<ReminderSlashCommandModule> logger)
        {
            _reminderSvc = reminderSvc;
            _logger = logger;
        }

        [SlashCommand("reminder", "Set a reminder for some time in the future")]
        [EnabledInDm(true)]
        public async Task CreateReminder(
            [MinValue(0), MaxValue(730), Summary(description: "Number of days in the future to set the reminder")] int days,
            [MinValue(0), MaxValue(17520), Summary(description: "Number of hours in the future to set the reminder")] int hours,
            [MinValue(0), MaxValue(1051200), Summary(description: "Number of minutes in the future to set the reminder")] int minutes,
            [MinLength(1), MaxLength(500), Summary(description: "The message to remind you about")] string message)
        {
            if (days < 0 && hours < 0 && minutes < 0)
            {
                await RespondAsync("You must provide at least one time value (days, hours, or minutes).", ephemeral: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                await RespondAsync("The message cannot be blank.", ephemeral: true);
                return;
            }

            if (message.Length > 500)
            {
                await RespondAsync("The message cannot be longer than 500 characters.", ephemeral: true);
                return;
            }

            var delayTs = TimeSpan.FromDays(days);
            delayTs = delayTs.Add(TimeSpan.FromHours(hours));
            delayTs = delayTs.Add(TimeSpan.FromMinutes(minutes));

            if (delayTs.TotalMinutes < 1)
            {
                if (!_allowImmediateReminder)
                {
                    await RespondAsync("You can't set a reminder for right now, that defeats the purpose.", ephemeral: true);
                    return;
                }
                else
                {
                    delayTs = TimeSpan.FromSeconds(3);
                }
            }

            if (delayTs.TotalMinutes > 1051200)
            {
                await RespondAsync("I don't think you need a reminder more than two years in the future.", ephemeral: true);
                return;
            }

            var reminder = await _reminderSvc.CreateReminderAsync(
                Context.User.Id, Context.Channel.Id, null, Context.Guild?.Id, message, delayTs);

            await RespondAsync($"Reminder set for {BuildDelayString(days, hours, minutes)} from now ({BuildFinalTimeString(reminder.FireAt)}).");

            var response = await Context.Interaction.GetOriginalResponseAsync();
            await _reminderSvc.UpdateReminderMessageIdAsync(reminder, response.Id);
        }

#if DEBUG
        [SlashCommand("immediatereminder", "Fires a reminder immediately")]
        [EnabledInDm(true)]
        [RequireOwner]
        public async Task ImmediateReminder()
        {
            _allowImmediateReminder = true;
            await CreateReminder(0, 0, 0, "immediate reminder");
        }
#endif

        [ComponentInteraction($"{ReminderService.SNOOZE_REMINDER_ID}:*")]
        public async Task SnoozeReminder(string reminderIdString, string[] selections)
        {
            const string errMsg = "Error occurred when trying to snooze reminder.";
            var interaction = (SocketMessageComponent)Context.Interaction;
            if (!int.TryParse(reminderIdString, out int reminderId))
            {
                await RespondAsync(text: errMsg, ephemeral: true);
                _logger.LogError($"Couldn't parse reminder ID: {reminderIdString} | {interaction.Message.GetJumpUrl()}");
                return;
            }

            var reminderUserId = _reminderSvc.GetReminderUserId(reminderId);
            if (!reminderUserId.HasValue || reminderUserId.Value != Context.User.Id)
            {
                await RespondAsync("Only the person who created the reminder can snooze it.", ephemeral: true);
                return;
            }

            if (!int.TryParse(reminderIdString, out int reminderId))
            {
                await RespondAsync(text: errMsg, ephemeral: true);
                _logger.LogError($"Couldn't parse reminder ID: {reminderIdString} | {interaction.Message.GetJumpUrl()}");
                return;
            }

            var reminderUserId = _reminderSvc.GetReminderUserId(reminderId);
            if (!reminderUserId.HasValue || reminderUserId.Value != Context.User.Id)
            {
                await RespondAsync("You can't snooze someone else's reminder.", ephemeral: true);
                return;
            }

            if (selections.Length != 1)
            {
                await RespondAsync(text: errMsg, ephemeral: true);
                _logger.LogError($"Received 0 or 2+ selections when snoozing reminder: {string.Join(", ", selections)} | {interaction.Message.GetJumpUrl()}");
                return;
            }

            if (!int.TryParse(selections[0], out int snoozeMinutes))
            {
                await RespondAsync(text: errMsg, ephemeral: true);
                _logger.LogError($"Couldn't parse snooze reminder selection: {selections[0]} | {interaction.Message.GetJumpUrl()}");
                return;
            }

            var snoozeFor = TimeSpan.FromMinutes(snoozeMinutes);
            if (snoozeMinutes == 1)
                snoozeFor = TimeSpan.FromSeconds(3);

            var fireAt = await _reminderSvc.SnoozeReminderByIdAsync(reminderId, snoozeFor);
            if (fireAt.HasValue)
            {
                await RespondAsync($"Snoozed reminder until {BuildFinalTimeString(fireAt.Value)}. This overrides any previous snoozes you may have set.");
            }
            else
            {
                await RespondAsync(text: errMsg, ephemeral: true);
            }
        }

        private static string BuildFinalTimeString(DateTimeOffset fireAt) =>
            fireAt.ToString("HH:mm") + " on " + fireAt.ToString("yyyy-MM-dd") + " UTC";

        private static string BuildDelayString(int days, int hours, int minutes)
        {
            var s = "";

            if (days > 0)
            {
                s += $"{days} ";
                if (days == 1)
                    s += "day, ";
                else
                    s += "days, ";
            }

            if (hours > 0)
            {
                s += $"{hours} ";
                if (hours == 1)
                    s += "hour, ";
                else
                    s += "hours, ";
            }

            if (minutes > 0)
            {
                s += $"{minutes} ";
                if (minutes == 1)
                    s += "minute, ";
                else
                    s += "minutes, ";
            }

            return s.TrimEnd(' ', ',');
        }
    }
}
