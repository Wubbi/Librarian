using System.Collections.Generic;

namespace Librarian
{
    /// <summary>
    /// Contains conditions for when this action is supposed to trigger as well as the action itself
    /// </summary>
    public class ConditionalAction
    {
        /// <summary>
        /// The unique identifier for this <see cref="ConditionalAction"/>
        /// </summary>
        public int Id { get; }

        private readonly Conditions _conditions;

        private readonly Actions _actions;

        public ConditionalAction(int id, Conditions conditions, Actions actions)
        {
            Id = id;

            _conditions = conditions;
            _actions = actions;
        }

        public bool ConditionsFulfilled(LauncherInventory.Diff inventoryUpdate, IEnumerable<int> completedIds)
        {
            return _conditions.Fulfilled(inventoryUpdate, completedIds);
        }

        public bool ActionsPerformed(LauncherInventory.Diff inventoryUpdate)
        {
            return _actions.Perform();
        }

        /// <summary>
        /// Represents a set of conditions that must be met for the action to be processed
        /// </summary>
        public class Conditions
        {
            /// <summary>
            /// Checks all set conditions
            /// </summary>
            /// <returns>True if all conditions are met, False otherwise</returns>
            public bool Fulfilled(LauncherInventory.Diff inventoryUpdate, IEnumerable<int> completedIds)
            {
                return true;
            }
        }

        /// <summary>
        /// Represents a set of actions to execute after the conditions have been met
        /// </summary>
        public class Actions
        {
            /// <summary>
            /// Performs all set actions
            /// </summary>
            /// <returns>True if all actions executed successfully, False otherwise</returns>
            public bool Perform()
            {
                return true;
            }
        }
    }
}
