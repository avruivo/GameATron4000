using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameATron4000.Models;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace GameATron4000.Dialogs
{
    public class Room : Dialog, IDialogContinue, IDialogResume
    {
        private static readonly string[] _cannedResponses = new string[]
        {
            "You can't do that.",
            "Why?",
            "Hmm, better not.",
            "That will probably crash the game!"
        };

        private readonly Random _random;
        private readonly List<Command> _commands;

        public Room(List<Command> commands)
        {
            _random = new Random();
            _commands = commands;
        }

        public async Task DialogBegin(DialogContext dc, IDictionary<string, object> dialogArgs = null)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                await RunCommand(Command.RoomEntered, dc);
            }
        }

        public async Task DialogContinue(DialogContext dc)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                await RunCommand(dc);
            }
        }

        public async Task DialogResume(DialogContext dc, IDictionary<string, object> result)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));

            var state = dc.Context.GetConversationState<Dictionary<string, object>>();

            object onResumeActions = null;
            if (state.Remove("actionStack", out onResumeActions))
            {
                await ExecuteActions(dc, (List<Models.Action>)onResumeActions);
            }
        }

        private Task RunCommand(DialogContext dc)
        {
            return RunCommand(dc.Context.Activity.Text, dc);
        }

        private async Task RunCommand(string commandText, DialogContext dc)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));

            var state = dc.Context.GetConversationState<Dictionary<string, object>>();

            // Function to check if the dialog state contains the given flag.
            Func<Precondition, bool> verifyPrecondition = new Func<Precondition, bool>(pc =>
            {
                var key = "flag_" + pc.Flag;
                return pc.Value ? state.ContainsKey(key) : !state.ContainsKey(key);
            });

            var command = _commands
                .Where(cmd => string.Equals(cmd.Text, commandText, StringComparison.OrdinalIgnoreCase)
                    && cmd.Preconditions.All(verifyPrecondition))
                .FirstOrDefault();

            if (command != null)
            {
                var actions = command.Actions.Where(a => a.Preconditions.All(verifyPrecondition));

                await ExecuteActions(dc, actions);
            }
            else
            {
                // The player typed something we didn't expect; reply with a standard response.
                await dc.Context.SendActivity(
                    MessageFactory.Text("Narrator > " + _cannedResponses[_random.Next(0, _cannedResponses.Length)]));
            }
        }

        private async Task ExecuteActions(DialogContext dc, IEnumerable<Models.Action> actions)
        {
            if (dc == null) throw new ArgumentNullException(nameof(dc));

            var activities = new List<IActivity>();
            var updatedFlags = new Dictionary<string, bool>();

            var state = dc.Context.GetConversationState<Dictionary<string, object>>();
            var actionStack = new Stack<Models.Action>(actions.Reverse());

            Models.Action action;
            while (actionStack.TryPop(out action))
            {
                if (action.Name == Models.Action.TalkTo)
                {
                    if (activities.Any())
                    {
                        await dc.Context.SendActivities(activities.ToArray());
                    }

                    // Stacks don't serialize in the correct order.
                    // See https://github.com/JamesNK/Newtonsoft.Json/issues/971.
                    state["actionStack"] = actionStack.ToList();

                    await dc.Begin(action.Args[0]);
                    return;
                }

                switch (action.Name)
                {
                    case Models.Action.AddToInventory:
                        // Don't update the state directly, because the behaviour of other actions may
                        // still depend on the original state.
                        updatedFlags[action.Args[0]] = true;
                        break;

                    case Models.Action.Speak:
                        activities.Add(MessageFactory.Text($"{action.Args[1]} > {action.Args[0]}"));
                        break;

                    case Models.Action.TextDescribe:
                        activities.Add(MessageFactory.Text(action.Args[0]));
                        break;
                }
            }

            // Commit the temporary updated flags dictionary to the actual state object.
            UpdateState(state, updatedFlags);

            await dc.Context.SendActivities(activities.ToArray());
        }

        private void UpdateState(Dictionary<string, object> state, Dictionary<string, bool> updatedFlags)
        {
            foreach (var updatedFlag in updatedFlags)
            {
                var key = "flag_" + updatedFlag.Key;
                var flagIsSet = state.ContainsKey(key);

                if (flagIsSet && !updatedFlag.Value)
                {
                    state.Remove(key);
                }
                else if (!flagIsSet && updatedFlag.Value)
                {
                    state.Add(key, true);
                }
            }
        }
    }
}